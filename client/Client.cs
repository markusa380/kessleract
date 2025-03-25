using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Kessleract {

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Client : MonoBehaviour {

        public static Client Instance { get; private set; }

        public static Vessel.Situations[] allowableSituations = new Vessel.Situations[] {
          // TODO: Vessel.Situations.LANDED,
          Vessel.Situations.ORBITING
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

            if (FlightGlobals.ActiveVessel == null) {
                Log.Info("No active vessel to upload");
                yield break;
            }

            var vesselSpec = VesselSpec.From(FlightGlobals.ActiveVessel.protoVessel);
            var requestBody = new UploadRequest {
                body = FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex,
                vessel = vesselSpec
            };
            var requestBodyJson = requestBody.ToJSON().Stringify();

            var request = new UnityWebRequest("http://localhost:8080/upload", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyJson);
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

            foreach (var body in FlightGlobals.Bodies) {
                Log.Info($"Downloading abandoned vessels for body {body.name} ({body.flightGlobalsIndex})");

                var nonAbandonedVesselsCount = 0;
                List<int> abandonedVesselsHashes = new List<int>();
                FlightGlobals.Vessels.ForEach(vessel => {
                    var notAbandoned = !vessel.vesselName.StartsWith(Naming.ABANDONED_VESSEL_PREFIX);
                    var okSituation = allowableSituations.Contains(vessel.situation);
                    var okBody = vessel.mainBody == body;

                    if (okBody) {
                        if (notAbandoned) {
                            if (okSituation) {
                                nonAbandonedVesselsCount++;
                            }
                        }
                        else {
                            var hash = vessel.vesselName.Substring(Naming.ABANDONED_VESSEL_PREFIX.Length);
                            var hashInt = int.Parse(hash);
                            abandonedVesselsHashes.Add(hashInt);
                        }
                    }
                });

                var take = Math.Max(0, MAX_COUNT_PER_BODY - nonAbandonedVesselsCount);

                var requestBody = new DownloadRequest {
                    body = body.flightGlobalsIndex,
                    take = take,
                    excludedHashes = abandonedVesselsHashes.ToArray()
                };

                var requestBodyJson = requestBody.ToJSON().Stringify();

                var request = new UnityWebRequest("http://localhost:8080/download", "POST");
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyJson);
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
                    var responseBodyJson = request.downloadHandler.text;
                    var downloadResponse = DownloadResponse.FromJSON(Json.FromJson(responseBodyJson));

                    foreach (var uniqueVessel in downloadResponse.vessels) {
                        var vesselSpec = uniqueVessel.vessel;

                        var protoVessel = vesselSpec.ToProtoVessel(body, uniqueVessel.hash);
                        // Enable when ready
                        // var lifeTime = 100 * 24 * 60 * 60;
                        // protoVessel.discoveryInfo = ProtoVessel.CreateDiscoveryNode(DiscoveryLevels.Presence, UntrackedObjectClass.A, 0, lifeTime);
                        protoVessel.Load(HighLogic.CurrentGame.flightState);
                    }

                }
            }
        }
    }
}
