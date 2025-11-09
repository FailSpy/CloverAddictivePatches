using BepInEx;
using HarmonyLib;
using Panik;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
    [HarmonyPatch]
    public static class Debug
    {
        private static Plugin pluginInstance;

        private static bool enableColliderVisualization = false;

        public static void Initialize(Plugin instance)
        {
            pluginInstance = instance;
        }

        [HarmonyPatch(typeof(GameplayMaster), "Update")]
        [HarmonyPostfix]
        static void DebugHotkeys()
        {
            if (!Plugin.DebugPatch.Value)
                return;

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DialogueScript.SetDialogue(false, "DIALOGUE_WELCOME_BACK_AFTER_BAD_ENDING");
                pluginInstance.ModLogger.LogInfo("Debug: Playing DIALOGUE_WELCOME_BACK_AFTER_BAD_ENDING");
            }
        }

        public static void CreateColliderVisualization(BoxCollider box)
        {
            if (!enableColliderVisualization) return;

            GameObject visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCube.name = "DEBUG_ColliderViz_" + box.gameObject.name;

            GameObject.Destroy(visualCube.GetComponent<Collider>());

            visualCube.transform.SetParent(box.transform);
            visualCube.transform.localPosition = box.center;
            visualCube.transform.localRotation = Quaternion.identity;
            visualCube.transform.localScale = box.size;

            var renderer = visualCube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0f, 1f, 0f, 0.3f);

                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                renderer.material = mat;
            }

            pluginInstance.ModLogger.LogInfo($"Created visualization cube for {box.gameObject.name}");
        }

        public static void DrawWireframeCube(Transform transform, Vector3 center, Vector3 size, Color color)
        {
            Vector3 halfSize = size * 0.5f;

            Vector3[] corners = new Vector3[8];
            corners[0] = transform.TransformPoint(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
            corners[1] = transform.TransformPoint(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
            corners[2] = transform.TransformPoint(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
            corners[3] = transform.TransformPoint(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
            corners[4] = transform.TransformPoint(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
            corners[5] = transform.TransformPoint(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
            corners[6] = transform.TransformPoint(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
            corners[7] = transform.TransformPoint(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));

            UnityEngine.Debug.DrawLine(corners[0], corners[1], color);
            UnityEngine.Debug.DrawLine(corners[1], corners[2], color);
            UnityEngine.Debug.DrawLine(corners[2], corners[3], color);
            UnityEngine.Debug.DrawLine(corners[3], corners[0], color);

            UnityEngine.Debug.DrawLine(corners[4], corners[5], color);
            UnityEngine.Debug.DrawLine(corners[5], corners[6], color);
            UnityEngine.Debug.DrawLine(corners[6], corners[7], color);
            UnityEngine.Debug.DrawLine(corners[7], corners[4], color);

            UnityEngine.Debug.DrawLine(corners[0], corners[4], color);
            UnityEngine.Debug.DrawLine(corners[1], corners[5], color);
            UnityEngine.Debug.DrawLine(corners[2], corners[6], color);
            UnityEngine.Debug.DrawLine(corners[3], corners[7], color);
        }

        public static void SetColliderVisualization(bool enabled)
        {
            enableColliderVisualization = enabled;
            pluginInstance.ModLogger.LogInfo($"Collider visualization: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        public static void LogColliderHierarchy(GameObject obj, string context = "")
        {
            if (!string.IsNullOrEmpty(context))
            {
                pluginInstance.ModLogger.LogInfo($"=== COLLIDER HIERARCHY: {context} ===");
            }

            Collider[] allColliders = obj.GetComponentsInChildren<Collider>();
            pluginInstance.ModLogger.LogInfo($"Total colliders in {obj.name} hierarchy: {allColliders.Length}");

            foreach (Collider col in allColliders)
            {
                pluginInstance.ModLogger.LogInfo($"  - {col.GetType().Name} on {col.gameObject.name} at {GetGameObjectPath(col.gameObject)}");

                if (col is BoxCollider box)
                {
                    pluginInstance.ModLogger.LogInfo($"    Size: {box.size}, Center: {box.center}, IsTrigger: {box.isTrigger}");
                }
            }
        }

        public static void LogMenuElements(GameObject obj, string context = "")
        {
            if (!string.IsNullOrEmpty(context))
            {
                pluginInstance.ModLogger.LogInfo($"=== MENU ELEMENTS: {context} ===");
            }

            DiegeticMenuElement[] menuElements = obj.GetComponentsInChildren<DiegeticMenuElement>();
            pluginInstance.ModLogger.LogInfo($"Found {menuElements.Length} DiegeticMenuElement(s) in {obj.name}:");

            foreach (var elem in menuElements)
            {
                pluginInstance.ModLogger.LogInfo($"  - On {elem.gameObject.name} at {GetGameObjectPath(elem.gameObject)}");
            }
        }
    }
}