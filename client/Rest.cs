using System;

namespace Kessleract {

    [Serializable]
    class UploadRequest {
        public int body;
        public VesselSpec vessel;
    }

    [Serializable]
    class DownloadRequest {
        public int body;
        public int take;
        public int[] excludedHashes;
    }

    [Serializable]
    class DownloadResponse {
        public UniqueVessel[] vessels;
    }

    [Serializable]
    class UniqueVessel {
        public int hash;
        public VesselSpec vessel;
    }
}