using UnityEngine;

namespace CloverAddictivePatches.Utilities
{
    /// <summary>
    /// Type-safe accessors for CameraController private fields.
    /// </summary>
    public static class CameraAccessors
    {
        public static Camera GetMyCamera(CameraController instance)
        {
            return ReflectionCache.CameraControllerCache.myCamera?.GetValue(instance) as Camera;
        }

        public static bool GetDollyZoomEnabled(CameraController instance)
        {
            var value = ReflectionCache.CameraControllerCache.dollyZoomEnabled?.GetValue(instance);
            return value != null && (bool)value;
        }

        public static void SetDollyZoomEnabled(CameraController instance, bool value)
        {
            ReflectionCache.CameraControllerCache.dollyZoomEnabled?.SetValue(instance, value);
        }

        public static object GetPositionKind(CameraController instance)
        {
            return ReflectionCache.CameraControllerCache.positionKind?.GetValue(instance);
        }

        public static void SetPositionKind(CameraController instance, object value)
        {
            ReflectionCache.CameraControllerCache.positionKind?.SetValue(instance, value);
        }

        public static float GetLerpSpeedMultiplier(CameraController instance)
        {
            var value = ReflectionCache.CameraControllerCache.lerpSpeedMultiplier?.GetValue(instance);
            return value != null ? (float)value : 1f;
        }

        public static void SetLerpSpeedMultiplier(CameraController instance, float value)
        {
            ReflectionCache.CameraControllerCache.lerpSpeedMultiplier?.SetValue(instance, value);
        }

        public static Transform GetTargetTransform(CameraController instance)
        {
            return ReflectionCache.CameraControllerCache.targetTransform?.GetValue(instance) as Transform;
        }

        public static void SetTargetTransform(CameraController instance, Transform value)
        {
            ReflectionCache.CameraControllerCache.targetTransform?.SetValue(instance, value);
        }

        public static float GetDeathCameraY(CameraController instance)
        {
            var value = ReflectionCache.CameraControllerCache.deathCameraY?.GetValue(instance);
            return value != null ? (float)value : 0f;
        }

        public static void SetDeathCameraY(CameraController instance, float value)
        {
            ReflectionCache.CameraControllerCache.deathCameraY?.SetValue(instance, value);
        }

        /// <summary>
        /// Checks if position kind matches enum value by name.
        /// </summary>
        public static bool PositionKindEquals(object positionKind, string enumValueName)
        {
            return positionKind?.ToString() == enumValueName;
        }

        /// <summary>
        /// Parses PositionKind enum value by name. Returns null if parsing fails.
        /// </summary>
        public static object ParsePositionKind(string enumValueName)
        {
            var positionKindType = ReflectionCache.CameraControllerCache.positionKind?.FieldType;
            if (positionKindType != null && positionKindType.IsEnum)
            {
                try
                {
                    return System.Enum.Parse(positionKindType, enumValueName);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}
