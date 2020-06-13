
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Threading.Tasks;

namespace PnPConvention
{
  public interface IPnPDeviceClient
  {
    Task<Twin> GetTwinAsync();
    Task SendEventAsync(Message message);
    Task UpdateReportedPropertiesAsync(TwinCollection collection);
    Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext);
    Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext);
  }
}
