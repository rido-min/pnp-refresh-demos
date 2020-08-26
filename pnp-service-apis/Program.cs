using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
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
      var cs = "HostName=ridohub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=lbobW7o9SKg1WJho6kZ6ZlQkub325YI3eLXmlvzXLOw=";
      var deviceId = "dev10";
      var registry = RegistryManager.CreateFromConnectionString(cs);
      var twin = await registry.GetTwinAsync(deviceId);
      Console.WriteLine(twin.ModelId);
      Console.WriteLine(twin.ToJson());
      
      var patch =
              @"{
                  properties: {
                    desired: {
                      deviceStatus: 'from serviceSDK',
                      comp1: {
                        compStatus: 'fromseviceSDK'
                      }
                    }
                  }
              }";

      var t2 = await registry.UpdateTwinAsync(deviceId, patch, twin.ETag);
      Console.WriteLine(t2.ToJson());

      twin = await registry.GetTwinAsync(deviceId);
      Console.WriteLine(twin.ToJson());

    }
  }
}
