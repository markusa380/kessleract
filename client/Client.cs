using System.Text.Json;
using UnityEngine;

namespace Kessleract {

  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class Client : MonoBehaviour {

    public static Client Instance { get; private set; }

    public void Start() {
      Instance = this;
    }

    public void Update() {
    }

    public string GetVehicleJson() {
      var vehicle = FlightGlobals.ActiveVessel;
      var configNode = new ConfigNode();
      vehicle.protoVessel.Save(configNode);
      return Json.ConfigNodeToJson(configNode).ToJsonString();
    }

    public void CreateVehicleFromJson(string json) {
      var jsonElement = JsonDocument.Parse(json).RootElement;
      var configNode = Json.JsonToConfigNode(jsonElement, "");
      ProtoVesselUtils.CreateAbandonedVessel(configNode);
    }
  }
}
