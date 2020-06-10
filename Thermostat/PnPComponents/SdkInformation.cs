using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Thermostat.PnPConvention;

namespace Thermostat.PnPComponents
{
    public class SdkInformationInterface  : PnPComponent
    {
        public SdkInformationInterface(string componentName, DeviceClient client) : base(componentName, client)
        {
        }

        public async Task ReportSdkInfoPropertiesAsync()
        {
            PnPPropertyCollection propertyCollection = base.NewReportedProperties();
            propertyCollection.Set("language", "C# 8.0");
            propertyCollection.Set("version", "Device Client 1.25.0");
            propertyCollection.Set("vendor", "Microsoft");
            
            await base.client.UpdateReportedPropertiesAsync(propertyCollection);
            Console.WriteLine($"SdkInformationInterface: sent {propertyCollection.Count} properties.");
        }
    }
}
