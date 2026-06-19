namespace RDTrainer
{
    // Logging seam: BepInEx ManualLogSource on Windows, UnityEngine.Debug (mac/Log.cs) on macOS/Linux.
    public interface ILog
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }

    // The platform seam the shared trainer core (Shared/TrainerMenu.cs) depends on. Implemented by
    // each thin host shell:
    //   windows/Plugin.cs    — BepInEx BaseUnityPlugin
    //   mac/TrainerHost.cs   — plain MonoBehaviour spawned by the Cecil-woven Loader
    // Everything platform-independent (the IMGUI menu, per-frame cheat application, save edits,
    // Harmony patches, CJK font) lives in Shared/ and reaches the host ONLY through this interface.
    public interface IHost
    {
        ILog Log { get; }

        // Menu hotkey. Windows binds a rebindable BepInEx ConfigEntry<KeyboardShortcut>; macOS/Linux
        // hardcode F3. Returns true only on the frame the key is first pressed (same semantics as
        // BepInEx KeyboardShortcut.IsDown()).
        bool MenuKeyDown();

        // Human-readable hotkey label for the menu title bar / hint window (e.g. "F3").
        string MenuKeyLabel { get; }

        // Persisted trainer preferences. Windows -> BepInEx Config; macOS/Linux -> PlayerPrefs.
        // Currently used only to remember the top-left hint window's visibility (showHintWindow).
        bool GetBool(string key, bool fallback);
        void SetBool(string key, bool value);
    }
}
