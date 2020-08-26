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

namespace myDevice
{

  class Device : IRunnableWithConnectionString
  {
    long refreshInterval = 5;
    const string modelId = "dtmi:com:example:mydevice;5";
    const string serialNumber = "S/N123";
    ILogger logger;
    DeviceClient deviceClient;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;
      deviceClient = await DeviceClientFactory.CreateDeviceClientAsync(connectionString, logger, modelId);
      await deviceClient.SetMethodDefaultHandlerAsync(DefaultCommandHadlerAsync, null, quitSignal);
      await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, null, quitSignal);

      var reported = new TwinCollection();
      reported["serialNumber"] = serialNumber;
      reported["baseSerialNumber"] = serialNumber;
      await deviceClient.UpdateReportedPropertiesAsync(reported);

      var reportedInterface01 = new TwinCollection();
      reportedInterface01["myinterface01"] = new {
        __t="c",
        serialNumber = serialNumber
      };
      await deviceClient.UpdateReportedPropertiesAsync(reportedInterface01);

      var reportedInterface02 = new TwinCollection();
      reportedInterface02["myinterface02"] = new {
        __t = "c",
        serialNumber = serialNumber
      };
      await deviceClient.UpdateReportedPropertiesAsync(reportedInterface02);

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {

          await deviceClient.SendEventAsync(
            new Message(
              Encoding.UTF8.GetBytes(
                "{" +
                  "\"workingSet\" : " + Environment.WorkingSet +
                "}"))
            {
              ContentEncoding = "utf-8",
              ContentType = "application/json"
            });

          logger.LogInformation("Sending workingset ");
          await Task.Delay(Convert.ToInt32(refreshInterval) * 1000);
        }
      });
    }

    private async Task<MethodResponse> DefaultCommandHadlerAsync(MethodRequest req, object ctx)
    {
      logger.LogWarning(req.Name);
      logger.LogWarning(req.DataAsJson);
      var payload = new {
        aName = "asString",
        aNumber = 123.32,
        aDate = DateTime.Now
      };
      var constPayload = JsonConvert.SerializeObject(payload);
      logger.LogInformation(constPayload);
      return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(constPayload), 200));
    }
    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      this.logger.LogWarning($"Received desired updates [{desiredProperties.ToJson()}]");
      var desiredPropertyValue = GetPropertyValue<long>(desiredProperties, "refreshInterval");
      if (desiredPropertyValue > 0)
      {
        refreshInterval = desiredPropertyValue;
        await AckDesiredPropertyReadAsync("refreshInterval", desiredPropertyValue, 200, "property synced", desiredProperties.Version);
      }
    }

    T GetPropertyValue<T>(TwinCollection collection, string propertyName)
    {
      T result = default(T);
      if (collection.Contains(propertyName))
      {
        var propertyJson = collection[propertyName] as JObject;
        if (propertyJson != null)
        {
          if (propertyJson.ContainsKey("value"))
          {
            var propertyValue = propertyJson["value"];
            result = propertyValue.Value<T>();
          }
        }
        else
        {
          try
          {
            result = collection[propertyName].Value;
          }
          catch (Exception ex)
          {
            this.logger.LogError(ex, ex.Message);
          }
        }
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