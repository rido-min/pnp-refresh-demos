using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Threading.Tasks;

namespace TemperatureController.PnPComponents
{
  public class RebootCommandEventArgs : EventArgs
  {
    public int Delay { get; private set; }
    public RebootCommandEventArgs(int delay)
    {
      Delay = delay;
    }
  }

  class DefaultComponent : PnPComponent
  {
    public event EventHandler<RebootCommandEventArgs> OnRebootCommand;
    public DefaultComponent(DeviceClient client, ILogger logger ) : base(client, logger)
    {
      base.SetPnPCommandHandlerAsync("reboot", (MethodRequest req, object ctx) =>
      {
        var delay = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value").Value<int>();
        OnRebootCommand?.Invoke(this, new RebootCommandEventArgs(delay));
        return Task.FromResult(new MethodResponse(200));
      }, this).Wait();
    }
    public async Task SendWorkingSetTelemetryAsync(double workingSet)
    {
      await base.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { workingset = workingSet }));
    }
    public async Task ReportSerialNumberAsync(string serialNumber)
    {
      await base.ReportPropertyAsync("serialNumber", serialNumber);
    }
  }
}
