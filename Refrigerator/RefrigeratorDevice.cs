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
                await ReportProps();
                return await Task.FromResult(new MethodResponse(200));
            }, null);
            #endregion

            #region desired props
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection desiredProperties, object ctx) =>
            {
                Console.WriteLine($"Received desired updates [{desiredProperties.ToJson()}]");
                await ReportWritablePropertyAsync("RefreshInterval", RefreshInterval, 202, "Refresh Interval Pending", desiredProperties.Version);

                string desiredPropertyValue = GetPropertyValueIfFound(desiredProperties, "RefreshInterval");
                if (int.TryParse(desiredPropertyValue, out int refreshInterval))
                {
                    //await Task.Delay(refreshInterval * 1000);
                    _logger.LogWarning("=====================> RefreshInterval: " + refreshInterval);
                    RefreshInterval = refreshInterval;
                    await ReportWritablePropertyAsync("RefreshInterval", RefreshInterval, 200, "Refresh Interval Updated", desiredProperties.Version);
                }
                else
                {
                    await ReportWritablePropertyAsync("RefreshInterval", RefreshInterval, 400, "Refresh Interval Invalid", desiredProperties.Version);
                }
                await Task.FromResult("200");
            }, null);

            async Task ReadDesiredProperties()
            {
                var twin = await deviceClient.GetTwinAsync();
                var desiredProperties = twin.Properties.Desired;
                string desiredPropertyValue = GetPropertyValueIfFound(desiredProperties, "RefreshInterval");
                if (int.TryParse(desiredPropertyValue, out int refreshInterval))
                {
                    RefreshInterval = refreshInterval;
                    _logger.LogInformation("Refresh Interval intialized to :" + refreshInterval.ToString());
                    await ReportWritablePropertyAsync("RefreshInterval", refreshInterval, 200, "RefreshInterval updated on read", desiredProperties.Version);
                }
                else
                {
                    _logger.LogWarning("Refresh interval cant be assigned to int: " + desiredPropertyValue);
                }
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


            #endregion

            #region reported props
            async Task ReportProps()
            {
                var props = new TwinCollection();
                props["SerialNumber"] = "0032434";
                props["LastInitDateTime"] = DateTime.Now.ToUniversalTime();
                await deviceClient.UpdateReportedPropertiesAsync(props);
            }

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
                await ReadDesiredProperties();
                await ReportProps();
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
