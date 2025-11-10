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
        private const float INPUT_COOLDOWN_DURATION = 0.3f;
        private const float RESTORATION_COOLDOWN_DURATION = 0.5f;

        private static bool deckBoxMenuActive = false;
        private static bool menuWasOpen = false;
        private static CameraController.PositionKind savedCameraPosition;
        private static float inputCooldown = 0f;
        private static float restorationCooldown = 0f;

        private static bool CanOpenMenu()
        {
            return DeckBoxUI.IsEnabled() &&
                   DeckBoxUI.IsPickingCard(true) &&
                   !MainMenuScript.IsEnabled() &&
                   inputCooldown <= 0f;
        }

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
                                        gamePhase == GameplayMaster.GamePhase.endingWithoutDeath ||
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

            if (!CanOpenMenu())
                return;

            // Keep cursor visible - fights against other systems hiding it
            if (!VirtualCursors.CursorDesiredVisibilityGet(0))
                VirtualCursors.CursorDesiredVisibilitySet(0, true);

            if (Controls.ActionButton_PressedGet(0, Controls.InputAction.menuPause))
            {
                if (GameplayMaster.instance == null)
                    return;

                if (Plugin.Instance != null)
                    Plugin.Instance.ModLogger.LogInfo("MemoryCardMenuAccess: Opening menu during card selection");

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

        [HarmonyPatch(typeof(CameraController), "SetPosition")]
        [HarmonyPrefix]
        static bool PreventCameraChangesDuringDeckBoxMenu(CameraController.PositionKind kind)
        {
            if (!Plugin.MemoryCardMenuAccessPatch.Value)
                return true;

            bool shouldLockCamera = (deckBoxMenuActive && MainMenuScript.IsEnabled()) || restorationCooldown > 0f;

            if (shouldLockCamera)
                return kind == savedCameraPosition;

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

        private static void RestoreDeckBoxUI()
        {
            if (Plugin.Instance != null)
                Plugin.Instance.ModLogger.LogInfo("MemoryCardMenuAccess: Restoring DeckBoxUI state");

            if (DeckBoxUI.instance != null && DeckBoxUI.instance.holder != null)
            {
                DeckBoxUI.instance.holder.SetActive(true);

                // Triple cursor set: each operation between calls can hide cursor, must reapply
                VirtualCursors.CursorDesiredVisibilitySet(0, true);
                CameraController.SetPosition(savedCameraPosition, false, 0f);
                VirtualCursors.CursorDesiredVisibilitySet(0, true);

                CardsInspectorScript.Open(
                    "CARDS_INSPECTOR_TITLE__UNDISCOVERED",
                    "CARDS_INSPECTOR_DESCRIPTION__UNDISCOVERED",
                    CardsInspectorScript.PromptKind.none
                );

                VirtualCursors.CursorDesiredVisibilitySet(0, true);

                inputCooldown = INPUT_COOLDOWN_DURATION;
                restorationCooldown = RESTORATION_COOLDOWN_DURATION;

                if (Plugin.Instance != null)
                    Plugin.Instance.ModLogger.LogInfo($"MemoryCardMenuAccess: DeckBoxUI restored, cursor visibility: {VirtualCursors.CursorDesiredVisibilityGet(0)}");
            }

            deckBoxMenuActive = false;
            menuWasOpen = false;
        }
    }
}
