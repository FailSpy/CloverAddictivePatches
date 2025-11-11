using BepInEx;
using HarmonyLib;
using Panik;
using UnityEngine;
using System.Reflection;
using System;

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

        // Camera movement accumulation for closing (for slow movements)
        private static Vector3 lastCameraRotationForAccumulation = Vector3.zero;
        private static float accumulatedCameraMovement = 0f;
        private static bool isAccumulatingMovement = false;

        // Swap cooldown
        private static float lastSwapTime = 0f;
        private const float SWAP_COOLDOWN = 0.2f;

        private const float CLOSE_GRACE_PERIOD = 0.3f;

        // Store original collider sizes for resetting
        private static System.Collections.Generic.Dictionary<BoxCollider, Vector3> originalColliderSizes =
            new System.Collections.Generic.Dictionary<BoxCollider, Vector3>();
        private static System.Collections.Generic.Dictionary<BoxCollider, Vector3> originalColliderCenters =
            new System.Collections.Generic.Dictionary<BoxCollider, Vector3>();

        // Map colliders to drawer indices for open state tracking
        private static System.Collections.Generic.Dictionary<BoxCollider, int> colliderToDrawerIndex =
            new System.Collections.Generic.Dictionary<BoxCollider, int>();

        public static void Initialize(Plugin instance)
        {
            pluginInstance = instance;
            Debug.Initialize(instance);

            drawerIsOpenField = AccessTools.Field(typeof(DrawersScript), "drawerIsOpen");
            myControllerField = AccessTools.Field(typeof(DiegeticMenuElement), "myController");
            hoveredElementProperty = AccessTools.Property(typeof(DiegeticMenuController), "HoveredElement");

            // Enable visual collider cubes if debug is enabled
            if (Plugin.DebugPatch.Value)
            {
                Debug.SetColliderVisualization(true);
            }
        }

        [HarmonyPatch(typeof(DrawersScript), "Awake")]
        [HarmonyPostfix]
        static void OnDrawersScriptAwake(DrawersScript __instance)
        {
            drawerHoverState.Clear();
            menuElementToDrawerIndex.Clear();
            drawerCloseTime.Clear();
            peekOpenedDrawers.Clear();
            currentlyPeekedDrawer = -1;
            colliderToDrawerIndex.Clear();
            isAccumulatingMovement = false;
            accumulatedCameraMovement = 0f;
            lastSwapTime = 0f;

            // Store original sizes ONLY if we haven't stored them yet
            // This ensures we always work from the true original Unity collider sizes
            BoxCollider[] boxColliders = __instance.GetComponentsInChildren<BoxCollider>();

            foreach (BoxCollider box in boxColliders)
            {
                if (!originalColliderSizes.ContainsKey(box))
                {
                    originalColliderSizes[box] = box.size;
                    originalColliderCenters[box] = box.center;
                }

                // Map collider to drawer index
                int drawerIndex = GetDrawerIndexForCollider(box, __instance);
                if (drawerIndex != -1)
                {
                    colliderToDrawerIndex[box] = drawerIndex;
                }
            }

            ExtendDrawerColliders(__instance);

            // Create visual collider cubes if debug is enabled
            if (Plugin.DebugPatch.Value && !Plugin.HideCoinsTicketsUI.Value)
            {
                CreateColliderVisualizations(__instance);
            }
        }

        // Public method to reapply collider extensions (called from debug menu)
        public static void ReapplyColliderExtensions()
        {
            if (DrawersScript.instance == null)
            {
                pluginInstance.ModLogger.LogWarning("Cannot reapply colliders: DrawersScript.instance is null");
                return;
            }

            // Remove old visualization cubes
            RemoveColliderVisualizations(DrawersScript.instance);

            // Reset to original sizes first
            BoxCollider[] boxColliders = DrawersScript.instance.GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider box in boxColliders)
            {
                if (originalColliderSizes.ContainsKey(box))
                {
                    box.size = originalColliderSizes[box];
                    box.center = originalColliderCenters[box];
                }
            }

            // Reapply extensions with new values
            ExtendDrawerColliders(DrawersScript.instance);

            // Recreate visualizations
            if (Plugin.DebugPatch.Value && !Plugin.HideCoinsTicketsUI.Value)
            {
                CreateColliderVisualizations(DrawersScript.instance);
            }
        }

        // Create visual cubes for all drawer colliders
        private static void CreateColliderVisualizations(DrawersScript instance)
        {
            BoxCollider[] boxColliders = instance.GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider box in boxColliders)
            {
                Debug.CreateColliderVisualization(box);
            }
        }

        // Update visualization cubes to match current collider positions
        private static void UpdateColliderVisualizations(DrawersScript instance)
        {
            BoxCollider[] boxColliders = instance.GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider box in boxColliders)
            {
                // Find the visualization cube for this collider
                string vizName = "DEBUG_ColliderViz_" + box.gameObject.name;
                Transform vizTransform = box.transform.Find(vizName);

                if (vizTransform != null)
                {
                    // Update position and scale to match current collider
                    vizTransform.localPosition = box.center;
                    vizTransform.localScale = box.size;
                }
            }
        }

        // Remove visualization cubes
        private static void RemoveColliderVisualizations(DrawersScript instance)
        {
            // Find and destroy all DEBUG_ColliderViz objects
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(UnityEngine.FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("DEBUG_ColliderViz_"))
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }

        private static void ExtendDrawerColliders(DrawersScript instance)
        {
            BoxCollider[] boxColliders = instance.GetComponentsInChildren<BoxCollider>();

            foreach (BoxCollider box in boxColliders)
            {
                // Get the TRUE original size (before any modifications)
                if (!originalColliderSizes.ContainsKey(box))
                {
                    pluginInstance.ModLogger.LogWarning($"No original size stored for {box.gameObject.name}!");
                    continue;
                }

                Vector3 originalSize = originalColliderSizes[box];
                Vector3 originalCenter = originalColliderCenters[box];

                // Determine which drawer this collider belongs to
                int drawerIndex = GetDrawerIndexForCollider(box, instance);

                // Choose multipliers based on drawer index
                float depthMultiplier, widthMultiplier, heightMultiplier;
                if (drawerIndex == 0 || drawerIndex == 1)
                {
                    // Top drawers - use configured top drawer values
                    depthMultiplier = Plugin.TopDrawerDepthMultiplier.Value;
                    widthMultiplier = Plugin.TopDrawerWidthMultiplier.Value;
                    heightMultiplier = Plugin.TopDrawerHeightMultiplier.Value;
                }
                else
                {
                    // Other drawers - use configured other drawer values
                    depthMultiplier = Plugin.OtherDrawerDepthMultiplier.Value;
                    widthMultiplier = Plugin.OtherDrawerWidthMultiplier.Value;
                    heightMultiplier = Plugin.OtherDrawerHeightMultiplier.Value;
                }

                // Calculate new size based on ORIGINAL size
                Vector3 newSize = originalSize;
                Vector3 newCenter = originalCenter;

                newSize.z *= depthMultiplier;
                newCenter.z -= originalSize.z * ((depthMultiplier - 1.0f) / 2.0f);
                newSize.x *= widthMultiplier;
                newSize.y *= heightMultiplier;

                box.size = newSize;
                box.center = newCenter;
            }
        }

        private static int GetDrawerIndexForCollider(BoxCollider collider, DrawersScript instance)
        {
            if (instance.normalDrawerHolders == null)
            {
                return -1;
            }

            for (int i = 0; i < instance.normalDrawerHolders.Length; i++)
            {
                GameObject holder = instance.normalDrawerHolders[i];
                if (holder == null)
                {
                    continue;
                }

                // Check if collider is same, child, or parent of holder
                bool isSame = collider.gameObject == holder;
                bool isChild = collider.transform.IsChildOf(holder.transform);
                bool isParent = holder.transform.IsChildOf(collider.transform);

                if (isSame || isChild || isParent)
                {
                    return i;
                }
            }

            return -1;
        }

        // Adjust collider centers based on drawer open state and animation progress
        private static void UpdateColliderOffsetsForOpenDrawers(DrawersScript instance)
        {
            if (instance.drawerTransforms == null)
                return;

            // Get drawer open states
            bool[] drawerIsOpen = null;
            if (drawerIsOpenField != null)
            {
                drawerIsOpen = (bool[])drawerIsOpenField.GetValue(instance);
            }

            if (drawerIsOpen == null)
                return;

            // Update each collider based on its drawer's open state
            foreach (var kvp in colliderToDrawerIndex)
            {
                BoxCollider box = kvp.Key;
                int drawerIndex = kvp.Value;

                if (drawerIndex < 0 || drawerIndex >= instance.drawerTransforms.Length)
                    continue;

                // Get the drawer transform and its current Z position (animation progress)
                Transform drawerTransform = instance.drawerTransforms[drawerIndex];
                if (drawerTransform == null)
                    continue;

                // Drawer animates from localZ=0 (closed) to localZ=1 (open)
                float drawerOpenAmount = drawerTransform.localPosition.z;

                // Get the open and close offsets for this drawer
                float openOffset, closeOffset;

                if (drawerIndex == 0 || drawerIndex == 1)
                {
                    // Top drawers
                    openOffset = Plugin.TopDrawerOpenDepthOffset.Value;
                    closeOffset = Plugin.TopDrawerCloseDepthOffset.Value;
                }
                else
                {
                    // Other drawers
                    openOffset = Plugin.OtherDrawerOpenDepthOffset.Value;
                    closeOffset = Plugin.OtherDrawerCloseDepthOffset.Value;
                }

                // Calculate adjusted center based on drawer state
                if (!originalColliderCenters.ContainsKey(box))
                    continue;

                Vector3 originalCenter = originalColliderCenters[box];
                Vector3 adjustedCenter = originalCenter;

                // Lerp between close offset (drawerOpenAmount=0) and open offset (drawerOpenAmount=1)
                float interpolatedOffset = Mathf.Lerp(closeOffset, openOffset, drawerOpenAmount);
                adjustedCenter.z += interpolatedOffset;

                box.center = adjustedCenter;
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
                if (menuElement == null)
                    continue;

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
                    if (drawerCloseTime.ContainsKey(menuElement))
                    {
                        drawerCloseTime.Remove(menuElement);
                    }

                    int drawerIndex = GetOrDetermineDrawerIndex(menuElement);

                    if (drawerIndex != -1)
                    {
                        // Reset close accumulation if cursor returns to the currently peeked drawer
                        if (drawerIndex == currentlyPeekedDrawer && isAccumulatingMovement)
                        {
                            isAccumulatingMovement = false;
                            accumulatedCameraMovement = 0f;
                        }

                        // If a different drawer is already open, swap if cooldown has passed
                        if (currentlyPeekedDrawer != -1 && currentlyPeekedDrawer != drawerIndex)
                        {
                            if (Time.time >= lastSwapTime + SWAP_COOLDOWN)
                            {
                                CloseDrawerQuietly(currentlyPeekedDrawer);
                                PeekDrawer(drawerIndex);
                                lastSwapTime = Time.time;
                                drawerHoverState[menuElement] = true;
                            }
                        }
                        else if (currentlyPeekedDrawer == -1)
                        {
                            // No drawer open, just open this one
                            PeekDrawer(drawerIndex);
                            drawerHoverState[menuElement] = true;
                        }
                        else
                        {
                            // Same drawer we're already hovering - mark as hovered
                            drawerHoverState[menuElement] = true;
                        }
                    }
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

                        // Only check camera movement for the currently peeked drawer
                        if (drawerIndex == currentlyPeekedDrawer)
                        {
                            // Start accumulating camera movement instead of instant check
                            if (Plugin.DrawerPeekCameraMovementThreshold.Value > 0f)
                            {
                                StartAccumulatingCameraMovement();

                                if (Plugin.DebugPatch.Value)
                                {
                                    pluginInstance.ModLogger.LogInfo($"Drawer {drawerIndex}: Started accumulating camera movement");
                                }
                            }
                            else
                            {
                                // No threshold, allow immediate close
                                if (!drawerCloseTime.ContainsKey(menuElement))
                                {
                                    drawerCloseTime[menuElement] = Time.time + CLOSE_GRACE_PERIOD;
                                }
                            }
                        }
                        else
                        {
                            // Not the currently peeked drawer, allow normal close
                            if (!drawerCloseTime.ContainsKey(menuElement))
                            {
                                drawerCloseTime[menuElement] = Time.time + CLOSE_GRACE_PERIOD;
                            }
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

            // Adjust collider positions based on drawer open state
            UpdateColliderOffsetsForOpenDrawers(__instance);

            // Update visualization cubes to match current collider positions
            if (Plugin.DebugPatch.Value && !Plugin.HideCoinsTicketsUI.Value)
            {
                UpdateColliderVisualizations(__instance);
            }

            // Accumulate camera movement for closing if needed
            if (isAccumulatingMovement && currentlyPeekedDrawer != -1)
            {
                AccumulateCameraMovement();

                // Check if accumulated movement exceeds threshold
                if (accumulatedCameraMovement >= Plugin.DrawerPeekCameraMovementThreshold.Value)
                {
                    CloseDrawerQuietly(currentlyPeekedDrawer);
                    isAccumulatingMovement = false;
                    accumulatedCameraMovement = 0f;
                }
            }
        }

        // Start accumulating camera movement
        private static void StartAccumulatingCameraMovement()
        {
            if (CameraController.instance == null || CameraController.instance.freeCamTransform == null)
            {
                return;
            }

            isAccumulatingMovement = true;
            accumulatedCameraMovement = 0f;
            lastCameraRotationForAccumulation = CameraController.instance.freeCamTransform.eulerAngles;
        }

        // Accumulate camera movement each frame (for closing)
        private static void AccumulateCameraMovement()
        {
            if (CameraController.instance == null || CameraController.instance.freeCamTransform == null)
            {
                return;
            }

            Vector3 currentRotation = CameraController.instance.freeCamTransform.eulerAngles;

            // Calculate angular distance since last frame
            float deltaX = Mathf.DeltaAngle(lastCameraRotationForAccumulation.x, currentRotation.x);
            float deltaY = Mathf.DeltaAngle(lastCameraRotationForAccumulation.y, currentRotation.y);
            float deltaZ = Mathf.DeltaAngle(lastCameraRotationForAccumulation.z, currentRotation.z);

            float frameRotation = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

            // Add to accumulator
            accumulatedCameraMovement += frameRotation;

            // Update last rotation for next frame
            lastCameraRotationForAccumulation = currentRotation;
        }

        private static bool IsHoveringAnyDrawerElement(DiegeticMenuElement mainMenuElement, DiegeticMenuController controller)
        {
            if (mainMenuElement == null || controller == null)
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
                isAccumulatingMovement = false; // Clear close accumulation
                accumulatedCameraMovement = 0f;
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
    }
}