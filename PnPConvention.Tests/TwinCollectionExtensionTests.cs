using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Xunit;

namespace PnPConvention.Tests
{
  public class TwinCollectionExtensionTests
  {
    [Fact]
    public void GetPropertyFromComponentAsDouble()
    {
      string json = File.ReadAllText(@"..\..\..\desired-comp-prop-double.json");
      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(1.23, propVal);
    }

    [Fact]
    public void GetPropertyWithoutFlagDoesRaiseException()
    {
      string json = File.ReadAllText(@"..\..\..\desired-comp-prop-noflag.json");
      TwinCollection twinCollection = new TwinCollection(json);
      try
      {
        var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");

      }
      catch (Exception ex)
      {
        Assert.Equal("Component tempSensor1 does not have the expected '__t' flag", ex.Message);
      }
      
    }

    [Fact]
    public void GetPropertyReturnsDefaultIfPropertyValueIsNotFound()
    {
      string json = File.ReadAllText(@"..\..\..\desired-comp-prop-novalue.json");
      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(0, propVal);
    }

    [Fact]
    public void GetPropertyReturnsDefaultIfPropertyNotFound()
    {
      string json = File.ReadAllText(@"..\..\..\desired-comp-prop-novalue.json");
      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "NotFound");
      Assert.Equal(0, propVal);
    }

    [Fact]
    public void GetPropertyReturnsDefaultIfComponent()
    {
      string json = File.ReadAllText(@"..\..\..\desired-comp-prop-novalue.json");
      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("notfound", "NotFound");
      Assert.Equal(0, propVal);
    }

    [Fact]
    public void AddPropertyToAnExistingComponent()
    {
      string json = File.ReadAllText(@"..\..\..\desired-comp-prop-novalue.json");
      TwinCollection twinCollection = new TwinCollection(json);
      twinCollection.AddComponentProperty("tempSensor1", "newProperty", true);
      TwinCollection updatedCollection = new TwinCollection(twinCollection.ToJson());
      var result = twinCollection.GetPropertyValue<bool>("tempSensor1", "newProperty");
      Assert.True(result);
    }



    [Fact]
    public void InitComponent()
    {
      TwinCollection collection = new TwinCollection();
      collection.GetOrCreateComponent("myComp");
      Assert.True(collection.Contains("myComp"));
      var comp = collection["myComp"] as JObject;
      Assert.True(comp.ContainsKey("__t"));
    }

    [Fact]
    public void AddComponentProp()
    {
      TwinCollection collection = new TwinCollection();
      collection.AddComponentProperty("myComp", "myProp", 12.3);
      Assert.True(collection.Contains("myComp"));
      var comp = collection["myComp"] as JObject;
      Assert.True(comp.ContainsKey("__t"));
      Assert.True(comp.ContainsKey("myProp"));
      var prop = comp["myProp"];
      Assert.NotNull(prop);
      var propValue = prop["value"];
      Assert.Equal(12.3, propValue.Value<double>());
    }
  }
}
