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

        public List<string> partsList = new List<string>();

        public static Vessel.Situations[] allowableSituations = new Vessel.Situations[] {
          Vessel.Situations.ORBITING
        };

        public void Start() {
            Instance = this;
            partsList = PartLoader.LoadedPartsList.Select(part => part.name).ToList();
        }

        double timeToUpload = 10.0;
        double timeToDownload = 10.0;

        public void Update() {
            timeToUpload -= Time.deltaTime;
            timeToDownload -= Time.deltaTime;

            if (KessleractConfig.Instance.UploadEnabled) {
                if (timeToUpload <= 0.0) {
                    timeToUpload = KessleractConfig.Instance.UploadIntervalSeconds;
                    if (FlightGlobals.ActiveVessel != null && allowableSituations.Contains(FlightGlobals.ActiveVessel.situation)) {
                        StartUploadCurrentVehicle();
                    }
                }
            }

            if (KessleractConfig.Instance.DownloadEnabled) {
                if (timeToDownload <= 0.0) {
                    timeToDownload = KessleractConfig.Instance.DownloadIntervalSeconds;
                    StartDownloadAbandonedVehicles();
                }
            }

            if (FlightGlobals.ActiveVessel != null) {
                if (Naming.GetVesselHash(FlightGlobals.ActiveVessel) != -1) {
                    if (FlightGlobals.ActiveVessel.DiscoveryInfo.Level != DiscoveryLevels.Owned) {
                        Log.Info($"Taking ownership over abandoned vessel ({FlightGlobals.ActiveVessel.vesselName}): {FlightGlobals.ActiveVessel.DiscoveryInfo.Level} => Owned");
                        FlightGlobals.ActiveVessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Owned);
                    }
                }
            }
        }

        public void StartUploadCurrentVehicle() {
            StartCoroutine(UploadCurrentVehicleCoroutine());
        }

        public void StartDownloadAbandonedVehicles() {
            StartCoroutine(DownloadAbandonedVehiclesCoroutine());
        }

        public void StartVoteOnVessel(Vessel vessel, bool upvote) {
            var vesselHash = Naming.GetVesselHash(vessel);
            var body = vessel.mainBody.flightGlobalsIndex;
            StartCoroutine(VoteOnVesselCoroutine(vesselHash, body, upvote));
        }

        private IEnumerator UploadCurrentVehicleCoroutine() {
            Log.Info("Uploading current vehicle");

            if (FlightGlobals.ActiveVessel == null) {
                Log.Info("No active vessel to upload");
                yield break;
            }

            var protoVessel = FlightGlobals.ActiveVessel.BackupVessel();
            var vesselSpec = ToProtobuf.To(protoVessel);
            var requestBody = new Pb.UploadRequest {
                Body = FlightGlobals.ActiveVessel.mainBody.flightGlobalsIndex,
                Vessel = vesselSpec
            };

            yield return Request.UploadVesselCoroutine(requestBody, () =>
                Log.Info("Upload of active vessel complete")
            );
        }

        private IEnumerator DownloadAbandonedVehiclesCoroutine() {
            Log.Info("Downloading abandoned vehicles");

            foreach (var body in FlightGlobals.Bodies) {
                Log.Info($"Downloading abandoned vessels for body {body.name} ({body.flightGlobalsIndex})");

                List<int> abandonedVesselsHashes = ExistingAbandonedVesselHashes(body);

                var take = Math.Max(0, KessleractConfig.Instance.MaxAbandonedVehiclesPerBody - abandonedVesselsHashes.Count);

                var requestBody = new Pb.DownloadRequest {
                    Body = body.flightGlobalsIndex,
                    Take = take,
                };


                yield return Request.DownloadVesselCoroutine(
                    requestBody,
                    (downloadResponse) => {
                        foreach (var uniqueVessel in downloadResponse.Vessels) {
                            LoadProtoVesselFromSpec(uniqueVessel, body, true);
                        }
                    }
                );
            }
        }

        private static List<int> ExistingAbandonedVesselHashes(CelestialBody body) {
            List<int> abandonedVesselsHashes = new List<int>();
            foreach (var vessel in FlightGlobals.Vessels) {
                if (vessel.mainBody == body) {
                    var hash = Naming.GetVesselHash(vessel);
                    if (hash != -1) {
                        abandonedVesselsHashes.Add(hash);
                    }
                }
            }
            return abandonedVesselsHashes;
        }

        public void LoadProtoVesselFromSpec(
                Pb.UniqueVesselSpec uniqueVessel,
                CelestialBody body,
                bool safetyChecks
            ) {
            var vesselSpec = uniqueVessel.Vessel;

            var protoVessel = FromProtobuf.From(vesselSpec, body, uniqueVessel.Hash);

            var orbit = protoVessel.orbitSnapShot.Load();
            var relativePos = orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

            if (safetyChecks && !OrbitSafetyCheck(uniqueVessel, body, relativePos)) {
                return;
            }

            if (KessleractConfig.Instance.DiscoveryModeEnabled) {
                protoVessel.discoveryInfo = ProtoVessel.CreateDiscoveryNode(
                    DiscoveryLevels.Presence,
                    UntrackedObjectClass.A,
                    KessleractConfig.Instance.LifeTime,
                    KessleractConfig.Instance.MaxLifeTime
                );
            }

            Log.Info($"Loading abandoned vessel {uniqueVessel.Hash} into the game, orbit: {OrbitSnapshotToString(protoVessel.orbitSnapShot)}");

            protoVessel.Load(HighLogic.CurrentGame.flightState);
        }

        private static bool OrbitSafetyCheck(Pb.UniqueVesselSpec uniqueVessel, CelestialBody body, Vector3d relativePos) {
            var isSafeOrbit = true;

            foreach (var vessel in FlightGlobals.Vessels) {
                if (vessel.orbit.referenceBody != body) {
                    continue;
                }

                var vesselRelativePos = vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());

                var distance = (relativePos - vesselRelativePos).magnitude;

                if (distance < 1000) {
                    Log.Info($"Not loading abandoned vessel {uniqueVessel.Hash} because it is too close to another vessel ({vessel.vesselName} - {distance}m)");
                    isSafeOrbit = false;
                    break;
                }
            }

            return isSafeOrbit;
        }

        private string OrbitSnapshotToString(OrbitSnapshot orbitSnap) {
            return $"SMA: {orbitSnap.semiMajorAxis}, ECC: {orbitSnap.eccentricity}, INC: {orbitSnap.inclination}, AOP: {orbitSnap.argOfPeriapsis}, LAN: {orbitSnap.LAN}, MAAE: {orbitSnap.meanAnomalyAtEpoch}, EPOCH: {orbitSnap.epoch}, REFIDX: {orbitSnap.ReferenceBodyIndex}";
        }

        private IEnumerator VoteOnVesselCoroutine(int vesselHash, int body, bool upvote) {
            Log.Info($"Voting on abandoned vessel {vesselHash}, upvote: {upvote}");

            var requestBody = new Pb.VoteRequest {
                Body = body,
                VesselHash = vesselHash,
                Upvote = upvote
            };
            var requestBodyJson = JsonFormatter.Default.Format(requestBody);

            var request = new UnityWebRequest(KessleractConfig.Instance.ServerUrl + "/vote", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.isNetworkError) {
                Log.Info("Error while voting on vessel: " + request.error);
            }
            else if (request.responseCode != 200) {
                Log.Info("Unexpected response code while voting on vessel: " + request.responseCode);
            }

            Log.Info("Vote on vessel complete");
        }
    }
}
