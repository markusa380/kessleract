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

        private Vector2 scrollPosition = Vector2.zero;

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
                    AppScenes.SPACECENTER,
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

        private string vehicleJson = "";

        private void WindowFunction(int windowID) {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Generate Vehicle XML")) {
                vehicleJson = Client.Instance.GetVehicleJson().ToJsonString();
            }

            scrollPosition = GUILayout.BeginScrollView(
                  scrollPosition, GUILayout.Width(180), GUILayout.Height(100));

            GUILayout.TextArea(vehicleJson);

            GUILayout.EndScrollView();

            if (GUILayout.Button("Copy to Clipboard")) {
                GUIUtility.systemCopyBuffer = vehicleJson;
            }

            if (GUILayout.Button("Paste from Clipboard")) {
                vehicleJson = GUIUtility.systemCopyBuffer;
            }

            if (GUILayout.Button("Instantiate")) {
                Client.Instance.CreateVehicleFromJson(vehicleJson);
            }

            if (GUILayout.Button("Clear")) {
                vehicleJson = "";
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