using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thermostat.PnPComponents;

namespace Thermostat
{
  class ThermostatNoClass
  {
    const string modelId = "dtmi:com:example:Thermostat;1";

    readonly string connectionString;
    readonly ILogger logger;
    readonly CancellationToken quitSignal;

    DeviceClient deviceClient;

    double CurrentTemperature { get; set; } = 0d;

    PnPComponent tempSensor;
    PnPComponent diag;
    PnPComponent deviceInfo;
    PnPComponent sdkInfo;

    public ThermostatNoClass(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.quitSignal = quitSignal;
      this.logger = logger;
      this.connectionString = connectionString;
    }

    public async Task RunDeviceAsync()
    {
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
          TransportType.Mqtt, new ClientOptions { ModelId = modelId });

      tempSensor = new PnPComponent(deviceClient, "tempSensor1", logger);
      diag = new PnPComponent(deviceClient, "diag");
      deviceInfo = new PnPComponent(deviceClient, "deviceInfo");
      sdkInfo = new PnPComponent(deviceClient, "sdkInfo");

      await tempSensor.SetPnPDesiredPropertyHandlerAsync("targetTemperature", async (newValue) =>
          {
            if (newValue != null && double.TryParse(newValue.ToString(), out double target))
            {
              await this.ProcessTempUpdateAsync(target);
            }
          }, this);

      await deviceInfo.ReportPropertyCollectionAsync(ThisDeviceInfo.ToDictionary());
      
      var sdkInfoProps = new Dictionary<string, object>(3);
      sdkInfoProps.Add("language", "C# 8.0");
      sdkInfoProps.Add("version", "Device Client 1.25.0");
      sdkInfoProps.Add("vendor", "Microsoft");
      await sdkInfo.ReportPropertyCollectionAsync(sdkInfoProps);

      await diag.SetPnPCommandHandlerAsync("reboot", (MethodRequest req, object ctx) => 
      {
        int delay = 0;
        var delayVal = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value");
        if (delayVal != null && int.TryParse(delayVal.Value<string>(), out delay))
        {
          for (int i = 0; i < delay; i++)
          {
            logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
            Task.Delay(1000).Wait();
          }
          CurrentTemperature = 0;
          this.ProcessTempUpdateAsync(21).Wait();
        }
        return Task.FromResult(new MethodResponse(200));
      }, this);

      var targetTemperature = await tempSensor.ReadDesiredPropertyAsync<double>("targetTemperature");

      await Task.Run(async () =>
      {
        logger.LogWarning("Entering Device Loop");
        while (!quitSignal.IsCancellationRequested)
        {
          await tempSensor.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { temperature = CurrentTemperature }));
          await diag.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { workingSet = Environment.WorkingSet }));

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
        await tempSensor.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { temperature = CurrentTemperature }));
        await tempSensor.ReportProperty("currentTemperature", CurrentTemperature);
        await Task.Delay(1000);
      }
      logger.LogWarning($"Adjustment complete");
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
