using DeviceRunner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{
  class Program
  {
    public static async Task Main(string[] args)
    { 
      //ILogger log = LoggerFactory.Create(b => {
      //  b.AddFilter("DeviceRunner", LogLevel.Trace);
      //  b.AddConsole();
      //}).CreateLogger("DeviceRunner");
      //string cs = "HostName=rido-pnp-ppr.azure-devices.net;DeviceId=simple-01;SharedAccessKey=p4WCJcwJLlicdzi7gKLZiub3C+2LES430kA/VJabSIM=";
      //await  new SimpleThermostatDevice().RunDeviceAsync(cs, log, CancellationToken.None);

      await ConsoleRunnerService<SimpleThermostatDevice>.RunDeviceAsync(args);
    }
  }
}
