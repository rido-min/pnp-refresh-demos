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

  public class tempReport
  {
    public double maxTemp { get; set; }
    public double minTemp { get; set; }
    public double avgTemp { get; set; }
    public DateTime startTime { get; set; }
    public DateTime endTime { get; set; }
  }

  class SimpleThermostatDevice : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:com:example:Thermostat;1";
    double CurrentTemperature;
    readonly Dictionary<DateTimeOffset, double> temperatureSeries = new Dictionary<DateTimeOffset, double>();

    ILogger logger;
    DeviceClient deviceClient;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      //deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
      //  TransportType.Mqtt, new ClientOptions { ModelId = modelId });

      deviceClient = await DeviceClientFactory.CreateDeviceClientAsync(connectionString + ";ModelId=" + modelId);

      await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback, this, quitSignal);
      await deviceClient.SetMethodHandlerAsync("getMaxMinReport", root_getMaxMinReportCommandHadler, this);

      var twin = await deviceClient.GetTwinAsync();
      double targetTemperature = GetPropertyValue<double>(twin.Properties.Desired, "targetTemperature");
      if (targetTemperature>0)
      {
        await AckDesiredPropertyReadAsync("targetTemperature", targetTemperature, 200, "property synced", twin.Properties.Desired.Version);
      }

      await this.ProcessTempUpdateAsync(targetTemperature);

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {
          temperatureSeries.Add(DateTime.Now, CurrentTemperature);

          await deviceClient.SendEventAsync(
            new Message(
              Encoding.UTF8.GetBytes(
                "{" +
                  "\"temperature\": " + CurrentTemperature + "," +
                  "\"workingSet\" : " + Environment.WorkingSet +
                "}"))
            {
              ContentEncoding = "utf-8",
              ContentType = "application/json"
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
        await Task.Delay(1000);
      }
      logger.LogWarning($"Adjustment complete");
    }

    private async Task<MethodResponse> root_getMaxMinReportCommandHadler(MethodRequest req, object ctx)
    {
      var payload = JsonConvert.DeserializeObject(req.DataAsJson);
      if (payload is DateTime)
      {
        DateTime since = (DateTime)payload;


        var series = temperatureSeries.Where(t => t.Key > since).ToDictionary(i => i.Key, i => i.Value);
        var report = new tempReport()
        {
          maxTemp = series.Values.Max<double>(),
          minTemp = series.Values.Min<double>(),
          avgTemp = series.Values.Average(),
          startTime = series.Keys.Min<DateTimeOffset>().DateTime,
          endTime = series.Keys.Max<DateTimeOffset>().DateTime
        };
        var constPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(report));
        return await Task.FromResult(new MethodResponse(constPayload, 200));
      }
      else
      {
        var constPayload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("error parsing input"));
        return await Task.FromResult(new MethodResponse(constPayload, 500));
      }
    }

    private async Task DesiredPropertyUpdateCallback(TwinCollection desiredProperties, object userContext)
    {
      this.logger.LogTrace($"Received desired updates [{desiredProperties.ToJson()}]");
      double desiredPropertyValue = GetPropertyValue<double>(desiredProperties, "targetTemperature");
      if (desiredPropertyValue>0)
      {
        await AckDesiredPropertyReadAsync("targetTemperature", desiredPropertyValue, 200, "property synced", desiredProperties.Version);
      }
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


    public async Task AckDesiredPropertyReadAsync(string propertyName, object payload, int statuscode, string description, long version)
    {
      var ack = CreateAck(propertyName, payload, statuscode, version, description);
      await deviceClient.UpdateReportedPropertiesAsync(ack);
    }

    private TwinCollection CreateAck(string propertyName, object value, int statusCode, long statusVersion, string statusDescription = "")
    {
      TwinCollection ack = new TwinCollection();
      var ackProps = new TwinCollection();
      ackProps["value"] = value;
      ackProps["ac"] = statusCode;
      ackProps["av"] = statusVersion;
      if (!string.IsNullOrEmpty(statusDescription)) ackProps["ad"] = statusDescription;
      ack[propertyName] = ackProps;
      return ack;
    }
  }
}