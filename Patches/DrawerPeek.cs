using BepInEx;
using HarmonyLib;
using Panik;
using UnityEngine;
using System;
using System.Reflection;

namespace CloverAddictivePatches.Patches
{
    public class DrawerPeek
    {
        private static Plugin pluginInstance;
        private static FieldInfo drawerIsOpenField;
        private static FieldInfo myControllerField;
        private static PropertyInfo hoveredElementProperty;

        private static System.Collections.Generic.Dictionary<DiegeticMenuElement, bool> drawerHoverState =
            new System.Collections.Generic.Dictionary<DiegeticMenuElement, bool>();

        private static System.Collections.Generic.Dictionary<DiegeticMenuElement, int> menuElementToDrawerIndex =
            new System.Collections.Generic.Dictionary<DiegeticMenuElement, int>();

        private static System.Collections.Generic.Dictionary<DiegeticMenuElement, float> drawerCloseTime =
            new System.Collections.Generic.Dictionary<DiegeticMenuElement, float>();

        private static System.Collections.Generic.HashSet<int> peekOpenedDrawers =
            new System.Collections.Generic.HashSet<int>();

        private static int currentlyPeekedDrawer = -1;

        private const float CLOSE_GRACE_PERIOD = 0.3f;

        public static void Initialize(Plugin instance)
        {
            pluginInstance = instance;
            Debug.Initialize(instance);

            drawerIsOpenField = AccessTools.Field(typeof(DrawersScript), "drawerIsOpen");
            myControllerField = AccessTools.Field(typeof(DiegeticMenuElement), "myController");
            hoveredElementProperty = AccessTools.Property(typeof(DiegeticMenuController), "HoveredElement");

            pluginInstance.ModLogger.LogInfo("DrawerPeek initialized");
        }

        [HarmonyPatch(typeof(DrawersScript), "Awake")]
        [HarmonyPostfix]
        static void OnDrawersScriptAwake(DrawersScript __instance)
        {
            ExtendDrawerColliders(__instance);
        }

        private static void ExtendDrawerColliders(DrawersScript instance)
        {
            BoxCollider[] boxColliders = instance.GetComponentsInChildren<BoxCollider>();

            foreach (BoxCollider box in boxColliders)
            {
                Vector3 newSize = box.size;
                Vector3 newCenter = box.center;

                // Extend collider depth 5x and width/height 1.3x for easier hover detection
                newSize.z *= 5.0f;
                newCenter.z -= box.size.z * 2.0f;
                newSize.x *= 1.3f;
                newSize.y *= 1.3f;

                box.size = newSize;
                box.center = newCenter;

                Debug.CreateColliderVisualization(box);
            }
        }

        [HarmonyPatch(typeof(DrawersScript), "Update")]
        [HarmonyPrefix]
        static void DrawColliderDebug(DrawersScript __instance)
        {
            BoxCollider[] boxColliders = __instance.GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider box in boxColliders)
            {
                Debug.DrawWireframeCube(box.transform, box.center, box.size, Color.yellow);
            }
        }

