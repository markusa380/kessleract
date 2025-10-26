using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;

namespace Kessleract {

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Client : MonoBehaviour {

        public static Client Instance { get; private set; }

        public static Vessel.Situations[] allowableSituations = new Vessel.Situations[] {
          Vessel.Situations.ORBITING
        };

        public void Start() {
            Instance = this;
        }

        double timeSinceUpload = 0.0;
        double timeSinceDownload = 0.0;

        public void Update() {
            timeSinceUpload += Time.deltaTime;
            timeSinceDownload += Time.deltaTime;


            if (timeSinceUpload > KessleractConfig.Instance.UploadIntervalSeconds) {
                timeSinceUpload = 0.0;
                if (FlightGlobals.ActiveVessel != null && allowableSituations.Contains(FlightGlobals.ActiveVessel.situation)) {
                    if (KessleractConfig.Instance.UploadEnabled) {
                        StartUploadCurrentVehicle();
                    }
                }

            }

            if (timeSinceDownload > KessleractConfig.Instance.DownloadIntervalSeconds) {
                timeSinceDownload = 0.0;
                if (KessleractConfig.Instance.DownloadEnabled) {
                    StartDownloadAbandonedVehicles();
                }
                else {
                    Log.Info("Not uploading vehicle because it is not in a valid situation");
                }
            }
        }

        public void StartUploadCurrentVehicle() {
            StartCoroutine(UploadCurrentVehicleCoroutine());
        }

        public void StartDownloadAbandonedVehicles() {
            StartCoroutine(DownloadAbandonedVehiclesCoroutine());
        }

        private IEnumerator UploadCurrentVehicleCoroutine() {
            Log.Info("Uploading current vehicle");

            if (FlightGlobals.ActiveVessel == null) {
                Log.Info("No active vessel to upload");
                yield break;
            }

            var vesselSpec = ToProtobuf.To(FlightGlobals.ActiveVessel.protoVessel);
            var requestBody = new Pb.UploadRequest {
                Body = FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex,
                Vessel = vesselSpec
            };
            var requestBodyJson = JsonFormatter.Default.Format(requestBody);

            var request = new UnityWebRequest(KessleractConfig.Instance.ServerUrl + "/upload", "POST");
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

            Log.Info("Upload of active vessel complete");
        }

        private IEnumerator DownloadAbandonedVehiclesCoroutine() {
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
                            if (int.TryParse(hash, out int hashInt)) {
                                abandonedVesselsHashes.Add(hashInt);
                            }
                        }
                    }
                });

                var take = Math.Max(0, KessleractConfig.Instance.MaxAbandonedVehiclesPerBody - nonAbandonedVesselsCount);

                var requestBody = new Pb.DownloadRequest {
                    Body = body.flightGlobalsIndex,
                    Take = take,
                };

                requestBody.ExcludedHashes.AddRange(abandonedVesselsHashes);

                var requestBodyJson = JsonFormatter.Default.Format(requestBody);

                var request = new UnityWebRequest(KessleractConfig.Instance.ServerUrl + "/download", "POST");
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
                    var downloadResponse = Pb.DownloadResponse.Parser.ParseJson(responseBodyJson);

                    foreach (var uniqueVessel in downloadResponse.Vessels) {
                        var vesselSpec = uniqueVessel.Vessel;

                        var protoVessel = FromProtobuf.From(vesselSpec, body, uniqueVessel.Hash);

                        var orbit = protoVessel.orbitSnapShot.Load();
                        var relativePos = orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

                        var isSafeOrbit = true;
                        foreach (var vessel in FlightGlobals.Vessels) {
                            if (vessel.orbit.referenceBody != body) {
                                continue;
                            }

                            var vesselRelativePos = vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

                            var distance = (relativePos - vesselRelativePos).magnitude;

                            if (distance < 100) {
                                Log.Info($"Not loading abandoned vessel because it is too close to another vessel ({vessel.vesselName} - {distance}m)");
                                isSafeOrbit = false;
                                break;
                            }
                        }

                        if (!isSafeOrbit) {
                            continue;
                        }

                        if (KessleractConfig.Instance.DiscoveryModeEnabled) {
                            var lifeTime = 100 * 24 * 60 * 60;
                            protoVessel.discoveryInfo = ProtoVessel.CreateDiscoveryNode(
                                DiscoveryLevels.Presence,
                                UntrackedObjectClass.A,
                                0,
                                lifeTime
                            );
                        }

                        protoVessel.Load(HighLogic.CurrentGame.flightState);
                    }

                }
            }
        }
    }
}
