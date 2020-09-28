using DeviceRunner;
using Microsoft.Azure.Devices.Client;
//using Microsoft.Azure.Devices.Client.PlugAndPlay;
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

        //var arrMsg = PnpHelper.CreateMessage("telWithArray", JsonConvert.SerializeObject(
        //    new { telWithArray = new int[] { 1, 2, 3 } }), "adv_telem");

          var arrMsg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
              new { telWithArray = new int[] { 1, 2, 3 } })))
          { ContentEncoding = "utf-8", ContentType = "application/json" };
          arrMsg.Properties.Add("$.sub", "adv_telem");
          await deviceClient.SendEventAsync(arrMsg);


        //var mapMsg = PnpHelper.CreateMessage(
        //  "telWithMap",
        //  JsonConvert.SerializeObject(new { telWithMap = new Dictionary<string, string> { { "uno", "11111" }, { "dos", "222222222222" } } }),
        //  "adv_telem");


          var mapMsg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
            new { telWithMap = new Dictionary<string, string> { { "uno", "11111" }, { "dos", "222222222222" } } }
          )))
          { ContentEncoding = "utf-8", ContentType = "application/json" };
          mapMsg.Properties.Add("$.sub", "adv_telem");
          await deviceClient.SendEventAsync(mapMsg);

          //var pointMsg = PnpHelper.CreateMessage(
          //  "telWithPoint",
          //  JsonConvert.SerializeObject(new { telWithPoint = new { lon = -87.728943, lat = 42.051697 } }),
          //  "adv_telem");

          var pointMsg = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
            new { telWithPoint = new { lon = -87.728943, lat = 42.051697 } }
          )))
          { ContentEncoding = "utf-8", ContentType = "application/json" };
          pointMsg.Properties.Add("$.sub", "adv_telem");

          await deviceClient.SendEventAsync(pointMsg);

          logger.LogInformation("Sending Telemetry");
          await Task.Delay(5000);
        }
      });
    }
  }
}
