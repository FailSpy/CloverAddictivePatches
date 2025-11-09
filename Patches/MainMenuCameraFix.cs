using HarmonyLib;
using Panik;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Prevents camera movement to menu drawer position while opening main menu, keeping free camera mode.
    /// </summary>
    [HarmonyPatch]
    public class MainMenuCameraFix
    {
        [HarmonyPatch(typeof(CameraController), "SetPosition")]
        [HarmonyPrefix]
        static bool SetPosition_Prefix(CameraController.PositionKind kind, float lerpSpeedMultiplier)
        {
            if (!Plugin.MainMenuCameraFixPatch.Value)
                return true;

            if (kind == CameraController.PositionKind.MenuDrawer_Menu)
            {
                CameraAccessors.SetPositionKind(CameraController.instance, kind);
                CameraAccessors.SetLerpSpeedMultiplier(CameraController.instance, lerpSpeedMultiplier);
                VirtualCursors.CursorDesiredVisibilitySet(0, true);

                // Skip original to prevent targetTransform change
                return false;
            }

            return true;
        }
    }
}
