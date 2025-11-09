using HarmonyLib;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Skips trapdoor look animation when restarting via menu or R button.
    /// </summary>
    [HarmonyPatch]
    public class InstantRestartDeath
    {
        // Changes restart deaths (menu/R button) to instant falling step
        [HarmonyPatch(typeof(GameplayMaster), "DieTry")]
        [HarmonyPrefix]
        static void SkipRestartDeathAnimation(ref object initialDeathStep)
        {
            if (!Plugin.InstantRestartPatch.Value)
                return;

            bool isMenuRestart = initialDeathStep != null &&
                initialDeathStep.Equals(ReflectionCache.GameplayMasterCache.DeathStep_lookAtTrapdoor);

            bool isRButtonRestart = initialDeathStep != null &&
                initialDeathStep.Equals(ReflectionCache.GameplayMasterCache.DeathStep_lookAtAtm) &&
                GameplayMaster.restartQuickDeath;

            if (isMenuRestart || isRButtonRestart)
            {
                if (DeathHandlingUtils.InterceptDeathStepToFalling(ref initialDeathStep, initialDeathStep))
                {
                    DeathHandlingUtils.RestartDeathIntercepted = true;
                    DeathHandlingUtils.IsRButtonRestart = isRButtonRestart;
                }
            }
        }

        // Handles falling step for restart deaths - black screen, sounds, stats
        [HarmonyPatch(typeof(GameplayMaster), "DeathPhaseBehaviour")]
        [HarmonyPrefix]
        static bool InstantFallingTransition(GameplayMaster __instance)
        {
            if (!Plugin.InstantRestartPatch.Value)
                return true;

            if (!DeathHandlingUtils.RestartDeathIntercepted)
                return true;

            bool wasHandled = DeathHandlingUtils.HandleInstantFallingStep(__instance, CameraController.instance);
            return !wasHandled;
        }
    }
}
