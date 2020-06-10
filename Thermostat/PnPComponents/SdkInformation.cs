using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using PnPConvention;
using System.Collections.Generic;

namespace Thermostat.PnPComponents
{
  public class SdkInformationInterface : PnPComponent
  {
    public SdkInformationInterface(DeviceClient client, string componentName) : base(client, componentName)
    {
    }

    public async Task ReportSdkInfoPropertiesAsync()
    {
      var properties = new Dictionary<string, object>(3);
      properties.Add("language", "C# 8.0");
      properties.Add("version", "Device Client 1.25.0");
      properties.Add("vendor", "Microsoft");

      await base.ReportPropertyCollectionAsync(properties);
      Console.WriteLine($"SdkInformationInterface: sent {properties.Count} properties.");
    }
  }
}
