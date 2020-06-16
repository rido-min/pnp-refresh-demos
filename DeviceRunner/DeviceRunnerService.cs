using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceRunner
{
  /// <summary>
  /// DeviceRunnerService uses .NET Hosting infraestructure to integrate with
  /// IConfiguration and ILogger from a Console application.
  /// 
  /// It forces to have a /DeviceConnectionString config setting to start.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class DeviceRunnerService<T> : BackgroundService where T : IRunnableDevice, new()
  {
    readonly ILogger<DeviceRunnerService<T>> logger;
    readonly IConfiguration configuration;

    public DeviceRunnerService(ILogger<DeviceRunnerService<T>> logger, IConfiguration configuration)
    {
      this.logger = logger;
      this.configuration = configuration;
    }
    public static async Task RunDeviceAsync(string[] args)
    {
      var host = Host.CreateDefaultBuilder(args)
             .ConfigureServices((hostContext, services) =>
                 services.AddHostedService<DeviceRunnerService<T>>());
      await host.RunConsoleAsync().ConfigureAwait(true);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      try
      {
        var connectionString = ValidateConfigOrDie();
        var device = new T();
        await device.RunDeviceAsync(connectionString, logger, stoppingToken);
      }
      catch (Exception ex)
      {
        this.logger.LogError(ex.Message);
        this.logger.LogWarning(ex.ToString());
      }
    }

    private string ValidateConfigOrDie()
    {
      var connectionString = configuration.GetValue<string>("DeviceConnectionString");
      if (string.IsNullOrWhiteSpace(connectionString))
      {
        logger.LogError("ConnectionString not found using key: DeviceConnectionString");
        throw new ApplicationException("Connection String 'DeviceConnectionString' not found in the configured providers.");
      }
      return connectionString;
    }
  }
}
