using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{
    public class RebootCommandEventArgs : EventArgs
    {
        public int Delay { get; private set; }
        public RebootCommandEventArgs(int delay)
        {
            Delay = delay;
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

    class ThermostatDevice
    {
        const string modelId = "dtmi:com:example:simplethermostat;1";

        string _connectionString;
        private readonly ILogger _logger;
        private CancellationToken _quitSignal;
       
        double CurrentTemperature;

        DeviceClient deviceClient;
        
        public ThermostatDevice(string connectionString, ILogger logger, CancellationToken cancellationToken)
        {
            _quitSignal = cancellationToken;
            _logger = logger;
            _connectionString = connectionString + ";ModelId=" + modelId;
        }

        public async Task RunDeviceAsync()
        {
            deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, TransportType.Mqtt);
            deviceClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
            {
                Console.WriteLine(status + " " + reason);
            });


            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx) =>
            {
                Console.WriteLine($"Received desired updates [{desiredProperties.ToJson()}]");
                string desiredPropertyValue = GetPropertyValueIfFound(desiredProperties, "targetTemperature");
                if (double.TryParse(desiredPropertyValue, out double targetTemperature))
                {
                    //await ReportWritablePropertyAsync("targetTemperature", targetTemperature, 200, "update", desiredProperties.Version);
                    _logger.LogWarning("=====================> TargetTempUpdated: " + targetTemperature);
                     await this.ProcessTempUpdateAsync(targetTemperature);
                }
                await Task.FromResult("200");
            }, null);

            _ = deviceClient.SetMethodHandlerAsync("reboot", async (MethodRequest req, object ctx) =>
              {
                  int.TryParse(req.DataAsJson, out int delay);
                  CurrentTemperature = 0.1;
                  for (int i = 0; i < delay; i++)
                  {
                      _logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
                      await Task.Delay(5000);
                  }
                  await ReadDesiredPropertiesAsync();
                  return await Task.FromResult(new MethodResponse(200));
              }, null);
                                   
            
            await ReadDesiredPropertiesAsync();

            await Task.Run(async () =>
            {
                while (!_quitSignal.IsCancellationRequested)
                {
                    await SendTelemetryValueAsync(CurrentTemperature, "temperature");
                    //await diag.SendTelemetryValueAsync(Environment.WorkingSet);
                    _logger.LogInformation("Sending CurrentTemperature" + CurrentTemperature);
                    await Task.Delay(1000);
                }
            });
        }
        string GetPropertyValueIfFound(TwinCollection properties, string propertyName)
        {
            string result = string.Empty;

            if (properties.Contains(propertyName))
            {
                var prop = properties[propertyName];
                //var propVal = prop["value"];
                result = Convert.ToString(prop);
            }

            return result;
        }

        public async Task ReadDesiredPropertiesAsync()
        {
            var twin = await deviceClient.GetTwinAsync();
            var targetValue = GetPropertyValueIfFound(twin.Properties.Desired, "targetTemperature");
            if (double.TryParse(targetValue, out double targetTemp))
            {
                await this.ProcessTempUpdateAsync(targetTemp);
            }
        }


        private async Task ProcessTempUpdateAsync(double targetTemp)
        {
            // gradually increase current temp to target temp
            double step = (targetTemp - CurrentTemperature) / 10d;
            for (int i = 9; i >= 0; i--)
            {
                CurrentTemperature = targetTemp - step * (double)i;
                Console.WriteLine(targetTemp + " " + CurrentTemperature);
                //await SendTelemetryValueAsync(CurrentTemperature, "temperature");
                await Task.Delay(500);
            }
        }

        public async Task SendTelemetryValueAsync(double currentTemp, string schema)
        {

            var message = new Message(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new { temperature = currentTemp }
                    )
                )
            );

            message.ContentType = "application/json";
            message.MessageSchema = schema;

            await deviceClient.SendEventAsync(message);
        }
    }
}