using HarmonyLib;
using Panik;
using UnityEngine;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// FOV adjustment (60-110°, F1/F2 keys) and dolly zoom that scales with user FOV preference.
    /// </summary>
    [HarmonyPatch]
    public class CameraUtils
    {
        // Dolly zoom parameters
        private static float dollyZoomBaseDistance = 5f;

        // FOV adjustment parameters
        private const float FOV_MIN = 60f;
        private const float FOV_MAX = 110f;
        private const float FOV_STEP = 5f;

        /// <summary>
        /// Applies custom FOV with FOVExtra modifiers.
        /// </summary>
        [HarmonyPatch(typeof(CameraGame), "FieldOfViewUpdate")]
        [HarmonyPostfix]
        static void OverrideFOV(CameraGame __instance)
        {
            if (!Plugin.FOVAdjustmentPatch.Value)
                return;

            float currentFov = Plugin.PlayerFOV.Value;
            __instance.myCamera.fieldOfView = currentFov + CameraGame.FieldOfViewExtraGet();
        }

        /// <summary>
        /// Disables built-in dolly zoom by forcing parameter to false.
        /// </summary>
        [HarmonyPatch(typeof(CameraController), "DollyZoomEnable")]
        [HarmonyPrefix]
        static bool PreventBuiltinDollyZoom(CameraController __instance, ref bool enable)
        {
            enable = false;
            return true;
        }

        /// <summary>
        /// Disables built-in dolly zoom on startup.
        /// </summary>
        [HarmonyPatch(typeof(CameraController), "Awake")]
        [HarmonyPostfix]
        static void InitializeDollyZoomSettings(CameraController __instance)
        {
            CameraController.DollyZoomEnable(false);
        }

        /// <summary>
        /// Custom dolly zoom: adjusts camera distance based on FOV to maintain subject size, scaled by base FOV.
        /// </summary>
        [HarmonyPatch(typeof(CameraController), "Update")]
        [HarmonyPostfix]
        static void CustomDollyZoom(CameraController __instance)
        {
            if (Plugin.NoVertigoInducersPatch.Value)
                return;

            if (!Plugin.DollyZoomPatch.Value)
                return;

            var myCamera = CameraAccessors.GetMyCamera(__instance);
            bool dollyZoomEnabled = CameraAccessors.GetDollyZoomEnabled(__instance);
            var positionKind = CameraAccessors.GetPositionKind(__instance);

            if (myCamera == null || positionKind == null)
                return;

            bool isSlotFixed = CameraAccessors.PositionKindEquals(positionKind, "Slot_Fixed");

            if (dollyZoomEnabled && !isSlotFixed)
            {
                CameraGame cameraGame = myCamera.GetComponent<CameraGame>();
                if (cameraGame == null || cameraGame.myCamera == null)
                    return;

                float currentFOV = cameraGame.myCamera.fieldOfView;
                float baseFOV = Plugin.PlayerFOV.Value;

                // Scale dolly intensity based on base FOV (1.0 at 60°, reduced at higher FOV)
                float dollyIntensityFactor = CalculateDollyIntensityFactor(baseFOV);

                float currentTan = Mathf.Tan(currentFOV * 0.5f * Mathf.Deg2Rad);
                float referenceTan = Mathf.Tan(baseFOV * 0.5f * Mathf.Deg2Rad);

                float distanceScale = currentTan / referenceTan;

                Vector3 cameraPos = cameraGame.transform.position;
                Vector3 targetPos = CameraController.GetTargetTransform().position;

                Vector3 directionToCamera = (cameraPos - targetPos).normalized;

                // Apply dolly offset with intensity scaling
                Vector3 dollyOffset = directionToCamera * dollyZoomBaseDistance * (1f - distanceScale) * dollyIntensityFactor;

                cameraGame.transform.position += dollyOffset;
            }
        }

        /// <summary>
        /// Calculates dolly zoom intensity: logarithmic scaling from 1.0 at 60° to 0.2 at 110°.
        /// </summary>
        private static float CalculateDollyIntensityFactor(float baseFOV)
        {
            const float defaultFOV = 60f;
            const float maxFOV = 110f;

            if (baseFOV <= defaultFOV)
                return 1.0f;

            float normalizedFOV = (baseFOV - defaultFOV) / (maxFOV - defaultFOV);
            float intensityFactor = Mathf.Pow(0.2f, normalizedFOV);

            return Mathf.Clamp(intensityFactor, 0.2f, 1.0f);
        }

        /// <summary>
        /// Changes FOV by delta with wraparound.
        /// </summary>
        public static void ChangeFOV(float delta)
        {
            float currentFov = Plugin.PlayerFOV.Value;
            float newFov = currentFov + delta;

            if (newFov > FOV_MAX)
                newFov = FOV_MIN;
            else if (newFov < FOV_MIN)
                newFov = FOV_MAX;

            Plugin.PlayerFOV.Value = newFov;
        }

        /// <summary>
        /// Handles F1/F2 FOV adjustment input.
        /// </summary>
        [HarmonyPatch(typeof(Controls), "PlayersUpdate")]
        [HarmonyPostfix]
        static void HandleFOVInput()
        {
            if (!Plugin.FOVAdjustmentPatch.Value)
                return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                ChangeFOV(-FOV_STEP);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                ChangeFOV(FOV_STEP);
            }
        }
    }
}
