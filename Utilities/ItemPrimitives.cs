using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CloverAddictivePatches.Utilities
{
    /// <summary>
    /// 4-level primitive system for safe item operations.
    ///
    /// ARCHITECTURE:
    /// Level 0 (Direct): Direct state manipulation - rollback only
    /// Level 1 (Query): Read-only queries - always safe
    /// Level 2 (Validated): Game method wrappers - respect validation, use for normal operations
    /// Level 4 (Transactional): Multi-step operations with automatic rollback
    ///
    /// KEY CONCEPT - LIMBO STATE:
    /// Items temporarily exist outside all lists during swaps. This enables optimal ordering:
    /// unequip to limbo (frees space) → equip from drawer → move limbo to drawer.
    /// Safe because operations are synchronous - no game ticks between steps.
    ///
    /// CRITICAL: Items in limbo can't use Level 2 methods (FindPowerup can't locate them).
    /// Use Level 0 operations directly with saved object references.
    /// </summary>
    public static class ItemPrimitives
    {
        private static FieldInfo _onUnequipField;
        private static FieldInfo _onUnequipStaticField;
        private static FieldInfo _equippedChachedField;
        private static FieldInfo _inDrawerChachedField;

        static ItemPrimitives()
        {
            var powerupType = typeof(PowerupScript);
            _onUnequipField = AccessTools.Field(powerupType, "onUnequip");
            _onUnequipStaticField = AccessTools.Field(powerupType, "onUnequipStatic");
            _equippedChachedField = AccessTools.Field(powerupType, "equippedChached");
            _inDrawerChachedField = AccessTools.Field(powerupType, "inDrawerChached");
        }

        // ========================================================================
        // LEVEL 0: Direct State Manipulation (rollback only)
        // ========================================================================
        public static void RemoveFromEquippedList(PowerupScript powerup)
        {
            if (powerup == null) return;

            if (powerup.category == PowerupScript.Category.skeleton)
                PowerupScript.list_EquippedSkeleton.Remove(powerup);
            else
                PowerupScript.list_EquippedNormal.Remove(powerup);
        }

        public static void InsertIntoEquippedList(PowerupScript powerup, int position)
        {
            if (powerup == null) return;

            var list = powerup.category == PowerupScript.Category.skeleton
                ? PowerupScript.list_EquippedSkeleton
                : PowerupScript.list_EquippedNormal;

            if (position < 0 || position > list.Count)
                list.Add(powerup);
            else
                list.Insert(position, powerup);
        }

        public static void SetDrawerSlot(int drawerIndex, PowerupScript powerup)
        {
            if (drawerIndex < 0 || drawerIndex >= 4) return;
            PowerupScript.array_InDrawer[drawerIndex] = powerup;
        }

        public static void ClearDrawerSlot(int drawerIndex)
        {
            if (drawerIndex < 0 || drawerIndex >= 4) return;
            PowerupScript.array_InDrawer[drawerIndex] = null;
        }

        // ========================================================================
        // LEVEL 1: Query Operations
        // ========================================================================
        public static (ItemState state, int location) GetItemState(PowerupScript.Identifier item)
        {
            if (PowerupScript.IsEquipped(item))
            {
                var powerup = PowerupScript.FindPowerup(item, out _, out _);
                int position = powerup != null ? PowerupScript.list_EquippedNormal.IndexOf(powerup) : -1;
                return (ItemState.Equipped, position);
            }

            int drawerIndex = PowerupScript.IsInDrawer(item);
            if (drawerIndex >= 0)
                return (ItemState.InDrawer, drawerIndex);

            if (PowerupScript.IsNotBought(item))
                return (ItemState.NotBought, -1);

            return (ItemState.Unknown, -1);
        }

        public static bool IsDrawerSlotEmpty(int drawerIndex)
        {
            if (drawerIndex < 0 || drawerIndex >= 4) return false;
            return PowerupScript.array_InDrawer[drawerIndex] == null;
        }

        public static int GetEquippedPosition(PowerupScript.Identifier item)
        {
            var powerup = PowerupScript.FindPowerup(item, out bool isEquipped, out _);
            if (!isEquipped || powerup == null) return -1;
            return PowerupScript.list_EquippedNormal.IndexOf(powerup);
        }

        public static PowerupScript GetDrawerItem(int drawerIndex)
        {
            if (drawerIndex < 0 || drawerIndex >= 4) return null;
            return PowerupScript.array_InDrawer[drawerIndex];
        }

        // ========================================================================
        // LEVEL 2: Game Method Wrappers
        // ========================================================================
        public static bool TryEquip(PowerupScript.Identifier item)
        {
            return PowerupScript.Equip(item, false, false);
        }

        public static bool TryPutInDrawer(PowerupScript.Identifier item, int drawerIndex)
        {
            return PowerupScript.PutInDrawer(item, false, drawerIndex);
        }

        /// <summary>
        /// Unequips an item to "limbo" state (not in any list).
        /// Calls onUnequip (event cleanup) but NOT onThrowAway (preserves activation counters, uses, etc.).
        /// Safe because operations are synchronous - item must be moved to valid state before frame ends.
        /// </summary>
        public static bool UnequipToLimbo(PowerupScript.Identifier item)
        {
            var powerup = PowerupScript.FindPowerup(item, out bool isEquipped, out _);
            if (!isEquipped || powerup == null)
                return false;

            var onUnequip = _onUnequipField?.GetValue(powerup) as PowerupScript.PowerupEvent;
            if (onUnequip != null)
                onUnequip(powerup);

            var onUnequipStatic = _onUnequipStaticField?.GetValue(null) as PowerupScript.PowerupEvent;
            if (onUnequipStatic != null)
                onUnequipStatic(powerup);

            RemoveFromEquippedList(powerup);

            _equippedChachedField?.SetValue(powerup, false);
            _inDrawerChachedField?.SetValue(powerup, false);

            if (powerup.sacredGlowHolder != null)
                powerup.sacredGlowHolder.SetActive(false);

            return true;
        }

        /// <summary>Bypasses inventory space validation. For rollback only.</summary>
        public static void ForceEquip(PowerupScript.Identifier item)
        {
            PowerupScript.EquipFlag_IgnoreSpaceCondition();
            PowerupScript.Equip(item, false, false);
        }

        /// <summary>Forces item into drawer. For rollback only.</summary>
        public static void ForcePutInDrawer(PowerupScript.Identifier item, int drawerIndex)
        {
            if (drawerIndex < 0 || drawerIndex >= 4) return;

            var powerup = PowerupScript.FindPowerup(item, out bool isEquipped, out bool isInDrawer);
            if (powerup == null) return;

            if (isEquipped)
                RemoveFromEquippedList(powerup);
            else if (isInDrawer)
            {
                int currentDrawerIndex = PowerupScript.IsInDrawer(item);
                if (currentDrawerIndex >= 0)
                    ClearDrawerSlot(currentDrawerIndex);
            }

            SetDrawerSlot(drawerIndex, powerup);
            _equippedChachedField?.SetValue(powerup, false);
            _inDrawerChachedField?.SetValue(powerup, true);
        }
        public static void MoveInEquippedList(PowerupScript.Identifier item, int newPosition)
        {
            var powerup = PowerupScript.FindPowerup(item, out bool isEquipped, out _);
            if (!isEquipped || powerup == null) return;

            RemoveFromEquippedList(powerup);
            InsertIntoEquippedList(powerup, newPosition);
            PowerupScript.RefreshPlacementAll();
        }

        // ========================================================================
        // LEVEL 4: Transactional Compositions
        // ========================================================================

        /// <summary>
        /// Swaps an equipped item with a drawer item. Automatically rolls back on failure.
        /// Preserves item state (activations, uses) by using limbo instead of ThrowAway.
        /// Ordering: unequip to limbo → equip from drawer → move limbo to drawer.
        /// </summary>
        public static bool SwapEquippedWithDrawer(
            PowerupScript.Identifier equippedItem,
            int drawerIndex,
            bool preservePosition = true)
        {
            if (drawerIndex < 0 || drawerIndex >= 4)
                return false;

            var equippedPowerup = PowerupScript.FindPowerup(equippedItem, out bool isEquipped, out _);
            if (!isEquipped || equippedPowerup == null)
                return false;

            var drawerPowerup = GetDrawerItem(drawerIndex);
            if (drawerPowerup == null)
                return false;

            PowerupScript.Identifier drawerItem = drawerPowerup.identifier;
            int savedPosition = preservePosition ? GetEquippedPosition(equippedItem) : -1;

            PowerupScript.SuppressThrowAwaySound();
            PowerupScript.SuppressThrowAwayAnimation();

            if (!UnequipToLimbo(equippedItem))
                return false;

            bool equipSuccess = TryEquip(drawerItem);
            if (!equipSuccess)
            {
                // Rollback: Item is in limbo, must use Level 0 with saved object reference
                InsertIntoEquippedList(equippedPowerup, savedPosition >= 0 ? savedPosition : 0);
                _equippedChachedField?.SetValue(equippedPowerup, true);
                _inDrawerChachedField?.SetValue(equippedPowerup, false);
                PowerupScript.RefreshPlacementAll();
                return false;
            }

            // Item is in limbo - use Level 0 operations directly
            SetDrawerSlot(drawerIndex, equippedPowerup);
            _equippedChachedField?.SetValue(equippedPowerup, false);
            _inDrawerChachedField?.SetValue(equippedPowerup, true);

            if (preservePosition && savedPosition >= 0)
                MoveInEquippedList(drawerItem, savedPosition);
            else
                PowerupScript.RefreshPlacementAll();

            return true;
        }

        public enum ItemState
        {
            Unknown,
            NotBought,
            Equipped,
            InDrawer
        }
    }
}
