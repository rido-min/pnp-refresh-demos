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
        public SdkInformationInterface(DeviceClient client, string componentName) : base(client, componentName)
        {
        }

        public async Task ReportSdkInfoPropertiesAsync()
        {
            PnPPropertyCollection propertyCollection = base.NewReportedProperties();
            propertyCollection.Set("language", "C# 8.0");
            propertyCollection.Set("version", "Device Client 1.25.0");
            propertyCollection.Set("vendor", "Microsoft");
            
            await base.client.UpdateReportedPropertiesAsync(propertyCollection.Instance);
            Console.WriteLine($"SdkInformationInterface: sent {propertyCollection.Instance.Count} properties.");
        }
    }
}
