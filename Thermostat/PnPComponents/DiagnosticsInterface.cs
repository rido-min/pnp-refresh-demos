using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using PnPConvention;

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
            base.SetPnPCommandHandler("reboot", (MethodRequest req, object ctx) =>
            {
                int delay = 0;
                var delayVal = JObject.Parse(req.DataAsJson).SelectToken("commandRequest.value");
                if (delayVal!=null && int.TryParse(delayVal.Value<string>(), out delay))
                {
                    OnRebootCommand?.Invoke(this, new RebootCommandEventArgs(delay));
                }
                else
                {
                    OnRebootCommand?.Invoke(this, new RebootCommandEventArgs(1)); // default value?
                }    
                return Task.FromResult(new MethodResponse(200));
            }, this).Wait();
        }

        public async Task SendWorkingTelemetryAsync(double workingSet)
        {   
            await base.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { workingset = workingSet}));
        }
    }
}
