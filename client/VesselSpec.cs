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
                var child = allParts[childIndex];
                partSpecs[i] = new PartSpec {
                    partName = child.partName,
                    position = child.position,
                    rotation = child.rotation,
                    children = PartSpecChildrenOfPartIndex(childIndex, allParts, parentToChildren)
                };
            }

            return partSpecs;
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
    }

    [Serializable]
    public class PartSpec {
        public string partName;
        public Vector3 position;

        public Quaternion rotation;

        public PartSpec[] children;

        public void ToProtoParts(
          int parent,
          uint flightId,
          List<ConfigNode> resultParts
        ) {
            var partNode = ProtoVessel.CreatePartNode(partName, flightId, null);
            partNode.SetValue("parent", parent);
            partNode.SetValue("position", KSPUtil.WriteVector(position));
            partNode.SetValue("rotation", KSPUtil.WriteQuaternion(rotation));

            resultParts.Add(partNode);
            var newParent = resultParts.Count - 1;

            for (int i = 0; i < children.Length; i++) {
                children[i].ToProtoParts(newParent, flightId, resultParts);
            }
        }
    }

}