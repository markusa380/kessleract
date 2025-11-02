
using System;
using System.Collections;
using System.Text;
using Google.Protobuf;
using UnityEngine.Networking;

namespace Kessleract {

    class Request {
        public static IEnumerator UploadVesselCoroutine(
          Pb.UploadRequest requestBody,
          Action onSuccess
        ) {
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

            onSuccess.Invoke();
        }

        public static IEnumerator DownloadVesselCoroutine(
          Pb.DownloadRequest requestBody,
          Action<Pb.DownloadResponse> onSuccess
        ) {
            var requestBodyJson = JsonFormatter.Default.Format(requestBody);

            var request = new UnityWebRequest(KessleractConfig.Instance.ServerUrl + "/download", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.isNetworkError) {
                Log.Info("Error while downloading vessels: " + request.error);
                yield break;
            }
            else if (request.responseCode != 200) {
                Log.Info("Unexpected response code while downloading vessels: " + request.responseCode);
                yield break;
            }

            var responseBodyJson = request.downloadHandler.text;
            var downloadResponse = Pb.DownloadResponse.Parser.ParseJson(responseBodyJson);

            onSuccess.Invoke(downloadResponse);
        }

        public static IEnumerator GetVesselInfoCoroutine(
          Pb.VesselInfoRequest requestBody,
          Action<Pb.VesselInfoResponse> onSuccess
        ) {
            var requestBodyJson = JsonFormatter.Default.Format(requestBody);

            var request = new UnityWebRequest(KessleractConfig.Instance.ServerUrl + "/vesselinfo", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.isNetworkError) {
                Log.Info("Error while getting vessel info: " + request.error);
                yield break;
            }
            else if (request.responseCode != 200) {
                Log.Info("Unexpected response code while getting vessel info: " + request.responseCode);
                yield break;
            }

            var responseBodyJson = request.downloadHandler.text;
            var vesselInfoResponse = Pb.VesselInfoResponse.Parser.ParseJson(responseBodyJson);

            onSuccess.Invoke(vesselInfoResponse);
        }
    }
}