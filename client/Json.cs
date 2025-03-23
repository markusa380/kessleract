using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kessleract {
  public class Json {
    public static JsonObject ConfigNodeToJson(ConfigNode configNode) {
      var json = new JsonObject();

      for (int i = 0; i < configNode.values.Count; i++) {
        var value = configNode.values[i];
        if (json.ContainsKey(value.name)) {
          if (json[value.name] is JsonArray array) {
            array.Add(value.value);
          }
        }
        else {
          json.Add(value.name, new JsonArray { value.value });
        }
      }

      foreach (var node in configNode.GetNodes()) {
        if (json.ContainsKey(node.name)) {
          if (json[node.name] is JsonArray array) {
            array.Add(ConfigNodeToJson(node));
          }
        }
        else {
          json.Add(node.name, new JsonArray { ConfigNodeToJson(node) });
        }
      }

      return json;
    }

    public static ConfigNode JsonToConfigNode(JsonElement json, string name) {
      var configNode = new ConfigNode(name);

      foreach (var prop in json.EnumerateObject()) {
        if (prop.Value.ValueKind == JsonValueKind.Array) {
          foreach (var item in prop.Value.EnumerateArray()) {
            if (item.ValueKind == JsonValueKind.Object) {
              configNode.AddNode(JsonToConfigNode(item, prop.Name));
            }
            else if (item.ValueKind == JsonValueKind.String) {
              configNode.AddValue(prop.Name, item.GetString());
            }
            else {
              throw new Exception("Unsupported JSON value kind in array: " + item.ValueKind);
            }
          }
        }
        else {
          throw new Exception("Expected JSON array but found: " + prop.Value.ValueKind);
        }
      }

      return configNode;
    }
  }
}