        [HarmonyPatch(typeof(DrawersScript), "Update")]
        [HarmonyPostfix]
        static void DrawerUpdatePatch(DrawersScript __instance)
        {
            if (!Plugin.DrawerPeekPatch.Value)
                return;

            // Close drawer if inspector shows different content
            if (currentlyPeekedDrawer != -1 && InspectorScript.IsEnabled())
            {
                PowerupScript currentlyInspected = InspectorScript.CurrentlyInspectedPowerupGet();
                PowerupScript ourDrawerPowerup = PowerupScript.GetDrawerPowerup(currentlyPeekedDrawer);

                if (currentlyInspected != ourDrawerPowerup)
                {
                    int drawerToClose = currentlyPeekedDrawer;

                    if (drawerIsOpenField != null)
                    {
                        bool[] drawerIsOpen = (bool[])drawerIsOpenField.GetValue(__instance);
                        drawerIsOpen[drawerToClose] = false;
                    }

                    peekOpenedDrawers.Remove(drawerToClose);
                    currentlyPeekedDrawer = -1;

                    var keysToRemove = new System.Collections.Generic.List<DiegeticMenuElement>();
                    foreach (var kvp in menuElementToDrawerIndex)
                    {
                        if (kvp.Value == drawerToClose && drawerCloseTime.ContainsKey(kvp.Key))
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                    foreach (var key in keysToRemove)
                    {
                        drawerCloseTime.Remove(key);
                    }

                    // Refresh inspector to show new content
                    if (currentlyInspected != null)
                    {
                        InspectorScript.Close();
                        InspectorScript.Open_AsPowerup(currentlyInspected);
                    }
                }
            }

            DiegeticMenuElement[] allMenuElements = __instance.GetComponentsInChildren<DiegeticMenuElement>();

            if (allMenuElements.Length == 0)
            {
                return;
            }

            foreach (DiegeticMenuElement menuElement in allMenuElements)
            {
                var controller = myControllerField?.GetValue(menuElement) as DiegeticMenuController;

                if (controller == null || !controller.IsRunning())
                {
                    continue;
                }

                var hoveredElement = hoveredElementProperty?.GetValue(controller) as DiegeticMenuElement;
                bool isHovered = hoveredElement == menuElement;

                bool wasHovered = drawerHoverState.ContainsKey(menuElement) && drawerHoverState[menuElement];

                if (isHovered && !wasHovered)
                {
                    pluginInstance.ModLogger.LogInfo($"Started hovering drawer menu element: {menuElement.gameObject.name}");

                    if (drawerCloseTime.ContainsKey(menuElement))
                    {
                        drawerCloseTime.Remove(menuElement);
                    }

                    int drawerIndex = GetOrDetermineDrawerIndex(menuElement);

                    if (drawerIndex != -1)
                    {
                        PeekDrawer(drawerIndex);
                    }
                    else
                    {
                        pluginInstance.ModLogger.LogWarning($"Could not determine drawer index for {menuElement.gameObject.name}");
                    }

                    drawerHoverState[menuElement] = true;
                }
                else if (!isHovered && wasHovered)
                {
                    int drawerIndex = GetOrDetermineDrawerIndex(menuElement);

                    if (drawerIndex != -1 && DrawersScript.IsDrawerOpen(drawerIndex))
                    {
                        bool stillHoveringDrawer = IsHoveringAnyDrawerElement(menuElement, controller);

                        if (stillHoveringDrawer)
                        {
                            return;
                        }

                        if (!drawerCloseTime.ContainsKey(menuElement))
                        {
                            drawerCloseTime[menuElement] = Time.time + CLOSE_GRACE_PERIOD;
                        }
                    }
                    else if (drawerIndex != -1)
                    {
                        CloseDrawerQuietly(drawerIndex);
                    }

                    drawerHoverState[menuElement] = false;
                }

                if (drawerCloseTime.ContainsKey(menuElement) && Time.time >= drawerCloseTime[menuElement])
                {
                    int drawerIndex = GetOrDetermineDrawerIndex(menuElement);
                    CloseDrawerQuietly(drawerIndex);
                    drawerCloseTime.Remove(menuElement);
                }
            }
        }

        private static bool IsHoveringAnyDrawerElement(DiegeticMenuElement mainMenuElement, DiegeticMenuController controller)
        {
            if (controller == null)
            {
                return false;
            }

            var hoveredElement = hoveredElementProperty?.GetValue(controller) as DiegeticMenuElement;
            if (hoveredElement == null)
            {
                return false;
            }

            // If we're hovering the main element still, keep open
            if (hoveredElement == mainMenuElement)
            {
                return true;
            }

            // Check if the hovered element is a descendant of the main drawer element
            if (hoveredElement.transform.IsChildOf(mainMenuElement.transform))
            {
                return true;
            }

            // Check if the hovered element is an ancestor (parent) of the main element
            if (mainMenuElement.transform.IsChildOf(hoveredElement.transform))
            {
                return true;
            }

            return false;
        }

        private static int GetOrDetermineDrawerIndex(DiegeticMenuElement menuElement)
        {
            if (menuElementToDrawerIndex.ContainsKey(menuElement))
            {
                return menuElementToDrawerIndex[menuElement];
            }

            if (DrawersScript.instance == null || menuElement == null)
            {
                return -1;
            }

            if (DrawersScript.instance.normalDrawerHolders == null)
            {
                return -1;
            }

            for (int i = 0; i < DrawersScript.instance.normalDrawerHolders.Length; i++)
            {
                GameObject holder = DrawersScript.instance.normalDrawerHolders[i];
                if (holder == null)
                {
                    continue;
                }

                bool isSame = menuElement.gameObject == holder;
                bool isChild = menuElement.transform.IsChildOf(holder.transform);
                bool isParent = holder.transform.IsChildOf(menuElement.transform);

                if (isSame || isChild || isParent)
                {
                    menuElementToDrawerIndex[menuElement] = i;
                    return i;
                }
            }

            return -1;
        }

        private static void PeekDrawer(int index)
        {
            if (DrawersScript.instance == null)
            {
                return;
            }

            if (!DrawersScript.IsDrawerUnlocked(index))
            {
                return;
            }

            if (DrawersScript.IsDrawerOpen(index))
            {
                return;
            }

            // Close inspector first when switching drawers to force text update
            if (currentlyPeekedDrawer != -1 && currentlyPeekedDrawer != index && InspectorScript.IsEnabled())
            {
                InspectorScript.Close();
            }

            if (drawerIsOpenField != null)
            {
                bool[] drawerIsOpen = (bool[])drawerIsOpenField.GetValue(DrawersScript.instance);
                drawerIsOpen[index] = true;
            }
            else
            {
                return;
            }

            peekOpenedDrawers.Add(index);
            currentlyPeekedDrawer = index;

            Sound.Play("SoundDrawerOpen", 1f, 1f);

            PowerupScript drawerPowerup = PowerupScript.GetDrawerPowerup(index);
            if (drawerPowerup != null)
            {
                InspectorScript.Open_AsPowerup(drawerPowerup);
            }
        }

        private static void CloseDrawerQuietly(int index)
        {
            if (DrawersScript.instance == null)
            {
                return;
            }

            if (!DrawersScript.IsDrawerOpen(index))
            {
                return;
            }

            if (drawerIsOpenField != null)
            {
                bool[] drawerIsOpen = (bool[])drawerIsOpenField.GetValue(DrawersScript.instance);
                drawerIsOpen[index] = false;
            }
            else
            {
                return;
            }

            peekOpenedDrawers.Remove(index);

            Sound.Play("SoundDrawerClose", 1f, 1f);

            if (currentlyPeekedDrawer == index)
            {
                InspectorScript.Close();
                currentlyPeekedDrawer = -1;
            }
        }

        [HarmonyPatch(typeof(DrawersScript), "OpenTry")]
        [HarmonyPrefix]
        static bool HandlePeekToNormalOpen(int index)
        {
            if (peekOpenedDrawers.Contains(index))
            {
                if (drawerIsOpenField != null && DrawersScript.instance != null)
                {
                    bool[] drawerIsOpen = (bool[])drawerIsOpenField.GetValue(DrawersScript.instance);
                    drawerIsOpen[index] = false;
                }

                peekOpenedDrawers.Remove(index);

                if (currentlyPeekedDrawer == index)
                {
                    InspectorScript.Close();
                    currentlyPeekedDrawer = -1;
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(ScreenMenuScript), "Open")]
        [HarmonyPrefix]
        static void Open_Prefix(ref string[] options, ref ScreenMenuScript.OptionEvent[] optionEvents, string title)
        {
            if (title != "Pick an Option" || options == null || options.Length != 2)
                return;

            pluginInstance.ModLogger.LogInfo($"Intercepted drawer menu with {options.Length} options");

            var newOptions = new string[options.Length + 1];
            var newEvents = new ScreenMenuScript.OptionEvent[options.Length + 1];

            newOptions[0] = options[0];
            newOptions[1] = "Hello, World!";
            newOptions[2] = options[1];

            newEvents[0] = optionEvents[0];
            newEvents[1] = HelloWorldCallback;
            newEvents[2] = optionEvents[1];

            options = newOptions;
            optionEvents = newEvents;

            pluginInstance.ModLogger.LogInfo($"Modified drawer menu to have {newOptions.Length} options");
        }

        private static void HelloWorldCallback()
        {
            pluginInstance.ModLogger.LogInfo("Hello, World! button clicked!");

            var dialogueType = Type.GetType("DialogueScript, Assembly-CSharp");
            var setDialogueMethod = dialogueType?.GetMethod("SetDialogue", new Type[] { typeof(bool), typeof(string) });
            setDialogueMethod?.Invoke(null, new object[] { false, "Hello, World! This is a custom drawer menu option!" });
        }
    }
}