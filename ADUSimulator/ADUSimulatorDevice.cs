using DeviceRunner;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rido;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ADUSimulator
{
  public class Orchestrator
  {
    public int Action { get; set; }
    public int TargetVersion { get; set; }
    public Dictionary<string, string> Files { get; set; }
    public string ExpectedContentId { get; set; }
    public string InstalledCriteria { get; set; }
  }

  public class Client
  {
    public int ResultCode { get; set; }
    public int ExtendedResultCode { get; set; }
    public int State { get; set; }
    public string InstalledContentId { get; set; }
    public string ToJson()
    {
      return JsonConvert.SerializeObject(this);
    }
  }


  class ADUSimulatorDevice : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:azureiot:AzureDeviceUpdateCore;1";

    ILogger logger;
    DeviceClient deviceClient;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt, 
        new ClientOptions { ModelId = modelId });

      await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, this, quitSignal);

      var twin = await deviceClient.GetTwinAsync();
      logger.LogInformation(twin.ToJson());

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {
          await UpdateClient();
          await Task.Delay(1000);
        }
      });
    }

    private async Task UpdateClient()
    {
      var client = new Client
      {
        ExtendedResultCode = Environment.TickCount,
        ResultCode = 1,
        InstalledContentId = "ABC123",
        State = 1
      };
      var reportedProperties = new TwinCollection();
      reportedProperties["Client"] = client;
      await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      this.logger.LogWarning($"Received desired updates [{desiredProperties.ToJson()}]");
      var orchProp = GetPropertyValue<Orchestrator>(desiredProperties, "Orchestrator");

      await AckDesiredPropertyReadAsync("Orchestrator", orchProp, 200, "property synced", desiredProperties.Version);

      this.logger.LogWarning("Prop synced");
    }

    T GetPropertyValue<T>(TwinCollection collection, string propertyName)
    {
      T result = default(T);
      if (collection.Contains(propertyName))
      {
        var propertyJson = collection[propertyName] as JObject;
        if (propertyJson != null)
        {
          result = propertyJson.ToObject<T>();
        }
        else
        {
          this.logger.LogError($"Property {propertyName} not found");
        }
      }
      return result;
    }


    public async Task AckDesiredPropertyReadAsync(string propertyName, object payload, int statuscode, string description, long version)
    {

      TwinCollection ack = new TwinCollection();
      var ackProps = new TwinCollection();
      ackProps["value"] = payload;
      ackProps["ac"] = statuscode;
      ackProps["av"] = version;
      if (!string.IsNullOrEmpty(description)) ackProps["ad"] = description;
      ack[propertyName] = ackProps;
      await deviceClient.UpdateReportedPropertiesAsync(ack);
    }


  }
}