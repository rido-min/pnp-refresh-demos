using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PnPConvention;
using System;
using System.Threading.Tasks;

namespace Thermostat.PnPComponents
{
  public class RebootCommandEventArgs : EventArgs
  {
    public int Delay { get; private set; }
    public RebootCommandEventArgs(int delay)
    {
      Delay = delay;
    }
  }

  class DiagnosticsInterface : PnPComponent
  {
    public event EventHandler<RebootCommandEventArgs> OnRebootCommand;
    public DiagnosticsInterface(DeviceClient client, string componentName) : base(client, componentName)
    {
      base.SetPnPCommandHandlerAsync("reboot", (MethodRequest req, object ctx) =>
      {
        var delayVal = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value.delay");
        int delay = delayVal.Value<int>();
        if (delayVal != null && int.TryParse(delayVal.Value<string>(), out delay))
        
        OnRebootCommand?.Invoke(this, new RebootCommandEventArgs(delay));
        
        return Task.FromResult(new MethodResponse(200));
      }, this).Wait();
    }
    public async Task SendWorkingTelemetryAsync(double workingSet)
    {
      await base.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { workingset = workingSet }));
    }
  }
}
