using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Xunit;

namespace PnPConvention.Tests
{
    public class PnPDeviceTwinTests
    {
        //[Fact]
        //public void GetPropertyFromGoodJson()
        //{
        //    string json = File.ReadAllText(@"..\..\..\desired-comp-prop-double.json");
        //    PnPDeviceTwin twin = new PnPDeviceTwin(json);
        //    var propVal = twin.GetPropertyValue<double>("tempSensor1", "targetTemperature");
        //    Assert.Equal(1.23, propVal);
        //}

        //[Fact]
        //public void GetPropertyWithoutFlag()
        //{
        //    string json = File.ReadAllText(@"..\..\..\desired-comp-prop-noflag.json");
        //    PnPDeviceTwin twin = new PnPDeviceTwin(json);
        //    var propVal = twin.GetPropertyValue<double>("tempSensor1", "targetTemperature");
        //    Assert.Equal(1.23, propVal);
        //}

        //[Fact]
        //public void AddComponent()
        //{
        //    string json = "{}";
        //    PnPDeviceTwin twin = new PnPDeviceTwin(json);
        //    twin.AddProperty("tempSensor1", "targetTemperature", JToken.FromObject(1.23));
        //                Assert.Equal("{\r\n  \"tempSensor1\": {\r\n    \"__t\": \"c\",\r\n    \"targetTemperature\": 1.23\r\n  }\r\n}", 
        //        twin.twin.ToString());
        //}

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
            Assert.Equal(12.3, prop.Value<double>());

        }

    }
}
