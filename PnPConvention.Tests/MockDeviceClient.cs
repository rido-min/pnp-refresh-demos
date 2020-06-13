using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Threading.Tasks;

namespace PnPConvention.Tests
{
  class MockDeviceClient : IPnPDeviceClient
  {
    public Twin DesiredProperties;
    public Task<Twin> GetTwinAsync()
    {
      return Task.FromResult(DesiredProperties);
    }
    public Message MessageSent;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task SendEventAsync(Message message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
      MessageSent = message;
    }
    public DesiredPropertyUpdateCallback DesiredPropertyUpdateCallback;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, object userContext)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
      DesiredPropertyUpdateCallback = callback;
    }

    public string MethodSubscription;
    public MethodCallback MethodCallback;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task SetMethodHandlerAsync(string methodName, MethodCallback methodHandler, object userContext)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
      MethodSubscription = methodName;
      MethodCallback = methodHandler;
    }

    public TwinCollection ReportedCollection;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task UpdateReportedPropertiesAsync(TwinCollection collection)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
      ReportedCollection = collection;
    }
  }
}
