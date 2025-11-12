using HarmonyLib;
using Panik;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Swaps equipped powerups with drawer items from inventory menu.
    /// </summary>
    [HarmonyPatch]
    public class InventoryDrawerSwap
    {
        private static Plugin pluginInstance;

        private static PowerupScript.Identifier currentInspectedPowerup = PowerupScript.Identifier.undefined;
        private static Dictionary<int, int> swapOptionIndexToDrawerIndex = new Dictionary<int, int>();
        private static bool menuWasModified = false;
        private static int modifiedMenuOptionCount = 0;

        public static void Initialize(Plugin instance)
        {
            pluginInstance = instance;
            pluginInstance.ModLogger.LogInfo("InventoryDrawerSwap initialized");
        }

        [HarmonyPatch(typeof(ScreenMenuScript), "Open")]
        [HarmonyPrefix]
        static void AddSwapOptions_Prefix(ref string[] options, ref ScreenMenuScript.OptionEvent[] optionEvents, string title)
        {
            menuWasModified = false;
            modifiedMenuOptionCount = 0;

            if (!Plugin.InventoryDrawerSwapPatch.Value)
                return;

            if (options == null || options.Length != 3)
                return;

            // Menu opens before InspectorScript.Open_AsPowerup(), so use PowerupScript.inspectedPowerup
            PowerupScript inspectedPowerup = PowerupScript.inspectedPowerup;
            if (inspectedPowerup == null)
                return;

            int drawerIndex = PowerupScript.IsInDrawer(inspectedPowerup.identifier);
            if (drawerIndex >= 0)
                return;

            // Skeleton items don't use inventory space normally
            if (inspectedPowerup.category == PowerupScript.Category.skeleton)
                return;

            currentInspectedPowerup = inspectedPowerup.identifier;

            List<int> drawersWithItems = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                PowerupScript drawerPowerup = PowerupScript.GetDrawerPowerup(i);
                if (drawerPowerup != null &&
                    DrawersScript.IsDrawerUnlocked(i) &&
                    drawerPowerup.category != PowerupScript.Category.skeleton)
                {
                    drawersWithItems.Add(i);
                }
            }

            if (drawersWithItems.Count == 0)
                return;

            int newSize = options.Length + drawersWithItems.Count;
            var newOptions = new string[newSize];
            var newEvents = new ScreenMenuScript.OptionEvent[newSize];

            newOptions[0] = options[0];
            newEvents[0] = optionEvents[0];

            newOptions[1] = options[1];
            newEvents[1] = optionEvents[1];

            swapOptionIndexToDrawerIndex.Clear();

            int currentIndex = 2;
            foreach (int i in drawersWithItems)
            {
                PowerupScript drawerPowerup = PowerupScript.GetDrawerPowerup(i);
                string itemName = drawerPowerup.NameGet(false, false);

                newOptions[currentIndex] = $"Swap with {itemName}";
                newEvents[currentIndex] = new ScreenMenuScript.OptionEvent(() => SwapWithDrawer(i));

                swapOptionIndexToDrawerIndex[currentIndex] = i;

                currentIndex++;
            }

            newOptions[currentIndex] = options[2];
            newEvents[currentIndex] = optionEvents[2];

            options = newOptions;
            optionEvents = newEvents;

            menuWasModified = true;
            modifiedMenuOptionCount = newSize;
        }

        /// <summary>
        /// Shifts menu upward when >4 options to prevent falling off screen bottom.
        /// </summary>
        [HarmonyPatch(typeof(ScreenMenuScript), "Open")]
        [HarmonyPostfix]
        static void ApplyCustomPositioning_Postfix()
        {
            if (!menuWasModified)
                return;

            if (modifiedMenuOptionCount <= 4)
                return;

            float menuHeight = ScreenMenuScript.instance.backImage.rectTransform.sizeDelta.y;

            // 4-option menu baseline ~170 canvas units
            float baselineHeight = 170f;
            float extraHeight = Mathf.Max(0, menuHeight - baselineHeight);

            // Shift upward to preserve bottom margin
            float marginBuffer = 40f;
            float yPosition = -20f - (menuHeight / 2f) + extraHeight + marginBuffer;

            ScreenMenuScript.instance.positionShifter.anchoredPosition = new Vector2(0f, yPosition);

            pluginInstance.ModLogger.LogInfo($"Applied custom positioning: menuHeight={menuHeight}, yPosition={yPosition}, optionCount={modifiedMenuOptionCount}");
        }

        private static void SwapWithDrawer(int drawerIndex)
        {
            if (currentInspectedPowerup == PowerupScript.Identifier.undefined)
                return;

            PowerupScript drawerPowerup = PowerupScript.GetDrawerPowerup(drawerIndex);
            if (drawerPowerup == null)
            {
                currentInspectedPowerup = PowerupScript.Identifier.undefined;
                return;
            }

            PowerupScript.Identifier drawerItem = drawerPowerup.identifier;
            PowerupScript.Identifier equippedItem = currentInspectedPowerup;

            // ThrowAway -> PutInDrawer -> Equip sequence to manage powerup lists correctly
            // ThrowAway adds drawer item to list_NotBought so it can be equipped
            PowerupScript.ThrowAwayCanTriggerEffects_Set(false);
            PowerupScript.SuppressThrowAwaySound();
            PowerupScript.SuppressThrowAwayAnimation();
            bool throwSuccess = PowerupScript.ThrowAway(drawerItem, false);
            PowerupScript.ThrowAwayCanTriggerEffects_Set(true);

            if (!throwSuccess)
                return;

            bool putSuccess = PowerupScript.PutInDrawer(equippedItem, false, drawerIndex);

            if (!putSuccess)
            {
                PowerupScript.PutInDrawer(drawerItem, false, drawerIndex);
                return;
            }

            bool equipSuccess = PowerupScript.Equip(drawerItem, false, false);

            if (!equipSuccess)
            {
                PowerupScript.array_InDrawer[drawerIndex] = null;
                PowerupScript.Equip(equippedItem, false, false);
                PowerupScript.PutInDrawer(drawerItem, false, drawerIndex);
                return;
            }

            // Verify the item was actually equipped (not just that Equip() returned true)
            // This handles cases like overfull inventory (9/8 charms) where Equip() shows
            // "You don't have enough slots" dialogue but returns true
            if (!PowerupScript.IsEquipped(drawerItem))
            {
                // Equip claimed success but item isn't actually equipped - rollback
                PowerupScript.array_InDrawer[drawerIndex] = null;
                PowerupScript.Equip(equippedItem, false, false);
                PowerupScript.PutInDrawer(drawerItem, false, drawerIndex);
                return;
            }

            // Only perform cleanup if swap fully succeeded
            Sound.Play("SoundMenuSelect");

            PowerupScript.inspectedPowerup = null;
            VirtualCursors.CursorDesiredVisibilitySet(0, false);
            DrawersScript.CloseAll();
            InspectorScript.Close();

            currentInspectedPowerup = PowerupScript.Identifier.undefined;
        }
    }
}
