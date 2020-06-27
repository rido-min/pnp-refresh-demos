using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceRunner
{
  public interface IRunnableWithConnectionString
  {
    Task RunAsync(string connectionString, ILogger logger, CancellationToken cancellationToken);
  }
}
