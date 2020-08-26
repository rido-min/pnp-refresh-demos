using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rido;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{

  class DatesTestDevice : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:com:rido:datestest;1";
    

    ILogger logger;
    DeviceClient deviceClient;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      deviceClient = await DeviceClientFactory.CreateDeviceClientAsync(connectionString, logger, modelId);

      await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, this, quitSignal);
      await deviceClient.SetMethodDefaultHandlerAsync(root_DefaultCommandHadler, null, quitSignal);
      var twin = await deviceClient.GetTwinAsync();

      var reportADate = CreateAck("aDate", DateTime.Now, 200, 1);
      await deviceClient.UpdateReportedPropertiesAsync(reportADate);

      //TwinCollection reported = new TwinCollection();
      //reported["aDateTime"] = "1010-10-10T10:10";
      //reported["aDate"] = "1010-10-10";
      //await deviceClient.UpdateReportedPropertiesAsync(reported);

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {
          logger.LogInformation("not Sending Telemetry");
          await Task.Delay(5000);
        }
      });
    }



    private async Task<MethodResponse> root_DefaultCommandHadler(MethodRequest req, object ctx)
    {
      logger.LogWarning(req.Name);
      logger.LogWarning(req.DataAsJson);

      

      var constPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(DateTime.Now));
      return await Task.FromResult(new MethodResponse(constPayload, 200));
      
    }

    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");
      var aDate = GetPropertyValue<DateTime>(desiredProperties, "aDate");
      if (aDate != null)
      {
        await AckDesiredPropertyReadAsync("aDate", aDate, 200, "property synced", desiredProperties.Version);
      }
      else
      {
        logger.LogError("Cant parse desired props");
      }

      var aDateTime = GetPropertyValue<DateTime>(desiredProperties, "aDateTime");
      if (aDateTime != null)
      {
        await AckDesiredPropertyReadAsync("aDateTime", aDateTime, 200, "property synced", desiredProperties.Version);
      }
      else
      {
        logger.LogError("Cant parse desired props");
      }
    }

    T GetPropertyValue<T>(TwinCollection collection, string propertyName)
    {
      T result = default(T);
      if (collection.Contains(propertyName))
      {
        JToken propVal = collection[propertyName];
        result = propVal.Value<T>();
      }
      return result;
    }


    public async Task AckDesiredPropertyReadAsync(string propertyName, object payload, int statuscode, string description, long version)
    {
      var ack = CreateAck(propertyName, payload, statuscode, version, description);
      await deviceClient.UpdateReportedPropertiesAsync(ack);
    }

    private TwinCollection CreateAck(string propertyName, object value, int statusCode, long statusVersion, string statusDescription = "")
    {
      TwinCollection ackProp = new TwinCollection();
      ackProp[propertyName] = new {
        value = value,
        ac = statusCode,
        av = statusVersion,
        ad = statusDescription
      };
      return ackProp;
    }
  }
}