using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Thermostat.PnPConvention
{
    public class PnPPropertyCollection 
    {
        public TwinCollection Instance;
        string componentName;
        public PnPPropertyCollection(string name)
        {
            this.componentName = name;
            Instance = new TwinCollection();
        }

        public PnPPropertyCollection(string name, string twinJson) 
        {
            this.componentName = name;
            Instance = new TwinCollection(twinJson);
        }

        public string Get(string propertyName)
        {
            string result = string.Empty;
            if (Instance.Contains(this.componentName))
            {
                var compJson = Instance[this.componentName];
                if (compJson != null)
                {
                    WarnIfDoesNotHaveTheFlag(compJson);
                    
                    if (compJson.ContainsKey(propertyName))
                    {
                        var prop = compJson[propertyName];
                        if (prop["value"] != null)
                        {
                            var propVal = prop["value"];
                            result = Convert.ToString(propVal); //TODO: review if we should return string
                        }
                    }
                }
            }
            return result;
        }

        private void WarnIfDoesNotHaveTheFlag(JObject compJson)
        {
            if (compJson.ContainsKey("__t"))
            {
                var flagValue = compJson.Value<string>("__t");
                if (flagValue!="c")
                {
                    Console.WriteLine("!!!!! Invalid flag value !!!!!!!!!!!!!" + compJson.ToString());
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine("!!!!! Component without flag !!!!!!!!!!!!!" + compJson.ToString());
                Console.ReadLine();
            }
        }

        public void Set(string propertyName, object value)
        {

            var property = JToken.FromObject(new { value });

            //TODO: Review how to update with PATCH syntax
            if (Instance.Contains(this.componentName))
            {
                Instance[this.componentName][propertyName] = property;
            }
            else
            {
                TwinCollection root = new TwinCollection();
                root["__t"] = "c";
                root[propertyName] = property;
                Instance[this.componentName] = root;
            }
        }

       
    }
}
