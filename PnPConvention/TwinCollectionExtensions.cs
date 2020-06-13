using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System;

namespace PnPConvention
{
  public static class TwinCollectionExtensions
  {
    public static JObject GetOrCreateComponent(this TwinCollection collection, string componentName)
    {
      if (collection.Contains(componentName))
      {
        var component = collection[componentName] as JObject;
        CheckComponentFlag(component, componentName);
      }
      else
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
        CheckComponentFlag(componentJson, componentName);
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

    public static T GetPropertyValue<T>(this TwinCollection collection, string propertyName)
    {
      T result = default(T);
      if (collection.Contains(propertyName))
      {
        var propertyJson = collection[propertyName] as JObject;
        if (propertyJson != null)
        {
          if (propertyJson.ContainsKey("value"))
          {
            var propertyValue = propertyJson["value"];
            result = propertyValue.Value<T>();
          }
        } 
        else
        {
          result = collection[propertyName].Value;
        }
      }
      return result;
    }

    private static void CheckComponentFlag(JObject component, string componentName)
    {
      if (!component.ContainsKey("__t"))
      {
        throw new Exception($"Component {componentName} does not have the expected '__t' flag");
      }
      else
      {
        var flag = component["__t"];
        if (flag.Value<string>() != "c")
        {
          throw new Exception($"Component {componentName} does not have the expected '__t' value");
        }
      }
    }
  }
}
