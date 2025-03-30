using System;
using System.Collections.Generic;

namespace Kessleract {

    public class Json {
        public static JsonNode FromJson(string json) {
            json = json.Trim();

            if (json.StartsWith("{") && json.EndsWith("}")) {
                var content = json.Substring(1, json.Length - 2).Trim();
                var keyValuePairs = SplitJsonObject(content);
                var keys = new List<string>();
                var values = new List<JsonNode>();

                foreach (var pair in keyValuePairs) {
                    var keyValue = pair.Split(new[] { ':' }, 2);
                    var key = UnescapeString(keyValue[0].Trim().Trim('"'));
                    var value = keyValue[1].Trim();
                    keys.Add(key);
                    values.Add(FromJson(value));
                }

                var obj = new JsonObject();

                for (int i = 0; i < keys.Count; i++) {
                    obj.dict[keys[i]] = values[i];
                }

                return obj;
            }
            else if (json.StartsWith("[") && json.EndsWith("]")) {
                var content = json.Substring(1, json.Length - 2).Trim();
                var elements = SplitJsonArray(content);
                var nodes = new List<JsonNode>();

                foreach (var element in elements) {
                    nodes.Add(FromJson(element.Trim()));
                }

                return new JsonArray { nodes = nodes.ToArray() };
            }
            else if (json.StartsWith("\"") && json.EndsWith("\"")) {
                return new JsonString(UnescapeString(json.Substring(1, json.Length - 2)));
            }
            else if (json == "true" || json == "false") {
                return new JsonBoolean(json == "true");
            }
            else if (json == "null") {
                return new JsonNull();
            }
            else {
                if (double.TryParse(json, out var number)) {
                    return new JsonNumber(number);
                }
                throw new FormatException("Invalid JSON format.");
            }
        }

