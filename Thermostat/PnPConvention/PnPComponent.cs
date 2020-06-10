
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Thermostat.PnPConvention
{
    public abstract class PnPComponent 
    {
        public DeviceClient client;
        
        private readonly string  componentName;
        private readonly ILogger logger;

        public delegate void OnDesiredPropertyFoundCallback(object newValue);
        
        public PnPComponent(DeviceClient client, string componentname) 
            : this(client, componentname, new NullLogger<PnPComponent>()){}

        public PnPComponent(DeviceClient client, string componentname, ILogger log)
        {
            this.componentName = componentname;
            this.client = client;
            this.logger = log;
            this.logger.LogInformation("New PnPComponent for " + componentname);
        }


        public async Task<string> ReadDesiredProperty(string propertyName)
        {
            this.logger.LogTrace("ReadDesiredProperty " + propertyName);
            var twin = await this.client.GetTwinAsync();
            var desired = new  PnPPropertyCollection(this.componentName, twin.Properties.Desired.ToJson());
            var result = desired.Get(propertyName);
            this.logger.LogTrace("ReadDesiredProperty returned: " + result);
            return result;
        }

        public PnPPropertyCollection NewReportedProperties()
        {
            this.logger.LogTrace("Creating new PnPPropertyCollection");
            return new PnPPropertyCollection(this.componentName);
        }

        public async Task SetPnPCommandHandler(string commandName, MethodCallback callback, object ctx)
        {
            this.logger.LogTrace("Set Command Handler for " + commandName);
            await this.client.SetMethodHandlerAsync($"{this.componentName}*{commandName}", callback, ctx);
        }

        public void SetPnPDesiredPropertyHandler(string propertyName, OnDesiredPropertyFoundCallback callback, object ctx)
        {
            StatusCodes result = StatusCodes.NotImplemented;
            this.logger.LogTrace("Set Desired Handler for " + propertyName);
            this.client.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx) => {

                
                this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");

                var pnpDesiredProperties = new PnPPropertyCollection(this.componentName, desiredProperties.ToJson());
                string desiredPropertyValue = pnpDesiredProperties.Get(propertyName);
                result = StatusCodes.Pending;
                await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Pending, "update in progress", desiredProperties.Version);

                if (!string.IsNullOrEmpty(desiredPropertyValue))
                {
                    callback(desiredPropertyValue);
                    result = StatusCodes.Completed;
                    await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Completed, "update complete", desiredProperties.Version);
                    this.logger.LogTrace($"Desired properties processed successfully");
                }
                else
                {
                    result = StatusCodes.Invalid;
                    await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Invalid, "invalid, empty value", desiredProperties.Version);
                    this.logger.LogTrace($"Invalid desired properties processed ");
                }
                await Task.FromResult(result);
            }, this).Wait();
        }

        async Task AckDesiredPropertyReadAsync(string propertyName, object payload, StatusCodes statuscode, string description, long version)
        {
            var ack = new PnPPropertyCollection(this.componentName);
            SetAck(ack, propertyName, payload, statuscode, version, description);
            await client.UpdateReportedPropertiesAsync(ack.Instance);
            Console.WriteLine($"Reported writable property [{this.componentName}] - {JsonConvert.SerializeObject(payload)}");
        }

        public async Task SendTelemetryValueAsync(string serializedTelemetry)
        {
            this.logger.LogTrace($"Sending Telemetry [${serializedTelemetry}]");
            var message = new Message(Encoding.UTF8.GetBytes(serializedTelemetry));
            message.Properties.Add("$.sub", this.componentName);
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";
            await this.client.SendEventAsync(message);
        }

        void SetAck(PnPPropertyCollection ack, string propertyName, object value, StatusCodes statusCode, long statusVersion, string statusDescription = "")
        {            
            var property = new TwinCollection();
            property["value"] = value;
            property["ac"] = statusCode;
            property["av"] = statusVersion;
            if (!string.IsNullOrEmpty(statusDescription)) property["ad"] = statusDescription;

            if (ack.Instance.Contains(this.componentName))
            {
                JToken token = JToken.FromObject(property);
                ack.Instance[this.componentName][propertyName] = token;
            }
            else
            {
                TwinCollection root = new TwinCollection();
                root["__t"] = "c"; // TODO: Review, should the ACK require the flag
                root[propertyName] = property;
                ack.Instance[this.componentName] = root;
            }
        }
    }
}
