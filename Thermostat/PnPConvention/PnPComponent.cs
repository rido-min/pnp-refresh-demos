
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
        private readonly ILogger logger;

        public string componentName;
        public DeviceClient client;

        public delegate void OnDesiredPropertyFoundCallback(object newValue);
        
        public PnPComponent(string componentname, DeviceClient client) 
            : this(componentname, client, new NullLogger<PnPComponent>()){}

        public PnPComponent(string componentname, DeviceClient client, ILogger log)
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
            this.logger.LogTrace("Set Desired Handler for " + propertyName);
            this.client.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx) => {

                this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");

                var pnpDesiredProperties = new PnPPropertyCollection(this.componentName, desiredProperties.ToJson());
                string desiredPropertyValue = pnpDesiredProperties.Get(propertyName);
                await ReportWritablePropertyAsync(propertyName, desiredPropertyValue, StatusCodes.Pending, "update in progress", desiredProperties.Version);

                if (!string.IsNullOrEmpty(desiredPropertyValue))
                {
                    callback(desiredPropertyValue);
                    await ReportWritablePropertyAsync(propertyName, desiredPropertyValue, StatusCodes.Completed, "update complete", desiredProperties.Version);
                    this.logger.LogTrace($"Desired properties processed successfully");
                }
                else
                {
                    await ReportWritablePropertyAsync(propertyName, desiredPropertyValue, StatusCodes.Invalid, "Error parsing to double", desiredProperties.Version);
                    this.logger.LogTrace($"Desired properties processed with error");
                }
                await Task.FromResult("200");
            }, this).Wait();
        }

        async Task ReportWritablePropertyAsync(string propertyName, object payload, StatusCodes statuscode, string description, long version)
        {
            var props = new PnPPropertyCollection(this.componentName);
            props.Set(propertyName, payload, statuscode, version, description);
            await client.UpdateReportedPropertiesAsync(props);
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
    }
}
