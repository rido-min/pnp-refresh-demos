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

namespace Thermostat
{

  

  class MyCertifiedDeviceSimulator : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:com:rido:myTestDevice;1";
    

    ILogger logger;
    DeviceClient deviceClient;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      deviceClient = await DeviceClientFactory.CreateDeviceClientAsync(connectionString, logger, modelId);

      await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, this, quitSignal);
      await deviceClient.SetMethodDefaultHandlerAsync(root_DefaultCommandHadler, null, quitSignal);
      var twin = await deviceClient.GetTwinAsync();

      TwinCollection reported = new TwinCollection();
      reported["Owner"] = new {
        personName = "owner",
        birthday = DateTime.Now,
        isValid = true
      };
      await deviceClient.UpdateReportedPropertiesAsync(reported);

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {

          await deviceClient.SendEventAsync(
            new Message(
              Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
               new {
                 People = new {
                   personName = "rido",
                   isValid = true,
                   birthday = DateTime.Now.ToUniversalTime()
                }
               }
               )))
            {
              ContentEncoding = "utf-8",
              ContentType = "application/json"
            });

          logger.LogInformation("Sending Telemetry");
          await Task.Delay(5000);
        }
      });
    }



    private async Task<MethodResponse> root_DefaultCommandHadler(MethodRequest req, object ctx)
    {
      logger.LogWarning(req.Name);
      logger.LogWarning(req.DataAsJson);

      Person p = JsonConvert.DeserializeObject<Person>(req.DataAsJson);

      Person[] people = new[] {
        new Person() { birthday = DateTime.Now, isValid = true, personName ="rido"},
        p
      };

      var constPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("people"));
      return await Task.FromResult(new MethodResponse(constPayload, 200));
      
    }

    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");
      Person person = GetPropertyValue<Person>(desiredProperties, "Delegate");
      if (person != null)
      {
        await AckDesiredPropertyReadAsync("Delegate", person, 200, "property synced", desiredProperties.Version);
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
        JObject propVal = collection[propertyName];
        result = propVal.ToObject<T>();
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