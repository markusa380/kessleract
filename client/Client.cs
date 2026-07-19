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

        private const int MAX_REQUESTS_PER_INTERVAL = 3;

        public static Vessel.Situations[] allowableSituations = new Vessel.Situations[] {
          Vessel.Situations.ORBITING
        };

        public void Start() {
            Instance = this;
            partsList = PartLoader.LoadedPartsList.Select(part => part.name).ToList();
        }

        double timeToUpload = 10.0;
        double timeToDownload = 10.0;
        private int downloadIndex = 0;

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

            var startDownloadIndex = downloadIndex;
            var numBodies = FlightGlobals.Bodies.Count;

            for (var requestCount = 0;
                requestCount < MAX_REQUESTS_PER_INTERVAL &&
                    downloadIndex < startDownloadIndex + numBodies;
                downloadIndex++
            ) {
                var bodyIndex = downloadIndex % numBodies;
                var body = FlightGlobals.Bodies[bodyIndex];

                Log.Info($"Downloading abandoned vessels for body {body.name} ({body.flightGlobalsIndex})");

                // Include all vessels, not just the same body, important if the downloaded orbit happens
                // to actually drop into the sphere of influence of another body
                List<AbandonedVessel> abandonedVessels = ExistingAbandonedVesselHashes();
                int bodyAbandonedVesselsCount = abandonedVessels.Count(v => v.vessel.mainBody == body);
                List<int> abandonedVesselsHashes = abandonedVessels.Select(v => v.hash).ToList();

                var take = KessleractConfig.Instance.MaxAbandonedVehiclesPerBody - bodyAbandonedVesselsCount;

                if (take <= 0) {
                    Log.Info($"Skipping download for body {body.name} ({body.flightGlobalsIndex}) because we already have {bodyAbandonedVesselsCount} abandoned vessels and the max is {KessleractConfig.Instance.MaxAbandonedVehiclesPerBody}");
                    continue;
                }

                var allowableParts = PartLoader.LoadedPartsList
                    .Where(part => part.amountAvailable > 0)
                    .Select(part => part.name)
                    .ToList();

                var requestBody = new Pb.DownloadRequest {
                    Body = body.flightGlobalsIndex,
                    Take = take,
                };

                requestBody.ExcludedHashes.AddRange(abandonedVesselsHashes);
                requestBody.AllowableParts.AddRange(allowableParts);

                yield return Request.DownloadVesselCoroutine(
                    requestBody,
                    (downloadResponse) => {
                        foreach (var uniqueVessel in downloadResponse.Vessels) {
                            LoadProtoVesselFromSpec(uniqueVessel, body, true);
                        }
                    }
                );

                requestCount++;
            }
        }

        class AbandonedVessel {
            public Vessel vessel;
            public int hash;
        }

        private static List<AbandonedVessel> ExistingAbandonedVesselHashes() {
            List<AbandonedVessel> abandonedVesselsHashes = new List<AbandonedVessel>();
            foreach (var vessel in FlightGlobals.Vessels) {
                var hash = Naming.GetVesselHash(vessel);
                if (hash != -1) {
                    abandonedVesselsHashes.Add(
                        new AbandonedVessel {
                            vessel = vessel,
                            hash = hash
                        }
                    );
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

            if (!FromProtobuf.From(vesselSpec, body, uniqueVessel.Hash, out var protoVessel)) {
                return;
            }

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
