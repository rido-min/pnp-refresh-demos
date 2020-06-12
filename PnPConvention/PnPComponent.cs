
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PnPConvention
{
  public class PnPComponent
  {
    DeviceClient client;

    public readonly string componentName;
    public readonly ILogger logger;

    public delegate void OnDesiredPropertyFoundCallback(object newValue);

    public PnPComponent(DeviceClient client, string componentname)
        : this(client, componentname, new NullLogger<PnPComponent>()) { }

    public PnPComponent(DeviceClient client, string componentname, ILogger log)
    {
      this.componentName = componentname;
      this.client = client;
      this.logger = log;
      this.logger.LogInformation("New PnPComponent for " + componentname);
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

    public async Task ReportProperty(string propertyName, object propertyValue)
    {
      this.logger.LogTrace("Reporting " + propertyName);
      var twin = new TwinCollection();
      twin.AddComponentProperty(this.componentName, propertyName, propertyValue);
      await this.client.UpdateReportedPropertiesAsync(twin);
    }

    public async Task ReportPropertyCollectionAsync(Dictionary<string, object> properties)
    {
      var reported = new TwinCollection();
      foreach (var p in properties)
      {
        reported.AddComponentProperty(this.componentName, p.Key, p.Value);
      }
      await this.client.UpdateReportedPropertiesAsync(reported);
    }

    public async Task SetPnPCommandHandlerAsync(string commandName, MethodCallback callback, object ctx)
    {
      this.logger.LogTrace("Set Command Handler for " + commandName);
      await this.client.SetMethodHandlerAsync($"{this.componentName}*{commandName}", callback, ctx);
    }

    public async Task<T> ReadDesiredPropertyAsync<T>(string propertyName)
    {
      this.logger.LogTrace("ReadDesiredProperty " + propertyName);
      var twin = await this.client.GetTwinAsync();
      var desiredPropertyValue = twin.Properties.Desired.GetPropertyValue<T>(this.componentName, propertyName);
      await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Completed, "update complete", twin.Properties.Desired.Version);
      this.logger.LogTrace("ReadDesiredProperty returned: " + desiredPropertyValue);
      return desiredPropertyValue;
    }

    public async Task SetPnPDesiredPropertyHandlerAsync(string propertyName, OnDesiredPropertyFoundCallback callback, object ctx)
    {
      StatusCodes result = StatusCodes.NotImplemented;
      this.logger.LogTrace("Set Desired Handler for " + propertyName);

      await this.client.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx2) =>
      {
        this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");
        string desiredPropertyValue = desiredProperties.GetPropertyValue<string>(this.componentName, propertyName);
        result = StatusCodes.Pending;
        await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Pending, "update in progress", desiredProperties.Version);

        if (!string.IsNullOrEmpty(desiredPropertyValue))
        {
          callback(desiredPropertyValue);
          result = StatusCodes.Completed;
          await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Completed, "update complete", desiredProperties.Version);
          this.logger.LogInformation($"Desired properties processed successfully");
        }
        else
        {
          result = StatusCodes.Invalid;
          await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Invalid, "invalid, empty value", desiredProperties.Version);
          this.logger.LogTrace($"Invalid desired properties processed ");
        }
        await Task.FromResult(result);
      }, this);
    }

    async Task AckDesiredPropertyReadAsync(string propertyName, object payload, StatusCodes statuscode, string description, long version)
    {
      var ack = CreateAck(propertyName, payload, statuscode, version, description);
      await client.UpdateReportedPropertiesAsync(ack);
      this.logger.LogTrace($"Reported writable property [{this.componentName}] - {JsonConvert.SerializeObject(payload)}");
    }

    TwinCollection CreateAck(string propertyName, object value, StatusCodes statusCode, long statusVersion, string statusDescription = "")
    {
      TwinCollection ack = new TwinCollection();
      var property = new TwinCollection();
      property["value"] = value;
      property["ac"] = statusCode;
      property["av"] = statusVersion;
      if (!string.IsNullOrEmpty(statusDescription)) property["ad"] = statusDescription;

      if (ack.Contains(this.componentName))
      {
        JToken token = JToken.FromObject(property);
        ack[this.componentName][propertyName] = token;
      }
      else
      {
        TwinCollection root = new TwinCollection();
        root["__t"] = "c"; // TODO: Review, should the ACK require the flag
        root[propertyName] = property;
        ack[this.componentName] = root;
      }
      return ack;
    }
  }
}
