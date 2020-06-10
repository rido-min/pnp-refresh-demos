using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Thermostat.PnPComponents;

namespace Thermostat
{
    class ThermostatDevice
    {
        const string modelId = "dtmi:com:example:Thermostat;1";

        string _connectionString;
        private readonly ILogger _logger;
        private CancellationToken _quitSignal;

        DeviceClient deviceClient;

        TemperatureSensor tempSensor;
        DiagnosticsInterface diag;
        DeviceInformationInterface deviceInfo;
        SdkInformationInterface sdkInfo;

        public ThermostatDevice(string connectionString, ILogger logger, CancellationToken cancellationToken)
        {
            _quitSignal = cancellationToken;
            _logger = logger;
            _connectionString = connectionString;
        }

        public async Task RunDeviceAsync()
        {
            deviceClient = DeviceClient.CreateFromConnectionString(_connectionString + ";ModelId=" + modelId, TransportType.Mqtt);
          
            tempSensor = new TemperatureSensor("tempSensor1", deviceClient);
            diag = new DiagnosticsInterface("diag", deviceClient);
            deviceInfo = new DeviceInformationInterface("deviceInfo", deviceClient);
            sdkInfo = new SdkInformationInterface( "sdkInfo", deviceClient);

            diag.OnRebootCommand += Diag_OnRebootCommand;
            tempSensor.OnTargetTempReceived += TempSensor_OnTargetTempReceived;
            
            await deviceInfo.ReportDeviceInfoPropertiesAsync(ThisDeviceInfo);
            await sdkInfo.ReportSdkInfoPropertiesAsync();

            await tempSensor.InitAsync();

            await Task.Run(async () =>
            {
                while (!_quitSignal.IsCancellationRequested)
                {
                    await tempSensor.SendTemperatureTelemetryValueAsync(tempSensor.CurrentTemperature);
                    await diag.SendWorkingTelemetryAsync(Environment.WorkingSet);

                    _logger.LogInformation("Sending workingset and temp " + tempSensor.CurrentTemperature);
                    await Task.Delay(5000);
                }
            });
        }

       
        private async Task ProcessTempUpdateAsync(double targetTemp)
        {
            // gradually increase current temp to target temp
            double step = (targetTemp - tempSensor.CurrentTemperature) / 10d;
            for (int i = 9; i >= 0; i--)
            {
                tempSensor.CurrentTemperature = targetTemp - step * (double)i;
                await tempSensor.SendTemperatureTelemetryValueAsync(tempSensor.CurrentTemperature);
                await tempSensor.ReportCurrentTemperatureAsync(tempSensor.CurrentTemperature);
                _logger.LogWarning("Current Temp Adjusted to: " + tempSensor.CurrentTemperature.ToString());
                await Task.Delay(1000);
            }
        }


        private void Diag_OnRebootCommand(object sender, RebootCommandEventArgs e)
        {
            for (int i = 0; i < e.Delay; i++)
            {
                _logger.LogWarning("================> REBOOT COMMAND RECEIVED <===================");
                Task.Delay(1000).Wait();
            }
            tempSensor.CurrentTemperature = 0;
            this.ProcessTempUpdateAsync(21).Wait();
        }

        private void TempSensor_OnTargetTempReceived(object sender, TemperatureEventArgs ea)
        {
            _logger.LogWarning("TargetTempUpdated: " + ea.Temperature);
            this.ProcessTempUpdateAsync(ea.Temperature).Wait();
        }

        DeviceInfo ThisDeviceInfo
        {
            get
            {
                return new DeviceInfo
                {
                    Manufacturer = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
                    Model = Environment.OSVersion.Platform.ToString(),
                    SoftwareVersion = Environment.OSVersion.VersionString,
                    OperatingSystemName = Environment.GetEnvironmentVariable("OS"),
                    ProcessorArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
                    ProcessorManufacturer = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
                    TotalStorage = 123,// System.IO.DriveInfo.GetDrives()[0].TotalSize,
                    TotalMemory = Environment.WorkingSet
                };
            }
        }
    }
}