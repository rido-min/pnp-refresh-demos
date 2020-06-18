using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{
  class SimpleThermostatDevice : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:com:example:simplethermostat;2";
    double CurrentTemperature;

    ILogger logger;
    DeviceClient deviceClient;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
        TransportType.Mqtt, new ClientOptions { ModelId = modelId });

      await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, this, quitSignal);
      await deviceClient.SetMethodHandlerAsync("reboot", root_RebootCommandHadler, this);

      var twin = await deviceClient.GetTwinAsync();
      double targetTemperature = GetPropertyValue<double>(twin.Properties.Desired, "targetTemperature");
      
      await this.ProcessTempUpdateAsync(targetTemperature);

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {
          await deviceClient.SendEventAsync(
            new Message(
              Encoding.UTF8.GetBytes(
                "{" +
                  "\"temperature\": " + CurrentTemperature + "," +
                  "\"workingSet\" : "  + Environment.WorkingSet + 
                "}"))
            {
              ContentEncoding = "utf-8", 
              ContentType= "application/json"
            });

          logger.LogInformation("Sending CurrentTemperature and workingset " + CurrentTemperature);
          await Task.Delay(1000);
        }
      });
    }

    private async Task ProcessTempUpdateAsync(double targetTemp)
    {
      logger.LogWarning($"Ajusting temp from {CurrentTemperature} to {targetTemp}");
      // gradually increase current temp to target temp
      double step = (targetTemp - CurrentTemperature) / 10d;
      for (int i = 9; i >= 0; i--)
      {
        CurrentTemperature = targetTemp - step * i;

        await deviceClient.SendEventAsync(
          new Message(
            Encoding.UTF8.GetBytes(
              "{ \"temperature\":" + CurrentTemperature + "}"))
          {
            ContentEncoding = "utf-8",
            ContentType = "application/json",
            MessageSchema = "temperature"
          });

                
        var reported = new TwinCollection();
        reported["currentTemperature"] = CurrentTemperature;
        await deviceClient.UpdateReportedPropertiesAsync(reported);
        await Task.Delay(1000);
      }
      logger.LogWarning($"Adjustment complete");
    }

    private async Task<MethodResponse> root_RebootCommandHadler(MethodRequest req, object ctx)
    {
      int delay = 0;
      var delayVal = JObject.Parse(req.DataAsJson).Value<double>(); // Review if we need the commandRequest wrapper
      
        for (int i = 0; i < delay; i++)
        {
          logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
          await Task.Delay(1000);
        }
        CurrentTemperature = 0;
        await this.ProcessTempUpdateAsync(21);
      return new MethodResponse(200);
    }

    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");
      double desiredPropertyValue = GetPropertyValue<double>(desiredProperties, "targetTemperature");
      await this.ProcessTempUpdateAsync(desiredPropertyValue);
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
          result = collection[propertyName].Value;
        }
      }
      return result;
    }

  }
}