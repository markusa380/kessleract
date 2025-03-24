using System.Text.Json;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Text.Json.Nodes;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Kessleract {

    // [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Client : MonoBehaviour {

        public static Client Instance { get; private set; }

        public static Vessel.Situations[] allowableSituations = new Vessel.Situations[] {
          Vessel.Situations.LANDED,
          Vessel.Situations.ORBITING,
          Vessel.Situations.ESCAPING
        };

        public void Start() {
            Instance = this;
        }

        double timeSinceUpload = 60.0;

        public void Update() {
            timeSinceUpload += Time.deltaTime;

            if (timeSinceUpload > 60.0) {
                timeSinceUpload = 0.0;
                if (FlightGlobals.ActiveVessel != null && allowableSituations.Contains(FlightGlobals.ActiveVessel.situation)) {
                    StartCoroutine(UploadCurrentVehicle());
                    StartCoroutine(DownloadAbandonedVehicles());
                }
                else {
                    Log.Info("Not uploading vehicle because it is not in a valid situation");
                }
            }
        }

        private IEnumerator UploadCurrentVehicle() {
            Log.Info("Uploading current vehicle");

            var requestBody = new JsonObject {
                { "data", GetVehicleJson() },
                { "body", FlightGlobals.currentMainBody.flightGlobalsIndex },
                { "id", FlightGlobals.ActiveVessel.id.ToString() }
            };
            var request = new UnityWebRequest("http://localhost:8080/upload", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody.ToJsonString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.isNetworkError) {
                Log.Info("Error while uploading vessel: " + request.error);
            }
            else if (request.responseCode != 200) {
                Log.Info("Unexpected response code while uploading vessel: " + request.responseCode);
            }
        }

        static readonly int MAX_COUNT_PER_BODY = 5;

        private IEnumerator DownloadAbandonedVehicles() {
            Log.Info("Downloading abandoned vehicles");

            var allCelestialBodies = FlightGlobals.Bodies.Select(body => body.flightGlobalsIndex).ToList();
            var abandonedVesselsPerCelestialBody = new Dictionary<int, int>();

            FlightGlobals.Vessels.ForEach(vessel => {
                if (vessel.vesselName.StartsWith("[Abandoned]")) {
                    var celestialBodyIndex = vessel.mainBody.flightGlobalsIndex;
                    if (!abandonedVesselsPerCelestialBody.ContainsKey(celestialBodyIndex)) {
                        abandonedVesselsPerCelestialBody[celestialBodyIndex] = 0;
                    }
                    abandonedVesselsPerCelestialBody[celestialBodyIndex]++;
                }
            });

            var excludedIds = new JsonArray();

            FlightGlobals.Vessels.ForEach(vessel => {
                excludedIds.Add(vessel.id.ToString());
            });

            var bodies = new JsonObject();

            for (int i = 0; i < allCelestialBodies.Count; i++) {
                var existingCount = abandonedVesselsPerCelestialBody.ContainsKey(allCelestialBodies[i]) ? abandonedVesselsPerCelestialBody[allCelestialBodies[i]] : 0;
                var countToDownload = Math.Max(0, MAX_COUNT_PER_BODY - existingCount);
                bodies.Add(allCelestialBodies[i].ToString(), countToDownload);
            }

            var requestBody = new JsonObject {
                    { "bodies", bodies },
                    { "excludedIds", excludedIds }
                };

            var request = new UnityWebRequest("http://localhost:8080/download", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody.ToJsonString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.isNetworkError) {
                Log.Info("Error while downloading vessels: " + request.error);
            }
            else if (request.responseCode != 200) {
                Log.Info("Unexpected response code while downloading vessels: " + request.responseCode);
            }
            else {
                Log.Info("Downloaded: " + request.downloadHandler.text);
                // TODO: Create vessels from the downloaded data after sanitizing it

            }
        }


        public JsonObject GetVehicleJson() {
            var vehicle = FlightGlobals.ActiveVessel;
            var configNode = new ConfigNode();
            vehicle.protoVessel.Save(configNode);
            return Json.ConfigNodeToJson(configNode);
        }

        public void CreateVehicleFromJson(string json) {
            var jsonElement = JsonDocument.Parse(json).RootElement;
            var configNode = Json.JsonToConfigNode(jsonElement, "");
            ProtoVesselUtils.CreateAbandonedVessel(configNode);
        }
    }
}
