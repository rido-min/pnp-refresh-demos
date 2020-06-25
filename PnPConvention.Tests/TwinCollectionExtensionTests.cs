using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace PnPConvention.Tests
{
  public class TwinCollectionExtensionTests
  {
    [Fact]
    public void GetPropertyFromComponentAsDouble()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: {
            value : 1.23
          }
        }
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(1.23, propVal);
    }

  

    [Fact]
    public void GetPropertyFromComponentWithoutValue()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: 1.23
        }
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(1.23, propVal);
    }


    [Fact]
    public void GetPropertyFromComponentWithIncorrectType()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: {
            value : 1.8
          }
        }
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<string>("tempSensor1", "targetTemperature");
      Assert.Equal("1.8", propVal);
    }


    [Fact]
    public void GetRootPropertyWithValue()
    {
      string json = @"
      {
         targetTemperature: {
            value : 1.23
          }        
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("targetTemperature");
      Assert.Equal(1.23, propVal);
    }

    [Fact]
    public void GetRootPropertyWithNoValue()
    {
      string json = @"
      {
         targetTemperature: 1.23
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("targetTemperature");
      Assert.Equal(1.23, propVal);
    }


    // [Fact]
    public void GetPropertyWithoutFlagReturnsDefaultValue()
    {
      string json = @"
      {
        tempSensor1: {
          targetTemperature: {
            value : 1.23
          }
        }
      }";
      TwinCollection twinCollection = new TwinCollection(json);
      
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(default, propVal);
    }

    // [Fact]
    public void GetPropertyWithInvalidFlagDoesRaiseException()
    {
      string json = @"
      {
        tempSensor1: {
          __t : 'invalid',
          targetTemperature: {
            value : 1.23
          }
        }
      }";
      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(default, propVal);
     
    }

    [Fact]
    public void GetPropertyReturnsDefaultIfPropertyValueIsNotFound()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: {}
        }
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "targetTemperature");
      Assert.Equal(0, propVal);
    }

    [Fact]
    public void GetPropertyReturnsDefaultIfPropertyNotFound()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: {
            value : 1.23
          }
        }
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("tempSensor1", "NotFound");
      Assert.Equal(0, propVal);
    }

    [Fact]
    public void GetPropertyReturnsDefaultIfCompoNotFound()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: {}
        }
      }";

      TwinCollection twinCollection = new TwinCollection(json);
      var propVal = twinCollection.GetPropertyValue<double>("notfound", "NotFound");
      Assert.Equal(0, propVal);
    }

    [Fact]
    public void AddPropertyToAnExistingComponent()
    {
      string json = @"
      {
        tempSensor1: {
          __t: 'c',
          targetTemperature: {}
        }
      }";
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
    public void AddComponentPropAddsTheFlagIfNeeded()
    {
      TwinCollection collection = new TwinCollection();
      collection.AddComponentProperty("myComp", "myProp", 12.3);
      Assert.True(collection.Contains("myComp"));
      var comp = collection["myComp"] as JObject;
      Assert.True(comp.ContainsKey("__t"));
      Assert.True(comp.ContainsKey("myProp"));
      var prop = comp["myProp"];
      Assert.NotNull(prop);
      Assert.Equal(12.3, prop);
      //var propValue = prop["value"];
      //Assert.Equal(12.3, propValue.Value<double>());
    }
  }
}
