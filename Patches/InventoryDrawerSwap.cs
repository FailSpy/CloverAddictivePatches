using HarmonyLib;
using Panik;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloverAddictivePatches.Patches
{
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

            PowerupScript inspectedPowerup = PowerupScript.inspectedPowerup;
            if (inspectedPowerup == null)
                return;

            int drawerIndex = PowerupScript.IsInDrawer(inspectedPowerup.identifier);
            if (drawerIndex >= 0)
                return;

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

        [HarmonyPatch(typeof(ScreenMenuScript), "Open")]
        [HarmonyPostfix]
        static void ApplyCustomPositioning_Postfix()
        {
            if (!menuWasModified || modifiedMenuOptionCount <= 4)
                return;

            float menuHeight = ScreenMenuScript.instance.backImage.rectTransform.sizeDelta.y;
            float baselineHeight = 170f;
            float extraHeight = Mathf.Max(0, menuHeight - baselineHeight);
            float marginBuffer = 40f;
            float yPosition = -20f - (menuHeight / 2f) + extraHeight + marginBuffer;

            ScreenMenuScript.instance.positionShifter.anchoredPosition = new Vector2(0f, yPosition);
        }

        private static void CleanupAfterFailedSwap()
        {
            PowerupScript.inspectedPowerup = null;
            VirtualCursors.CursorDesiredVisibilitySet(0, false);
            DrawersScript.CloseAll();
            InspectorScript.Close();
            currentInspectedPowerup = PowerupScript.Identifier.undefined;
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

            PowerupScript equippedPowerup = PowerupScript.FindPowerup(equippedItem, out bool isEquipped, out bool isInDrawer);
            if (equippedPowerup == null || !isEquipped)
                return;

            int equippedPosition = PowerupScript.list_EquippedNormal.IndexOf(equippedPowerup);

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
                CleanupAfterFailedSwap();
                return;
            }

            bool equipSuccess = PowerupScript.Equip(drawerItem, false, false);
            if (!equipSuccess)
            {
                RollbackSwap(equippedItem, drawerItem, drawerIndex, equippedPosition);
                return;
            }

            if (!PowerupScript.IsEquipped(drawerItem))
            {
                RollbackSwap(equippedItem, drawerItem, drawerIndex, equippedPosition);
                return;
            }

            // Position-preserving: move drawer item to equipped item's original position
            PowerupScript drawerPowerupNowEquipped = PowerupScript.FindPowerup(drawerItem, out _, out _);
            if (drawerPowerupNowEquipped != null && equippedPosition >= 0)
            {
                PowerupScript.list_EquippedNormal.Remove(drawerPowerupNowEquipped);

                if (equippedPosition <= PowerupScript.list_EquippedNormal.Count)
                    PowerupScript.list_EquippedNormal.Insert(equippedPosition, drawerPowerupNowEquipped);
                else
                    PowerupScript.list_EquippedNormal.Add(drawerPowerupNowEquipped);

                PowerupScript.RefreshPlacementAll();
            }

            Sound.Play("SoundMenuSelect");
            PowerupScript.inspectedPowerup = null;
            VirtualCursors.CursorDesiredVisibilitySet(0, false);
            DrawersScript.CloseAll();
            InspectorScript.Close();
            currentInspectedPowerup = PowerupScript.Identifier.undefined;
        }

        private static void RollbackSwap(
            PowerupScript.Identifier equippedItem,
            PowerupScript.Identifier drawerItem,
            int drawerIndex,
            int equippedPosition)
        {
            PowerupScript equippedPowerupInDrawer = PowerupScript.GetDrawerPowerup(drawerIndex);
            if (equippedPowerupInDrawer != null)
            {
                PowerupScript.ThrowAwayCanTriggerEffects_Set(false);
                PowerupScript.SuppressThrowAwaySound();
                PowerupScript.SuppressThrowAwayAnimation();
                PowerupScript.ThrowAway(equippedItem, false);
                PowerupScript.ThrowAwayCanTriggerEffects_Set(true);

                // Force re-equip bypasses inventory space limits
                PowerupScript.EquipFlag_IgnoreSpaceCondition();
                PowerupScript.Equip(equippedItem, false, false);

                // Position-preserving rollback: restore to original position
                PowerupScript reequippedPowerup = PowerupScript.FindPowerup(equippedItem, out _, out _);
                if (reequippedPowerup != null && equippedPosition >= 0)
                {
                    PowerupScript.list_EquippedNormal.Remove(reequippedPowerup);

                    if (equippedPosition <= PowerupScript.list_EquippedNormal.Count)
                        PowerupScript.list_EquippedNormal.Insert(equippedPosition, reequippedPowerup);
                    else
                        PowerupScript.list_EquippedNormal.Add(reequippedPowerup);

                    PowerupScript.RefreshPlacementAll();
                }
            }

            PowerupScript.PutInDrawer(drawerItem, false, drawerIndex);
            CleanupAfterFailedSwap();
        }
    }
}
