using UnityEngine;

namespace RDTrainer
{
    // Thin native host shell (macOS/Linux), spawned by the Cecil-woven Loader (mac/Loader.cs).
    // All trainer behaviour lives in Shared/TrainerMenu.cs behind IHost; this only wires the
    // hardcoded F3 hotkey, PlayerPrefs persistence and the Player.log logger (mac/Log.cs) to that
    // shared core, then forwards the Awake/Update/OnGUI lifecycle.
    public class TrainerHost : MonoBehaviour, IHost
    {
        private static readonly KeyCode MenuKey = KeyCode.F3;
        private TrainerMenu _menu;
        private readonly LogAdapter _log = new LogAdapter();

        private void Awake()
        {
            _menu = new TrainerMenu(this);
            _menu.Boot();
        }

        private void Update() => _menu?.Tick();
        private void OnGUI() => _menu?.Draw();

        // ---- IHost ----
        public ILog Log => _log;
        public bool MenuKeyDown() => Input.GetKeyDown(MenuKey);
        public string MenuKeyLabel => "F3";

        // PlayerPrefs, keyed under the original "RDTrainerMac." prefix so a pref saved by earlier mac
        // builds (e.g. RDTrainerMac.showHintWindow) carries over unchanged.
        public bool GetBool(string key, bool fallback)
            => PlayerPrefs.GetInt("RDTrainerMac." + key, fallback ? 1 : 0) != 0;
        public void SetBool(string key, bool value)
        { PlayerPrefs.SetInt("RDTrainerMac." + key, value ? 1 : 0); PlayerPrefs.Save(); }

        // Adapt the static Player.log logger (mac/Log.cs) to ILog. Fully-qualified RDTrainer.Log so it
        // doesn't collide with this class's own IHost.Log property.
        private sealed class LogAdapter : ILog
        {
            public void Info(string m) => RDTrainer.Log.Info(m);
            public void Warn(string m) => RDTrainer.Log.Warn(m);
            public void Error(string m) => RDTrainer.Log.Error(m);
        }
    }
}
