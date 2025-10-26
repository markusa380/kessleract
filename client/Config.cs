namespace Kessleract {
  [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
  public class KessleractConfig: ScenarioModule {
    public static KessleractConfig Instance { get; private set; }

    public KessleractConfig() {
      Instance = this;
    }

    public string ServerUrl { get; set; } = "http://localhost:8080";
    public double UploadIntervalSeconds { get; set; } = 60.0;
    public bool UploadEnabled { get; set; } = false;
    public double DownloadIntervalSeconds { get; set; } = 300.0;
    public bool DownloadEnabled { get; set; } = false;
    public bool DiscoveryModeEnabled { get; set; } = true;
    public int MaxAbandonedVehiclesPerBody { get; set; } = 5;

    public override void OnLoad(ConfigNode node) {
      if (node.HasValue("ServerUrl")) {
        ServerUrl = node.GetValue("ServerUrl");
      }
      if (node.HasValue("UploadIntervalSeconds")) {
        UploadIntervalSeconds = double.Parse(node.GetValue("UploadIntervalSeconds"));
      }
      if (node.HasValue("UploadEnabled")) {
        UploadEnabled = bool.Parse(node.GetValue("UploadEnabled"));
      }
      if (node.HasValue("DownloadIntervalSeconds")) {
        DownloadIntervalSeconds = double.Parse(node.GetValue("DownloadIntervalSeconds"));
      }
      if (node.HasValue("DownloadEnabled")) {
        DownloadEnabled = bool.Parse(node.GetValue("DownloadEnabled"));
      }
      if (node.HasValue("DiscoveryModeEnabled")) {
        DiscoveryModeEnabled = bool.Parse(node.GetValue("DiscoveryModeEnabled"));
      }
      if (node.HasValue("MaxAbandonedVehiclesPerBody")) {
        MaxAbandonedVehiclesPerBody = int.Parse(node.GetValue("MaxAbandonedVehiclesPerBody"));
      }
    }

		public override void OnSave(ConfigNode node) {
      node.AddValue("ServerUrl", ServerUrl);
      node.AddValue("UploadIntervalSeconds", UploadIntervalSeconds);
      node.AddValue("UploadEnabled", UploadEnabled);
      node.AddValue("DownloadIntervalSeconds", DownloadIntervalSeconds);
      node.AddValue("DownloadEnabled", DownloadEnabled);
      node.AddValue("DiscoveryModeEnabled", DiscoveryModeEnabled);
      node.AddValue("MaxAbandonedVehiclesPerBody", MaxAbandonedVehiclesPerBody);
    }
  }
}