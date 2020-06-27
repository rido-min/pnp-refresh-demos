using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PnPConvention
{
  public delegate void OnDesiredPropertyFoundCallback(TwinCollection newValue);
  public sealed class PnPFacade
  {
    static readonly Dictionary<string, OnDesiredPropertyFoundCallback> components = new Dictionary<string, OnDesiredPropertyFoundCallback>();

    static PnPFacade() { }
    private PnPFacade() { }
    private static readonly PnPFacade instance = new PnPFacade();
    private static IPnPDeviceClient deviceClient;
    internal static PnPFacade CreateFromDeviceClient(IPnPDeviceClient client)
    {
      deviceClient = client;
      deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, deviceClient);
      return instance;
    }
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static PnPFacade CreateFromConnectionStringAndModelId(string connectionString, string modelId)
    {
      deviceClient = new PnPDeviceClient(DeviceClient.CreateFromConnectionString(connectionString,
                                                  TransportType.Mqtt,
                                                  new ClientOptions() { ModelId = modelId }));

      deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, deviceClient);
      return instance;
    }

    public async Task SendTelemetryValueAsync(string serializedTelemetry)
    {
      var message = new Message(Encoding.UTF8.GetBytes(serializedTelemetry));
      message.ContentType = "application/json";
      message.ContentEncoding = "utf-8";
      await deviceClient.SendEventAsync(message);
    }

    public async Task SendComponentTelemetryValueAsync(string componentName, string serializedTelemetry)
    {
      var message = new Message(Encoding.UTF8.GetBytes(serializedTelemetry));
      message.Properties.Add("$.sub", componentName);
      message.ContentType = "application/json";
      message.ContentEncoding = "utf-8";
      await deviceClient.SendEventAsync(message);
    }

    public void SetDesiredPropertyUpdateCommandHandler(string componentName, OnDesiredPropertyFoundCallback callback)
    {
      components.Add(componentName, callback);
    }

    public async Task ReportComponentPropertyCollectionAsync(string componentName, Dictionary<string, object> properties)
    {
      var reported = new TwinCollection();
      foreach (var p in properties)
      {
        reported.AddComponentProperty(componentName, p.Key, p.Value);
      }
      await deviceClient.UpdateReportedPropertiesAsync(reported);
    }

    public async Task ReportPropertyAsync(string propertyName, object propertyValue)
    {
      var twin = new TwinCollection();
      twin[propertyName] = propertyValue;
      await deviceClient.UpdateReportedPropertiesAsync(twin);
    }

    public async Task ReportComponentPropertyAsync(string componentName, string propertyName, object propertyValue)
    {
      var twin = new TwinCollection();
      twin.AddComponentProperty(componentName, propertyName, propertyValue);
      await deviceClient.UpdateReportedPropertiesAsync(twin);
    }

    public async Task SetCommandHandlerAsync(string commandName, MethodCallback callback, object ctx)
    {
      await deviceClient.SetMethodHandlerAsync($"{commandName}", callback, ctx);
    }

    public async Task SetComponentCommandHandlerAsync(string componentName, string commandName, MethodCallback callback, object ctx)
    {
      await deviceClient.SetMethodHandlerAsync($"{componentName}*{commandName}", callback, ctx);
    }

    public async Task<T> ReadDesiredComponentPropertyAsync<T>(string componentName, string propertyName)
    {
      var twin = await deviceClient.GetTwinAsync();
      var desiredPropertyValue = twin.Properties.Desired.GetPropertyValue<T>(componentName, propertyName);
      await AckDesiredPropertyReadAsync(componentName, propertyName, desiredPropertyValue, StatusCodes.Completed, "update complete", twin.Properties.Desired.Version);
      return desiredPropertyValue;
    }

    private static Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      //desired event should be fired for a single, so first, component.
      var componentName = desiredProperties.EnumerateComponents().FirstOrDefault(); ;
      var comp = components[componentName];
      comp?.Invoke(desiredProperties);
      return Task.FromResult(0);
    }

    public async Task AckDesiredPropertyReadAsync(string componentName, string propertyName, object payload, StatusCodes statuscode, string description, long version)
    {
      var ack = CreateAck(componentName, propertyName, payload, statuscode, version, description);
      await deviceClient.UpdateReportedPropertiesAsync(ack);
    }

    private TwinCollection CreateAck(string componentName, string propertyName, object value, StatusCodes statusCode, long statusVersion, string statusDescription = "")
    {
      TwinCollection ack = new TwinCollection();
      var ackProps = new TwinCollection();
      ackProps["value"] = value;
      ackProps["ac"] = statusCode;
      ackProps["av"] = statusVersion;
      if (!string.IsNullOrEmpty(statusDescription)) ackProps["ad"] = statusDescription;
      TwinCollection ackChildren = new TwinCollection();
      ackChildren["__t"] = "c"; // TODO: Review, should the ACK require the flag
      ackChildren[propertyName] = ackProps;
      ack[componentName] = ackChildren;
      return ack;
    }
  }
}
