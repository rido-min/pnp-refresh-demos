using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PnPConvention
{
  public static class TwinCollectionExtensions
  {
    public static JObject GetOrCreateComponent(this TwinCollection collection, string componentName)
    {
      if (!collection.Contains(componentName))
      {
        JToken flag = JToken.Parse("{\"__t\" : \"c\"}");
        collection[componentName] = flag;
      }
      JObject componentJson = collection[componentName] as JObject;
      return componentJson;
    }

    public static void AddComponentProperty(this TwinCollection collection, string componentName, string propertyName, object propertyValue)
    {
      JObject componentJson = collection.GetOrCreateComponent(componentName);

      if (!componentJson.ContainsKey(propertyName))
      {
        componentJson[propertyName] = JToken.FromObject(new { value = propertyValue });
      }
    }

    public static T GetPropertyValue<T>(this TwinCollection collection, string componentName, string propertyName)
    {
      T result = default(T);
      if (collection.Contains(componentName))
      {
        var componentJson = collection[componentName] as JObject;
        if (componentJson.ContainsKey(propertyName))
        {
          var propertyJson = componentJson[propertyName] as JObject;
          if (propertyJson != null)
          {
            if (propertyJson.ContainsKey("value"))
            {
              var propertyValue = propertyJson["value"];
              result = propertyValue.Value<T>();
            }
          }
        }
      }
      return result;
    }
  }
}
