using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace pnp_service_apis
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var cs = "HostName=summerrelease-test-03.private.azure-devices-int.net;SharedAccessKeyName=iothubowner;SharedAccessKey=7NOwXejCnfJKrlFJ8GcWXlTw51W1jDq+SBuQExEnh9c=";

      var registry = RegistryManager.CreateFromConnectionString(cs);
      var device = await registry.GetDeviceAsync("rido-pnp-01");


      Console.WriteLine(JsonConvert.SerializeObject(device));

      var serviceClient = ServiceClient.CreateFromConnectionString(cs);
      var c2dm = new CloudToDeviceMethod("reboot");
      c2dm.SetPayloadJson(@"
        {
          ""commandRequest"": {
              ""value"": 2,
              ""requestId"": ""b5251ea1-a5c5-4906-b414-046d00d2cfb1""
          }
        }
      ");
      var resp = await serviceClient.InvokeDeviceMethodAsync("rido-pnp-01", c2dm );
      Console.WriteLine(resp.Status);
      string result = resp.GetPayloadAsJson();
      Console.WriteLine(result);



    }
  }
}
