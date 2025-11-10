using HarmonyLib;
using Panik;
using System.Collections;
using System.Reflection;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Confirms new run start if save has progress past first deadline.
    /// </summary>
    [HarmonyPatch]
    public class NewRunConfirmation
    {
        private static Plugin pluginInstance;
        private static bool confirmationPending = false;
        private static GeneralUiScript storedGeneralUiScript = null;

        public static void Initialize(Plugin instance)
        {
            pluginInstance = instance;
            pluginInstance.ModLogger.LogInfo("NewRunConfirmation initialized");
        }

        [HarmonyPatch(typeof(GeneralUiScript), "_IntroMenuNewGame")]
        [HarmonyPrefix]
        static bool NewGame_Prefix(GeneralUiScript __instance)
        {
            if (!Plugin.NewRunConfirmationPatch.Value)
                return true;

            if (confirmationPending)
            {
                confirmationPending = false;
                pluginInstance.ModLogger.LogInfo("NewRunConfirmation: Proceeding with new game after confirmation");
                return true;
            }

            int roundsPlayed = GameplayData.RoundsOfDeadline_PlayedGet();
            long deadlinesCompleted = GameplayData.Stats_DeadlinesCompleted_Get();
            int equippedPowerups = PowerupScript.list_EquippedNormal.Count;

            pluginInstance.ModLogger.LogInfo($"NewRunConfirmation: Checking progress - roundsPlayed={roundsPlayed}, deadlinesCompleted={deadlinesCompleted}, equippedPowerups={equippedPowerups}");

            if (roundsPlayed == 0 && deadlinesCompleted == 0 && equippedPowerups == 0)
            {
                pluginInstance.ModLogger.LogInfo("NewRunConfirmation: No progress detected, allowing new game without confirmation");
                return true;
            }

            pluginInstance.ModLogger.LogInfo("NewRunConfirmation: Progress detected, blocking new game to show confirmation");
            storedGeneralUiScript = __instance;
            return false;
        }

        [HarmonyPatch(typeof(GeneralUiScript), "_IntroMenuNewGame")]
        [HarmonyPostfix]
        static void NewGame_Postfix(GeneralUiScript __instance)
        {
            if (storedGeneralUiScript != null && !confirmationPending)
            {
                pluginInstance.ModLogger.LogInfo("NewRunConfirmation: Postfix showing confirmation");
                ShowConfirmationDelayed();
            }
        }

        private static void ShowConfirmationDelayed()
        {
            storedGeneralUiScript.StartCoroutine(ShowConfirmationCoroutine());
        }

        private static IEnumerator ShowConfirmationCoroutine()
        {
            yield return null;

            if (storedGeneralUiScript == null)
            {
                pluginInstance.ModLogger.LogWarning("NewRunConfirmation: GeneralUiScript was destroyed before confirmation");
                confirmationPending = false;
                yield break;
            }

            pluginInstance.ModLogger.LogInfo($"NewRunConfirmation: Coroutine starting, menu enabled: {ScreenMenuScript.IsEnabled()}");

            ShowConfirmation(storedGeneralUiScript);

            storedGeneralUiScript = null;
        }

        private static void ShowConfirmation(GeneralUiScript generalUiScript)
        {
            pluginInstance.ModLogger.LogInfo("NewRunConfirmation: ShowConfirmation called");

            storedGeneralUiScript = generalUiScript;

            ScreenMenuScript.Close(false);
            pluginInstance.ModLogger.LogInfo($"NewRunConfirmation: Closed existing menu, IsEnabled={ScreenMenuScript.IsEnabled()}");

            string[] options = new string[2]
            {
                "No, Load Existing Run",
                "Yes, Start New Run"
            };

            ScreenMenuScript.OptionEvent[] events = new ScreenMenuScript.OptionEvent[2]
            {
                new ScreenMenuScript.OptionEvent(OnCancel),
                new ScreenMenuScript.OptionEvent(() => OnConfirm(generalUiScript))
            };

            pluginInstance.ModLogger.LogInfo("NewRunConfirmation: Opening confirmation menu");

            ScreenMenuScript.Open(
                true,
                true,
                0,
                ScreenMenuScript.Positioning.center,
                5f,
                "Overwrite Current Run?",
                options,
                events
            );

            pluginInstance.ModLogger.LogInfo($"NewRunConfirmation: Called ScreenMenuScript.Open, IsEnabled={ScreenMenuScript.IsEnabled()}");
            Sound.Play("SoundMenuPopUp");
        }

        private static void OnCancel()
        {
            confirmationPending = false;
            pluginInstance.ModLogger.LogInfo("NewRunConfirmation: User canceled");

            ReopenRunSelectionMenu();

            storedGeneralUiScript = null;
        }

        private static void ReopenRunSelectionMenu()
        {
            bool hasOldSession = GameplayData.NewGameIntroFinished_Get();
            bool canInputSeed = GameplayMaster.CanInputSeed();

            if (!hasOldSession && !canInputSeed)
            {
                return;
            }

            var generalUiScript = storedGeneralUiScript != null ? storedGeneralUiScript : GeneralUiScript.instance;

            string[] options;
            ScreenMenuScript.OptionEvent[] optionEventArray;

            if (hasOldSession)
            {
                string input = Translation.Get("SCREEN_MENU_OPTION_NEW_RUN_SEEDED");
                if (!canInputSeed)
                    input += " <sprite name=\"RedLock\">";

                options = new string[3]
                {
                    Strings.Sanitize(Strings.SantizationKind.menus, Translation.Get("SCREEN_MENU_OPTION_CONTINUE")),
                    Strings.Sanitize(Strings.SantizationKind.menus, Translation.Get("SCREEN_MENU_OPTION_NEW_RUN")),
                    Strings.Sanitize(Strings.SantizationKind.menus, input)
                };

                var continueMethod = typeof(GeneralUiScript).GetMethod("_IntroMenuContinue", BindingFlags.Instance | BindingFlags.NonPublic);
                var newGameMethod = typeof(GeneralUiScript).GetMethod("_IntroMenuNewGame", BindingFlags.Instance | BindingFlags.NonPublic);
                var seededGameMethod = typeof(GeneralUiScript).GetMethod("_IntroMenuNewSeededGame", BindingFlags.Instance | BindingFlags.NonPublic);

                optionEventArray = new ScreenMenuScript.OptionEvent[3]
                {
                    new ScreenMenuScript.OptionEvent(() => continueMethod?.Invoke(generalUiScript, null)),
                    new ScreenMenuScript.OptionEvent(() => newGameMethod?.Invoke(generalUiScript, null)),
                    new ScreenMenuScript.OptionEvent(() => seededGameMethod?.Invoke(generalUiScript, null))
                };
            }
            else
            {
                options = new string[2]
                {
                    Strings.Sanitize(Strings.SantizationKind.menus, Translation.Get("SCREEN_MENU_OPTION_NEW_RUN")),
                    Strings.Sanitize(Strings.SantizationKind.menus, Translation.Get("SCREEN_MENU_OPTION_NEW_RUN_SEEDED"))
                };

                var newGameMethod = typeof(GeneralUiScript).GetMethod("_IntroMenuNewGame", BindingFlags.Instance | BindingFlags.NonPublic);
                var seededGameMethod = typeof(GeneralUiScript).GetMethod("_IntroMenuNewSeededGame", BindingFlags.Instance | BindingFlags.NonPublic);

                optionEventArray = new ScreenMenuScript.OptionEvent[2]
                {
                    new ScreenMenuScript.OptionEvent(() => newGameMethod?.Invoke(generalUiScript, null)),
                    new ScreenMenuScript.OptionEvent(() => seededGameMethod?.Invoke(generalUiScript, null))
                };
            }

            ScreenMenuScript.Open(true, false, -1, ScreenMenuScript.Positioning.center, 5f, Translation.Get("SCREEN_MENU_TITLE_RUN"), options, optionEventArray);
            Sound.Play("SoundMenuPopUp");
        }

        private static void OnConfirm(GeneralUiScript generalUiScript)
        {
            confirmationPending = true;
            pluginInstance.ModLogger.LogInfo("NewRunConfirmation: User confirmed, proceeding with new game");

            storedGeneralUiScript = null;

            if (generalUiScript == null)
            {
                pluginInstance.ModLogger.LogWarning("NewRunConfirmation: GeneralUiScript null in OnConfirm");
                confirmationPending = false;
                return;
            }

            var newGameMethod = typeof(GeneralUiScript).GetMethod("_IntroMenuNewGame", BindingFlags.Instance | BindingFlags.NonPublic);
            if (newGameMethod != null)
            {
                newGameMethod.Invoke(generalUiScript, null);
            }
        }
    }
}
