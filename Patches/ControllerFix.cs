using BepInEx;
using HarmonyLib;
using Panik;
using Rewired;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
    public class ControllerFix
    {
        [HarmonyPatch(typeof(Controls), "PlayersUpdate")]
        [HarmonyPrefix]
        static bool FixControllerNullReference(Controls __instance)
        {
            if (!Plugin.ControllerFixPatch.Value)
                return true;

            bool flag = false;
            if (ReInput.players == null || ReInput.players.playerCount == 0)
            {
                return false;
            }

            if (ReInput.players.playerCount != Controls.playersExtList.Count)
            {
                typeof(Controls).GetMethod("_PlayersListUpdate",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(__instance, null);
                UnityEngine.Debug.LogWarning("Controls: Players count changed! list was updated!");
            }

            foreach (Controls.PlayerExt playerExt in Controls.playersExtList)
            {
                if (playerExt == null)
                {
                    UnityEngine.Debug.LogError("Controls: Player is null!");
                    continue;
                }

                if (playerExt.rePlayer.controllers.hasKeyboard &&
                    playerExt.rePlayer.controllers.Keyboard.GetAnyButtonDown())
                {
                    playerExt.lastInputKindUsed = Controls.InputKind.Keyboard;
                }

                if (playerExt.rePlayer.controllers.hasMouse)
                {
                    bool anyButtonDown = playerExt.rePlayer.controllers.Mouse.GetAnyButtonDown();
                    bool flag2 = Mathf.Abs(Controls.MouseAxis_ValueGet(playerExt, Controls.MouseElement.axisX)) > 0.1f ||
                                 Mathf.Abs(Controls.MouseAxis_ValueGet(playerExt, Controls.MouseElement.axisY)) > 0.1f;
                    bool flag3 = Controls.MouseAxis_ValueGet(playerExt, Controls.MouseElement.axisScrollWheelHorizontal) != 0f ||
                                 Controls.MouseAxis_ValueGet(playerExt, Controls.MouseElement.axisScrollWheelVertical) != 0f;

                    if (anyButtonDown || flag3 ||
                        (Controls.MouseMovementSwitchesLastInputGet(Controls.GetPlayerIndex(playerExt)) && flag2))
                    {
                        playerExt.lastInputKindUsed = Controls.InputKind.Mouse;
                    }
                }

                if (playerExt.rePlayer.controllers.joystickCount > 0)
                {
                    for (int i = 0; i < playerExt.rePlayer.controllers.joystickCount; i++)
                    {
                        Joystick joystick = playerExt.rePlayer.controllers.Joysticks[i];
                        if (joystick == null) continue;

                        IGamepadTemplate template = joystick.GetTemplate<IGamepadTemplate>();
                        if (template == null) continue;

                        bool flag4 = template.leftStick.horizontal.value != 0f ||
                                     template.leftStick.vertical.value != 0f ||
                                     template.rightStick.horizontal.value != 0f ||
                                     template.rightStick.vertical.value != 0f ||
                                     template.leftTrigger.value != 0f ||
                                     template.rightTrigger.value != 0f;

                        if (joystick.GetAnyButtonDown() || flag4)
                        {
                            playerExt.lastInputKindUsed = Controls.InputKind.Joystick;
                            playerExt.lastUsedJoystickIndex = i;
                            playerExt.lastJoystickUsed = joystick;
                            playerExt.lastUsedJoystickTemplate = template;
                        }
                    }
                }

                if (playerExt.lastInputKindUsed != playerExt.lastInputKindUsedOld)
                {
                    playerExt.lastInputKindUsedOld = playerExt.lastInputKindUsed;
                    flag = true;
                }
            }

            if (flag)
            {
                Controls.MapCallback mapCallback = Controls.onLastInputKindChangedAny;
                if (mapCallback != null)
                {
                    mapCallback(null);
                }
                Controls.MapCallback mapCallback2 = Controls.onPromptsUpdateRequest;
                if (mapCallback2 != null)
                {
                    mapCallback2(null);
                }
            }

            return false;
        }
    }
}