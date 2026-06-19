using UnityEngine;
using HarmonyLib;

namespace RDTrainer
{
    // (Cheats runtime state lives in the shared Cheats.cs, compiled into both builds.)

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
