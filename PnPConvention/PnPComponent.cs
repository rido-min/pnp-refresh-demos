
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace PnPConvention
{
  public class PnPComponent
  {
    readonly IPnPDeviceClient client;

    public readonly string componentName;
    public readonly ILogger logger;

    private readonly bool isRootComponent = false;

    public delegate void OnDesiredPropertyFoundCallback(object newValue);

    [ExcludeFromCodeCoverage]
    public PnPComponent(DeviceClient client)
        : this(new PnPDeviceClient(client), string.Empty, new NullLogger<PnPComponent>()) { }
    [ExcludeFromCodeCoverage]
    public PnPComponent(DeviceClient client, string componentName)
        : this(new PnPDeviceClient(client), componentName, new NullLogger<PnPComponent>()) { }
    [ExcludeFromCodeCoverage]
    public PnPComponent(DeviceClient client, string componentName, ILogger logger)
        : this(new PnPDeviceClient(client), componentName, logger) { }

    internal PnPComponent(IPnPDeviceClient client, ILogger logger)
        : this(client, string.Empty, logger) { }

    internal PnPComponent(IPnPDeviceClient client, string compName, ILogger log)
    {
      this.isRootComponent = string.IsNullOrEmpty(compName);
      this.componentName = compName;
      this.client = client;
      this.logger = log;
      this.logger.LogInformation("New PnPComponent for " + compName);
    }

    public async Task SendTelemetryValueAsync(string serializedTelemetry)
    {
      this.logger.LogTrace($"Sending Telemetry [${serializedTelemetry}]");
      var message = new Message(Encoding.UTF8.GetBytes(serializedTelemetry));
      if (!this.isRootComponent)
      {
        message.Properties.Add("$.sub", this.componentName);
      }
      message.ContentType = "application/json";
      message.ContentEncoding = "utf-8";
      await this.client.SendEventAsync(message);
    }

    public async Task ReportPropertyAsync(string propertyName, object propertyValue)
    {
      this.logger.LogTrace("Reporting " + propertyName);
      var twin = new TwinCollection();
      if (isRootComponent)
      {
        twin[propertyName] = propertyValue;
      }
      else
      {
        twin.AddComponentProperty(this.componentName, propertyName, propertyValue);
      }
      await this.client.UpdateReportedPropertiesAsync(twin);
    }

    public async Task ReportPropertyCollectionAsync(Dictionary<string, object> properties)
    {
      var reported = new TwinCollection();
      foreach (var p in properties)
      {
        if (this.isRootComponent)
        {
          reported[p.Key] = p.Value;
        }
        else
        {
          reported.AddComponentProperty(this.componentName, p.Key, p.Value);
        }
      }

      await this.client.UpdateReportedPropertiesAsync(reported);
    }

    public async Task SetPnPCommandHandlerAsync(string commandName, MethodCallback callback, object ctx)
    {
      this.logger.LogTrace("Set Command Handler for " + commandName);
      if (isRootComponent)
      {
        await this.client.SetMethodHandlerAsync(commandName, callback, ctx);
      }
      else
      {
        await this.client.SetMethodHandlerAsync($"{this.componentName}*{commandName}", callback, ctx);
      }

    }

    public async Task<T> ReadDesiredPropertyAsync<T>(string propertyName)
    {
      this.logger.LogTrace("ReadDesiredProperty " + propertyName);
      var twin = await this.client.GetTwinAsync();
      T desiredPropertyValue;
      if (isRootComponent)
      {
        desiredPropertyValue = twin.Properties.Desired.GetPropertyValue<T>(propertyName);
      }
      else
      {
        desiredPropertyValue = twin.Properties.Desired.GetPropertyValue<T>(this.componentName, propertyName);
      }
      await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Completed, "update complete", twin.Properties.Desired.Version);
      this.logger.LogTrace("ReadDesiredProperty returned: " + desiredPropertyValue);
      return desiredPropertyValue;
    }

    public async Task SetPnPDesiredPropertyHandlerAsync<T>(string propertyName, OnDesiredPropertyFoundCallback callback, object ctx)
    {
      StatusCodes result = StatusCodes.NotImplemented;
      this.logger.LogTrace("Set Desired Handler for " + propertyName);

      await this.client.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx2) =>
      {
        this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");
        T desiredPropertyValue;
        if (isRootComponent)
        {
          desiredPropertyValue = desiredProperties.GetPropertyValue<T>(propertyName);
        }
        else
        {
          desiredPropertyValue = desiredProperties.GetPropertyValue<T>(this.componentName, propertyName);
        }
        result = StatusCodes.Pending;
        await AckDesiredPropertyReadAsync(propertyName, desiredPropertyValue, StatusCodes.Pending, "update in progress", desiredProperties.Version);

        if (desiredPropertyValue != null)
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

      var ackProps = new TwinCollection();
      ackProps["value"] = value;
      ackProps["ac"] = statusCode;
      ackProps["av"] = statusVersion;
      if (!string.IsNullOrEmpty(statusDescription)) ackProps["ad"] = statusDescription;

      if (isRootComponent)
      {
        ack[propertyName] = ackProps;
      }
      else
      {
        TwinCollection ackChildren = new TwinCollection();
        ackChildren["__t"] = "c"; // TODO: Review, should the ACK require the flag
        ackChildren[propertyName] = ackProps;
        ack[this.componentName] = ackChildren;
      }
      return ack;
    }
  }
}
