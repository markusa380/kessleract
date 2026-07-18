using System;
namespace Kessleract {
    class Log {
        public static void Info(string message) {
            UnityEngine.Debug.Log("[Kessleract] " + message);
        }

        public static void Error(string message) {
            UnityEngine.Debug.LogError("[Kessleract] " + message);
        }

        public static void Error(string message, Exception e) {
            Error(message);
            UnityEngine.Debug.LogException(e);
        }
    }
}