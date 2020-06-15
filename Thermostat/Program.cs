using DeviceRunner;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Thermostat
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await DeviceRunnerService<ThermostatNoClass>.RunDeviceAsync(args);
    }
  }
}
