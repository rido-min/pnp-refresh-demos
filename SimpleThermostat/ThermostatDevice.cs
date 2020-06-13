using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{
  class ThermostatDevice
  {
    const string modelId = "dtmi:com:example:simplethermostat;2";
    readonly string connectionString;
    private readonly ILogger logger;
    private readonly CancellationToken quitSignal;

    double CurrentTemperature;

    DeviceClient deviceClient;
    PnPComponent component;

    public ThermostatDevice(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
      quitSignal = cancellationToken;
      this.logger = logger;
      this.connectionString = connectionString;
    }

    public async Task RunDeviceAsync()
    {
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString,
        TransportType.Mqtt, new ClientOptions { ModelId = modelId });

      component = new PnPComponent(deviceClient);

      await component.SetPnPDesiredPropertyHandlerAsync<double>("targetTemperature", root_tergetTemperature_UpdateHandler, this);
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

    private void root_tergetTemperature_UpdateHandler(object newValue)
    {
      if (newValue != null && double.TryParse(newValue.ToString(), out double target))
      {
        this.ProcessTempUpdateAsync(target).Wait();
      }
    }
  }
}