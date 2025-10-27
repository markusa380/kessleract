namespace Kessleract {
    public class Naming {
        public static string ABANDONED_VESSEL_PREFIX = "Abandoned Vessel ";

        public static int GetVesselHash(Vessel vessel) {
            if (!vessel.vesselName.StartsWith(ABANDONED_VESSEL_PREFIX)) {
                return -1;
            }
            var hash = vessel.vesselName.Substring(ABANDONED_VESSEL_PREFIX.Length);
            if (int.TryParse(hash, out int hashInt)) {
                return hashInt;
            }
            return -1;
        }
    }
}