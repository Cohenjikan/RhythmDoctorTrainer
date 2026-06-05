using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RDTrainer
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
        public static bool noFail = false;          // skip FailLevel
        public static bool unlimitedFps = false;    // DebugSettings.UnlimitedFramerate
        public static bool muteBeatSounds = false;  // DebugSettings.BeatSounds = !this

        // ---- developer ----
        public static bool devMode = false;         // RDBase.isDev -> true
        public static bool debugMode = false;       // DebugSettings.Debug (shows debug overlay)
        public static bool noAchievements = false;  // DebugSettings.GiveAchievements = !this
        public static bool samuraiMode = false;     // RDString.samuraiMode

        // ---- advanced (calibration, edited then applied) ----
        public static float calV, calI, calIP2, calLat;
    }

    // Force autoplay's flawless/JCI marker on a clean run (game suppresses it when autoplay is on).
    [HarmonyPatch(typeof(LevelBase), nameof(LevelBase.isZeroOffset), MethodType.Getter)]
    internal static class FlawlessPatch
    {
        private static void Postfix(LevelBase __instance, ref bool __result)
        {
            if (!Cheats.forceFlawless || __result) return;
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
            if (Cheats.devMode) __result = true;
        }
    }

    // Widen the hit-judgment window so manual play scores Perfect even when slightly off.
    [HarmonyPatch(typeof(scnGame), nameof(scnGame.GetHitMargin))]
    internal static class HitMarginPatch
    {
        private static void Postfix(ref float __result)
        {
            if (Cheats.widenJudge)
                __result *= Mathf.Max(1f, Cheats.judgeMult);
        }
    }

    // No-fail: neutralise every LevelBase.FailLevel(...) override across the game assembly.
    [HarmonyPatch]
    internal static class NoFailPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var list = new List<MethodBase>();
            Type[] types;
            try { types = typeof(LevelBase).Assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types; }

            foreach (var t in types)
            {
                if (t == null) continue;
                MethodInfo m = null;
                try
                {
                    m = t.GetMethod("FailLevel",
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }
                catch { }
                if (m != null && !m.IsAbstract && m.GetParameters().Length == 1)
                    list.Add(m);
            }
            return list;
        }

        // No __result here on purpose: FailLevel comes in both bool (LevelBase family) and
        // void (scnGame) forms. Returning false skips the original for BOTH; the bool ones
        // then default to false (= not failed), which is exactly what we want.
        private static bool Prefix()
        {
            return !Cheats.noFail;
        }
    }
}
