using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PnPConvention.Tests
{
  public class PnPFacadeTest : IDisposable
  {
    MockDeviceClient mockClient;
    PnPFacade facade;
    public PnPFacadeTest()
    {
      mockClient = new MockDeviceClient();
      facade = PnPFacade.CreateFromDeviceClient(mockClient);
    }

    public void Dispose()
    {
      mockClient = null;
      facade = null;
    }

    [Fact]
    public async Task NoComponentReportProperty()
    {
      await facade.ReportPropertyAsync("prop1", "val1");
      Assert.Equal("val1", mockClient.ReportedCollection["prop1"].Value);
    }

    [Fact]
    public async Task ComponentReadDesiredProperties()
    {
      TwinProperties desiredProps = new TwinProperties();
      desiredProps.Desired = new TwinCollection(@"{c1: { __t: 'c', prop1: { value: 'val1'}}}");
      Twin desired = new Twin(desiredProps);
      mockClient.DesiredProperties = desired;
      var result = await facade.ReadDesiredComponentPropertyAsync<string>("c1", "prop1");
      Assert.Equal("val1", result);
    }

    [Fact]
    public async Task ComponentReportProperty()
    {
      await facade.ReportComponentPropertyAsync("c1", "prop1", "val1");
      var compTwinValue = mockClient.ReportedCollection.GetPropertyValue<string>("c1", "prop1");
      Assert.Equal("val1", compTwinValue);
    }

    [Fact]
    public async Task ComponentReportPropertyCollection()
    {
      var props = new Dictionary<string, object>
      {
        {"prop1", "val1" },
        {"prop2", "val2" }
      };
      await facade.ReportComponentPropertyCollectionAsync("c1", props);
      var compTwinValue1 = mockClient.ReportedCollection.GetPropertyValue<string>("c1", "prop1");
      Assert.Equal("val1", compTwinValue1);
      var compTwinValue2 = mockClient.ReportedCollection.GetPropertyValue<string>("c1", "prop2");
      Assert.Equal("val2", compTwinValue2);
    }

    [Fact]
    public async Task ComponentSendTelemetry()
    {
      await facade.SendComponentTelemetryValueAsync("c1", "{t1:2}");
      Assert.Equal("application/json", mockClient.MessageSent.ContentType);
      Assert.Equal("utf-8", mockClient.MessageSent.ContentEncoding);
      Assert.Equal("c1", mockClient.MessageSent.Properties["$.sub"]);
      Assert.Equal("{t1:2}", Encoding.UTF8.GetString(mockClient.MessageSent.GetBytes()));
    }

    [Fact]
    public async Task NoComponentSendTelemetry()
    {
      await facade.SendTelemetryValueAsync("{t1:2}");
      Assert.Equal("application/json", mockClient.MessageSent.ContentType);
      Assert.Equal("utf-8", mockClient.MessageSent.ContentEncoding);
      Assert.False(mockClient.MessageSent.Properties.ContainsKey("$.sub"));
      Assert.Equal("{t1:2}", Encoding.UTF8.GetString(mockClient.MessageSent.GetBytes()));
    }

    [Fact]
    public async Task ComponentSetMethodHandler()
    {
      await facade.SetComponentCommandHandlerAsync("c1", "cmd", (MethodRequest req, object ctx) =>
      {
        return Task.FromResult(new MethodResponse(0));
      }, this);

      Assert.Equal("c1*cmd", mockClient.MethodSubscription);
    }

    [Fact]
    public async Task NoComponentSetMethodHandler()
    {
      await facade.SetCommandHandlerAsync("cmd", (MethodRequest req, object ctx) =>
      {
        return Task.FromResult(new MethodResponse(0));
      }, this);
      Assert.Equal("cmd", mockClient.MethodSubscription);
    }

    [Fact]
    public async Task ComponentSetDesiredPropertyHandler()
    {
      string valueReaded = string.Empty;
      facade.SetDesiredPropertyUpdateCommandHandler("c1", (TwinCollection newValue) =>
      {
        valueReaded = newValue.ToJson();
      });

      TwinCollection desired = new TwinCollection(@"{ c1: {prop1: 'val1'}}");
      await mockClient.DesiredPropertyUpdateCallback(desired, this);
      Assert.Equal("{\"c1\":{\"prop1\":\"val1\"}}", valueReaded);
    }

  
  }
}
