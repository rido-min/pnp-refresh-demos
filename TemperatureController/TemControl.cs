using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TemperatureController.PnPComponents;

namespace TemperatureController
{
  class TemControl : IRunnableWithConnectionString
  {
    const string serialNumber = "S/N3123123";
    const string modelId = "dtmi:com:example:TemperatureController;1";
    ILogger logger;

    DeviceClient deviceClient;

    double CurrentTemperature1 { get; set; } = 0d;
    double CurrentTemperature2 { get; set; } = 0d;

    Dictionary<DateTimeOffset, double> temperatureSeries1 = new Dictionary<DateTimeOffset, double>();
    Dictionary<DateTimeOffset, double> temperatureSeries2 = new Dictionary<DateTimeOffset, double>();

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;
      
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
          TransportType.Mqtt, new ClientOptions { ModelId = modelId });
      
      var facade = PnPFacade.CreateFromDeviceClient(deviceClient);

      await facade.ReportComponentPropertyCollectionAsync("deviceInfo", DeviceInfo.ThisDeviceInfo.ToDictionary());

      await facade.ReportPropertyAsync("serialNumber", serialNumber);
      await deviceClient.SetMethodHandlerAsync("reboot", root_RebootCommandHadler, deviceClient);

      facade.SubscribeToComponentUpdates("thermostat1", thermostat1_OnDesiredPropertiesReceived);
      await facade.SetPnPCommandHandlerAsync("thermostat1", "getMaxMinReport", thermostat1_GetMinMaxReportCommandHadler, this);
      var targetTemp1 = await facade.ReadDesiredComponentPropertyAsync<double>("thermostat1", "targetTemperature");
      await ProcessTempUpdateAsync(targetTemp1, "thermostat1");

      facade.SubscribeToComponentUpdates("thermostat2", thermostat2_OnDesiredPropertiesReceived);
      await facade.SetPnPCommandHandlerAsync("thermostat2", "getMaxMinReport", thermostat2_GetMinMaxReportCommandHadler, this);
      var targetTemp2 = await facade.ReadDesiredComponentPropertyAsync<double>("thermostat2", "targetTemperature");
      await ProcessTempUpdateAsync(targetTemp2, "thermostat2");
      
      await Task.Run(async () =>
      {
        logger.LogWarning("Entering Device Loop");
        while (!quitSignal.IsCancellationRequested)
        {
          temperatureSeries1.Add(DateTime.Now, CurrentTemperature1);
          temperatureSeries2.Add(DateTime.Now, CurrentTemperature2);

          await facade.SendTelemetryValueAsync(
              JsonConvert.SerializeObject(new { workingSet = Environment.WorkingSet}));

          await facade.SendComponentTelemetryValueAsync("thermostat1", 
              JsonConvert.SerializeObject(new { temperature = CurrentTemperature1 }));

          await facade.SendComponentTelemetryValueAsync("thermostat2",
              JsonConvert.SerializeObject(new { temperature = CurrentTemperature2 }));

          logger.LogInformation($"Telemetry sent. t1 {CurrentTemperature1}, t2 {CurrentTemperature2}, ws:{Environment.WorkingSet}");
          await Task.Delay(1000);
        }
      });
    }
    private async Task<MethodResponse> root_RebootCommandHadler(MethodRequest req, object ctx)
    {
      var delay = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value").Value<int>();
      for (int i = 0; i < delay; i++)
      {
        logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
        await Task.Delay(1000);
      }
      return new MethodResponse(200);
    }

    private async Task<MethodResponse> thermostat1_GetMinMaxReportCommandHadler(MethodRequest req, object ctx)
    {
      var since = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value").Value<DateTime>();
      var series = temperatureSeries1.Where(t => t.Key > since).ToDictionary(i => i.Key, i => i.Value);
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

    private async Task<MethodResponse> thermostat2_GetMinMaxReportCommandHadler(MethodRequest req, object ctx)
    {
      var since = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value").Value<DateTime>();
      var series = temperatureSeries2.Where(t => t.Key > since).ToDictionary(i => i.Key, i => i.Value);
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


    private void thermostat1_OnDesiredPropertiesReceived(TwinCollection desired)
    {
      var targetTemp = desired.GetPropertyValue<double>("thermostat1", "targetTemperature");
      logger.LogWarning($"TargetTempUpdated on thermostat1: " + targetTemp);
      Task.Run(async () => await this.ProcessTempUpdateAsync(targetTemp, "thermostat1"));
    }

    private void thermostat2_OnDesiredPropertiesReceived(TwinCollection desired)
    {
      var targetTemp = desired.GetPropertyValue<double>("thermostat2", "targetTemperature");
      logger.LogWarning($"TargetTempUpdated on thermostat2: " + targetTemp);
      Task.Run(async () => await this.ProcessTempUpdateAsync(targetTemp, "thermostat2"));
    }



    private void Thermostat1_OnGetMinMaxReportCommand(object sender, GetMinMaxReportCommandEventArgs e)
    {
      var series = temperatureSeries1.Where(t => t.Key > e.Since).ToDictionary(i => i.Key, i => i.Value);
      
      e.tempReport = new tempReport()
      {
        maxTemp = series.Values.Max<double>(),
        minTemp = series.Values.Min<double>(),
        avgTemp = series.Values.Average(),
        startTime = series.Keys.Min<DateTimeOffset>().DateTime,
        endTime = series.Keys.Max<DateTimeOffset>().DateTime
      };
    }

    private void Thermostat2_OnGetMinMaxReportCommand(object sender, GetMinMaxReportCommandEventArgs e)
    {
      var series = temperatureSeries2.Where(t => t.Key > e.Since).ToDictionary(i => i.Key, i => i.Value);

      e.tempReport = new tempReport()
      {
        maxTemp = series.Values.Max<double>(),
        minTemp = series.Values.Min<double>(),
        avgTemp = series.Values.Average(),
        startTime = series.Keys.Min<DateTimeOffset>().DateTime,
        endTime = series.Keys.Max<DateTimeOffset>().DateTime
      };
    }

    private async Task ProcessTempUpdateAsync(double targetTemp, string component)
    {
      logger.LogWarning($"Ajusting temp for {component} to {targetTemp}");
      // gradually increase current temp to target temp
      double step = 0;
      if (component == "thermostat1") step = (targetTemp - CurrentTemperature1) / 10d;
      if (component == "thermostat2") step = (targetTemp - CurrentTemperature2) / 10d;

      for (int i = 9; i >= 0; i--)
      {
        if (component == "thermostat1") CurrentTemperature1 = targetTemp - step * (double)i;
        if (component == "thermostat2") CurrentTemperature2 = targetTemp - step * (double)i;

        await Task.Delay(500);
      }
      logger.LogWarning($"Adjustment complete for " + component);
    }

    private void DefaultComponent_OnRebootCommand(object sender, RebootCommandEventArgs e)
    {
      for (int i = 0; i < e.Delay; i++)
      {
        logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
        Task.Delay(1000).Wait();
      }
      CurrentTemperature1 = 0;
      // Task.Run(()=>thermostat1.InitAsync());
    }
  }
}