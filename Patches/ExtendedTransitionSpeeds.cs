using HarmonyLib;
using Panik;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Extends maximum transition speed from 4x to 16x.
    /// </summary>
    [HarmonyPatch]
    public class ExtendedTransitionSpeeds
    {
        private const int MAX_TRANSITION_SPEED = 16;

        [HarmonyPatch(typeof(MainMenuScript), "MFunc_TransitionSpeed")]
        [HarmonyPrefix]
        static bool IncreaseMaxTransitionSpeed(int _selectionDirection, bool saveSettingsWhenClosing, MainMenuScript __instance)
        {
            if (!Plugin.ExtendedTransitionSpeedsPatch.Value)
                return true;

            if (Data.settings == null)
                return true;

            Sound.Play("SoundMenuSelect");
            Data.settings.transitionSpeed += _selectionDirection;

            if (Data.settings.transitionSpeed < 1)
                Data.settings.transitionSpeed = MAX_TRANSITION_SPEED;
            else if (Data.settings.transitionSpeed > MAX_TRANSITION_SPEED)
                Data.settings.transitionSpeed = 1;

            var saveSettingsField = ReflectionCache.MainMenuScriptCache.saveSettingsOnClose;
            saveSettingsField?.SetValue(__instance, saveSettingsWhenClosing);

            return false;
        }
    }
}
