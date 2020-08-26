
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace PnPConvention
{
  [ExcludeFromCodeCoverage]
  public class PnPDeviceClient : IPnPDeviceClient
  {
    readonly DeviceClient client;
    readonly IPnPDeviceClient mockClient;
    public PnPDeviceClient(IPnPDeviceClient mockClient)
    {
      this.mockClient = mockClient;
    }

    public PnPDeviceClient(DeviceClient client)
    {
      this.client = client;
    }

    public async Task<Twin> GetTwinAsync()
    {
      if (client == null)
      {
        return await mockClient.GetTwinAsync();
      }
      else
      {
        return await client.GetTwinAsync();
      }
    }

    public async Task SendEventAsync(Message message)
    {
      if (client == null)
      {
        await mockClient.SendEventAsync(message);
      }
      else
      {
        await client.SendEventAsync(message);
      }

    }

    public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
    {
      if (client == null)
      {
        await mockClient.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
      }
      else
      {
        await client.SetDesiredPropertyUpdateCallbackAsync(callback, userContext);
      }
    }

    public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
    {
      if (client == null)
      {
        await mockClient.SetMethodHandlerAsync(methodName, methodHandler, userContext);
      }
      else
      {
        await client.SetMethodHandlerAsync(methodName, methodHandler, userContext);
      }
    }

    public async Task UpdateReportedPropertiesAsync(TwinCollection collection)
    {
      if (client == null)
      {
        await mockClient.UpdateReportedPropertiesAsync(collection);
      }
      else
      {
        await client.UpdateReportedPropertiesAsync(collection);
      }
    }
  }
}
