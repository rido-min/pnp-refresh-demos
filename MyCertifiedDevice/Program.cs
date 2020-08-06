using DeviceRunner;
using System.Threading.Tasks;

namespace Thermostat
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await ConsoleRunnerService<DatesTestDevice>.RunDeviceAsync(args);
    }
  }
}
