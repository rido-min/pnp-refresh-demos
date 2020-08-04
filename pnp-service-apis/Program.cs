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
      var cs = "HostName=ridohub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=EcSKltC/G6tc8jYaWWDvQh2wdCWMr5XLFRSBvwg0YdA=";
      var deviceId = "adu-sim-01";
      var registry = RegistryManager.CreateFromConnectionString(cs);

      var twin = await registry.GetTwinAsync(deviceId);
      Console.WriteLine(twin.ModelId);

      //var patch =
      //          @"{
      //              properties: {
      //                desired: {
      //                  Orchestrator: {
      //                    Action: 332,
      //                TargetVersion: 332,
      //                Files: {
      //                      aaaa: 'https://aka.ms.332',
      //                 sdfa: 332
      //                    },
      //              ExpectedContentId: '332',
      //              InstalledCriteria: '332'
      //                }
      //              }
      //          }";

      var patch =
              @"{
                    properties: {
                      desired: {
                        Orchestrator: {
                          Action: 44,
				                  TargetVersion: 332,
				                  Files: null
                        },
				                InstalledCriteria: '332'
                      }
                    }
                }";



      var t2 = await registry.UpdateTwinAsync(deviceId, patch, twin.ETag);
      Console.WriteLine(t2.ToJson());

      var device = await registry.GetDeviceAsync(deviceId);
      
      
            
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
      var resp = await serviceClient.InvokeDeviceMethodAsync(deviceId, c2dm );
      Console.WriteLine(resp.Status);
      string result = resp.GetPayloadAsJson();
      Console.WriteLine(result);



    }
  }
}
