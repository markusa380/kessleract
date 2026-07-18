using ModuleWheels;
using UnityEngine;
using System;
using System.Collections.Generic;

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

        public static bool From(Pb.VesselSpec vesselSpec, CelestialBody body, int hash, out ProtoVessel vessel) {
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

            vessel = new ProtoVessel(vesselNode, HighLogic.CurrentGame);

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

                var availablePart = PartLoader.getPartInfoByName(partSnapshot.partName);
                if (availablePart == null) {
                    Log.Error($"Part {partSnapshot.partName} not found in PartLoader, aborting");
                    return false;
                }

                PopulateModules(partSnapshot, availablePart, partSpec);

                SetVariant(partSnapshot, partSpec.ModuleVariantName, availablePart);
            }

            return true;
        }

        private static void SetVariant(ProtoPartSnapshot partSnapshot, string variantName, AvailablePart availablePart) {
            if (!string.IsNullOrEmpty(variantName)) {
                var modulePartVariants = partSnapshot.FindModule("ModulePartVariants");

                if (modulePartVariants == null) {
                    Log.Error($"Part {partSnapshot.partName} does not have ModulePartVariants, cannot set variant {variantName}");
                }
                else if (availablePart.GetVariant(variantName) == null) {
                    Log.Error($"Part {partSnapshot.partName} does not have variant {variantName}, skipping");
                }
                else {
                    modulePartVariants
                    .moduleValues
                    .AddValue("selectedVariant", variantName);
                }
            }
        }

        private static void PopulateModules(
                ProtoPartSnapshot partSnapshot,
                AvailablePart availablePart,
                Pb.PartSpec partSpec
            ) {
            foreach (var prefabModule in availablePart.partPrefab.Modules) {
                var config = new ConfigNode();

                // ModuleScienceLab will throw a NullReferenceException if we try to save it without initializing the ExperimentData list.
                // Nothing bad has happened by setting it to an empty list directly.
                if (prefabModule is ModuleScienceLab lab) {
                    lab.ExperimentData = new List<string>();
                }

                try {
                    prefabModule.Save(config);
                }
                catch (Exception e) {
                    Log.Error($"Failed to save module {prefabModule.moduleName} for part {partSnapshot.partName}: {e.Message}", e);
                    continue;
                }

                AdjustModuleConfig(config, partSpec, prefabModule);
                partSnapshot.modules.Add(new ProtoPartModuleSnapshot(config));
            }
        }

        private static void AdjustModuleConfig(ConfigNode config, Pb.PartSpec partSpec, PartModule prefabModule) {
            var name = config.GetValue("name");

            if (partSpec.HasIsDeployed) {

                if (name == "ModuleDeployableSolarPanel" ||
                    name == "ModuleDeployableAntenna" ||
                    name == "ModuleDeployableRadiator") {
                    config.SetValue("deployState", partSpec.IsDeployed ? "EXTENDED" : "RETRACTED");
                }

                if (prefabModule is ModuleWheelDeployment) {
                    var prefabWheelDeploymentModule = prefabModule as ModuleWheelDeployment;
                    var deployedPosition = prefabWheelDeploymentModule.deployedPosition;
                    var retractedPosition = prefabWheelDeploymentModule.retractedPosition;
                    config.SetValue("position", partSpec.IsDeployed ? deployedPosition : retractedPosition);
                    config.SetValue("stateString", partSpec.IsDeployed ? "Deployed" : "Retracted");
                }

                if (prefabModule is ModuleAnimateGeneric) {
                    config.SetValue("animSwitch", partSpec.IsDeployed);
                    config.SetValue("animTime", partSpec.IsDeployed ? 1 : 0);
                }
            }
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
            var availablePart = PartLoader.getPartInfoByName(snapshot.partName);
            var attachments = new string[snapshot.attachNodes.Count];
            for (int i = 0; i < snapshot.attachNodes.Count; i++) {
                attachments[i] = snapshot.attachNodes[i].Save();
            }
            var partSpec = new Pb.PartSpec {
                Name = snapshot.partName,
                Position = To(snapshot.position),
                Rotation = To(snapshot.rotation),
                ParentIndex = snapshot.parentIdx,
                SurfaceAttachment = snapshot.srfAttachNode.Save(),
            };

            if (!string.IsNullOrEmpty(snapshot.moduleVariantName)) {
                partSpec.ModuleVariantName = snapshot.moduleVariantName;
            }

            SetDeploymentState(snapshot, availablePart, partSpec);

            partSpec.Attachments.AddRange(attachments);

            return partSpec;
        }

        private static void SetDeploymentState(ProtoPartSnapshot snapshot, AvailablePart availablePart, Pb.PartSpec partSpec) {
            foreach (var module in snapshot.modules) {
                if (module.moduleName == "ModuleDeployableSolarPanel" ||
                    module.moduleName == "ModuleDeployableAntenna" ||
                    module.moduleName == "ModuleDeployableRadiator") {
                    var deployState = module.moduleValues.GetValue("deployState");
                    if (deployState == null) {
                        Log.Error($"Could not find deployState for {module.moduleName} on part {snapshot.partName}, skipping");
                        continue;
                    }
                    var isDeployed = deployState == "EXTENDED";
                    partSpec.IsDeployed = isDeployed;
                }

                if (module.moduleName == "ModuleWheelDeployment") {
                    var position = module.moduleValues.GetValue("position");
                    var prefabModule = availablePart.partPrefab.FindModuleImplementing<ModuleWheelDeployment>();
                    if (position == null) {
                        Log.Error($"Could not find position for ModuleWheelDeployment on part {snapshot.partName}, skipping");
                        continue;
                    }
                    if (prefabModule == null) {
                        Log.Error($"Could not find ModuleWheelDeployment on part {snapshot.partName}, skipping");
                        continue;
                    }
                    float positionNum;
                    try {
                        positionNum = float.Parse(position);
                    }
                    catch (System.Exception) {
                        Log.Error($"Could not parse position {position} for ModuleWheelDeployment on part {snapshot.partName}, skipping");
                        continue;
                    }
                    var isDeployed = prefabModule.deployedPosition == positionNum;
                    partSpec.IsDeployed = isDeployed;
                }

                // Cargo bays, shielded docking ports, etc.
                if (module.moduleName == "ModuleAnimateGeneric") {
                    var animSwitch = module.moduleValues.GetValue("animSwitch");
                    if (animSwitch == null) {
                        Log.Error($"Could not find animSwitch for ModuleAnimateGeneric on part {snapshot.partName}, skipping");
                        continue;
                    }
                    var isDeployed = animSwitch == "True";
                    partSpec.IsDeployed = isDeployed;
                }
            }
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