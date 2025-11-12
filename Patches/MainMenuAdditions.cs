using HarmonyLib;
using TMPro;
using Panik;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
    [HarmonyPatch]
    public class MainMenuAdditions
    {
        private static Plugin pluginInstance;

        public static void Initialize(Plugin instance)
        {
            pluginInstance = instance;
            pluginInstance.ModLogger.LogInfo("MainMenuAdditions initialized");
        }

        // Inject custom translations for our new menu options
        [HarmonyPatch(typeof(Translation), "Get", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        static void InjectCustomTranslations(string key, ref string __result)
        {
            // If Translation.Get didn't find the key, provide our custom translations
            if (__result.StartsWith("No string found for key:"))
            {
                var currentLanguage = Data.settings?.language ?? Translation.Language.English;

                if (key == "MENU_OPTION_SETTINGS_ACCESSIBILITY_FLASHING_LIGHTS_ON")
                {
                    // Hope the LLM was right about these lol
                    switch (currentLanguage)
                    {
                        case Translation.Language.English:
                            __result = "Flashing Reduction: On";
                            break;
                        case Translation.Language.Italian:
                            __result = "Riduzione Lampeggi: On";
                            break;
                        case Translation.Language.French:
                            __result = "Réduction des flashs : oui";
                            break;
                        case Translation.Language.German:
                            __result = "Blitzreduzierung: Ein";
                            break;
                        case Translation.Language.Spanish:
                        case Translation.Language.SpanishAmerica:
                            __result = "Reducción de destellos: Sí";
                            break;
                        case Translation.Language.Portuguese:
                        case Translation.Language.PortugueseBrazil:
                            __result = "Redução de flashes: lig.";
                            break;
                        case Translation.Language.ChineseSimplified:
                            __result = "闪光减弱：开";
                            break;
                        case Translation.Language.Japanese:
                            __result = "フラッシュ軽減：オン";
                            break;
                        case Translation.Language.Ukraine:
                            __result = "Зменшення спалахів: увімк.";
                            break;
                        case Translation.Language.Russian:
                            __result = "Снижение вспышек: вкл.";
                            break;
                        case Translation.Language.Korean:
                            __result = "깜빡임 감소: 켜기";
                            break;
                        default:
                            __result = "Flashing Reduction: On";
                            break;
                    }
                }
                else if (key == "MENU_OPTION_SETTINGS_ACCESSIBILITY_FLASHING_LIGHTS_OFF")
                {
                    switch (currentLanguage)
                    {
                        case Translation.Language.English:
                            __result = "Flashing Reduction: Off";
                            break;
                        case Translation.Language.Italian:
                            __result = "Riduzione Lampeggi: Off";
                            break;
                        case Translation.Language.French:
                            __result = "Réduction des flashs : non";
                            break;
                        case Translation.Language.German:
                            __result = "Blitzreduzierung: Aus";
                            break;
                        case Translation.Language.Spanish:
                        case Translation.Language.SpanishAmerica:
                            __result = "Reducción de destellos: No";
                            break;
                        case Translation.Language.Portuguese:
                        case Translation.Language.PortugueseBrazil:
                            __result = "Redução de flashes: desl.";
                            break;
                        case Translation.Language.ChineseSimplified:
                            __result = "闪光减弱：关";
                            break;
                        case Translation.Language.Japanese:
                            __result = "フラッシュ軽減：オフ";
                            break;
                        case Translation.Language.Ukraine:
                            __result = "Зменшення спалахів: вимк.";
                            break;
                        case Translation.Language.Russian:
                            __result = "Снижение вспышек: выкл.";
                            break;
                        case Translation.Language.Korean:
                            __result = "깜빡임 감소: 끄기";
                            break;
                        default:
                            __result = "Flashing Reduction: Off";
                            break;
                    }
                }
                else if (key.StartsWith("MENU_OPTION_SETTINGS_ACCESSIBILITY_FOV_"))
                {
                    string fovValue = key.Replace("MENU_OPTION_SETTINGS_ACCESSIBILITY_FOV_", "");

                    switch (currentLanguage)
                    {
                        case Translation.Language.English:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.Italian:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.French:
                            __result = $"FOV : {fovValue}";
                            break;
                        case Translation.Language.German:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.Spanish:
                        case Translation.Language.SpanishAmerica:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.Portuguese:
                        case Translation.Language.PortugueseBrazil:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.ChineseSimplified:
                            __result = $"视野：{fovValue}";
                            break;
                        case Translation.Language.Japanese:
                            __result = $"視野：{fovValue}";
                            break;
                        case Translation.Language.Ukraine:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.Russian:
                            __result = $"FOV: {fovValue}";
                            break;
                        case Translation.Language.Korean:
                            __result = $"FOV: {fovValue}";
                            break;
                        default:
                            __result = $"FOV: {fovValue}";
                            break;
                    }
                }
                else if (key == "MENU_OPTION_SETTINGS_MOD_OPTIONS")
                {
                    switch (currentLanguage)
                    {
                        case Translation.Language.English:
                            __result = "Mod Options";
                            break;
                        case Translation.Language.Italian:
                            __result = "Opzioni Mod";
                            break;
                        case Translation.Language.French:
                            __result = "Options du Mod";
                            break;
                        case Translation.Language.German:
                            __result = "Mod-Optionen";
                            break;
                        case Translation.Language.Spanish:
                        case Translation.Language.SpanishAmerica:
                            __result = "Opciones del Mod";
                            break;
                        case Translation.Language.Portuguese:
                        case Translation.Language.PortugueseBrazil:
                            __result = "Opções do Mod";
                            break;
                        case Translation.Language.ChineseSimplified:
                            __result = "模组选项";
                            break;
                        case Translation.Language.Japanese:
                            __result = "MODオプション";
                            break;
                        case Translation.Language.Ukraine:
                            __result = "Параметри мода";
                            break;
                        case Translation.Language.Russian:
                            __result = "Параметры мода";
                            break;
                        case Translation.Language.Korean:
                            __result = "모드 옵션";
                            break;
                        default:
                            __result = "Mod Options";
                            break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuScript), "OptionsUpdateText_Desktop", new Type[] { })]
        [HarmonyPostfix]
        static void SetFlashingLightsText(MainMenuScript __instance)
        {
            if (!Plugin.MainMenuAdditionsPatch.Value)
                return;

            var menuIndexField = AccessTools.Field(typeof(MainMenuScript), "menuIndex");
            var currentMenu = (MainMenuScript.MenuIndex)menuIndexField.GetValue(__instance);

            if (currentMenu != MainMenuScript.MenuIndex.settingsAccessiblity)
                return;

            var optionTextsField = AccessTools.Field(typeof(MainMenuScript), "optionTexts");
            var menuElementsField = AccessTools.Field(typeof(MainMenuScript), "menuElements");
            var menuControllerField = AccessTools.Field(typeof(MainMenuScript), "menuController");

            var optionTexts = (TextMeshProUGUI[])optionTextsField.GetValue(__instance);
            var menuElements = (DiegeticMenuElement[])menuElementsField.GetValue(__instance);
            var menuController = (DiegeticMenuController)menuControllerField.GetValue(__instance);

            string vanillaBackText = optionTexts != null && optionTexts.Length > 4 ? optionTexts[4].text : "Back";

            if (menuElements != null && menuElements.Length > 6)
            {
                if (Plugin.FOVAdjustmentPatch.Value)
                {
                    for (int i = 5; i <= 6; i++)
                    {
                        var element = menuElements[i];
                        element.transform.parent.gameObject.SetActive(true);
                        if (!menuController.elements.Contains(element))
                        {
                            menuController.elements.Add(element);
                            element.SetMyController(menuController);
                        }
                    }
                }
                else
                {
                    var element5 = menuElements[5];
                    element5.transform.parent.gameObject.SetActive(true);
                    if (!menuController.elements.Contains(element5))
                    {
                        menuController.elements.Add(element5);
                        element5.SetMyController(menuController);
                    }

                    var element6 = menuElements[6];
                    if (menuController.elements.Contains(element6))
                    {
                        menuController.elements.Remove(element6);
                    }
                    element6.transform.parent.gameObject.SetActive(false);
                }
            }

            if (optionTexts != null && optionTexts.Length > 6)
            {
                string flashingLightsText = "Flashing Reduction: OFF";
                if (Data.settings != null)
                {
                    var flashingLightsField = Data.settings.GetType().GetField("flashingLightsReducedEnabled");
                    bool flashingLightsEnabled = (bool)(flashingLightsField?.GetValue(Data.settings) ?? false);

                    var translationType = typeof(MainMenuScript).Assembly.GetType("Panik.Translation");
                    var translationGetMethod = translationType?.GetMethod("Get", new Type[] { typeof(string) });

                    string flashingKey = flashingLightsEnabled
                        ? "MENU_OPTION_SETTINGS_ACCESSIBILITY_FLASHING_LIGHTS_ON"
                        : "MENU_OPTION_SETTINGS_ACCESSIBILITY_FLASHING_LIGHTS_OFF";
                    flashingLightsText = (string)translationGetMethod?.Invoke(null, new object[] { flashingKey });
                }

                optionTexts[4].text = flashingLightsText;

                if (Plugin.FOVAdjustmentPatch.Value)
                {
                    int currentFOV = Mathf.RoundToInt(Plugin.PlayerFOV.Value);
                    string fovKey = $"MENU_OPTION_SETTINGS_ACCESSIBILITY_FOV_{currentFOV}";
                    string fovText = (string)typeof(MainMenuScript).Assembly.GetType("Panik.Translation")
                        ?.GetMethod("Get", new Type[] { typeof(string) })
                        ?.Invoke(null, new object[] { fovKey });

                    optionTexts[5].text = fovText;
                    optionTexts[6].text = vanillaBackText;
                }
                else
                {
                    optionTexts[5].text = vanillaBackText;
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuScript), "OptionsUpdateText_Desktop", new Type[] { })]
        [HarmonyPostfix]
        static void SetModOptionsText(MainMenuScript __instance)
        {
            if (!Plugin.MainMenuAdditionsPatch.Value)
                return;

            var menuIndexField = AccessTools.Field(typeof(MainMenuScript), "menuIndex");
            var currentMenu = (MainMenuScript.MenuIndex)menuIndexField.GetValue(__instance);

            if (currentMenu != MainMenuScript.MenuIndex.settings)
                return;

            var optionTextsField = AccessTools.Field(typeof(MainMenuScript), "optionTexts");
            var menuElementsField = AccessTools.Field(typeof(MainMenuScript), "menuElements");
            var menuControllerField = AccessTools.Field(typeof(MainMenuScript), "menuController");

            var optionTexts = (TextMeshProUGUI[])optionTextsField.GetValue(__instance);
            var menuElements = (DiegeticMenuElement[])menuElementsField.GetValue(__instance);
            var menuController = (DiegeticMenuController)menuControllerField.GetValue(__instance);

            if (menuElements != null && menuElements.Length > 4)
            {
                var element4 = menuElements[4];

                element4.transform.parent.gameObject.SetActive(true);

                if (!menuController.elements.Contains(element4))
                {
                    menuController.elements.Add(element4);
                    element4.SetMyController(menuController);
                }
            }

            if (optionTexts != null && optionTexts.Length > 4)
            {
                string vanillaBackText = optionTexts[3].text;

                var translationType = typeof(MainMenuScript).Assembly.GetType("Panik.Translation");
                var translationGetMethod = translationType?.GetMethod("Get", new Type[] { typeof(string) });
                string modOptionsText = (string)translationGetMethod?.Invoke(null, new object[] { "MENU_OPTION_SETTINGS_MOD_OPTIONS" });

                optionTexts[3].text = modOptionsText;
                optionTexts[4].text = vanillaBackText;
            }
        }

        [HarmonyPatch(typeof(MainMenuScript), "Select_Desktop")]
        [HarmonyPrefix]
        static bool InterceptMenuSelection(MainMenuScript __instance, MainMenuScript.MenuIndex _menuIndex, int selectionIndex)
        {
            if (!Plugin.MainMenuAdditionsPatch.Value)
                return true;

            // Handle Settings menu
            if (_menuIndex == MainMenuScript.MenuIndex.settings)
            {
                // Remap selections for Settings menu:
                // 0: Accessibility (unchanged)
                // 1: Video and Audio (unchanged)
                // 2: Others (unchanged)
                // 3: Mod Options (NEW)
                // 4: Back (shifted)

                if (selectionIndex == 3)
                {
                    // Mod Options clicked - open custom menu
                    Sound.Play("SoundMenuSelect", 1f, 1f);
                    OpenModOptionsMenu();
                    return false;  // Skip original
                }
                else if (selectionIndex == 4)
                {
                    // Back - manually implement vanilla Back behavior (which was at index 3)
                    var leftNavigationPressField = AccessTools.Field(typeof(MainMenuScript), "leftNavigationPress");
                    bool leftNavigationPress = (bool)(leftNavigationPressField?.GetValue(__instance) ?? false);

                    if (!leftNavigationPress)
                    {
                        Sound.Play("SoundMenuBack", 1f, 1f);
                        var menuIndexField = AccessTools.Field(typeof(MainMenuScript), "menuIndex");
                        var desiredNavigationIndexField = AccessTools.Field(typeof(MainMenuScript), "desiredNavigationIndex");
                        menuIndexField?.SetValue(__instance, MainMenuScript.MenuIndex.mainMenu);
                        desiredNavigationIndexField?.SetValue(__instance, 0);

                        // Force UI refresh
                        var optionsUpdateTextMethod = AccessTools.Method(typeof(MainMenuScript), "OptionsUpdateText_Desktop");
                        optionsUpdateTextMethod?.Invoke(__instance, null);
                    }
                    return false;  // Skip original
                }

                // Let original handle 0-2 (Accessibility, Video and Audio, Others)
                return true;
            }

            // Handle Accessibility menu
            if (_menuIndex == MainMenuScript.MenuIndex.settingsAccessiblity)
            {
                // Index mapping depends on FOV Adjustment setting:
                // When FOV Adjustment is ON:
                //   0-3: Vanilla options (Language, Text Effects, Screen Shake, Wobbly Polygons)
                //   4: Flashing Lights (NEW)
                //   5: FOV (NEW)
                //   6: Back (shifted)
                // When FOV Adjustment is OFF:
                //   0-3: Vanilla options
                //   4: Flashing Lights (NEW)
                //   5: Back (shifted)

                if (selectionIndex == 4)
                {
                    // Flashing Lights clicked - toggle the setting
                    MFunc_FlashingLights(__instance, true);
                    return false;  // Skip original
                }
                else if (selectionIndex == 5)
                {
                    if (Plugin.FOVAdjustmentPatch.Value)
                    {
                        // FOV clicked - cycle through FOV values
                        MFunc_FOV(__instance);
                        return false;  // Skip original
                    }
                    else
                    {
                        // Back button (when FOV is disabled)
                        HandleAccessibilityBackButton(__instance);
                        return false;  // Skip original
                    }
                }
                else if (selectionIndex == 6 && Plugin.FOVAdjustmentPatch.Value)
                {
                    // Back button (when FOV is enabled)
                    HandleAccessibilityBackButton(__instance);
                    return false;  // Skip original
                }

                // Let original handle 0-3 (Language, Text Effects, Screen Shake, Wobbly Polygons)
                return true;
            }

            // Don't intercept other menus
            return true;
        }

        // Helper method: Handle Back button in Accessibility menu
        private static void HandleAccessibilityBackButton(MainMenuScript instance)
        {
            var leftNavigationPressField = AccessTools.Field(typeof(MainMenuScript), "leftNavigationPress");
            bool leftNavigationPress = (bool)(leftNavigationPressField?.GetValue(instance) ?? false);

            if (!leftNavigationPress)
            {
                Sound.Play("SoundMenuBack", 1f, 1f);
                var menuIndexField = AccessTools.Field(typeof(MainMenuScript), "menuIndex");
                var desiredNavigationIndexField = AccessTools.Field(typeof(MainMenuScript), "desiredNavigationIndex");
                menuIndexField?.SetValue(instance, MainMenuScript.MenuIndex.settings);
                desiredNavigationIndexField?.SetValue(instance, 0);

                // Force UI refresh to show the new menu
                var optionsUpdateTextMethod = AccessTools.Method(typeof(MainMenuScript), "OptionsUpdateText_Desktop");
                optionsUpdateTextMethod?.Invoke(instance, null);
            }
        }

        // Open the Mod Options menu
        private static void OpenModOptionsMenu()
        {
            pluginInstance.ModLogger.LogInfo("MainMenuAdditions: Opening Mod Options menu");

            // Build menu options dynamically - hide FOV option if FOV Adjustment is disabled
            var optionsList = new System.Collections.Generic.List<string>();
            var eventsList = new System.Collections.Generic.List<ScreenMenuScript.OptionEvent>();

            // Only show FOV option if FOV Adjustment is enabled
            if (Plugin.FOVAdjustmentPatch.Value)
            {
                int currentFOV = Mathf.RoundToInt(Plugin.PlayerFOV.Value);
                optionsList.Add($"FOV: {currentFOV}");
                eventsList.Add(new ScreenMenuScript.OptionEvent(OnModOptionsFOVClick));
            }

            // Always show submenu options
            optionsList.Add("Camera & FOV Patches");
            eventsList.Add(new ScreenMenuScript.OptionEvent(OpenCameraPatches));

            optionsList.Add("Menu & UI Patches");
            eventsList.Add(new ScreenMenuScript.OptionEvent(OpenQOLMenuPatches));

            optionsList.Add("Game Flow Patches");
            eventsList.Add(new ScreenMenuScript.OptionEvent(OpenQOLSpeedPatches));

            optionsList.Add("Misc Patches");
            eventsList.Add(new ScreenMenuScript.OptionEvent(OpenQOLCameraPatches));

            // Debug options (only show when Debug patch is enabled)
            if (Plugin.DebugPatch.Value)
            {
                optionsList.Add("Debug Options");
                eventsList.Add(new ScreenMenuScript.OptionEvent(OpenDebugOptions));
            }

            // Always show Back
            optionsList.Add("Back");
            eventsList.Add(new ScreenMenuScript.OptionEvent(OnModOptionsBack));

            // Convert to arrays
            string[] options = optionsList.ToArray();
            ScreenMenuScript.OptionEvent[] events = eventsList.ToArray();

            // Back button is always the last index
            int backIndex = options.Length - 1;

            ScreenMenuScript.Open(
                true,  // resetCursor
                false, // closeOnSelect
                backIndex, // cancelOptionIndex
                ScreenMenuScript.Positioning.center,
                5f,    // extraOptionsSpacing
                $"Mod Options (v{Plugin.Version})",
                options,
                events
            );

            Sound.Play("SoundMenuPopUp");
        }

        // Handle FOV option click in Mod Options menu
        private static void OnModOptionsFOVClick()
        {
            // Cycle FOV
            float currentFOV = Plugin.PlayerFOV.Value;
            float newFOV = currentFOV + 5f;
            if (newFOV > 110f)
                newFOV = 60f;

            Plugin.PlayerFOV.Value = newFOV;

            // Close and reopen the menu to refresh the FOV display
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(ReopenModOptionsAfterFOVCoroutine());
            }
        }

        private static IEnumerator ReopenModOptionsAfterFOVCoroutine()
        {
            yield return null;

            if (GeneralUiScript.instance != null)
            {
                OpenModOptionsMenu();
            }
        }

        // Open Camera & FOV Patches submenu
        private static void OpenCameraPatches()
        {
            // Close current menu and wait before opening submenu
            ScreenMenuScript.Close(true);

            // Use GeneralUiScript to start coroutine for delayed menu open
            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenCameraPatchesCoroutine());
            }
        }

        private static IEnumerator OpenCameraPatchesCoroutine()
        {
            // Wait one frame for menu to fully close
            yield return null;

            // Build menu options dynamically - hide Dolly Zoom if Disable Vertigo Effects is enabled
            var optionsList = new System.Collections.Generic.List<string>();
            var eventsList = new System.Collections.Generic.List<ScreenMenuScript.OptionEvent>();

            // Always show FOV Adjustment
            optionsList.Add($"FOV Adjustment: {(Plugin.FOVAdjustmentPatch.Value ? "On" : "Off")}");
            eventsList.Add(new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.FOVAdjustmentPatch.Value, v => Plugin.FOVAdjustmentPatch.Value = v, OpenCameraPatches)));

            // Always show No Vertigo Inducers
            optionsList.Add($"No Vertigo Inducers: {(Plugin.NoVertigoInducersPatch.Value ? "On" : "Off")}");
            eventsList.Add(new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.NoVertigoInducersPatch.Value, v => Plugin.NoVertigoInducersPatch.Value = v, OpenCameraPatches)));

            // Always show Reduced Motion
            optionsList.Add($"Reduced Motion: {(Plugin.ReducedMotionPatch.Value ? "On" : "Off")}");
            eventsList.Add(new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.ReducedMotionPatch.Value, v => Plugin.ReducedMotionPatch.Value = v, OpenCameraPatches)));

            // Only show Dolly Zoom if No Vertigo Inducers is OFF
            if (!Plugin.NoVertigoInducersPatch.Value)
            {
                optionsList.Add($"Dolly Zoom: {(Plugin.DollyZoomPatch.Value ? "On" : "Off")}");
                eventsList.Add(new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.DollyZoomPatch.Value, v => Plugin.DollyZoomPatch.Value = v, OpenCameraPatches)));
            }

            // Always show Back
            optionsList.Add("Back");
            eventsList.Add(new ScreenMenuScript.OptionEvent(BackToModOptions));

            // Convert to arrays
            string[] options = optionsList.ToArray();
            ScreenMenuScript.OptionEvent[] events = eventsList.ToArray();

            // Back button is always the last index
            int backIndex = options.Length - 1;

            ScreenMenuScript.Open(true, false, backIndex, ScreenMenuScript.Positioning.center, 5f, "Camera & FOV Patches", options, events);
            Sound.Play("SoundMenuPopUp");
        }

        // Open QOL Menu & UI Patches submenu
        private static void OpenQOLMenuPatches()
        {
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenQOLMenuPatchesCoroutine());
            }
        }

        private static IEnumerator OpenQOLMenuPatchesCoroutine()
        {
            yield return null;

            string[] options = new string[]
            {
                $"Drawer Peek: {(Plugin.DrawerPeekPatch.Value ? "On" : "Off")}",
                $"Main Menu Camera Fix: {(Plugin.MainMenuCameraFixPatch.Value ? "On" : "Off")}",
                $"Memory Card Menu Access: {(Plugin.MemoryCardMenuAccessPatch.Value ? "On" : "Off")}",
                $"Inventory Drawer Swap: {(Plugin.InventoryDrawerSwapPatch.Value ? "On" : "Off")}",
                $"New Run Confirmation: {(Plugin.NewRunConfirmationPatch.Value ? "On" : "Off")}",
                "Back"
            };

            ScreenMenuScript.OptionEvent[] events = new ScreenMenuScript.OptionEvent[]
            {
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.DrawerPeekPatch.Value, v => Plugin.DrawerPeekPatch.Value = v, OpenQOLMenuPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.MainMenuCameraFixPatch.Value, v => Plugin.MainMenuCameraFixPatch.Value = v, OpenQOLMenuPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.MemoryCardMenuAccessPatch.Value, v => Plugin.MemoryCardMenuAccessPatch.Value = v, OpenQOLMenuPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.InventoryDrawerSwapPatch.Value, v => Plugin.InventoryDrawerSwapPatch.Value = v, OpenQOLMenuPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.NewRunConfirmationPatch.Value, v => Plugin.NewRunConfirmationPatch.Value = v, OpenQOLMenuPatches)),
                new ScreenMenuScript.OptionEvent(BackToModOptions)
            };

            ScreenMenuScript.Open(true, false, 5, ScreenMenuScript.Positioning.center, 5f, "Menu & UI Patches", options, events);
            Sound.Play("SoundMenuPopUp");
        }

        // Open QOL Speed & Skip Patches submenu
        private static void OpenQOLSpeedPatches()
        {
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenQOLSpeedPatchesCoroutine());
            }
        }

        private static IEnumerator OpenQOLSpeedPatchesCoroutine()
        {
            yield return null;

            string[] options = new string[]
            {
                $"Instant Restart: {(Plugin.InstantRestartPatch.Value ? "On" : "Off")}",
                $"Skip Repetitive Warnings: {(Plugin.SkipRepetitiveWarningsPatch.Value ? "On" : "Off")}",
                $"Skip Trapdoor Warnings: {(Plugin.SkipTrapdoorWarningsPatch.Value ? "On" : "Off")}",
                $"Extended Transition Speeds: {(Plugin.ExtendedTransitionSpeedsPatch.Value ? "On" : "Off")}",
                $"Reduce Skip Delays: {(Plugin.ReduceSkipDelaysPatch.Value ? "On" : "Off")}",
                $"Skip Intro: {(Plugin.SkipIntroPatch.Value ? "On" : "Off")}",
                "Back"
            };

            ScreenMenuScript.OptionEvent[] events = new ScreenMenuScript.OptionEvent[]
            {
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.InstantRestartPatch.Value, v => Plugin.InstantRestartPatch.Value = v, OpenQOLSpeedPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.SkipRepetitiveWarningsPatch.Value, v => Plugin.SkipRepetitiveWarningsPatch.Value = v, OpenQOLSpeedPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.SkipTrapdoorWarningsPatch.Value, v => Plugin.SkipTrapdoorWarningsPatch.Value = v, OpenQOLSpeedPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.ExtendedTransitionSpeedsPatch.Value, v => Plugin.ExtendedTransitionSpeedsPatch.Value = v, OpenQOLSpeedPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.ReduceSkipDelaysPatch.Value, v => Plugin.ReduceSkipDelaysPatch.Value = v, OpenQOLSpeedPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.SkipIntroPatch.Value, v => Plugin.SkipIntroPatch.Value = v, OpenQOLSpeedPatches)),
                new ScreenMenuScript.OptionEvent(BackToModOptions)
            };

            ScreenMenuScript.Open(true, false, 6, ScreenMenuScript.Positioning.center, 5f, "Game Flow Patches", options, events);
            Sound.Play("SoundMenuPopUp");
        }

        // Open QOL Camera & Misc Patches submenu
        private static void OpenQOLCameraPatches()
        {
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenQOLCameraPatchesCoroutine());
            }
        }

        private static IEnumerator OpenQOLCameraPatchesCoroutine()
        {
            yield return null;

            string[] options = new string[]
            {
                $"Controller Fix: {(Plugin.ControllerFixPatch.Value ? "On" : "Off")}",
                $"ATM Cutscene Freeroam: {(Plugin.ATMCutsceneFreeroamPatch.Value ? "On" : "Off")}",
                $"Smart Deposit: {(Plugin.SmartDepositPatch.Value ? "On" : "Off")}",
                $"Quiet Drawers: {(Plugin.QuietDrawersPatch.Value ? "On" : "Off")}",
                "Back"
            };

            ScreenMenuScript.OptionEvent[] events = new ScreenMenuScript.OptionEvent[]
            {
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.ControllerFixPatch.Value, v => Plugin.ControllerFixPatch.Value = v, OpenQOLCameraPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.ATMCutsceneFreeroamPatch.Value, v => Plugin.ATMCutsceneFreeroamPatch.Value = v, OpenQOLCameraPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.SmartDepositPatch.Value, v => Plugin.SmartDepositPatch.Value = v, OpenQOLCameraPatches)),
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.QuietDrawersPatch.Value, v => Plugin.QuietDrawersPatch.Value = v, OpenQOLCameraPatches)),
                new ScreenMenuScript.OptionEvent(BackToModOptions)
            };

            ScreenMenuScript.Open(true, false, 4, ScreenMenuScript.Positioning.center, 5f, "Misc Patches", options, events);
            Sound.Play("SoundMenuPopUp");
        }

        // Open Debug Options submenu
        private static void OpenDebugOptions()
        {
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenDebugOptionsCoroutine());
            }
        }

        private static IEnumerator OpenDebugOptionsCoroutine()
        {
            yield return null;

            string[] options = new string[]
            {
                $"Hide UI: {(Plugin.HideCoinsTicketsUI.Value ? "On" : "Off")}",
                "Back"
            };

            ScreenMenuScript.OptionEvent[] events = new ScreenMenuScript.OptionEvent[]
            {
                new ScreenMenuScript.OptionEvent(() => TogglePatch(() => Plugin.HideCoinsTicketsUI.Value, v => Plugin.HideCoinsTicketsUI.Value = v, OpenDebugOptions)),
                new ScreenMenuScript.OptionEvent(BackToModOptions)
            };

            ScreenMenuScript.Open(true, false, 2, ScreenMenuScript.Positioning.center, 5f, "Debug Options", options, events);
            Sound.Play("SoundMenuPopUp");
        }

        // Back to Debug Options
        private static void BackToDebugOptions()
        {
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(BackToDebugOptionsCoroutine());
            }
        }

        private static IEnumerator BackToDebugOptionsCoroutine()
        {
            yield return null;

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenDebugOptionsCoroutine());
            }
        }

        // Go back to Mod Options menu from submenu
        private static void BackToModOptions()
        {
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(BackToModOptionsCoroutine());
            }
        }

        private static IEnumerator BackToModOptionsCoroutine()
        {
            yield return null;

            if (GeneralUiScript.instance != null)
            {
                OpenModOptionsMenu();
            }
        }

        // Generic toggle patch helper
        private static void TogglePatch(System.Func<bool> getter, System.Action<bool> setter, System.Action reopenMenu)
        {
            bool currentValue = getter();
            setter(!currentValue);

            // Close and reopen the menu to refresh the display
            ScreenMenuScript.Close(true);

            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(ReopenMenuCoroutine(reopenMenu));
            }
        }

        private static IEnumerator ReopenMenuCoroutine(System.Action reopenMenu)
        {
            yield return null;
            reopenMenu();
        }

        // Handle Back button click in Mod Options menu
        private static void OnModOptionsBack()
        {
            // Close the menu and reset cursor to ensure full cleanup
            ScreenMenuScript.Close(true);
            Sound.Play("SoundMenuBack");
        }

        // Helper method: Toggle Flashing Lights setting (mirrors vanilla pattern)
        private static void MFunc_FlashingLights(MainMenuScript instance, bool saveSettingsWhenClosing)
        {
            // Play menu select sound
            Sound.Play("SoundMenuSelect", 1f, 1f);

            // Access Data.settings directly (Data is in Panik namespace)
            if (Data.settings != null)
            {
                // Use reflection to access flashingLightsReducedEnabled field
                var flashingLightsField = Data.settings.GetType().GetField("flashingLightsReducedEnabled");

                if (flashingLightsField != null)
                {
                    bool currentValue = (bool)flashingLightsField.GetValue(Data.settings);
                    bool newValue = !currentValue;
                    flashingLightsField.SetValue(Data.settings, newValue);

                    // Manually update the text at position 4 (to avoid cursor reset)
                    var optionTextsField = AccessTools.Field(typeof(MainMenuScript), "optionTexts");
                    var optionTexts = (TextMeshProUGUI[])optionTextsField?.GetValue(instance);

                    if (optionTexts != null && optionTexts.Length > 4)
                    {
                        // Use Translation.Get for localization
                        var translationType = typeof(MainMenuScript).Assembly.GetType("Panik.Translation");
                        var translationGetMethod = translationType?.GetMethod("Get", new Type[] { typeof(string) });

                        string flashingKey = newValue
                            ? "MENU_OPTION_SETTINGS_ACCESSIBILITY_FLASHING_LIGHTS_ON"
                            : "MENU_OPTION_SETTINGS_ACCESSIBILITY_FLASHING_LIGHTS_OFF";
                        string flashingLightsText = (string)translationGetMethod?.Invoke(null, new object[] { flashingKey });

                        optionTexts[4].text = flashingLightsText;
                    }
                }
            }

            // Set save flag so settings are saved when menu closes
            var saveSettingsOnCloseField = AccessTools.Field(typeof(MainMenuScript), "saveSettingsOnClose");
            saveSettingsOnCloseField?.SetValue(instance, saveSettingsWhenClosing);
        }

        // Helper method: Cycle through FOV values (60 -> 65 -> ... -> 110 -> 60)
        private static void MFunc_FOV(MainMenuScript instance)
        {
            // Play menu select sound
            Sound.Play("SoundMenuSelect", 1f, 1f);

            // Get current FOV from mod config
            float currentFOV = Plugin.PlayerFOV.Value;

            // Increment by 5, with wraparound from 110 back to 60
            float newFOV = currentFOV + 5f;
            if (newFOV > 110f)
                newFOV = 60f;

            // Update the config value (automatically saved by BepInEx)
            Plugin.PlayerFOV.Value = newFOV;

            // Manually update the text at position 5 (to avoid cursor reset)
            var optionTextsField = AccessTools.Field(typeof(MainMenuScript), "optionTexts");
            var optionTexts = (TextMeshProUGUI[])optionTextsField?.GetValue(instance);

            if (optionTexts != null && optionTexts.Length > 5)
            {
                // Use Translation.Get for localization
                var translationType = typeof(MainMenuScript).Assembly.GetType("Panik.Translation");
                var translationGetMethod = translationType?.GetMethod("Get", new Type[] { typeof(string) });

                int fovValue = Mathf.RoundToInt(newFOV);
                string fovKey = $"MENU_OPTION_SETTINGS_ACCESSIBILITY_FOV_{fovValue}";
                string fovText = (string)translationGetMethod?.Invoke(null, new object[] { fovKey });

                optionTexts[5].text = fovText;
            }
        }
    }
}