        private static List<string> SplitJsonObject(string content) {
            var result = new List<string>();
            int braceCount = 0, bracketCount = 0;
            bool inString = false;
            var current = new System.Text.StringBuilder();

            foreach (var c in content) {
                if (c == '"' && (current.Length == 0 || current[current.Length - 1] != '\\')) {
                    inString = !inString;
                }

                if (!inString) {
                    if (c == '{') braceCount++;
                    if (c == '}') braceCount--;
                    if (c == '[') bracketCount++;
                    if (c == ']') bracketCount--;
                }

                if (c == ',' && braceCount == 0 && bracketCount == 0 && !inString) {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else {
                    current.Append(c);
                }
            }

            if (current.Length > 0) {
                result.Add(current.ToString().Trim());
            }

            return result;
        }

        private static List<string> SplitJsonArray(string content) {
            return SplitJsonObject(content); // Same logic applies for arrays
        }

        private static string UnescapeString(string str) {
            return str.Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\/", "/")
                .Replace("\\b", "\b")
                .Replace("\\f", "\f")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
    }

    public interface JsonNode {
        string Stringify();

        JsonArray AsArray();
        JsonObject AsObject();
        JsonString AsString();
        JsonNumber AsNumber();
        JsonBoolean AsBoolean();
        JsonNull AsNull();
    }

    public class JsonArray : JsonNode {
        public JsonNode[] nodes;

        public string Stringify() {
            string[] jsons = new string[nodes.Length];
            for (int i = 0; i < nodes.Length; i++) {
                jsons[i] = nodes[i].Stringify();
            }
            return "[" + string.Join(",", jsons) + "]";
        }

        public JsonArray AsArray() {
            return this;
        }

        public JsonObject AsObject() {
            throw new InvalidCastException("Cannot cast JsonArray to JsonObject.");
        }

        public JsonString AsString() {
            throw new InvalidCastException("Cannot cast JsonArray to JsonString.");
        }

        public JsonNumber AsNumber() {
            throw new InvalidCastException("Cannot cast JsonArray to JsonNumber.");
        }

        public JsonBoolean AsBoolean() {
            throw new InvalidCastException("Cannot cast JsonArray to JsonBoolean.");
        }

        public JsonNull AsNull() {
            throw new InvalidCastException("Cannot cast JsonArray to JsonNull.");
        }

    }

    public class JsonObject : JsonNode {
        public Dictionary<string, JsonNode> dict = new Dictionary<string, JsonNode>();

        public string Stringify() {
            var pairs = new string[dict.Count];
            int i = 0;
            foreach (var kvp in dict) {
                pairs[i++] = "\"" + kvp.Key + "\":" + kvp.Value.Stringify();
            }
            return "{" + string.Join(",", pairs) + "}";
        }

        public JsonArray AsArray() {
            throw new InvalidCastException("Cannot cast JsonObject to JsonArray.");
        }

        public JsonObject AsObject() {
            return this;
        }

        public JsonString AsString() {
            throw new InvalidCastException("Cannot cast JsonObject to JsonString.");
        }

        public JsonNumber AsNumber() {
            throw new InvalidCastException("Cannot cast JsonObject to JsonNumber.");
        }

        public JsonBoolean AsBoolean() {
            throw new InvalidCastException("Cannot cast JsonObject to JsonBoolean.");
        }

        public JsonNull AsNull() {
            throw new InvalidCastException("Cannot cast JsonObject to JsonNull.");
        }
    }

    public class JsonString : JsonNode {
        public string value;

        public JsonString(string value) {
            this.value = value;
        }

        public string Stringify() {
            return "\"" + value + "\"";
        }

        public JsonArray AsArray() {
            throw new InvalidCastException("Cannot cast JsonString to JsonArray.");
        }

        public JsonObject AsObject() {
            throw new InvalidCastException("Cannot cast JsonString to JsonObject.");
        }

        public JsonString AsString() {
            return this;
        }

        public JsonNumber AsNumber() {
            throw new InvalidCastException("Cannot cast JsonString to JsonNumber.");
        }

        public JsonBoolean AsBoolean() {
            throw new InvalidCastException("Cannot cast JsonString to JsonBoolean.");
        }

        public JsonNull AsNull() {
            throw new InvalidCastException("Cannot cast JsonString to JsonNull.");
        }
    }

    public class JsonNumber : JsonNode {
        public double value;

        public JsonNumber(double value) {
            this.value = value;
        }

        public string Stringify() {
            return value.ToString();
        }

        public JsonArray AsArray() {
            throw new InvalidCastException("Cannot cast JsonNumber to JsonArray.");
        }

        public JsonObject AsObject() {
            throw new InvalidCastException("Cannot cast JsonNumber to JsonObject.");
        }

        public JsonString AsString() {
            throw new InvalidCastException("Cannot cast JsonNumber to JsonString.");
        }

        public JsonNumber AsNumber() {
            return this;
        }

        public JsonBoolean AsBoolean() {
            throw new InvalidCastException("Cannot cast JsonNumber to JsonBoolean.");
        }

        public JsonNull AsNull() {
            throw new InvalidCastException("Cannot cast JsonNumber to JsonNull.");
        }
    }

    public class JsonBoolean : JsonNode {
        public bool value;

        public JsonBoolean(bool value) {
            this.value = value;
        }

        public string Stringify() {
            return value ? "true" : "false";
        }

        public JsonArray AsArray() {
            throw new InvalidCastException("Cannot cast JsonBoolean to JsonArray.");
        }

        public JsonObject AsObject() {
            throw new InvalidCastException("Cannot cast JsonBoolean to JsonObject.");
        }

        public JsonString AsString() {
            throw new InvalidCastException("Cannot cast JsonBoolean to JsonString.");
        }

        public JsonNumber AsNumber() {
            throw new InvalidCastException("Cannot cast JsonBoolean to JsonNumber.");
        }

        public JsonBoolean AsBoolean() {
            return this;
        }

        public JsonNull AsNull() {
            throw new InvalidCastException("Cannot cast JsonBoolean to JsonNull.");
        }
    }

    public class JsonNull : JsonNode {
        public string Stringify() {
            return "null";
        }

        public JsonArray AsArray() {
            throw new InvalidCastException("Cannot cast JsonNull to JsonArray.");
        }

        public JsonObject AsObject() {
            throw new InvalidCastException("Cannot cast JsonNull to JsonObject.");
        }

        public JsonString AsString() {
            throw new InvalidCastException("Cannot cast JsonNull to JsonString.");
        }

        public JsonNumber AsNumber() {
            throw new InvalidCastException("Cannot cast JsonNull to JsonNumber.");
        }

        public JsonBoolean AsBoolean() {
            throw new InvalidCastException("Cannot cast JsonNull to JsonBoolean.");
        }

        public JsonNull AsNull() {
            return this;
        }
    }
}