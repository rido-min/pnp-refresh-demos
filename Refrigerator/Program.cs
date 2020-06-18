using System.Threading.Tasks;

namespace Refrigerator
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await DeviceRunner.ConsoleRunnerService<RefrigeratorDevice>.RunDeviceAsync(args);
    }
  }
}
