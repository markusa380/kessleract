using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Kessleract {

    public class VesselSpec {
        public OrbitSpec orbitSpec;
        public PartSpec[] parts;

        public ProtoVessel ToProtoVessel(CelestialBody body, int hash) {
            var flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

            var protoParts = new ConfigNode[parts.Length];
            for (int i = 0; i < parts.Length; i++) {
                var partSpec = parts[i];
                protoParts[i] = partSpec.ToProto(flightId);
            }

            var orbit = orbitSpec.ToOrbit(body);

            ConfigNode vesselNode = ProtoVessel.CreateVesselNode(
              Naming.ABANDONED_VESSEL_PREFIX + hash.ToString(),
              VesselType.Unknown,
              orbit,
              0,
              protoParts
            );

            var vessel = new ProtoVessel(vesselNode, HighLogic.CurrentGame);

            for (int i = 0; i < protoParts.Length; i++) {
                var part = protoParts[i];
                var partSnapshot = vessel.protoPartSnapshots[i];
                partSnapshot.attachNodes.Clear();
                foreach (var attachment in parts[i].attachments) {
                    partSnapshot.attachNodes.Add(new AttachNodeSnapshot(attachment));
                }
            }

            return vessel;
        }

        public static VesselSpec From(ProtoVessel protoVessel) {

            var allParts = protoVessel.protoPartSnapshots;
            var parts = new PartSpec[allParts.Count];
            for (int i = 0; i < allParts.Count; i++) {
                var part = allParts[i];
                parts[i] = PartSpec.FromSnapshot(part);
            }
            

            return new VesselSpec {
                orbitSpec = OrbitSpec.FromSnapshot(protoVessel.orbitSnapShot),
                parts = parts
            };
        }

        public static VesselSpec FromJSON(JsonNode jsonNode) {
            var obj = jsonNode.AsObject();
            var orbitSpec = OrbitSpec.FromJSON(obj.dict["orbit"]);
            var partsSpecs = obj.dict["parts"].AsArray();
            var parts = new PartSpec[partsSpecs.nodes.Length];
            for (int i = 0; i < partsSpecs.nodes.Length; i++) {
                parts[i] = PartSpec.FromJSON(partsSpecs.nodes[i]);
            }
            
            return new VesselSpec {
                orbitSpec = orbitSpec,
                parts = parts
            };
        }

        public JsonObject ToJSON() {
            var parts = new JsonArray {
                nodes = new JsonNode[this.parts.Length]
            };

            for (int i = 0; i < this.parts.Length; i++) {
                parts.nodes[i] = this.parts[i].ToJSON();
            }

            return new JsonObject {
                dict = new Dictionary<string, JsonNode> {
                    { "orbit", orbitSpec.ToJSON() },
                    { "parts", parts },
                }
            };
        }
    }

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

    public class PartSpec {
        public string name;
        public int parentIndex;
        public Vector3 position;

        public Quaternion rotation;
        // TODO: Make this class based
        public string[] attachments;

        public ConfigNode ToProto(
          uint flightId
        ) {
            var partNode = ProtoVessel.CreatePartNode(name, flightId, null);
            partNode.SetValue("parent", parentIndex);
            partNode.SetValue("position", KSPUtil.WriteVector(position));
            partNode.SetValue("rotation", KSPUtil.WriteQuaternion(rotation));
            // Attach nodes doesn't seem to get picked up from here, so we set them after creating the ProtoVessel
            return partNode;
        }

        public static PartSpec FromSnapshot(ProtoPartSnapshot snapshot) {
            var attachments = new string[snapshot.attachNodes.Count];
            for (int i = 0; i < snapshot.attachNodes.Count; i++) {
                attachments[i] = snapshot.attachNodes[i].Save();
            }
            return new PartSpec {
                name = snapshot.partName,
                position = snapshot.position,
                rotation = snapshot.rotation,
                parentIndex = snapshot.parentIdx,
                attachments = attachments,
            };
        }

        public static PartSpec FromJSON(JsonNode jsonNode) {
            var obj = jsonNode.AsObject();
            var positionJson = obj.dict["position"].AsObject();
            var rotationJson = obj.dict["rotation"].AsObject();
            var attachmentsJson = obj.dict["attachments"].AsArray();
            var attachments = new string[attachmentsJson.nodes.Length];
            for (int i = 0; i < attachmentsJson.nodes.Length; i++) {
                attachments[i] = attachmentsJson.nodes[i].AsString().value;
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
                attachments = attachments,
                parentIndex = (int)obj.dict["parentIndex"].AsNumber().value
            };
        }

        public JsonObject ToJSON() {
            var attachmentsJson = new JsonArray {
                nodes = new JsonNode[attachments.Length]
            };

            for (int i = 0; i < attachments.Length; i++) {
                attachmentsJson.nodes[i] = new JsonString(attachments[i]);
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
                    { "attachments", attachmentsJson },
                    { "parentIndex", new JsonNumber(parentIndex) }
                }
            };
        }
    }

}