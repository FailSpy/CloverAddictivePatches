using HarmonyLib;
using Panik;
using UnityEngine;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Enables player movement and camera control during cutscenes.
    /// </summary>
    [HarmonyPatch]
    public class FreeroamDuringCutscenes
    {
        private static GameplayMaster.GamePhase lastPhase = GameplayMaster.GamePhase.intro;

        private static bool IsBlockedCameraPosition(CameraController.PositionKind kind)
        {
            return kind == CameraController.PositionKind.CloverTicketsMachine ||
                   kind == CameraController.PositionKind.ATM ||
                   kind == CameraController.PositionKind.ATMStraight ||
                   kind == CameraController.PositionKind.DeadlineBonus ||
                   kind == CameraController.PositionKind.RewardBox ||
                   kind == CameraController.PositionKind.SlotCoinPlate_Fixed ||
                   kind == CameraController.PositionKind.TrapDoor;
        }

        [HarmonyPatch(typeof(CameraController), "Update")]
        [HarmonyPrefix]
        static void EnsureFreeCameraInCutscenes()
        {
            if (!Plugin.ATMCutsceneFreeroamPatch.Value)
                return;

            GameplayMaster.GamePhase currentPhase = GameplayMaster.GetGamePhase();

            if (currentPhase == GameplayMaster.GamePhase.cutscene && lastPhase != GameplayMaster.GamePhase.cutscene)
            {
                CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
            }

            if (currentPhase == GameplayMaster.GamePhase.cutscene || currentPhase == GameplayMaster.GamePhase.gambling)
            {
                CameraController.PositionKind currentPosKind = CameraController.GetPositionKind();

                if (IsBlockedCameraPosition(currentPosKind))
                {
                    CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
                }
            }

            lastPhase = currentPhase;
        }

        [HarmonyPatch(typeof(CameraController), "SetPosition")]
        [HarmonyPrefix]
        static bool PreventCameraGrabDuringCutscenes(CameraController.PositionKind kind, bool instant, ref float lerpSpeedMultiplier)
        {
            if (!Plugin.ATMCutsceneFreeroamPatch.Value)
                return true;

            GameplayMaster.GamePhase currentPhase = GameplayMaster.GetGamePhase();

            if (currentPhase != GameplayMaster.GamePhase.cutscene && currentPhase != GameplayMaster.GamePhase.gambling)
                return true;

            // Maintain lerpSpeed=1.0 for Free camera to prevent input hitching
            if (kind == CameraController.PositionKind.Free && lerpSpeedMultiplier != 1f)
            {
                lerpSpeedMultiplier = 1f;
            }

            if (IsBlockedCameraPosition(kind))
            {
                CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(SlotMachineScript), "Set_NoMoreSpins", MethodType.Normal)]
        [HarmonyPostfix]
        static void NoSpinsLeft_ForceFreeCam()
        {
            if (!Plugin.ATMCutsceneFreeroamPatch.Value)
                return;

            CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
        }

        [HarmonyPatch(typeof(SlotMachineScript), "TurnOff")]
        [HarmonyPostfix]
        static void SlotTurnOff_ForceFreeCam()
        {
            if (!Plugin.ATMCutsceneFreeroamPatch.Value)
                return;

            GameplayMaster.GamePhase currentPhase = GameplayMaster.GetGamePhase();
            if (currentPhase == GameplayMaster.GamePhase.gambling)
            {
                CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
            }
        }

        [HarmonyPatch(typeof(PlayerScript), "Update")]
        [HarmonyPrefix]
        static bool AllowMovementDuringCutscenes(PlayerScript __instance)
        {
            if (!Plugin.ATMCutsceneFreeroamPatch.Value)
                return true;

            var rbField = ReflectionCache.PlayerScriptCache.rb;
            var playerIndexField = ReflectionCache.PlayerScriptCache.playerIndex;

            if (rbField == null || playerIndexField == null)
                return true;

            Rigidbody rb = (Rigidbody)rbField.GetValue(__instance);
            if (rb == null)
                return true;

            int playerIndex = (int)playerIndexField.GetValue(__instance);

            GameplayMaster.GamePhase currentPhase = GameplayMaster.GetGamePhase();
            bool shouldAllowMovement = Tick.IsGameRunning &&
                                     PlatformMaster.IsInitialized() &&
                                     (currentPhase == GameplayMaster.GamePhase.preparation ||
                                      currentPhase == GameplayMaster.GamePhase.cutscene ||
                                      currentPhase == GameplayMaster.GamePhase.gambling ||
                                      GameplayMaster.EndingFreeRoaming);

            if (shouldAllowMovement)
            {
                var playerExtChacheItMethod = ReflectionCache.PlayerScriptCache.PlayerExtChacheIt;
                playerExtChacheItMethod?.Invoke(__instance, null);

                if (currentPhase != GameplayMaster.GamePhase.intro &&
                    currentPhase != GameplayMaster.GamePhase.tutorialObsolete &&
                    !PowerupTriggerAnimController.HasAnimations() &&
                    !MemoryPackDealUI.IsDealRunnning() &&
                    !DeckBoxUI.IsEnabled() &&
                    !MainMenuScript.IsEnabled() &&
                    !CameraDebug.IsEnabled())
                {
                    Transform targetTransform = CameraController.GetTargetTransform();
                    if (targetTransform == null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        return false;
                    }

                    Vector2 input = new Vector2(
                        Controls.ActionAxisPair_GetValue(playerIndex, Controls.InputAction.moveRight, Controls.InputAction.moveLeft),
                        Controls.ActionAxisPair_GetValue(playerIndex, Controls.InputAction.moveUp, Controls.InputAction.moveDown)
                    );

                    if (ConsolePrompt.ConsoleIsEnabled())
                        input = Vector2.zero;

                    float maxSpeed = GameplayMaster.EndingFreeRoaming ? 12f : 6f;

                    rb.linearVelocity += Util.AxisToFpsVec3(input, targetTransform.eulerAngles.y) * 128f * Tick.Time;
                    rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed * Mathf.Min(1f, input.magnitude));

                    if (input.magnitude < 0.1f)
                    {
                        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Tick.Time * 50f);
                    }

                    return false;
                }
            }

            rb.linearVelocity = Vector3.zero;
            return false;
        }
    }
}
