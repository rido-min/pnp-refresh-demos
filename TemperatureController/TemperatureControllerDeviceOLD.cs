using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using PnPConvention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TemperatureController.PnPComponents;

namespace TemperatureController
{
  class TemperatureControllerDevice : IRunnableWithConnectionString
  {
    const string serialNumber = "S/N3123123";
    const string modelId = "dtmi:com:example:TemperatureController;1";
    ILogger logger;

    DeviceClient deviceClient;

    double CurrentTemperature1 { get; set; } = 0d;
    double CurrentTemperature2 { get; set; } = 0d;

    Dictionary<DateTimeOffset, double> temperatureSeries1 = new Dictionary<DateTimeOffset, double>();
    Dictionary<DateTimeOffset, double> temperatureSeries2 = new Dictionary<DateTimeOffset, double>();

    Thermostat thermostat1;
    Thermostat thermostat2;
    DeviceInformation deviceInfo;
    DefaultComponent defaultComponent;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;
      
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
          TransportType.Mqtt, new ClientOptions { ModelId = modelId });

      deviceInfo = new DeviceInformation(deviceClient, "deviceInfo");
      await deviceInfo.ReportDeviceInfoPropertiesAsync(DeviceInfo.ThisDeviceInfo);

      defaultComponent = new DefaultComponent(deviceClient, logger);
      await defaultComponent.ReportSerialNumberAsync(serialNumber);
      defaultComponent.OnRebootCommand += DefaultComponent_OnRebootCommand;

      thermostat1 = new Thermostat(deviceClient, "thermostat1", logger);
      thermostat1.OnTargetTempReceived += Thermostat1_OnTargetTempReceived;
      thermostat1.OnGetMinMaxReportCommand += Thermostat1_OnGetMinMaxReportCommand;
      await thermostat1.InitAsync();

      thermostat2 = new Thermostat(deviceClient, "thermostat2", logger);
      thermostat2.OnTargetTempReceived += Thermostat2_OnTargetTempReceived;
      thermostat2.OnGetMinMaxReportCommand += Thermostat2_OnGetMinMaxReportCommand;
      await thermostat2.InitAsync();

      await Task.Run(async () =>
      {
        logger.LogWarning("Entering Device Loop");
        while (!quitSignal.IsCancellationRequested)
        {
          temperatureSeries1.Add(DateTime.Now, CurrentTemperature1);
          temperatureSeries2.Add(DateTime.Now, CurrentTemperature2);

          await defaultComponent.SendWorkingSetTelemetryAsync(Environment.WorkingSet);
          await thermostat1.SendTemperatureTelemetryValueAsync(CurrentTemperature1);
          await thermostat2.SendTemperatureTelemetryValueAsync(CurrentTemperature2);

          logger.LogInformation($"Telemetry sent. t1 {CurrentTemperature1}, t2 {CurrentTemperature2}, ws:{Environment.WorkingSet}");
          await Task.Delay(1000);
        }
      });
    }

    private void Thermostat1_OnTargetTempReceived(object sender, TemperatureEventArgs ea)
    {
      var comp = sender as Thermostat;
      logger.LogWarning($"TargetTempUpdated on {comp.componentName}: {ea.Temperature}");
      Task.Run(async () => await this.ProcessTempUpdateAsync(ea.Temperature, "thermostat1") );
    }

    private void Thermostat2_OnTargetTempReceived(object sender, TemperatureEventArgs ea)
    {
      var comp = sender as Thermostat;
      logger.LogWarning($"TargetTempUpdated on {comp.componentName}: {ea.Temperature}");
      Task.Run(async () => await this.ProcessTempUpdateAsync(ea.Temperature, "thermostat2"));
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
      Task.Run(()=>thermostat1.InitAsync());
    }
  }
}