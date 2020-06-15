using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceRunner
{
  public interface IRunnableDevice
  {
    Task RunDeviceAsync(string connectionString, ILogger logger, CancellationToken cancellationToken);
  }
}
