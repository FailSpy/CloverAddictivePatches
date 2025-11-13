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

        [HarmonyPatch(typeof(GeneralUiScript), "Awake")]
        [HarmonyPostfix]
        static void GeneralUiScript_Awake_Postfix(GeneralUiScript __instance)
        {
            if (!Plugin.HideCoinsTicketsUI.Value)
                return;

            // Hide coins and tickets UI for better screenshots
            if (__instance.coinsHolder != null)
            {
                __instance.coinsHolder.gameObject.SetActive(false);
                pluginInstance.ModLogger.LogInfo("Debug: Disabled coins UI for screenshots");
            }

            if (__instance.ticketsHolder != null)
            {
                __instance.ticketsHolder.gameObject.SetActive(false);
                pluginInstance.ModLogger.LogInfo("Debug: Disabled tickets UI for screenshots");
            }
        }

        [HarmonyPatch(typeof(GameplayMaster), "Update")]
        [HarmonyPostfix]
        static void DebugHotkeys()
        {
            if (!Plugin.DebugPatch.Value)
                return;

            // F6: Toggle screenshot mode (hide/show coins and tickets UI)
            if (Input.GetKeyDown(KeyCode.F6))
            {
                if (GeneralUiScript.instance != null)
                {
                    bool newState = !Plugin.HideCoinsTicketsUI.Value;
                    Plugin.HideCoinsTicketsUI.Value = newState;

                    if (GeneralUiScript.instance.coinsHolder != null)
                        GeneralUiScript.instance.coinsHolder.gameObject.SetActive(!newState);

                    if (GeneralUiScript.instance.ticketsHolder != null)
                        GeneralUiScript.instance.ticketsHolder.gameObject.SetActive(!newState);

                    pluginInstance.ModLogger.LogInfo($"Debug: Screenshot mode {(newState ? "ENABLED" : "DISABLED")} (UI hidden: {newState})");
                }
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DialogueScript.SetDialogue(false, "DIALOGUE_WELCOME_BACK_AFTER_BAD_ENDING");
                pluginInstance.ModLogger.LogInfo("Debug: Playing DIALOGUE_WELCOME_BACK_AFTER_BAD_ENDING");
            }

            // ===== CINEMATIC CAMERA POSITION DEBUG KEYS =====
            // Using number row and standard keyboard keys for cinematic camera positions
            // (matches game's actual cinematic calls)

            // 1 (or Numpad 1): ATM (Interests cutscene camera)
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                CameraController.SetPosition(CameraController.PositionKind.ATM, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> ATM (false, 1f) [Interests cutscene]");
            }

            // 2 (or Numpad 2): ATM Straight (Deal/deadline cutscenes)
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                CameraController.SetPosition(CameraController.PositionKind.ATMStraight, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> ATMStraight (false, 1f) [Deal/deadline cutscenes]");
            }

            // 3 (or Numpad 3): Reward Box (Reward box cutscene)
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                CameraController.SetPosition(CameraController.PositionKind.RewardBox, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> RewardBox (false, 1f) [Reward box cutscene]");
            }

            // 4 (or Numpad 4): Clover Tickets Machine (Clover tickets cutscene)
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                CameraController.SetPosition(CameraController.PositionKind.CloverTicketsMachine, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> CloverTicketsMachine (false, 1f) [Clover tickets cutscene]");
            }

            // 5 (or Numpad 5): Deadline Bonus (Deadline bonus cutscene)
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                CameraController.SetPosition(CameraController.PositionKind.DeadlineBonus, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> DeadlineBonus (false, 1f) [Deadline bonus cutscene]");
            }

            // 6 (or Numpad 6): Falling (Death fall camera)
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                CameraController.SetPosition(CameraController.PositionKind.Falling, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> Falling (false, 1f) [Death fall]");
            }

            // 7 (or Numpad 7): Trap Door (Trapdoor shake)
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
            {
                CameraController.SetPosition(CameraController.PositionKind.TrapDoor, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> TrapDoor (false, 1f) [Trapdoor shake]");
            }

            // 8 (or Numpad 8): Door Ending Scene (Ending cutscene)
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
            {
                CameraController.SetPosition(CameraController.PositionKind.doorEndingScene, true, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> doorEndingScene (true, 1f) [Ending cutscene]");
            }

            // 9 (or Numpad 9): Terminal (Terminal camera)
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
            {
                CameraController.SetPosition(CameraController.PositionKind.terminal, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> terminal (false, 1f) [Terminal]");
            }

            // 0 (or Numpad 0): Slot From Top (Phone transformation)
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                CameraController.SetPosition(CameraController.PositionKind.SlotFromTop, false, 2f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> SlotFromTop (false, 2f) [Phone transformation]");
            }

            // / (or Numpad /): Store view
            if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide))
            {
                CameraController.SetPosition(CameraController.PositionKind.Store, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> Store (false, 1f) [Store view]");
            }

            // [ (or Numpad *): All Drawers view
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                CameraController.SetPosition(CameraController.PositionKind.DrawersAll, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> DrawersAll (false, 1f) [All drawers view]");
            }

            // - (or Numpad -): Room Top View
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                CameraController.SetPosition(CameraController.PositionKind.RoomTopView, true, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> RoomTopView (true, 1f) [Top view transition]");
            }

            // = (or Numpad +): Slot Machine
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                CameraController.SetPosition(CameraController.PositionKind.Slot_Fixed, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> Slot_Fixed (false, 1f) [Slot machine]");
            }

            // Return/Enter (or Numpad Enter): Slot Coin Plate
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                CameraController.SetPosition(CameraController.PositionKind.SlotCoinPlate_Fixed, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> SlotCoinPlate_Fixed (false, 1f) [Coin plate]");
            }

            // . (or Numpad .): Free Camera (reset to normal)
            if (Input.GetKeyDown(KeyCode.Period) || Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                CameraController.SetPosition(CameraController.PositionKind.Free, false, 1f);
                pluginInstance.ModLogger.LogInfo("Debug: Camera -> Free (false, 1f) [Free camera]");
            }

            // F9: Force equip a charm (ignores space limits - for testing overflow scenarios)
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ForceEquipCharm();
            }

            // F10: Give 100 clover tickets (for buying items from store)
            if (Input.GetKeyDown(KeyCode.F10))
            {
                GiveTickets();
            }
        }

        /// <summary>
        /// Force-equips a charm, bypassing inventory space limits.
        /// Uses charms from drawers or not-bought list for testing overflow scenarios (8/7 charms).
        /// </summary>
        private static void ForceEquipCharm()
        {
            // Get current charm count
            int normalCount = PowerupScript.list_EquippedNormal.Count;
            int maxCharms = GameplayData.MaxEquippablePowerupsGet(true);

            pluginInstance.ModLogger.LogInfo($"Debug: Current charms: {normalCount}/{maxCharms}");

            // Find a charm to equip from drawers
            PowerupScript.Identifier charmToEquip = PowerupScript.Identifier.undefined;
            int drawerIndex = -1;

            for (int i = 0; i < PowerupScript.array_InDrawer.Length; i++)
            {
                PowerupScript drawerPowerup = PowerupScript.array_InDrawer[i];
                if (drawerPowerup != null &&
                    drawerPowerup.category == PowerupScript.Category.normal &&
                    !PowerupScript.IsEquipped(drawerPowerup.identifier))
                {
                    charmToEquip = drawerPowerup.identifier;
                    drawerIndex = i;
                    break;
                }
            }

            // If no drawer charms, try not-bought list
            if (charmToEquip == PowerupScript.Identifier.undefined)
            {
                foreach (PowerupScript powerup in PowerupScript.list_NotBought)
                {
                    if (powerup.category == PowerupScript.Category.normal)
                    {
                        charmToEquip = powerup.identifier;
                        break;
                    }
                }
            }

            if (charmToEquip == PowerupScript.Identifier.undefined)
            {
                pluginInstance.ModLogger.LogError("Debug: No available charms to force-equip! Try putting a charm in a drawer first.");
                return;
            }

            pluginInstance.ModLogger.LogInfo($"Debug: Force equipping {charmToEquip}" +
                (drawerIndex >= 0 ? $" from drawer {drawerIndex}" : " from not-bought list"));

            // Set flag to ignore space check
            PowerupScript.EquipFlag_IgnoreSpaceCondition();

            // Try to equip the charm
            bool success = PowerupScript.Equip(charmToEquip, false, true);

            if (success)
            {
                int newCount = PowerupScript.list_EquippedNormal.Count;
                pluginInstance.ModLogger.LogInfo($"Debug: Successfully force-equipped {charmToEquip}! New count: {newCount}/{maxCharms}");

                if (newCount > maxCharms)
                {
                    pluginInstance.ModLogger.LogWarning($"Debug: OVERFLOW! {newCount}/{maxCharms} charms - perfect for testing drawer swap failures!");
                }
            }
            else
            {
                pluginInstance.ModLogger.LogError($"Debug: Failed to force-equip {charmToEquip}");
            }
        }

        /// <summary>
        /// Gives the player 100 clover tickets for buying items from the store.
        /// </summary>
        private static void GiveTickets()
        {
            long currentTickets = GameplayData.CloverTicketsGet();
            long ticketsToAdd = 100;

            GameplayData.CloverTicketsAdd(ticketsToAdd, false);

            long newTickets = GameplayData.CloverTicketsGet();
            pluginInstance.ModLogger.LogInfo($"Debug: Gave {ticketsToAdd} clover tickets! ({currentTickets} -> {newTickets})");

            // Play a sound for feedback
            Sound.Play("SoundMenuSelect");
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