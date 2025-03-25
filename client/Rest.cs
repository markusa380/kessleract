using System;
using System.Collections.Generic;

namespace Kessleract {

    [Serializable]
    public class UploadRequest {
        public int body;
        public VesselSpec vessel;

        public JsonNode ToJSON() {
            return new JsonObject {
                dict = new Dictionary<string, JsonNode> {
                { "body", new JsonNumber(body) },
                { "vessel", vessel.ToJSON() }
              }
            };
        }
    }

    [Serializable]
    public class DownloadRequest {
        public int body;
        public int take;
        public int[] excludedHashes;

        public JsonNode ToJSON() {
            var excludedHashesJson = new JsonArray { nodes = new JsonNode[excludedHashes.Length] };

            for (int i = 0; i < excludedHashes.Length; i++) {
                excludedHashesJson.nodes[i] = new JsonNumber(excludedHashes[i]);
            }

            return new JsonObject {
                dict = new Dictionary<string, JsonNode> {
                { "body", new JsonNumber(body) },
                { "take", new JsonNumber(take) },
                { "excludedHashes", excludedHashesJson }
              }
            };
        }
    }

    [Serializable]
    public class DownloadResponse {
        public UniqueVessel[] vessels;

        public static DownloadResponse FromJSON(JsonNode json) {
            var dict = json.AsObject().dict;
            var vesselsJson = dict["vessels"].AsArray().nodes;
            var vessels = new UniqueVessel[vesselsJson.Length];

            for (int i = 0; i < vesselsJson.Length; i++) {
                vessels[i] = UniqueVessel.FromJSON(vesselsJson[i]);
            }

            return new DownloadResponse {
                vessels = vessels
            };
        }
    }

    [Serializable]
    public class UniqueVessel {
        public int hash;
        public VesselSpec vessel;

        public static UniqueVessel FromJSON(JsonNode json) {
            var dict = json.AsObject().dict;
            return new UniqueVessel {
                hash = (int)dict["hash"].AsNumber().value,
                vessel = VesselSpec.FromJSON(dict["vessel"])
            };
        }
    }
}