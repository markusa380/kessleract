using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kessleract {

    [Serializable]
    public class VesselSpec {
        public OrbitSpec orbitSpec;
        public PartSpec[] partSpecs;

        public ProtoVessel ToProtoVessel(CelestialBody body, int hash) {
            var flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

            var parts = new List<ConfigNode>();
            for (int i = 0; i < partSpecs.Length; i++) {
                partSpecs[i].ToProtoParts(0, flightId, parts);
            }

            var orbit = orbitSpec.ToOrbit(body);

            ConfigNode vesselNode = ProtoVessel.CreateVesselNode(
              Naming.ABANDONED_VESSEL_PREFIX + hash.ToString(),
              VesselType.Unknown,
              orbit,
              0,
              parts.ToArray()
            );

            return new ProtoVessel(vesselNode, HighLogic.CurrentGame);
        }

        public static VesselSpec From(ProtoVessel protoVessel) {

            var allParts = protoVessel.protoPartSnapshots;
            var parentToChildren = new Dictionary<int, List<int>>();

            for (int i = 0; i < allParts.Count; i++) {
                var part = allParts[i];
                if (!parentToChildren.ContainsKey(part.parentIdx)) {
                    parentToChildren[part.parentIdx] = new List<int>();
                }
                parentToChildren[part.parentIdx].Add(i + 1);
            }

            // TODO: Delete me
            foreach (var kvp in parentToChildren) {
                Debug.Log($"Parent Index: {kvp.Key}, Children: [{string.Join(", ", kvp.Value)}]");
            }

            return new VesselSpec {
                orbitSpec = OrbitSpec.FromSnapshot(protoVessel.orbitSnapShot),
                partSpecs = PartSpecChildrenOfPartIndex(0, allParts, parentToChildren)
            };
        }

        private static PartSpec[] PartSpecChildrenOfPartIndex(
          int index,
          List<ProtoPartSnapshot> allParts,
          Dictionary<int, List<int>> parentToChildren
        ) {
            if (!parentToChildren.ContainsKey(index)) {
                return new PartSpec[0];
            }

            var children = parentToChildren[index];
            var partSpecs = new PartSpec[children.Count];

            for (int i = 0; i < children.Count; i++) {
                var childIndex = children[i];
                var child = allParts[childIndex - 1];
                var grandchildren = PartSpecChildrenOfPartIndex(childIndex, allParts, parentToChildren);
                partSpecs[i] = new PartSpec {
                    name = child.partName,
                    position = child.position,
                    rotation = child.rotation,
                    children = grandchildren
                };
            }

            return partSpecs;
        }

        public static VesselSpec FromJSON(JsonNode jsonNode) {
            var obj = jsonNode.AsObject();
            var orbitSpec = OrbitSpec.FromJSON(obj.dict["orbit"]);
            var partSpecsJson = obj.dict["parts"].AsArray();
            var partSpecs = new PartSpec[partSpecsJson.nodes.Length];
            for (int i = 0; i < partSpecsJson.nodes.Length; i++) {
                partSpecs[i] = PartSpec.FromJSON(partSpecsJson.nodes[i]);
            }

            return new VesselSpec {
                orbitSpec = orbitSpec,
                partSpecs = partSpecs
            };
        }

        public JsonObject ToJSON() {
            var partSpecs = new JsonArray {
                nodes = new JsonNode[this.partSpecs.Length]
            };

            for (int i = 0; i < this.partSpecs.Length; i++) {
                partSpecs.nodes[i] = this.partSpecs[i].ToJSON();
            }

            return new JsonObject {
                dict = new Dictionary<string, JsonNode> {
                    { "orbit", orbitSpec.ToJSON() },
                    { "parts", partSpecs }
                }
            };
        }
    }

    [Serializable]
    public class OrbitSpec {
        public double semiMajorAxis;
        public double eccentricity;
        public double inclination;
        public double argumentOfPeriapsis;
        public double longitudeOfAscendingNode;
        public double meanAnomalyAtEpoch;
        public double epoch;

        public static OrbitSpec FromSnapshot(OrbitSnapshot orbit) {
            return new OrbitSpec {
                semiMajorAxis = orbit.semiMajorAxis,
                eccentricity = orbit.eccentricity,
                inclination = orbit.inclination,
                argumentOfPeriapsis = orbit.argOfPeriapsis,
                longitudeOfAscendingNode = orbit.LAN,
                meanAnomalyAtEpoch = orbit.meanAnomalyAtEpoch,
                epoch = orbit.epoch
            };
        }

        public Orbit ToOrbit(
          CelestialBody body
        ) {
            return new Orbit(
              inclination,
              eccentricity,
              semiMajorAxis,
              longitudeOfAscendingNode,
              argumentOfPeriapsis,
              meanAnomalyAtEpoch,
              epoch,
              body
            );
        }

        public static OrbitSpec FromJSON(JsonNode jsonNode) {
            var obj = jsonNode.AsObject();
            return new OrbitSpec {
                semiMajorAxis = obj.dict["semiMajorAxis"].AsNumber().value,
                eccentricity = obj.dict["eccentricity"].AsNumber().value,
                inclination = obj.dict["inclination"].AsNumber().value,
                argumentOfPeriapsis = obj.dict["argumentOfPeriapsis"].AsNumber().value,
                longitudeOfAscendingNode = obj.dict["longitudeOfAscendingNode"].AsNumber().value,
                meanAnomalyAtEpoch = obj.dict["meanAnomalyAtEpoch"].AsNumber().value,
                epoch = obj.dict["epoch"].AsNumber().value
            };
        }

        public JsonObject ToJSON() {
            return new JsonObject {
                dict = new Dictionary<string, JsonNode> {
                    { "semiMajorAxis", new JsonNumber(semiMajorAxis) },
                    { "eccentricity", new JsonNumber(eccentricity) },
                    { "inclination", new JsonNumber(inclination) },
                    { "argumentOfPeriapsis", new JsonNumber(argumentOfPeriapsis) },
                    { "longitudeOfAscendingNode", new JsonNumber(longitudeOfAscendingNode) },
                    { "meanAnomalyAtEpoch", new JsonNumber(meanAnomalyAtEpoch) },
                    { "epoch", new JsonNumber(epoch) }
                }
            };
        }
    }

    [Serializable]
    public class PartSpec {
        public string name;
        public Vector3 position;

        public Quaternion rotation;

        // Unfortunate hack to ensure that we can serialize and deserialize
        // the children of a part. This is necessary because Unity's JSON
        // serialization does not support recursion.
        public PartSpec[] children;

        public void ToProtoParts(
          int parent,
          uint flightId,
          List<ConfigNode> resultParts
        ) {
            var partNode = ProtoVessel.CreatePartNode(name, flightId, null);
            partNode.SetValue("parent", parent);
            partNode.SetValue("position", KSPUtil.WriteVector(position));
            partNode.SetValue("rotation", KSPUtil.WriteQuaternion(rotation));

            resultParts.Add(partNode);
            var newParent = resultParts.Count - 1;

            for (int i = 0; i < children.Length; i++) {
                var parsedChild = children[i];
                parsedChild.ToProtoParts(newParent, flightId, resultParts);
            }
        }

        public static PartSpec FromJSON(JsonNode jsonNode) {
            var obj = jsonNode.AsObject();
            var positionJson = obj.dict["position"].AsObject();
            var rotationJson = obj.dict["rotation"].AsObject();
            var childrenJson = obj.dict["children"].AsArray();
            var children = new PartSpec[childrenJson.nodes.Length];
            for (int i = 0; i < childrenJson.nodes.Length; i++) {
                children[i] = FromJSON(childrenJson.nodes[i]);
            }

            return new PartSpec {
                name = obj.dict["name"].AsString().value,
                position = new Vector3(
                  (float)positionJson.dict["x"].AsNumber().value,
                  (float)positionJson.dict["y"].AsNumber().value,
                  (float)positionJson.dict["z"].AsNumber().value
                ),
                rotation = new Quaternion(
                  (float)rotationJson.dict["x"].AsNumber().value,
                  (float)rotationJson.dict["y"].AsNumber().value,
                  (float)rotationJson.dict["z"].AsNumber().value,
                  (float)rotationJson.dict["w"].AsNumber().value
                ),
                children = children
            };
        }

        public JsonObject ToJSON() {
            var children = new JsonArray {
                nodes = new JsonNode[this.children.Length]
            };

            for (int i = 0; i < this.children.Length; i++) {
                children.nodes[i] = this.children[i].ToJSON();
            }

            return new JsonObject {
                dict = new Dictionary<string, JsonNode> {
                    { "name", new JsonString(name) },
                    { "position", new JsonObject {
                        dict = new Dictionary<string, JsonNode> {
                            { "x", new JsonNumber(position.x) },
                            { "y", new JsonNumber(position.y) },
                            { "z", new JsonNumber(position.z) }
                        }
                    }},
                    { "rotation", new JsonObject {
                        dict = new Dictionary<string, JsonNode> {
                            { "x", new JsonNumber(rotation.x) },
                            { "y", new JsonNumber(rotation.y) },
                            { "z", new JsonNumber(rotation.z) },
                            { "w", new JsonNumber(rotation.w) }
                        }
                    }},
                    { "children", new JsonArray { nodes = children.nodes } }
                }
            };
        }
    }

}