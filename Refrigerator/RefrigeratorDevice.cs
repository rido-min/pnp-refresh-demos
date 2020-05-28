using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refrigerator
{
    class RefrigeratorDevice
    {
        const string modelId = "dtmi:dev:rido:refrigerator;1";

        string _connectionString;
        private readonly ILogger _logger;
        private CancellationToken _quitSignal;

        DeviceClient deviceClient;

        int RefreshInterval = 1;

        public RefrigeratorDevice(string connectionString, ILogger logger, CancellationToken cancellationToken)
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

            #region commands
            _ = deviceClient.SetMethodHandlerAsync("Reset", async (MethodRequest req, object ctx) =>
            {
                Console.WriteLine("============> Processing Reset");
                MemoryLeak.FreeMemory();
                RefreshInterval = 1;
                return await Task.FromResult(new MethodResponse(200));
            }, null);
            #endregion

            #region desired props
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx) =>
            {
                Console.WriteLine($"Received desired updates [{desiredProperties.ToJson()}]");
                string desiredPropertyValue = GetPropertyValueIfFound(desiredProperties, "RefreshInterval");
                if (int.TryParse(desiredPropertyValue, out int refreshInterval))
                {
                    _logger.LogWarning("=====================> RefreshInterval: " + refreshInterval);
                    RefreshInterval = refreshInterval;
                }
                await Task.FromResult("200");
            }, null);
            #endregion

            #region reported props
            var props = new TwinCollection();
            props["SerialNumber"] = "XDre3243245345-2";
            await deviceClient.UpdateReportedPropertiesAsync(props);
            #endregion

            #region telemetry
            async Task SendTelemetryValueAsync(object telemetryPayload)
            {
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetryPayload)))
                { ContentType = "application/json", ContentEncoding = "utf-8" };
                await deviceClient.SendEventAsync(message);
            }
            #endregion

            await Task.Run(async () =>
            {
                int avg = 21;
                var rnd = new Random(Environment.TickCount);
                while (!_quitSignal.IsCancellationRequested)
                {
                    var payload = new
                    {
                        temp = avg + rnd.Next(10)
                    };
                    await SendTelemetryValueAsync(payload);
                    _logger.LogInformation("Sending CurrentTemperature: " + payload.temp);
                    await Task.Delay(RefreshInterval * 1000);
                    MemoryLeak.FillMemory();
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



    }
}
