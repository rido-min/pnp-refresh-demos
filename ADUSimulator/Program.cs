using ADUSimulator;
using DeviceRunner;
using System.Threading.Tasks;

namespace ADUSimulator
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await ConsoleRunnerService<ADUSimulatorDevice>.RunDeviceAsync(args);
    }
  }
}
