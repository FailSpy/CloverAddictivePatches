using HarmonyLib;
using System;
using System.Collections.Generic;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Skips repetitive dialogue lines. Shows bad ending dialogue once, then auto-skips.
    /// </summary>
    [HarmonyPatch]
    public class SkipRepeatedDialogue
    {
        private const string BAD_ENDING_DIALOGUE = "DIALOGUE_WELCOME_BACK_AFTER_BAD_ENDING";

        private static readonly HashSet<string> SkippedDialogues = new HashSet<string>
        {
            "DIALOGUE_1_ROUND_LEFT_WARNING",
            "DIALOGUE_WELCOME_BACK_ALT_0",
            "DIALOGUE_WELCOME_BACK_ALT_1",
            "DIALOGUE_WELCOME_BACK_ALT_2",
            "DIALOGUE_INTRO_ALT_0",
            "DIALOGUE_INTRO_ALT_1",
            "DIALOGUE_INTRO_ALT_ALT_0",
        };

        [HarmonyPatch(typeof(DialogueScript), "SetDialogue", new Type[] { typeof(bool), typeof(string[]) })]
        [HarmonyPrefix]
        static bool SkipBlacklistedDialogue(bool concatenate, string[] keys)
        {
            if (!Plugin.SkipRepetitiveWarningsPatch.Value)
                return true;

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (key == BAD_ENDING_DIALOGUE)
                    {
                        if (!Plugin.BadEndingDialogueSeen.Value)
                        {
                            Plugin.BadEndingDialogueSeen.Value = true;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (SkippedDialogues.Contains(key))
                        return false;
                }
            }

            return true;
        }
    }
}
