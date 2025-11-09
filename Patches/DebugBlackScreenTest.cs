using HarmonyLib;
using UnityEngine;
using Panik;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// DEBUG PATCH: Press F9 to spawn a test black screen fade.
    /// Used to test the FlashScreen system without triggering death sequences.
    /// </summary>
    [HarmonyPatch]
    public class DebugBlackScreenTest
    {
        [HarmonyPatch(typeof(GameplayMaster), "Update")]
        [HarmonyPostfix]
        static void Update_Postfix()
        {
            if (!Plugin.EnableDebugBlackScreenTest.Value)
                return;

            // Press F9 to spawn test black screen
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Plugin.Instance.ModLogger.LogInfo("F9 pressed - spawning test black screen");
                SpawnTestBlackScreen();
            }
        }

        /// <summary>
        /// Spawns a test black screen with the same settings used for instant death.
        /// </summary>
        private static void SpawnTestBlackScreen()
        {
            // Use CameraGame instead of Camera.main (this is what the game uses)
            if (CameraGame.firstInstance == null || CameraGame.firstInstance.myCamera == null)
            {
                Plugin.Instance.ModLogger.LogWarning("CameraGame camera not found!");
                return;
            }

            Plugin.Instance.ModLogger.LogInfo("Spawning black screen overlay...");

            // Spawn just outside clipping plane to cover wide FOV
            var flashScreen = FlashScreen.SpawnEx(
                color: Color.black,
                alpha: 1.0f,
                alphaDecaySpeed: 0.5f,
                targetCamera: CameraGame.firstInstance.myCamera,
                cameraDistance: 0.35f, // Between 0.3f (clips) and 0.4f (partial coverage at 110°)
                forceSpawn: true
            );

            if (flashScreen != null)
            {
                Plugin.Instance.ModLogger.LogInfo("Black screen spawned successfully!");
            }
            else
            {
                Plugin.Instance.ModLogger.LogError("Failed to spawn black screen!");
            }
        }
    }
}
