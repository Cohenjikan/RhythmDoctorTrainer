using HarmonyLib;
using UnityEngine;

namespace RDTrainerMac
{
    // Shared runtime state, read by Update(), OnGUI() and the Harmony patches below.
    internal static class Cheats
    {
        // ---- normal player ----
        public static bool autoplay = false;       // DebugSettings.Auto in gameplay
        public static bool forceFlawless = true;    // keep JCI/flawless marker on a clean autoplay run
        public static bool speedOverride = false;   // RDTime.speed
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

        // ---- practice / overlay (v2.40-mf) ----
        public static bool autoRestartOnMiss = false; // restart level as soon as numMistakes > 0
        public static bool keyOverlay = false;        // on-screen pressed-keys window
        // hotkeys F4 (win level) / F5 (quick restart) are always on, no toggle

        // ---- master switch / UI (v2.40-mf2) ----
        // 修改器总开关，默认开启。关闭时 Update()/ApplyState() 跳过全部功能，并一次性把曾持续
        // 接管的开关（DebugSettings.Auto / 变速 / 武士模式 …）还原，把控制权交还游戏/编辑器——
        // 否则我们每帧改写，会导致编辑器自带的 Autoplay 等开关点不动。不持久化，每次启动默认开。
        public static bool masterEnabled = true;
        // 左上角提示窗口是否显示。有人反馈一直挂在左上角碍眼，给个开关；用 PlayerPrefs 记忆，重启保持。
        public static bool showHintWindow = true;

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
