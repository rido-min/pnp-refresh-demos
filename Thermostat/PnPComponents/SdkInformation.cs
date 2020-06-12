using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using PnPConvention;
using System.Collections.Generic;

namespace Thermostat.PnPComponents
{
  public class SdkInformation : PnPComponent
  {
    public SdkInformation(DeviceClient client, string componentName) : base(client, componentName)
    {
    }
    public async Task ReportSdkInfoPropertiesAsync()
    {
      await base.ReportPropertyCollectionAsync(ThisSdkInfo);
      Console.WriteLine($"SdkInformationInterface: sent {ThisSdkInfo.Count} properties.");
    }

    public static Dictionary<string, object> ThisSdkInfo
    {
      get
      {
        return new Dictionary<string, object>(3)
        {
          { "language", "C# 8.0" },
          { "version", "Device Client 1.25.0" },
          { "vendor", "Microsoft" }
        };
      }
    }
  }
}
