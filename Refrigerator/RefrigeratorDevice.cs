using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PnPConvention;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Refrigerator
{
  class RefrigeratorDevice : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:dev:rido:refrigerator;1";
    
    ILogger logger;

    DeviceClient deviceClient;
    readonly int defaultRefreshInterval = 1;
    int RefreshInterval;

    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
    
      this.logger = logger;
      
      deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt,
        new ClientOptions { ModelId = modelId });

      var refrigerator = new PnPComponent(deviceClient, logger);

      await refrigerator.SetPnPCommandHandlerAsync("Reset", async (MethodRequest req, object ctx) =>
      {
        logger.LogWarning("============> Processing Reset");
        MemoryLeak.FreeMemory();
        await refrigerator.ReportPropertyAsync("LastInitDateTime", DateTime.Now.ToUniversalTime());
        return await Task.FromResult(new MethodResponse(200));
      }, null);

      await refrigerator.SetPnPDesiredPropertyHandlerAsync<int>("RefreshInterval", (int newValue) =>
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
