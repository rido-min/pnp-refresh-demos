using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Thermostat.PnPComponents;

namespace Thermostat
{
  class ThermostatDevice
  {
    const string modelId = "dtmi:com:example:Thermostat;1";

    readonly string connectionString;
    readonly ILogger logger;
    readonly CancellationToken quitSignal;

    DeviceClient deviceClient;

    double CurrentTemperature { get; set; } = 0d;

    TemperatureSensor tempSensor;
    DiagnosticsInterface diag;
    DeviceInformation deviceInfo;
    SdkInformationInterface sdkInfo;

    public ThermostatDevice(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.quitSignal = quitSignal;
      this.logger = logger;
      this.connectionString = connectionString;
    }

    public async Task RunDeviceAsync()
    {
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString , 
          TransportType.Mqtt, new ClientOptions { ModelId = modelId } );

      tempSensor = new TemperatureSensor(deviceClient, "tempSensor1", logger);
      diag = new DiagnosticsInterface(deviceClient, "diag");
      deviceInfo = new DeviceInformation(deviceClient, "deviceInfo");
      sdkInfo = new SdkInformationInterface(deviceClient, "sdkInfo");

      diag.OnRebootCommand += Diag_OnRebootCommand;
      tempSensor.OnTargetTempReceived += TempSensor_OnTargetTempReceived;

      await deviceInfo.ReportDeviceInfoPropertiesAsync(ThisDeviceInfo);
      await sdkInfo.ReportSdkInfoPropertiesAsync();

      await tempSensor.InitAsync();

      await Task.Run(async () =>
      {
        logger.LogWarning("Entering Device Loop");
        while (!quitSignal.IsCancellationRequested)
        {
          await tempSensor.SendTemperatureTelemetryValueAsync(CurrentTemperature);
          await diag.SendWorkingTelemetryAsync(Environment.WorkingSet);

          logger.LogInformation("Sending workingset and temp " + CurrentTemperature);
          await Task.Delay(5000);
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
        CurrentTemperature = targetTemp - step * (double)i;
        await tempSensor.SendTemperatureTelemetryValueAsync(CurrentTemperature);
        await tempSensor.ReportCurrentTemperatureAsync(CurrentTemperature);
        await Task.Delay(1000);
      }
      logger.LogWarning($"Adjustment complete");
    }


    private void Diag_OnRebootCommand(object sender, RebootCommandEventArgs e)
    {
      for (int i = 0; i < e.Delay; i++)
      {
        logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
        Task.Delay(1000).Wait();
      }
      CurrentTemperature = 0;
      this.ProcessTempUpdateAsync(21).Wait();
    }

    private void TempSensor_OnTargetTempReceived(object sender, TemperatureEventArgs ea)
    {
      logger.LogWarning("TargetTempUpdated: " + ea.Temperature);
      this.ProcessTempUpdateAsync(ea.Temperature).Wait();
    }

    DeviceInfo ThisDeviceInfo
    {
      get
      {
        return new DeviceInfo
        {
          Manufacturer = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
          Model = Environment.OSVersion.Platform.ToString(),
          SoftwareVersion = Environment.OSVersion.VersionString,
          OperatingSystemName = Environment.GetEnvironmentVariable("OS"),
          ProcessorArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
          ProcessorManufacturer = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
          TotalStorage = 123,// System.IO.DriveInfo.GetDrives()[0].TotalSize,
          TotalMemory = Environment.WorkingSet
        };
      }
    }
  }
}