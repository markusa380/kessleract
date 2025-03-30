using System;
using UnityEngine;
using KSP.UI.Screens;
using static KSP.UI.Screens.ApplicationLauncher;

namespace Kessleract {

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class DebugInterface : MonoBehaviour {

        public static DebugInterface Instance { get; private set; }

        // false when entire UI is hidden by pressing F2
        private bool visible = true;

        // true when the window is opened from the toolbar
        private bool open = false;

        private ApplicationLauncherButton toolbarButton;
        private readonly int mainGuid = Guid.NewGuid().GetHashCode();
        private Rect rect = new Rect(100, 100, 200, 100);

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

        public void OnGUI() {
            if (IsShown()) {
                rect = GUILayout.Window(mainGuid, rect, WindowFunction, "Kessleract Debug");
            }
        }

        private void WindowFunction(int windowID) {
            GUILayout.BeginVertical();
            if (GUILayout.Button("Upload Active Vessel")) {
                Client.Instance.StartUploadCurrentVehicle();
            }
            if (GUILayout.Button("Download Abandoned Vehicles")) {
                Client.Instance.StartDownloadAbandonedVehicles();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void OnOpen() {
            open = true;
        }

        private void OnClose() {
            open = false;
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