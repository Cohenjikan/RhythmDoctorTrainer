using UnityEngine;

namespace RDTrainerMac
{
    // Minimal logger replacing BepInEx's ManualLogSource. Output lands in the Unity
    // Player.log (~/Library/Logs/7th Beat Games/Rhythm Doctor/Player.log).
    internal static class Log
    {
        private const string Tag = "[RDTrainerMac] ";
        public static void Info(string m) => Debug.Log(Tag + m);
        public static void Warn(string m) => Debug.LogWarning(Tag + m);
        public static void Error(string m) => Debug.LogError(Tag + m);
    }
}
