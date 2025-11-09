using HarmonyLib;
using Panik;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Disables vertigo-inducing effects: dolly zoom, scary look, death animations, FOV changes.
    /// </summary>
    [HarmonyPatch]
    public class DisableVertigoEffects
    {
        [HarmonyPatch(typeof(CameraController), "LookUpDown_ScaryRoutine")]
        [HarmonyPrefix]
        static bool DisableLookUpDownScary()
        {
            if (!Plugin.NoVertigoInducersPatch.Value)
                return true;

            return false;
        }

        // Returns 0 for all FOV modifiers (slot machine, scary moments, etc)
        [HarmonyPatch(typeof(CameraGame), "FieldOfViewExtraGet")]
        [HarmonyPrefix]
        static bool OverrideFOVExtraGet(ref float __result)
        {
            if (!Plugin.NoVertigoInducersPatch.Value)
                return true;

            __result = 0f;
            return false;
        }

        // Changes countdown deaths to instant falling (doesn't interfere with restart deaths)
        [HarmonyPatch(typeof(GameplayMaster), "DieTry")]
        [HarmonyPrefix]
        static void SkipCountdownDeathAnimation(ref object initialDeathStep)
        {
            if (!Plugin.NoVertigoInducersPatch.Value)
                return;

            bool isCountdownDeath = initialDeathStep != null &&
                initialDeathStep.Equals(ReflectionCache.GameplayMasterCache.DeathStep_lookAtAtm) &&
                !GameplayMaster.restartQuickDeath;

            if (isCountdownDeath)
            {
                if (DeathHandlingUtils.InterceptDeathStepToFalling(ref initialDeathStep, ReflectionCache.GameplayMasterCache.DeathStep_lookAtAtm))
                {
                    DeathHandlingUtils.VertigoDeathIntercepted = true;
                }
            }
        }

        // Handles falling step for countdown deaths - black screen, sounds, stats
        [HarmonyPatch(typeof(GameplayMaster), "DeathPhaseBehaviour")]
        [HarmonyPrefix]
        static bool InstantFallingTransition(GameplayMaster __instance)
        {
            if (!Plugin.NoVertigoInducersPatch.Value)
                return true;

            if (!DeathHandlingUtils.VertigoDeathIntercepted)
                return true;

            bool wasHandled = DeathHandlingUtils.HandleInstantFallingStep(__instance, CameraController.instance);
            return !wasHandled;
        }
    }
}
