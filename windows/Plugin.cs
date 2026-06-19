using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace RDTrainer
{
    // Thin BepInEx host shell. All trainer behaviour lives in Shared/TrainerMenu.cs behind IHost;
    // this class only wires BepInEx's logger, the rebindable menu hotkey and config persistence to
    // that shared core, then forwards the Awake/Update/OnGUI lifecycle to it.
    [BepInPlugin(TrainerInfo.Guid, TrainerInfo.Name, TrainerInfo.Version)]
    public class Plugin : BaseUnityPlugin, IHost
    {
        private TrainerMenu _menu;
        private ConfigEntry<KeyboardShortcut> _menuKey;
        private BepInLog _log;
        private readonly Dictionary<string, ConfigEntry<bool>> _bools = new Dictionary<string, ConfigEntry<bool>>();

        private void Awake()
        {
            _log = new BepInLog(Logger);
            _menuKey = Config.Bind("General", "MenuKey", new KeyboardShortcut(KeyCode.F3),
                "Hotkey to open/close the trainer overlay.");
            _menu = new TrainerMenu(this);
            _menu.Boot();
        }

        private void Update() => _menu?.Tick();
        private void OnGUI() => _menu?.Draw();

        // ---- IHost ----
        public ILog Log => _log;
        public bool MenuKeyDown() => _menuKey.Value.IsDown();
        public string MenuKeyLabel => _menuKey.Value.ToString();

        public bool GetBool(string key, bool fallback) => Bool(key, fallback).Value;
        public void SetBool(string key, bool value) => Bool(key, value).Value = value;

        // BepInEx Config persists trainer prefs to a .cfg file named after the plugin GUID.
        private ConfigEntry<bool> Bool(string key, bool fallback)
        {
            if (!_bools.TryGetValue(key, out var e))
            { e = Config.Bind("State", key, fallback); _bools[key] = e; }
            return e;
        }

        private sealed class BepInLog : ILog
        {
            private readonly ManualLogSource _s;
            public BepInLog(ManualLogSource s) { _s = s; }
            public void Info(string m) => _s.LogInfo(m);
            public void Warn(string m) => _s.LogWarning(m);
            public void Error(string m) => _s.LogError(m);
        }
    }
}
