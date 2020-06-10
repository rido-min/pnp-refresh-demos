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
    class ThermostatDevice
    {
        const string modelId = "dtmi:com:example:simplethermostat;2.1.2";

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
                    _logger.LogWarning("=====================> TargetTempUpdated: " + targetTemperature);

                    await ReportWritablePropertyAsync("targetTemperature", targetTemperature, 200, "targetTemperature  Updated", desiredProperties.Version);
                    await this.ProcessTempUpdateAsync(targetTemperature);

                }
                await Task.FromResult("200");
            }, null);

            _ = deviceClient.SetMethodHandlerAsync("reboot", async (MethodRequest req, object ctx) =>
              {
                  CurrentTemperature = 0.1;
                  int.TryParse(req.DataAsJson, out int delay);
                  for (int i = 0; i < delay; i++)
                  {
                      _logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
                      await Task.Delay(2000);
                  }
                  await ReadDesiredPropertiesAsync();
                  return await Task.FromResult(new MethodResponse(200));
              }, null);


            await Task.Run(async () =>
            {
                await ReportCurrentTemperature();
                await ReadDesiredPropertiesAsync();
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

        async Task ReportCurrentTemperature()
        {
            await deviceClient.UpdateReportedPropertiesAsync(
                new TwinCollection("{ currentTemperature : " + CurrentTemperature + "}"));
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
            await ReportCurrentTemperature();
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

            try
            {
                await deviceClient.SendEventAsync(message);
            }
            catch { Console.WriteLine(); }
        }

        async Task ReportWritablePropertyAsync(string propertyName, object payload, int statuscode, string description, long version)
        {
            //var root = new TwinCollection();
            var propertyVal = new TwinCollection();
            var valtc = new TwinCollection();
            valtc["value"] = payload;
            valtc["sc"] = statuscode;
            valtc["sd"] = description;
            valtc["sv"] = version;
            propertyVal[propertyName] = valtc;
            //root[this.componentName] = propertyVal;

            //var reportedVal = root;

            await deviceClient.UpdateReportedPropertiesAsync(propertyVal);
            Console.WriteLine($"Reported writable property [{propertyName}] - {JsonConvert.SerializeObject(payload)}");
        }
    }
}