using System.Collections.Generic;
using KSPAchievements;

namespace Kessleract {
    class ProtoVesselUtils {

        public static void CreateAbandonedVessel(ConfigNode configNode) {
            var protoVessel = new ProtoVessel(configNode, HighLogic.CurrentGame);
            MakeDiscoverable(protoVessel);
            RemoveCrew(protoVessel);
            protoVessel.flightPlan = new ConfigNode();
            protoVessel.Load(HighLogic.CurrentGame.flightState);
        }

        private static void MakeDiscoverable(ProtoVessel protoVessel) {
            protoVessel.discoveryInfo.SetValue("state", 1, true);
            protoVessel.discoveryInfo.SetValue("lastObservedTime", Planetarium.GetUniversalTime(), true);
            protoVessel.discoveryInfo.SetValue("lifetime", 100 * 24 * 60 * 60, true);
            protoVessel.discoveryInfo.SetValue("refTime", 20 * 24 * 60 * 60, true); // Seems to be the default for all asteroids
            protoVessel.discoveryInfo.SetValue("size", 999, true); // Should print ???, which seems fitting
        }

        private static void RemoveCrew(ProtoVessel protoVessel) {
            var crew = new List<ProtoCrewMember>(protoVessel.GetVesselCrew());
            foreach (var crewMember in crew) {
                protoVessel.RemoveCrew(crewMember);
            }
            protoVessel.RebuildCrewCounts();
        }
    }
}