using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TemperatureController.PnPComponents
{

  public class tempReport
  {
    public double maxTemp { get; set; }
    public double minTemp { get; set; }
    public double avgTemp { get; set; }
    public DateTime startTime { get; set; }
    public DateTime endTime { get; set; }
  }

  public class GetMinMaxReportCommandEventArgs : EventArgs
  {
    public DateTime Since{ get; private set; }
    public tempReport tempReport { get; set; }
    public GetMinMaxReportCommandEventArgs(DateTime since)
    {
      Since = since;
    }
  }

  public class TemperatureEventArgs : EventArgs
  {
    public TemperatureEventArgs(double t)
    {
      Temperature = t;
    }
    public double Temperature { get; }
  }

  class Thermostat : PnPComponent
  {
    public event EventHandler<TemperatureEventArgs> OnTargetTempReceived;

    public event EventHandler<GetMinMaxReportCommandEventArgs> OnGetMinMaxReportCommand;

    public Thermostat(DeviceClient client, string componentName, ILogger log) : base(client, componentName, log)
    {
      base.SetPnPDesiredPropertyHandlerAsync<double>(
          "targetTemperature", 
          (newValue) => OnTargetTempReceived?.Invoke(this, new TemperatureEventArgs(newValue)),
          this).Wait();

      base.SetPnPCommandHandlerAsync("getMaxMinReport", (MethodRequest req, object ctx) =>
      {
        log.LogWarning("==============> Processing command getMaxMinReport");
        var since = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value").Value<DateTime>();
        var cmdEventArgs = new GetMinMaxReportCommandEventArgs(since);
        OnGetMinMaxReportCommand?.Invoke(this, cmdEventArgs);
        var jsonResult = JsonConvert.SerializeObject(cmdEventArgs.tempReport);
        var response = new MethodResponse(Encoding.UTF8.GetBytes(jsonResult), 200);
        return Task.FromResult(response);
      }, this).Wait();

    }

    public async Task InitAsync()
    {
      var initialTarget = await base.ReadDesiredPropertyAsync<double>("targetTemperature");
      OnTargetTempReceived?.Invoke(this, new TemperatureEventArgs(initialTarget));
    }

    public async Task SendTemperatureTelemetryValueAsync(double currentTemp)
    {
      await base.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { temperature = currentTemp }));
      logger.LogTrace("sent telemetry temperature: " + currentTemp);
    }

    public async Task ReportMaxTempProp(double maxTemp)
    {
      await base.ReportPropertyAsync("maxTempSinceLastReboot", maxTemp);
    }
  }
}