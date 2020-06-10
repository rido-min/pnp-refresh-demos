using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using PnPConvention;

namespace Thermostat.PnPComponents
{
    public class SdkInformationInterface  : PnPComponent
    {
        public SdkInformationInterface(DeviceClient client, string componentName) : base(client, componentName)
        {
        }

        public async Task ReportSdkInfoPropertiesAsync()
        {
            var propertyCollection = new TwinCollection();
            propertyCollection.AddComponentProperty(base.componentName, "language", "C# 8.0");
            propertyCollection.AddComponentProperty(base.componentName, "version", "Device Client 1.25.0");
            propertyCollection.AddComponentProperty(base.componentName, "vendor", "Microsoft");
            
            await base.client.UpdateReportedPropertiesAsync(propertyCollection);
            Console.WriteLine($"SdkInformationInterface: sent {propertyCollection.Count} properties.");
        }
    }
}
