using UnityEngine;
using HarmonyLib;

namespace RDTrainer
{
    // Shared runtime state, read by Update(), OnGUI() and the Harmony patches below.
    internal static class Cheats
    {
        // ---- master switch ----
        // 总开关，默认开启。关闭时整个修改器停止工作并把对游戏状态的接管释放回原版
        // （见 Plugin.ApplyState / ReleaseToVanilla），游戏与编辑器恢复原生行为。
        public static bool masterEnabled = true;

        // ---- normal player ----
        public static bool autoplay = false;       // DebugSettings.Auto in gameplay
        public static bool forceFlawless = true;    // keep JCI/flawless marker on a clean autoplay run
        public static bool speedOverride = false;   // scnGame.levelSpeed (+ live RDTime.speed)
        public static float speed = 1.0f;
        public static bool widenJudge = false;      // multiply hit window (legacy menu only)
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

        // ---- practice / overlay (v2.4.0) ----
        public static bool autoRestartOnMiss = false; // restart level as soon as numMistakes > 0
        public static bool keyOverlay = false;        // on-screen pressed-keys window
        // hotkeys F4 (win level) / F5 (quick restart) are always on, no toggle

        // ---- advanced (calibration, edited then applied) ----
        public static float calV, calI, calIP2, calLat;
    }

    // Force autoplay's flawless/JCI marker on a clean run (game suppresses it when autoplay is on).
    [HarmonyPatch(typeof(LevelBase), nameof(LevelBase.isZeroOffset), MethodType.Getter)]
    internal static class FlawlessPatch
    {
        private static void Postfix(LevelBase __instance, ref bool __result)
        {
            if (!Cheats.masterEnabled || !Cheats.forceFlawless || __result) return;
            try
            {
                if (__instance.totalOffset == 0f && __instance.numMistakes == 0f)
                    __result = true;
            }
            catch { }
        }
    }

    // Unlock everything gated behind "developer build".
    [HarmonyPatch(typeof(RDBase), nameof(RDBase.isDev), MethodType.Getter)]
    internal static class IsDevPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (Cheats.masterEnabled && Cheats.devMode) __result = true;
        }
    }

    // Widen the hit-judgment window so manual play scores Perfect even when slightly off.
    // Still used by the legacy menu's 放宽判定 slider.
    [HarmonyPatch(typeof(scnGame), nameof(scnGame.GetHitMargin))]
    internal static class HitMarginPatch
    {
        private static void Postfix(ref float __result)
        {
            if (Cheats.masterEnabled && Cheats.widenJudge)
                __result *= Mathf.Max(1f, Cheats.judgeMult);
        }
    }
}
