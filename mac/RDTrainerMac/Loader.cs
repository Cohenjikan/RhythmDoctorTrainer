using System;
using UnityEngine;

namespace RDTrainerMac
{
    // Entry point. A single call to Loader.Init() is woven into the game's RDStartup.Setup
    // by the Cecil patcher (../Patcher). It spawns a persistent host GameObject carrying the
    // trainer MonoBehaviour. This is the macOS replacement for BepInEx's chainloader.
    public static class Loader
    {
        private static bool _started;

        public static void Init()
        {
            if (_started) return;
            _started = true;
            try
            {
                var go = new GameObject("RDTrainerMac");
                go.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<Trainer>();
                Log.Info("Loader started — trainer host spawned.");
            }
            catch (Exception e)
            {
                Log.Error("Loader.Init failed: " + e);
            }
        }
    }
}
