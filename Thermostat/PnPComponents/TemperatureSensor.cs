using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnPConvention;
using System;
using System.Threading.Tasks;

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
        ILogger logger;
        public event EventHandler<TemperatureEventArgs> OnTargetTempReceived;

        public TemperatureSensor(DeviceClient client, string componentName, ILogger log) : base(client, componentName, log)
        {
            this.logger = log;
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
            if (newValue != null && double.TryParse(newValue.ToString(), out double target))
            {
                OnTargetTempReceived?.Invoke(this, new TemperatureEventArgs(target));
            }
            else
            {
                logger.LogWarning("!!!!!!!!!!!! value is not double, skipping event");
            }
        }

        public async Task ReportTargetTemperatureAsync(double target)
        {
            await base.ReportProperty("targetTemperature", target);
        }

        public async Task ReportCurrentTemperatureAsync(double target)
        {
            await base.ReportProperty("currentTemperature", target);
        }

        public async Task SendTemperatureTelemetryValueAsync(double currentTemp)
        {
            await base.SendTelemetryValueAsync(JsonConvert.SerializeObject(new { temperature = currentTemp}));
        }
    }
}