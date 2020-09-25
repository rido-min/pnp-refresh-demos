using DeviceRunner;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rido;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyCertifiedDevice
{
  class MeshReproDevice : IRunnableWithConnectionString
  {
    const string modelId = "dtmi:com:rido:MeshRepro;1";

    ILogger logger;
    DeviceClient deviceClient;
    public async Task RunAsync(string connectionString, ILogger logger, CancellationToken quitSignal)
    {
      this.logger = logger;

      deviceClient = await DeviceClientFactory.CreateDeviceClientAsync(connectionString, logger, modelId);

      var twin = await deviceClient.GetTwinAsync();

      await Task.Run(async () =>
      {
        while (!quitSignal.IsCancellationRequested)
        {

          await deviceClient.SendEventAsync(
            new Message(
              Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
               new {
                 People = new {
                   personName = "rido",
                   isValid = true,
                   birthday = DateTime.Now.ToUniversalTime()
                 }
               }
               )))
            {
              ContentEncoding = "utf-8",
              ContentType = "application/json"
            });

          logger.LogInformation("Sending Telemetry");
          await Task.Delay(5000);
        }
      });
    }
  }
}
