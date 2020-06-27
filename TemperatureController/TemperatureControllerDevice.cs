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
namespace TemperatureController
{

  public class tempReport
  {
    public double maxTemp { get; set; }
    public double minTemp { get; set; }
    public double avgTemp { get; set; }
    public DateTime startTime { get; set; }
    public DateTime endTime { get; set; }
  }

  class TemperatureControllerDevice : IRunnableWithConnectionString
  {
    const string serialNumber = "S/N3123123";
    const string modelId = "dtmi:com:example:TemperatureController;1";
    ILogger logger;

    double CurrentTemperature1 { get; set; } = 0d;
    double CurrentTemperature2 { get; set; } = 0d;

    readonly Dictionary<DateTimeOffset, double> temperatureSeries1 = new Dictionary<DateTimeOffset, double>();
    readonly Dictionary<DateTimeOffset, double> temperatureSeries2 = new Dictionary<DateTimeOffset, double>();

    PnPFacade facade;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      facade = PnPFacade.CreateFromConnectionStringAndModelId(connectionString, modelId);

      await facade.ReportPropertyAsync("serialNumber", serialNumber);
      await facade.SetCommandHandlerAsync("reboot", root_RebootCommandHadler, this);

      await facade.ReportComponentPropertyCollectionAsync("deviceInfo", DeviceInfo.ThisDeviceInfo.ToDictionary());

      facade.SetDesiredPropertyUpdateCommandHandler("thermostat1", thermostat1_OnDesiredPropertiesReceived);
      await facade.SetComponentCommandHandlerAsync("thermostat1", "getMaxMinReport", thermostat1_GetMinMaxReportCommandHadler, this);
      var targetTemp1 = await facade.ReadDesiredComponentPropertyAsync<double>("thermostat1", "targetTemperature");
      CurrentTemperature1 = targetTemp1;

      facade.SetDesiredPropertyUpdateCommandHandler("thermostat2", thermostat2_OnDesiredPropertiesReceived);
      await facade.SetComponentCommandHandlerAsync("thermostat2", "getMaxMinReport", thermostat2_GetMinMaxReportCommandHadler, this);
      var targetTemp2 = await facade.ReadDesiredComponentPropertyAsync<double>("thermostat2", "targetTemperature");
      CurrentTemperature2 = targetTemp2;

      await Task.Run(async () =>
      {
        logger.LogWarning("Entering Device Loop");
        while (!quitSignal.IsCancellationRequested)
        {
          temperatureSeries1.Add(DateTime.Now, CurrentTemperature1);
          temperatureSeries2.Add(DateTime.Now, CurrentTemperature2);

          await facade.SendTelemetryValueAsync(
              JsonConvert.SerializeObject(new { workingSet = Environment.WorkingSet }));

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
      CurrentTemperature1 = 0;
      CurrentTemperature2 = 0;
      temperatureSeries1.Clear();
      temperatureSeries2.Clear();
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
      Task.Run(async () =>
      {
        await facade.AckDesiredPropertyReadAsync("thermostat1", "targetTemperature", targetTemp, StatusCodes.Pending, "update in progress", desired.Version);
        await this.ProcessTempUpdateAsync("thermostat1", targetTemp);
        await facade.AckDesiredPropertyReadAsync("thermostat1", "targetTemperature", targetTemp, StatusCodes.Completed, "update Complete", desired.Version);
      });
    }

    private void thermostat2_OnDesiredPropertiesReceived(TwinCollection desired)
    {
      var targetTemp = desired.GetPropertyValue<double>("thermostat2", "targetTemperature");
      logger.LogWarning($"TargetTempUpdated on thermostat2: " + targetTemp);
      Task.Run(async () =>
      {
        await facade.AckDesiredPropertyReadAsync("thermostat2", "targetTemperature", targetTemp, StatusCodes.Pending, "update in progress", desired.Version);
        await this.ProcessTempUpdateAsync("thermostat2", targetTemp);
        await facade.AckDesiredPropertyReadAsync("thermostat2", "targetTemperature", targetTemp, StatusCodes.Completed, "update Complete", desired.Version);
      });
    }

    private async Task ProcessTempUpdateAsync(string componentName, double targetTemp)
    {
      logger.LogWarning($"Ajusting temp for {componentName} to {targetTemp}");
      // gradually increase current temp to target temp
      double step = 0;
      if (componentName == "thermostat1") step = (targetTemp - CurrentTemperature1) / 10d;
      if (componentName == "thermostat2") step = (targetTemp - CurrentTemperature2) / 10d;

      for (int i = 9; i >= 0; i--)
      {
        if (componentName == "thermostat1") CurrentTemperature1 = targetTemp - step * i;
        if (componentName == "thermostat2") CurrentTemperature2 = targetTemp - step * i;

        await Task.Delay(500);
      }
      logger.LogWarning($"Adjustment complete for " + componentName);
    }
  }
}