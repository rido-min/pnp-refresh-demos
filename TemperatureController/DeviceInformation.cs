using System;
using System.Collections.Generic;

namespace TemperatureController
{
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

    public static DeviceInfo ThisDeviceInfo
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
