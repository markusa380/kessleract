using System;
using UnityEngine;
using KSP.UI.Screens;
using static KSP.UI.Screens.ApplicationLauncher;

namespace Kessleract {

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KessleractUI : MonoBehaviour {

        public static KessleractUI Instance { get; private set; }

        // false when entire UI is hidden by pressing F2
        private bool visible = true;

        // true when the window is opened from the toolbar
        private bool open = false; // primary UI open from toolbar

        // true when the settings window is opened
        private bool settingsOpen = false;

        private ApplicationLauncherButton toolbarButton;
        private readonly int mainGuid = Guid.NewGuid().GetHashCode();
        private readonly int settingsGuid = Guid.NewGuid().GetHashCode();
        private Rect mainRect = new Rect(100, 100, 200, 100);
        private Rect settingsRect = new Rect(100, 100, 200, 100);
        private bool showAdvancedSettings = false;

        private readonly Texture settingsIcon = GameDatabase.Instance.GetTexture("KessleractClient/Textures/options_w", false);
        private readonly Texture closeIcon = GameDatabase.Instance.GetTexture("KessleractClient/Textures/close_w", false);

        public void Start() {
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);
            Texture icon = GameDatabase.Instance.GetTexture("KessleractClient/Textures/icon", false);

            toolbarButton = ApplicationLauncher
                .Instance
                .AddModApplication(
                    OnOpen,
                    OnClose,
                    null,
                    null,
                    null,
                    null,
                    AppScenes.FLIGHT,
                    icon
                );

            Instance = this;
        }

        private bool IsShown() {
            return visible && open;
        }

        private bool IsSettingsShown() {
            return visible && settingsOpen;
        }

        public void OnGUI() {
            if (IsShown()) {
                mainRect = GUILayout.Window(mainGuid, mainRect, MainWindowFunction, "Kessleract");
            }
            if (IsSettingsShown()) {
                settingsRect = GUILayout.Window(settingsGuid, settingsRect, SettingsWindowFunction, "Kessleract Settings");
            }
        }

        private void MainWindowFunction(int windowID) {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(settingsIcon, GUILayout.Width(24), GUILayout.Height(24))) {
                settingsOpen = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Abandoned Vessels:");

            var numAbandonedVessels = 0;
            foreach (var vessel in FlightGlobals.VesselsLoaded) {
                if (Naming.GetVesselHash(vessel) != -1) {
                    numAbandonedVessels++;
                    GUILayout.Label($"Vessel: {vessel.vesselName}");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Upvote")) {
                        Client.Instance.StartVoteOnVessel(vessel, true);
                    }
                    if (GUILayout.Button("Downvote")) {
                        Client.Instance.StartVoteOnVessel(vessel, false);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (numAbandonedVessels == 0) {
                GUILayout.Label("No abandoned vessels currently loaded.");
            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void SettingsWindowFunction(int windowID) {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(closeIcon, GUILayout.Width(24), GUILayout.Height(24))) {
                settingsOpen = false;
            }
            GUILayout.EndHorizontal();

            var config = KessleractConfig.Instance;

            GUILayout.Label("Server URL:");
            config.ServerUrl = GUILayout.TextField(config.ServerUrl);

            config.UploadEnabled = GUILayout.Toggle(config.UploadEnabled, "Enable Upload");
            config.DownloadEnabled = GUILayout.Toggle(config.DownloadEnabled, "Enable Download");

            GUILayout.Label("Upload Interval (seconds):");
            double.TryParse(GUILayout.TextField(config.UploadIntervalSeconds.ToString()), out double uploadInterval);
            config.UploadIntervalSeconds = uploadInterval;

            GUILayout.Label("Download Interval (seconds):");
            double.TryParse(GUILayout.TextField(config.DownloadIntervalSeconds.ToString()), out double downloadInterval);
            config.DownloadIntervalSeconds = downloadInterval;

            if (GUILayout.Button(showAdvancedSettings ? "Hide Advanced Settings" : "Show Advanced Settings")) {
                showAdvancedSettings = !showAdvancedSettings;
            }

            if (showAdvancedSettings) {
                GUILayout.Label("Advanced Settings:");
                config.DiscoveryModeEnabled = GUILayout.Toggle(config.DiscoveryModeEnabled, "Enable Discovery Mode");

                GUILayout.Label("Life Time (seconds):");
                int.TryParse(GUILayout.TextField(config.LifeTime.ToString()), out int lifeTime);
                config.LifeTime = lifeTime;

                GUILayout.Label("Max Life Time (seconds):");
                int.TryParse(GUILayout.TextField(config.MaxLifeTime.ToString()), out int maxLifeTime);
                config.MaxLifeTime = maxLifeTime;

                GUILayout.Label("Max Abandoned Vehicles Per Body:");
                int.TryParse(GUILayout.TextField(config.MaxAbandonedVehiclesPerBody.ToString()), out int maxAbandoned);
                config.MaxAbandonedVehiclesPerBody = maxAbandoned;

                GUILayout.Label("Manual Actions:");

                if (GUILayout.Button("Upload Active Vessel")) {
                    Client.Instance.StartUploadCurrentVehicle();
                }
                if (GUILayout.Button("Download Abandoned Vehicles")) {
                    Client.Instance.StartDownloadAbandonedVehicles();
                }
                if (GUILayout.Button("Delete All Abandoned Vehicles")) {
                    foreach (var vessel in FlightGlobals.Vessels) {
                        var vesselHash = Naming.GetVesselHash(vessel);
                        if (vesselHash != -1) {
                            vessel.Die();
                        }
                    }
                }
                if (GUILayout.Button("Debug: Print all available parts")) {
                    foreach (var part in PartLoader.LoadedPartsList) {
                        Debug.Log($"Part: {part.name}, title: {part.title}");
                    }
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void OnOpen() {
            open = true;
        }

        private void OnClose() {
            open = false;
            settingsOpen = false;
        }

        private void OnShowUI() {
            visible = true;
        }

        private void OnHideUI() {
            visible = false;
        }

        public void OnDestroy() {
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);
            ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
        }
    }
}