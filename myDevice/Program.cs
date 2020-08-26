using DeviceRunner;
using System.Threading.Tasks;

namespace myDevice
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await ConsoleRunnerService<Device>.RunDeviceAsync(args);
    }
  }
}
