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
      var host = Host.CreateDefaultBuilder(args)
          .ConfigureServices((hostContext, services) =>
              services.AddHostedService<DeviceRunnerService>());
      await host.RunConsoleAsync().ConfigureAwait(true);
    }
  }
}
