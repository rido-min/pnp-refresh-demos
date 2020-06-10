using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;
using PnPConvention;

namespace Thermostat.PnPComponents
{
    public class DeviceInformation : PnPComponent
    {
        public DeviceInformation(DeviceClient client, string componentName) : base (client, componentName)
        {
            
        }

        public async Task ReportDeviceInfoPropertiesAsync(DeviceInfo di)
        {
            var propertyCollection = new TwinCollection();
            propertyCollection.AddComponentProperty(base.componentName, "manufacturer", di.Manufacturer);
            propertyCollection.AddComponentProperty(base.componentName, "model", di.Model);
            propertyCollection.AddComponentProperty(base.componentName, "swVersion", di.SoftwareVersion);
            propertyCollection.AddComponentProperty(base.componentName, "osName", di.OperatingSystemName);
            propertyCollection.AddComponentProperty(base.componentName, "processorArchitecture", di.ProcessorArchitecture);
            propertyCollection.AddComponentProperty(base.componentName, "processorManufacturer", di.ProcessorManufacturer);
            propertyCollection.AddComponentProperty(base.componentName, "totalMemory", di.TotalMemory);
            propertyCollection.AddComponentProperty(base.componentName, "totalStorage", di.TotalStorage);

            await base.client.UpdateReportedPropertiesAsync(propertyCollection);
            Console.WriteLine($"DeviceInformationInterface: sent {propertyCollection.Count} properties.");
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
