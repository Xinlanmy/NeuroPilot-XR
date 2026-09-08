using System;
using UnityEngine;

namespace NeuroPilotXR.Navigation
{
    // One shared endpoint for both EEG modes. The JSON asset supplies installation defaults;
    // PlayerPrefs supplies the headset user's override. Reading never implies a live connection.
    public static class CommunicationSettings
    {
        [Serializable] private sealed class Defaults { public string host; public int port; }
        private static Defaults defaults;
        private static Defaults Config => defaults ?? (defaults = JsonUtility.FromJson<Defaults>(Resources.Load<TextAsset>("CommunicationDefaults").text));
        private static string PreferenceKey => "NeuroPilot.Eeg.Host.v1"
#if UNITY_EDITOR
            + TestKeySuffix
#endif
            ;
#if UNITY_EDITOR
        public static string TestKeySuffix = "";
        public static void ClearTestOverride() { if (TestKeySuffix.Length > 0) PlayerPrefs.DeleteKey(PreferenceKey); }
#endif
        public static string DefaultHost => Config.host;
        public static int Port => Config.port;
        public static string Host
        {
            get
            {
                string saved = PlayerPrefs.GetString(PreferenceKey, DefaultHost);
                return TryNormalize(saved, out string normalized) ? normalized : DefaultHost;
            }
        }
        public static string Endpoint => "ws://" + Host + ":" + Port;
        public static bool TrySave(string input)
        {
            if (!TryNormalize(input, out string normalized)) return false;
            PlayerPrefs.SetString(PreferenceKey, normalized);
            PlayerPrefs.Save();
            return true;
        }
        public static bool TryNormalize(string input, out string normalized)
        {
            normalized = null;
            string[] parts = (input ?? "").Trim().Split('.');
            if (parts.Length != 4) return false;
            var values = new int[4];
            for (int i = 0; i < 4; i++)
            {
                if (parts[i].Length < 1 || parts[i].Length > 3) return false;
                foreach (char digit in parts[i]) if (digit < '0' || digit > '9') return false;
                if (!int.TryParse(parts[i], out values[i]) || values[i] > 255) return false;
            }
            if (values[0] == 0 || values[0] >= 224 || (values[0] == 255 && values[3] == 255)) return false;
            normalized = string.Join(".", values);
            return true;
        }
    }
}
