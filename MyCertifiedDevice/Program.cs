using DeviceRunner;
using MyCertifiedDevice;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace MyCertifiedDevice
{
  class Program
  {
    public static async Task Main(string[] args)
    {
      await ConsoleRunnerService<MeshReproDevice>.RunDeviceAsync(args);
    }
  }
}
