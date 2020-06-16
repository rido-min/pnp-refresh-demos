using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{
  class SimpleThermostatDevice : IRunnableDevice
  {
    const string modelId = "dtmi:com:example:simplethermostat;2";
    double CurrentTemperature;

    ILogger logger;
    DeviceClient deviceClient;
    PnPComponent component;

    public async Task RunDeviceAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
        TransportType.Mqtt, new ClientOptions { ModelId = modelId });

      component = new PnPComponent(deviceClient, logger);

      await component.SetPnPDesiredPropertyHandlerAsync<double>("targetTemperature", root_targetTemperature_UpdateHandler, this);
      await component.SetPnPCommandHandlerAsync("reboot", root_RebootCommandHadler, this);

      var targetTemperature = await component.ReadDesiredPropertyAsync<double>("targetTemperature");
      await this.ProcessTempUpdateAsync(targetTemperature);

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {
          await component.SendTelemetryValueAsync("{temperature:" + CurrentTemperature + "," +
                                                  " workingSet: " + Environment.WorkingSet + "}");

          logger.LogInformation("Sending CurrentTemperature and workingset" + CurrentTemperature);
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
        await component.SendTelemetryValueAsync("{temperature:" + CurrentTemperature + "}");
        await component.ReportPropertyAsync("currentTemperature", CurrentTemperature);
        await Task.Delay(1000);
      }
      logger.LogWarning($"Adjustment complete");
    }

    private async Task<MethodResponse> root_RebootCommandHadler(MethodRequest req, object ctx)
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

    private void root_targetTemperature_UpdateHandler(double newValue)
    {
      this.ProcessTempUpdateAsync(newValue).Wait();
    }
  }
}