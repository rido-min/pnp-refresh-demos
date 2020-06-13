using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PnPConvention.Tests
{

  public class PnPComponentTest
  {

    MockDeviceClient mockClient;
    PnPComponent nocomp;
    PnPComponent comp;

    public PnPComponentTest()
    {
      ILogger debugLogger = TestLogging.CreateLogger();
      mockClient = new MockDeviceClient();
      nocomp = new PnPComponent(mockClient, debugLogger);
      comp = new PnPComponent(mockClient, "c1", debugLogger);
    }

    [Fact]
    public async Task NoComponentReportProperty()
    {
      await nocomp.ReportPropertyAsync("prop1", "val1");
      Assert.Equal("val1", mockClient.ReportedCollection["prop1"].Value);
    }

    [Fact]
    public async Task ComponentReportProperty()
    {
      await comp.ReportPropertyAsync("prop1", "val1");
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
      await comp.ReportPropertyCollectionAsync(props);
      var compTwinValue1 = mockClient.ReportedCollection.GetPropertyValue<string>("c1", "prop1");
      Assert.Equal("val1", compTwinValue1);
      var compTwinValue2 = mockClient.ReportedCollection.GetPropertyValue<string>("c1", "prop2");
      Assert.Equal("val2", compTwinValue2);
    }

    [Fact]
    public async Task NoComponentReportPropertyCollection()
    {
      var props = new Dictionary<string, object>
      {
        {"prop1", "val1" },
        {"prop2", "val2" }
      };
      await nocomp.ReportPropertyCollectionAsync(props);
      var compTwinValue1 = mockClient.ReportedCollection.GetPropertyValue<string>("prop1");
      Assert.Equal("val1", compTwinValue1);
      var compTwinValue2 = mockClient.ReportedCollection.GetPropertyValue<string>("prop2");
      Assert.Equal("val2", compTwinValue2);
    }


    [Fact]
    public async Task ComponentSendTelemetry()
    {
      await comp.SendTelemetryValueAsync("{t1:2}");
      Assert.Equal("application/json", mockClient.MessageSent.ContentType);
      Assert.Equal("utf-8", mockClient.MessageSent.ContentEncoding);
      Assert.Equal("c1", mockClient.MessageSent.Properties["$.sub"]);
      Assert.Equal("{t1:2}", Encoding.UTF8.GetString(mockClient.MessageSent.GetBytes()));
    }

    [Fact]
    public async Task NoComponentSendTelemetry()
    {
      await nocomp.SendTelemetryValueAsync("{t1:2}");
      Assert.Equal("application/json", mockClient.MessageSent.ContentType);
      Assert.Equal("utf-8", mockClient.MessageSent.ContentEncoding);
      Assert.False(mockClient.MessageSent.Properties.ContainsKey("$.sub"));
      Assert.Equal("{t1:2}", Encoding.UTF8.GetString(mockClient.MessageSent.GetBytes()));
    }

    [Fact]
    public async Task ComponentSetMethodHandler()
    {
      await comp.SetPnPCommandHandlerAsync("cmd", (MethodRequest req, object ctx) => 
      {
        return  Task.FromResult(new MethodResponse(0));
      }, this);
      
      Assert.Equal("c1*cmd", mockClient.MethodSubscription);
    }

    [Fact]
    public async Task NoComponentSetMethodHandler()
    {
      await nocomp.SetPnPCommandHandlerAsync("cmd", (MethodRequest req, object ctx) =>
      {
        return Task.FromResult(new MethodResponse(0));
      }, this);
      Assert.Equal("cmd", mockClient.MethodSubscription);
    }

    [Fact]
    public async Task NoComponentSetDesiredPropertyHandler()
    {
      string valueReaded = string.Empty;
      await nocomp.SetPnPDesiredPropertyHandlerAsync<string>("prop1", (object newValue) =>
      {
        valueReaded = newValue.ToString();
      }, this);
      TwinCollection desired = new TwinCollection(@"{prop1: 'val1'}");
      await mockClient.DesiredPropertyUpdateCallback(desired, this);
      Assert.Equal("val1", valueReaded);
      var reported = mockClient.ReportedCollection;
      Assert.Equal("{\"prop1\":{\"value\":\"val1\",\"ac\":200,\"av\":0,\"ad\":\"update complete\"}}", reported.ToJson());
    }

    [Fact]
    public async Task ComponentSetDesiredPropertyHandler()
    {
      string valueReaded = string.Empty;
      await comp.SetPnPDesiredPropertyHandlerAsync<string>("prop1", (object newValue) =>
      {
        valueReaded = newValue.ToString();
      }, this);
      TwinCollection desired = new TwinCollection(@"{c1: { __t: 'c', prop1: { value: 'val1'}}}");
      await mockClient.DesiredPropertyUpdateCallback(desired, this);
      Assert.Equal("val1", valueReaded);
      var reported = mockClient.ReportedCollection;
      Assert.Equal("{\"c1\":{\"__t\":\"c\",\"prop1\":{\"value\":\"val1\",\"ac\":200,\"av\":0,\"ad\":\"update complete\"}}}", reported.ToJson());
    }

    [Fact]
    public async Task ComponentSetDesiredPropertyHandlerWithNullValue()
    {
      object valueReaded = null;
      await comp.SetPnPDesiredPropertyHandlerAsync<string>("prop1", (object newValue) =>
      {
        valueReaded = newValue;
      }, this);
      TwinCollection desired = new TwinCollection(@"{c1: { __t: 'c', notFound: { value: 'val1'}}}");
      await mockClient.DesiredPropertyUpdateCallback(desired, this);
      Assert.Null(valueReaded);
    }

    [Fact]
    public async Task NoComponentSetDesiredPropertyHandlerWithNullValue()
    {
      object valueReaded = null;
      await nocomp.SetPnPDesiredPropertyHandlerAsync<string>("prop1", (object newValue) =>
      {
        valueReaded = newValue;
      }, this);
      TwinCollection desired = new TwinCollection(@"{notFound: { value: 'val1'}}");
      await mockClient.DesiredPropertyUpdateCallback(desired, this);
      Assert.Null(valueReaded);
    }


    [Fact]
    public async Task ComponentReadDesiredProperties()
    {
      TwinProperties desiredProps = new TwinProperties();
      desiredProps.Desired = new TwinCollection(@"{c1: { __t: 'c', prop1: { value: 'val1'}}}");
      Twin desired = new Twin(desiredProps);
      mockClient.DesiredProperties = desired;
      var result = await comp.ReadDesiredPropertyAsync<string>("prop1");
      Assert.Equal("val1", result);
    }

    [Fact]
    public async Task NoComponentReadDesiredProperties()
    {
      TwinProperties desiredProps = new TwinProperties();
      desiredProps.Desired = new TwinCollection(@"{prop1: { value: 'val1'}}");
      Twin desired = new Twin(desiredProps);
      mockClient.DesiredProperties = desired;
      var result = await nocomp.ReadDesiredPropertyAsync<string>("prop1");
      Assert.Equal("val1", result);
    }
  }
}
