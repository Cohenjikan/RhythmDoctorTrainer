namespace RDTrainer
{
    // Shared runtime state, read by the trainer entry point (Plugin on Windows / Trainer on
    // macOS), its OnGUI menus, and the Harmony patches in Patches.cs. Platform-independent —
    // this single file is compiled into BOTH the BepInEx (Windows) and native (macOS/Linux)
    // builds (the mac project links it via <Compile Include="..\..\src\Cheats.cs">).
    internal static class Cheats
    {
        // ---- master switch ----
        // 修改器总开关，默认开启。关闭时整个修改器停止工作，并把曾持续接管的开关一次性释放回原版
        // （DebugSettings.Auto / 变速 / 武士模式 …），游戏与编辑器恢复原生行为。不持久化，每次启动默认开。
        public static bool masterEnabled = true;

        // ---- normal player ----
        public static bool autoplay = false;       // DebugSettings.Auto in gameplay
        public static bool forceFlawless = true;    // keep JCI/flawless marker on a clean autoplay run
        public static bool speedOverride = false;   // scnGame.levelSpeed (+ live RDTime.speed)
        public static float speed = 1.0f;
        public static bool widenJudge = false;      // multiply hit window
        public static float judgeMult = 3.0f;
        public static bool instantDialogue = false; // DebugSettings.InstantDialogue
        public static bool skipTransitions = false; // DebugSettings.SkipMenuTransitions
        public static bool unlimitedFps = false;    // DebugSettings.UnlimitedFramerate
        public static bool muteBeatSounds = false;  // DebugSettings.BeatSounds = !this

        // ---- developer ----
        public static bool devMode = false;         // RDBase.isDev -> true
        public static bool debugMode = false;       // DebugSettings.Debug (shows debug overlay)
        public static bool noAchievements = false;  // DebugSettings.GiveAchievements = !this
        public static bool samuraiMode = false;     // RDString.samuraiMode

        // ---- practice / overlay ----
        public static bool autoRestartOnMiss = false; // restart level as soon as numMistakes > 0
        public static bool keyOverlay = false;        // on-screen pressed-keys window
        // hotkeys F4 (win level) / F5 (quick restart) are always on, no toggle
        public static bool showHintWindow = true;      // top-left hint window (macOS: persisted via PlayerPrefs)

        // ---- advanced (calibration, edited then applied) ----
        public static float calV, calI, calIP2, calLat;
    }
}
