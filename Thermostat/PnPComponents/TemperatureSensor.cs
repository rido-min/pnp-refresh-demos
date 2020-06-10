using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thermostat.PnPConvention;

namespace Thermostat.PnPComponents
{
    public class TemperatureEventArgs : EventArgs
    {
        public TemperatureEventArgs(double t)
        {
            Temperature = t;
        }
        public double Temperature { get; }
    }

    class TemperatureSensor : PnPComponent
    {
        
        public event EventHandler<TemperatureEventArgs> OnTargetTempReceived;

        public double CurrentTemperature { get; set; } = 0d;

        public TemperatureSensor(string componentName, DeviceClient client) : base(componentName, client)
        {
            base.SetPnPDesiredPropertyHandler(
                "targetTemperature", 
                (newValue) => 
                {
                    TriggerEventIfValueIsDouble(newValue);
                }, 
                this);
        }

        public async Task InitAsync()
        {
            var initialTarget = await base.ReadDesiredProperty("targetTemperature");
            TriggerEventIfValueIsDouble(initialTarget);
        }

        private void TriggerEventIfValueIsDouble(object newValue)
        {
            if (double.TryParse(newValue.ToString(), out double target))
            {
                OnTargetTempReceived?.Invoke(this, new TemperatureEventArgs(target));
            }
        }

       

        public async Task ReportTargetTemperatureAsync(double target)
        {
            var twin = base.NewReportedProperties();
            twin.Set("targetTemperature", target);
            await this.client.UpdateReportedPropertiesAsync(twin);
        }

        public async Task ReportCurrentTemperatureAsync(double target)
        {
            var twin = base.NewReportedProperties();
            twin.Set("currentTemperature", target);
            await this.client.UpdateReportedPropertiesAsync(twin);
        }

        public async Task SendTemperatureTelemetryValueAsync(double currentTemp)
        {
            await base.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { temperature = currentTemp}));
        }
    }
}