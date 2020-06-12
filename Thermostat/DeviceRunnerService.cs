using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thermostat
{
  class DeviceRunnerService : BackgroundService
  {
    readonly ILogger<DeviceRunnerService> logger;
    readonly IConfiguration configuration;

    public DeviceRunnerService(ILogger<DeviceRunnerService> logger, IConfiguration configuration)
    {
      this.logger = logger;
      this.configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      try
      {
        var connectionString = ValidateConfigOrDie();
        var device = new ThermostatNoClass(connectionString, logger, stoppingToken);
        await device.RunDeviceAsync();
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
        throw new ConfigurationErrorsException("Connection String 'DeviceConnectionString' not found in the configured providers.");
      }
      return connectionString;
    }
  }
}
