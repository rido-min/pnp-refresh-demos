using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading.Tasks;
using PnPConvention;
using System.Collections.Generic;

namespace Thermostat.PnPComponents
{
  public class DeviceInformation : PnPComponent
  {
    public DeviceInformation(DeviceClient client, string componentName) : base(client, componentName)
    {
    }

    public async Task ReportDeviceInfoPropertiesAsync(DeviceInfo di)
    {
      var properties = di.ToDictionary();
      await base.ReportPropertyCollectionAsync(properties);
      Console.WriteLine($"DeviceInformationInterface: sent {properties.Count} properties.");
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

    public Dictionary<string, object> ToDictionary()
    {
      var properties = new Dictionary<string, object>(8);
      properties.Add("manufacturer", Manufacturer);
      properties.Add("model", Model);
      properties.Add("swVersion", SoftwareVersion);
      properties.Add("osName", OperatingSystemName);
      properties.Add("processorArchitecture", ProcessorArchitecture);
      properties.Add("processorManufacturer", ProcessorManufacturer);
      properties.Add("totalMemory", TotalMemory);
      properties.Add("totalStorage", TotalStorage);
      return properties;
    }
  }
}
