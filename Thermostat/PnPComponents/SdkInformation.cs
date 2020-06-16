using Microsoft.Azure.Devices.Client;
using PnPConvention;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
