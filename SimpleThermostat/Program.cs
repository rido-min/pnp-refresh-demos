using DeviceRunner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Thermostat
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await DeviceRunnerService<ThermostatDevice>.RunDeviceAsync(args);
    }
  }
}
