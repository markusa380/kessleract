using UnityEngine;

namespace Kessleract {
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  class Experiment : MonoBehaviour {

    double timeWaited = 0.0;
    bool experimentDone = false;
    public void Start() {
      Log.Info("################## Experiment Start ##################");
    }

    public void Update() {
      timeWaited += Time.deltaTime;
      if (timeWaited > 10.0 && !experimentDone) {
        experimentDone = true;
        Log.Info("################## Experiment Run ##################");
        Orbit orbit = new Orbit(FlightGlobals.ActiveVessel.orbit);
        orbit.inclination += 0.1;
        var id = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
        var partNode = ProtoVessel.CreatePartNode("mk1pod", id, null);
        var parts = new ConfigNode[]{ partNode };
        ConfigNode vesselNode = ProtoVessel.CreateVesselNode("Test Vessel", VesselType.Ship, orbit, 0, parts);
        Log.Info(vesselNode.ToString());
        var protoVessel = new ProtoVessel(vesselNode, HighLogic.CurrentGame);
        protoVessel.Load(HighLogic.CurrentGame.flightState);
      }
    }
  }
}