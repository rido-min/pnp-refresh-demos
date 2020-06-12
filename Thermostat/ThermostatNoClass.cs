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

      await deviceInfo.ReportPropertyCollectionAsync(DeviceInfo.ThisDeviceInfo.ToDictionary());

      await tempSensor.SetPnPDesiredPropertyHandlerAsync<double>("targetTemperature", tempSensor_tergetTemperature_UpdateHandler, this);
      var targetTemperature = await tempSensor.ReadDesiredPropertyAsync<double>("targetTemperature");
      await this.ProcessTempUpdateAsync(targetTemperature);

      await sdkInfo.ReportPropertyCollectionAsync(SdkInformation.ThisSdkInfo);

      await diag.SetPnPCommandHandlerAsync("reboot", Diag_RebootCommandHadler, this);

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

    private void tempSensor_tergetTemperature_UpdateHandler(object newValue)
    {
        if (newValue != null && double.TryParse(newValue.ToString(), out double target))
        {
          this.ProcessTempUpdateAsync(target).Wait();
        }
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

    private async Task<MethodResponse> Diag_RebootCommandHadler(MethodRequest req, object ctx)
    {
      int delay = 0;
      var delayVal = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value"); // Review if we need the commandRequest wrapper
      if (delayVal != null && int.TryParse(delayVal.Value<string>(), out delay))
      {
        for (int i = 0; i < delay; i++)
        {
          logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
          await Task.Delay(1000);
        }
        CurrentTemperature = 0;
        await this.ProcessTempUpdateAsync(21);
      }
      return new MethodResponse(200);
    }
  }
}
