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

            if (currentPhase == GameplayMaster.GamePhase.cutscene)
            {
                CameraController.PositionKind currentPosKind = CameraController.GetPositionKind();
                if (currentPosKind != CameraController.PositionKind.Free)
                {
                    CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
                }
            }

            lastPhase = currentPhase;
        }

        // Blocks non-Free camera positions during cutscenes
        [HarmonyPatch(typeof(CameraController), "SetPosition")]
        [HarmonyPrefix]
        static bool PreventCameraGrabDuringCutscenes(CameraController.PositionKind kind)
        {
            if (!Plugin.ATMCutsceneFreeroamPatch.Value)
                return true;

            GameplayMaster.GamePhase currentPhase = GameplayMaster.GetGamePhase();
            if (currentPhase != GameplayMaster.GamePhase.cutscene)
                return true;

            if (kind != CameraController.PositionKind.Free)
            {
                CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
                return false;
            }

            return true;
        }

        // Enables movement during cutscenes (footstep sounds omitted for simplicity)
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
            int playerIndex = (int)playerIndexField.GetValue(__instance);

            GameplayMaster.GamePhase currentPhase = GameplayMaster.GetGamePhase();
            bool shouldAllowMovement = Tick.IsGameRunning &&
                                     PlatformMaster.IsInitialized() &&
                                     (currentPhase == GameplayMaster.GamePhase.preparation ||
                                      currentPhase == GameplayMaster.GamePhase.cutscene ||
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
