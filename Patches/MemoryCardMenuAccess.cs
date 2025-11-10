using HarmonyLib;
using Panik;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Opens Main Menu during Memory Card selection without forcing card consumption.
    /// Hides DeckBoxUI while menu is open, blocks card selection, prevents camera changes.
    /// </summary>
    [HarmonyPatch]
    public class MemoryCardMenuAccess
    {
        private static bool deckBoxMenuActive = false;
        private static bool menuWasOpen = false;
        private static CameraController.PositionKind savedCameraPosition;
        private static float inputCooldown = 0f;
        private static float restorationCooldown = 0f;

        [HarmonyPatch(typeof(DeckBoxUI), "Update")]
        [HarmonyPostfix]
        static void AllowMenuDuringCardSelection()
        {
            if (!Plugin.MemoryCardMenuAccessPatch.Value)
                return;

            if (inputCooldown > 0f)
                inputCooldown -= Tick.Time;
            if (restorationCooldown > 0f)
                restorationCooldown -= Tick.Time;

            if (deckBoxMenuActive && menuWasOpen && !MainMenuScript.IsEnabled())
            {
                var gamePhase = GameplayMaster.GetGamePhase();
                bool shouldSkipRestore = gamePhase == GameplayMaster.GamePhase.death ||
                                        gamePhase == GameplayMaster.GamePhase.closingGame ||
                                        GameplayMaster.GameIsResetting();

                if (shouldSkipRestore)
                {
                    if (Plugin.Instance != null)
                    {
                        Plugin.Instance.ModLogger.LogInfo($"MemoryCardMenuAccess: Game phase is {gamePhase}, clearing state without restoring DeckBoxUI");
                    }

                    deckBoxMenuActive = false;
                    menuWasOpen = false;
                    restorationCooldown = 0f;
                    inputCooldown = 0f;
                }
                else
                {
                    RestoreDeckBoxUI();
                }
            }

            if (deckBoxMenuActive && MainMenuScript.IsEnabled())
                menuWasOpen = true;

            // Keep cursor visible - fights against other systems hiding it
            if (DeckBoxUI.IsEnabled() && DeckBoxUI.IsPickingCard(true) && !MainMenuScript.IsEnabled())
            {
                if (!VirtualCursors.CursorDesiredVisibilityGet(0))
                    VirtualCursors.CursorDesiredVisibilitySet(0, true);
            }

            if (!DeckBoxUI.IsEnabled() || !DeckBoxUI.IsPickingCard(true) || MainMenuScript.IsEnabled() || inputCooldown > 0f)
                return;

            if (Controls.ActionButton_PressedGet(0, Controls.InputAction.menuPause))
            {
                if (GameplayMaster.instance == null)
                    return;

                savedCameraPosition = CameraController.GetPositionKind();
                CardsInspectorScript.Close();

                if (DeckBoxUI.instance != null && DeckBoxUI.instance.holder != null)
                {
                    DeckBoxUI.instance.holder.SetActive(false);
                    deckBoxMenuActive = true;
                }

                GameplayMaster.instance.FCall_MenuDrawer_MainMenu_OpenTry();
                CameraController.SetPosition(savedCameraPosition, false, 0f);
            }
        }

        // Locks camera to DeckBox position while menu is open and during restoration
        [HarmonyPatch(typeof(CameraController), "SetPosition")]
        [HarmonyPrefix]
        static bool PreventCameraChangesDuringDeckBoxMenu(CameraController.PositionKind kind)
        {
            if (!Plugin.MemoryCardMenuAccessPatch.Value)
                return true;

            if (deckBoxMenuActive && MainMenuScript.IsEnabled())
            {
                if (kind == savedCameraPosition)
                    return true;
                return false;
            }

            if (restorationCooldown > 0f)
            {
                if (kind == savedCameraPosition)
                    return true;
                return false;
            }

            return true;
        }

        // Prevents menu clicks from also triggering card selection underneath
        [HarmonyPatch(typeof(DeckBoxUI), "Select", new System.Type[] { typeof(DeckBoxUI.UiKind), typeof(UnityEngine.RectTransform) })]
        [HarmonyPrefix]
        static bool BlockDeckBoxSelectionDuringMenu(ref bool __result)
        {
            if (!Plugin.MemoryCardMenuAccessPatch.Value)
                return true;

            if (deckBoxMenuActive && MainMenuScript.IsEnabled())
            {
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(MainMenuScript), "Close")]
        [HarmonyPrefix]
        static void DetectMenuClose()
        {
            if (!Plugin.MemoryCardMenuAccessPatch.Value || !deckBoxMenuActive)
                return;

            if (Plugin.Instance != null)
                Plugin.Instance.ModLogger.LogInfo("MemoryCardMenuAccess: Menu.Close() called - DeckBoxUI will be restored on next Update");
        }

        // Re-enables DeckBoxUI and fights cursor visibility issues
        private static void RestoreDeckBoxUI()
        {
            if (Plugin.Instance != null)
                Plugin.Instance.ModLogger.LogInfo("MemoryCardMenuAccess: Restoring DeckBoxUI state");

            if (DeckBoxUI.instance != null && DeckBoxUI.instance.holder != null)
            {
                DeckBoxUI.instance.holder.SetActive(true);

                // Set cursor visible multiple times - fights against systems hiding it
                VirtualCursors.CursorDesiredVisibilitySet(0, true);
                CameraController.SetPosition(savedCameraPosition, false, 0f);
                VirtualCursors.CursorDesiredVisibilitySet(0, true);

                CardsInspectorScript.Open(
                    "CARDS_INSPECTOR_TITLE__UNDISCOVERED",
                    "CARDS_INSPECTOR_DESCRIPTION__UNDISCOVERED",
                    CardsInspectorScript.PromptKind.none
                );

                VirtualCursors.CursorDesiredVisibilitySet(0, true);

                inputCooldown = 0.3f;
                restorationCooldown = 0.5f;

                if (Plugin.Instance != null)
                    Plugin.Instance.ModLogger.LogInfo($"MemoryCardMenuAccess: DeckBoxUI restored, cursor visibility: {VirtualCursors.CursorDesiredVisibilityGet(0)}");
            }

            deckBoxMenuActive = false;
            menuWasOpen = false;
        }
    }
}
