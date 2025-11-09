using HarmonyLib;
using Panik;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Reduces delay before dialogue/cutscenes can be skipped.
    /// </summary>
    [HarmonyPatch]
    public class ReduceSkipDelays
    {
        private const float REDUCED_DIALOGUE_DELAY = 0.1f;
        private const float REDUCED_QUESTION_DELAY = 0.1f;
        private const float REDUCED_CUTSCENE_DELAY = 0.1f;
        private const float REDUCED_INTERESTS_DELAY = 0.1f;
        private const float REDUCED_TICKETS_DELAY = 0.05f;
        private const float REDUCED_TRAPDOOR_DELAY = 0.3f;

        private static bool IsSkipInputPressed()
        {
            return Controls.ActionButton_PressedGet(0, Controls.InputAction.menuSelect) ||
                   Controls.ActionButton_PressedGet(0, Controls.InputAction.menuBack) ||
                   UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Space) ||
                   UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
                   UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter) ||
                   UnityEngine.Input.GetMouseButtonDown(0) ||
                   UnityEngine.Input.GetMouseButtonDown(1);
        }

        [HarmonyPatch(typeof(DialogueScript), "SetDialogueInputDelay")]
        [HarmonyPrefix]
        static bool ReduceDialogueInputDelay(ref float value)
        {
            if (!Plugin.ReduceSkipDelaysPatch.Value)
                return true;

            // Don't reduce 0.1s delays set when answering questions
            if (value >= 0.5f)
                value = REDUCED_DIALOGUE_DELAY;

            return true;
        }

        [HarmonyPatch(typeof(DialogueScript), "SetQuestionDialogue")]
        [HarmonyPostfix]
        static void ReduceQuestionDelay(DialogueScript __instance)
        {
            if (!Plugin.ReduceSkipDelaysPatch.Value)
                return;

            var questionDelayField = AccessTools.Field(typeof(DialogueScript), "questionDelay");
            if (questionDelayField != null)
            {
                float currentDelay = (float)questionDelayField.GetValue(__instance);

                if (currentDelay >= 0.5f)
                    questionDelayField.SetValue(__instance, REDUCED_QUESTION_DELAY);
            }
        }

        [HarmonyPatch(typeof(GameplayMaster), "CutscenePhaseBehaviour")]
        [HarmonyPostfix]
        static void ReduceCutscenePhaseDelay(GameplayMaster __instance)
        {
            if (!Plugin.ReduceSkipDelaysPatch.Value)
                return;

            var delayField = AccessTools.Field(typeof(GameplayMaster), "delay");
            if (delayField == null)
                return;

            float currentDelay = (float)delayField.GetValue(__instance);

            if (currentDelay >= 0.5f)
            {
                delayField.SetValue(__instance, REDUCED_CUTSCENE_DELAY);
                return;
            }

            // Don't skip if elements have their own skip mechanisms
            bool canSkip = !DialogueScript.IsEnabled() &&
                          !PowerupTriggerAnimController.HasAnimations() &&
                          !ATMScript.DebtClearCutsceneIsPlaying() &&
                          !ScreenMenuScript.IsEnabled() &&
                          !TutorialScript.IsEnabled() &&
                          !DeckBoxUI.IsEnabled();

            if (!canSkip)
                return;

            if (IsSkipInputPressed() && currentDelay > 0f)
            {
                delayField.SetValue(__instance, 0f);
            }
        }

        [HarmonyPatch(typeof(GameplayMaster), "CutscenePhaseBehaviour")]
        [HarmonyPrefix]
        static void ReduceInterestsAndTicketsDelays(GameplayMaster __instance)
        {
            if (!Plugin.ReduceSkipDelaysPatch.Value)
                return;

            var timerField = AccessTools.Field(typeof(GameplayMaster), "interestsAndTicketsTimer");
            if (timerField == null)
                return;

            float currentTimer = (float)timerField.GetValue(__instance);

            if (currentTimer >= 1.4f && currentTimer <= 1.6f)
            {
                timerField.SetValue(__instance, REDUCED_TRAPDOOR_DELAY);
                return;
            }
            else if (currentTimer >= 0.45f && currentTimer <= 0.55f)
            {
                timerField.SetValue(__instance, REDUCED_INTERESTS_DELAY);
                return;
            }
            else if (currentTimer >= 0.2f && currentTimer <= 0.3f)
            {
                timerField.SetValue(__instance, REDUCED_TICKETS_DELAY);
                return;
            }

            // Set to -1.0 to bypass input blocking checks
            if (IsSkipInputPressed() && currentTimer > 0f)
            {
                timerField.SetValue(__instance, -1.0f);
            }
        }
    }
}
