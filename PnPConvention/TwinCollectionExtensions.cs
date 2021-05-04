using Microsoft.Azure.Devices.Shared;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PnPConvention
{
  public static class TwinCollectionExtensions
  {
    public static List<string> EnumerateComponents(this TwinCollection collection)
    {
      var jcollection = JObject.Parse(collection.ToJson());
      var result = new List<string>();
      foreach (var item in jcollection)
      {
        if (!item.Key.StartsWith("$"))
        {
          if (item.Value.Type == JTokenType.Object)
          {
            var el = item.Value as JObject;
            if (CheckComponentFlag(el, item.Key))
            {
              result.Add(item.Key);
            }
          }
        }
      }
      return result;
    }

    public static JObject GetOrCreateComponent(this TwinCollection collection, string componentName)
    {
      if (collection.Contains(componentName))
      {
        var component = collection[componentName] as JObject;
        //if (!CheckComponentFlag(component, componentName))
        //{
        //  return null;
        //}  
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
        componentJson[propertyName] = JToken.FromObject(propertyValue);
      }
    }

    public static T GetPropertyValue<T>(this TwinCollection collection, string componentName, string propertyName)
    {
      T result = default(T);
      if (collection.Contains(componentName))
      {
        var componentJson = collection[componentName] as JObject;
        if (!CheckComponentFlag(componentJson, componentName))
        {
          throw new Exception($"The twin {componentName} does nor includes the PnP convention marker");
        }
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
          else
          {
            var propValue = componentJson[propertyName] as JToken;
            result = propValue.Value<T>();
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

    private static bool CheckComponentFlag(JObject component, string componentName)
    {

      if (!component.ContainsKey("__t"))
      {
        // throw new Exception($"Component {componentName} does not have the expected '__t' flag");
        return false;
      }
      else
      {
        var flag = component["__t"];
        if (flag.Value<string>() != "c")
        {
          throw new Exception($"Component {componentName} does not have the expected '__t' value");
          return false;
        }
      }
      return true;
    }
  }
}
