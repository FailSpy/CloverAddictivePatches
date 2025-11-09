using HarmonyLib;
using Panik;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Skips trapdoor shake cutscene during interests/tickets phase.
    /// </summary>
    [HarmonyPatch]
    public class DisableInterestsCutscene
    {
        [HarmonyPatch(typeof(CameraController), "SetPosition")]
        [HarmonyPrefix]
        static bool SkipTrapdoorCamera(CameraController.PositionKind kind)
        {
            if (!Plugin.SkipTrapdoorWarningsPatch.Value)
                return true;

            if (kind == CameraController.PositionKind.TrapDoor &&
                GameplayMaster.GetGamePhase() == GameplayMaster.GamePhase.cutscene)
            {
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(GameplayMaster), "CutscenePhaseBehaviour")]
        [HarmonyPostfix]
        static void SkipTrapdoorWaitTimer(GameplayMaster __instance)
        {
            if (!Plugin.SkipTrapdoorWarningsPatch.Value)
                return;

            var shakedField = ReflectionCache.GameplayMasterCache.intAndTickets_ShakedTrapdoor;
            var phaseField = ReflectionCache.GameplayMasterCache.interestsAndTicketsPhase;
            var timerField = ReflectionCache.GameplayMasterCache.interestsAndTicketsTimer;
            var delayField = ReflectionCache.GameplayMasterCache.delay;

            if (shakedField == null || phaseField == null || timerField == null || delayField == null)
                return;

            bool alreadyShaken = (bool)shakedField.GetValue(__instance);
            object currentPhase = phaseField.GetValue(__instance);

            if (alreadyShaken && currentPhase != null &&
                currentPhase.Equals(ReflectionCache.GameplayMasterCache.InterestsPhase_shakeTrapdoor_Optional))
            {
                phaseField.SetValue(__instance, ReflectionCache.GameplayMasterCache.InterestsPhase_done);
                timerField.SetValue(__instance, 0f);
                delayField.SetValue(__instance, 0f);
            }
        }
    }
}
