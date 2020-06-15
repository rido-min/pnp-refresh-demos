using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnPConvention;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refrigerator
{
  class RefrigeratorDevice
  {
    const string modelId = "dtmi:dev:rido:refrigerator;1";
    readonly string connectionString;
    private readonly ILogger logger;
    private readonly CancellationToken quitSignal;

    DeviceClient deviceClient;
    PnPComponent refrigerator;
    readonly int defaultRefreshInterval = 1;
    int RefreshInterval;

    public RefrigeratorDevice(string connectionString, ILogger logger, CancellationToken cancellationToken)
    {
      quitSignal = cancellationToken;
      this.logger = logger;
      this.connectionString = connectionString;
    }

    public async Task RunDeviceAsync()
    {
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt,
        new ClientOptions { ModelId = modelId });

      refrigerator = new PnPComponent(deviceClient, logger);

      await refrigerator.SetPnPCommandHandlerAsync("Reset", async (MethodRequest req, object ctx) =>
      {
        Console.WriteLine("============> Processing Reset");
        MemoryLeak.FreeMemory();
        await refrigerator.ReportPropertyAsync("LastInitDateTime", DateTime.Now.ToUniversalTime());
        return await Task.FromResult(new MethodResponse(200));
      }, null);

      await refrigerator.SetPnPDesiredPropertyHandlerAsync<int>("RefreshInterval", (object newValue) =>
      {
        if (int.TryParse(newValue.ToString(), out int refreshInterval))
        {
          logger.LogWarning("=====================> RefreshInterval: " + refreshInterval);
          RefreshInterval = refreshInterval;
        }
      }, this);

      await Task.Run(async () =>
      {
        RefreshInterval = await refrigerator.ReadDesiredPropertyAsync<int>("RefreshInterval");
        if (RefreshInterval == default(int)) RefreshInterval = defaultRefreshInterval;

        await refrigerator.ReportPropertyAsync("SerialNumber", "1235435");
        await refrigerator.ReportPropertyAsync("LastInitDateTime", DateTime.Now.ToUniversalTime());

        int avg = 21;
        var rnd = new Random(Environment.TickCount);
        while (!quitSignal.IsCancellationRequested)
        {
          var payload = new
          {
            temp = avg + rnd.Next(10)
          };
          await refrigerator.SendTelemetryValueAsync(JsonConvert.SerializeObject(payload));
          logger.LogInformation("Sending CurrentTemperature: " + payload.temp);
          await Task.Delay(RefreshInterval * 1000);
          MemoryLeak.FillMemory();
        }
      });
    }
  }
}
