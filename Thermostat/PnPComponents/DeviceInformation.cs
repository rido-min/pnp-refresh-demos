using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;
using Thermostat.PnPConvention;

namespace Thermostat.PnPComponents
{
    public class DeviceInformation : PnPComponent
    {
        public DeviceInformation(DeviceClient client, string componentName) : base (client, componentName)
        {
        }

        public async Task ReportDeviceInfoPropertiesAsync(DeviceInfo di)
        {
            PnPPropertyCollection propertyCollection = base.NewReportedProperties();
            propertyCollection.Set("manufacturer", di.Manufacturer);
            propertyCollection.Set("model", di.Model);
            propertyCollection.Set("swVersion", di.SoftwareVersion);
            propertyCollection.Set("osName", di.OperatingSystemName);
            propertyCollection.Set("processorArchitecture", di.ProcessorArchitecture);
            propertyCollection.Set("processorManufacturer", di.ProcessorManufacturer);
            propertyCollection.Set("totalMemory", di.TotalMemory);
            propertyCollection.Set("totalStorage", di.TotalStorage);

            await base.client.UpdateReportedPropertiesAsync(propertyCollection.Instance);
            Console.WriteLine($"DeviceInformationInterface: sent {propertyCollection.Instance.Count} properties.");
        }
    }

    public class DeviceInfo
    {
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SoftwareVersion { get; set; }
        public string OperatingSystemName { get; set; }
        public string ProcessorArchitecture { get; set; }
        public string ProcessorManufacturer { get; set; }
        public long TotalMemory { get; set; }
        public long TotalStorage { get; set; }
    }
}
