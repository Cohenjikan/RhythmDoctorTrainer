using System;
using System.Collections.Generic;
using UnityEngine;

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

    // CJK font for the IMGUI menu, shared by the Windows (Plugin) and native macOS/Linux
    // (Trainer) entry points so every platform behaves identically — no more divergent per-OS
    // font lists. Unity's legacy Font can only use OS-REGISTERED fonts (there is no runtime
    // "load this .ttf from disk" API), so the installer drops "Noto Sans SC" into the user's
    // OS font directory; we then load it by that single family name on all three platforms.
    // The remaining families are fallbacks for users who skipped the font install or already
    // have a CJK font — CreateDynamicFontFromOSFont treats the array as a glyph-fallback chain.
    internal static class CjkFont
    {
        private static Font _cached;
        private static int _cachedSize;

        // Best first. "Noto Sans SC" is what our installer ships; the rest are common system
        // CJK families on Windows / macOS / Linux respectively.
        private static readonly string[] Preferred =
        {
            "Noto Sans SC", "Noto Sans CJK SC",                                  // bundled by the installer
            "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun",         // Windows
            "PingFang SC", "Heiti SC", "Hiragino Sans GB", "Songti SC", "STHeiti", // macOS
            "WenQuanYi Micro Hei", "WenQuanYi Zen Hei", "Source Han Sans SC",
            "AR PL UMing CN", "Droid Sans Fallback",                            // Linux
            "Arial Unicode MS",
        };

        // Substrings to sniff a CJK family out of the full installed list when none of the
        // curated names match exactly (family strings vary across distros / font releases).
        private static readonly string[] Hints =
        {
            "noto sans sc", "noto sans cjk", "source han", "wenquanyi", "ar pl",
            "droid sans fallback", "pingfang", "heiti", "songti", "yahei", "微软雅黑", "黑体", "宋体",
        };

        // CreateDynamicFontFromOSFont silently yields a glyph-less font if the FIRST name is
        // absent on this OS, so we hand it ONLY names that are actually installed.
        public static Font Get(int size)
        {
            if (_cached != null && _cachedSize == size) return _cached;

            string[] inst;
            try { inst = Font.GetOSInstalledFontNames() ?? new string[0]; } catch { inst = new string[0]; }

            var pick = new List<string>();
            foreach (var name in Preferred)
                if (Array.IndexOf(inst, name) >= 0 && !pick.Contains(name)) pick.Add(name);

            if (pick.Count == 0)
                foreach (var h in Hints)
                    foreach (var f in inst)
                        if (f.ToLowerInvariant().Contains(h) && !pick.Contains(f)) pick.Add(f);

            try
            {
                _cached = pick.Count > 0
                    ? Font.CreateDynamicFontFromOSFont(pick.ToArray(), size)
                    : Font.CreateDynamicFontFromOSFont("Noto Sans SC", size);
                _cachedSize = size;
            }
            catch { }
            return _cached;
        }
    }
}
