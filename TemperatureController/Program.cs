using DeviceRunner;
using System.Threading.Tasks;

namespace TemperatureController
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await ConsoleRunnerService<TemperatureControllerDevice>.RunDeviceAsync(args);
    }
  }
}
