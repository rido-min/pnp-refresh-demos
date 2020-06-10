using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System;

namespace Thermostat.PnPConvention
{
    public class PnPPropertyCollection : TwinCollection
    {
        string componentName;
        public PnPPropertyCollection(string name)
        {
            this.componentName = name;
        }

        public PnPPropertyCollection(string name, string twinJson) : base(twinJson)
        {
            this.componentName = name;
        }

        public string Get(string propertyName)
        {
            string result = string.Empty;
            if (base.Contains(this.componentName))
            {
                var prop = base[this.componentName][propertyName];
                if (prop!=null && prop["value"]!=null)
                {
                    var propVal = prop["value"];
                    result = Convert.ToString(propVal); //TODO: review if we should return string
                }
            }
            return result;
        }

        public void Set(string propertyName, object value)
        {
            
            var property = JToken.FromObject(new { value });

            //TODO: Review how to update with PATCH syntax
            if (base.Contains(this.componentName))
            {
                base[this.componentName][propertyName] = property;
            }
            else
            {
                TwinCollection root = new TwinCollection();
                root["__t"] = "c";
                root[propertyName] = property;
                base[this.componentName] = root;
            }
        }

        public void Set(string propertyName, object value, StatusCodes statusCode, long statusVersion, string statusDescription)
        {
            
            var property = new TwinCollection();
            property["value"] = value;
            property["sc"] = statusCode;
            property["sv"] = statusVersion;
            property["sd"] = statusDescription;

            if (base.Contains(this.componentName))
            {
                JToken token = JToken.FromObject(property);
                base[this.componentName][propertyName] = token;
            }
            else
            {
                TwinCollection root = new TwinCollection();
                root[propertyName] = property;
                base[this.componentName] = root;
            }
        }
    }
}
