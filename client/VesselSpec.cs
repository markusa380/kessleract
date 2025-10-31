using UnityEngine;

namespace Kessleract {
    public class FromProtobuf {
        public static Quaternion From(
            Pb.Quaternion protoQuat
        ) {
            return new Quaternion(
                protoQuat.X,
                protoQuat.Y,
                protoQuat.Z,
                protoQuat.W
            );
        }

        public static Vector3 From(
            Pb.Vector3 protoVec
        ) {
            return new Vector3(
                protoVec.X,
                protoVec.Y,
                protoVec.Z
            );
        }

        public static ConfigNode From(
            Pb.PartSpec partSpec,
            uint flightId
        ) {
            var partNode = ProtoVessel.CreatePartNode(partSpec.Name, flightId, null);
            partNode.SetValue("parent", partSpec.ParentIndex);
            partNode.SetValue("position", KSPUtil.WriteVector(From(partSpec.Position)));
            partNode.SetValue("rotation", KSPUtil.WriteQuaternion(From(partSpec.Rotation)));
            // Attach nodes doesn't seem to get picked up from here, so we set them after creating the ProtoVessel
            return partNode;
        }

        public static Orbit From(
            Pb.OrbitSpec orbitSpec,
            CelestialBody body
        ) {
            return new Orbit(
              orbitSpec.Inclination,
              orbitSpec.Eccentricity,
              orbitSpec.SemiMajorAxis,
              orbitSpec.LongitudeOfAscendingNode,
              orbitSpec.ArgumentOfPeriapsis,
              orbitSpec.MeanAnomalyAtEpoch,
              orbitSpec.Epoch,
              body
            );
        }

        public static ProtoVessel From(Pb.VesselSpec vesselSpec, CelestialBody body, int hash) {
            var flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

            var protoParts = new ConfigNode[vesselSpec.PartSpecs.Count];
            for (int i = 0; i < vesselSpec.PartSpecs.Count; i++) {
                var partSpec = vesselSpec.PartSpecs[i];
                protoParts[i] = From(partSpec, flightId);
            }

            var orbit = From(vesselSpec.OrbitSpec, body);

            ConfigNode vesselNode = ProtoVessel.CreateVesselNode(
              Naming.ABANDONED_VESSEL_PREFIX + hash.ToString(),
              VesselType.Unknown,
              orbit,
              0,
              protoParts
            );

            var vessel = new ProtoVessel(vesselNode, HighLogic.CurrentGame);

            for (int i = 0; i < vessel.protoPartSnapshots.Count; i++) {
                var partSnapshot = vessel.protoPartSnapshots[i];
                var partSpec = vesselSpec.PartSpecs[i];
                partSnapshot.attachNodes.Clear();
                foreach (var attachment in partSpec.Attachments) {
                    partSnapshot.attachNodes.Add(new AttachNodeSnapshot(attachment));
                }
                if (partSpec.HasSurfaceAttachment) {
                    partSnapshot.srfAttachNode = new AttachNodeSnapshot(partSpec.SurfaceAttachment);
                }
            }

            return vessel;
        }

    }

    public class ToProtobuf {

        public static Pb.Quaternion To(
            Quaternion quat
        ) {
            var protoQuat = new Pb.Quaternion {
                X = quat.x,
                Y = quat.y,
                Z = quat.z,
                W = quat.w
            };
            return protoQuat;
        }

        public static Pb.Vector3 To(
            Vector3 vec
        ) {
            var protoVec = new Pb.Vector3 {
                X = vec.x,
                Y = vec.y,
                Z = vec.z
            };
            return protoVec;
        }

        public static Pb.PartSpec To(ProtoPartSnapshot snapshot) {
            var attachments = new string[snapshot.attachNodes.Count];
            for (int i = 0; i < snapshot.attachNodes.Count; i++) {
                attachments[i] = snapshot.attachNodes[i].Save();
            }
            var partSpec = new Pb.PartSpec {
                Name = snapshot.partName,
                Position = To(snapshot.position),
                Rotation = To(snapshot.rotation),
                ParentIndex = snapshot.parentIdx,
                SurfaceAttachment = snapshot.srfAttachNode.Save()
            };

            partSpec.Attachments.AddRange(attachments);

            return partSpec;
        }

        public static Pb.OrbitSpec To(OrbitSnapshot orbit) {
            return new Pb.OrbitSpec {
                SemiMajorAxis = orbit.semiMajorAxis,
                Eccentricity = orbit.eccentricity,
                Inclination = orbit.inclination,
                ArgumentOfPeriapsis = orbit.argOfPeriapsis,
                LongitudeOfAscendingNode = orbit.LAN,
                MeanAnomalyAtEpoch = orbit.meanAnomalyAtEpoch,
                Epoch = orbit.epoch
            };
        }

        public static Pb.VesselSpec To(ProtoVessel protoVessel) {
            var allParts = protoVessel.protoPartSnapshots;
            var parts = new Pb.PartSpec[allParts.Count];
            for (int i = 0; i < allParts.Count; i++) {
                var part = allParts[i];
                parts[i] = To(part);
            }

            var vesselSpec = new Pb.VesselSpec {
                OrbitSpec = To(protoVessel.orbitSnapShot)
            };

            vesselSpec.PartSpecs.AddRange(parts);

            return vesselSpec;
        }
    }
}