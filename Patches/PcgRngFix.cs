using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Panik;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Replaces CloverPit's custom PRNG with PCG (Permuted Congruential Generator).
    ///
    /// Benefits:
    /// - Better statistical quality (passes TestU01 BigCrush)
    /// - Longer period (2^64 vs ~2^31)
    /// - Faster execution
    /// - Industry-standard algorithm
    ///
    /// Maintains save compatibility by reusing existing serialized fields.
    /// Still deterministic - same seed produces same sequence (anti-save-scum works).
    /// </summary>
    [HarmonyPatch]
    public class PcgRngFix
    {
        // PCG-XSH-RR variant constant
        // Note: increment is derived from seed per-instance for stream independence
        private const ulong PCG_MULTIPLIER = 6364136223846793005UL;

        // Cached field reflections (initialized once, reused for all calls)
        private static readonly FieldInfo randomNumberField = AccessTools.Field(typeof(Rng), "randomNumber");
        private static readonly FieldInfo stateIndexField = AccessTools.Field(typeof(Rng), "stateIndex");
        private static readonly FieldInfo seedField = AccessTools.Field(typeof(Rng), "seed");

        // Per-instance increment cache (seed never changes after SetState, so cache increment)
        private static readonly ConditionalWeakTable<Rng, IncrementHolder> incrementCache =
            new ConditionalWeakTable<Rng, IncrementHolder>();

        private class IncrementHolder
        {
            public ulong Increment;
        }

        /// <summary>
        /// Replaces Rng.Raw() with PCG algorithm.
        ///
        /// PCG state is stored in existing fields:
        /// - state_high: randomNumber (uint)
        /// - state_low: stateIndex (uint)
        /// - increment: derived from seed (cached per-instance)
        /// </summary>
        [HarmonyPatch(typeof(Rng), "Raw")]
        [HarmonyPrefix]
        static bool Raw_Prefix(Rng __instance, ref uint __result)
        {
            if (!Plugin.EnablePcgRngFix.Value)
                return true; // Run original

            // Read PCG state from Rng fields (using cached FieldInfo)
            uint state_high = (uint)randomNumberField.GetValue(__instance);
            uint state_low = (uint)stateIndexField.GetValue(__instance);
            ulong state = ((ulong)state_high << 32) | state_low;

            // Get cached increment (or compute and cache on first access)
            if (!incrementCache.TryGetValue(__instance, out var holder))
            {
                uint seed = (uint)seedField.GetValue(__instance);
                holder = new IncrementHolder { Increment = ((ulong)seed << 1) | 1UL };
                incrementCache.Add(__instance, holder);
            }
            ulong increment = holder.Increment;

            // PCG-XSH-RR algorithm
            // 1. Save old state for output
            ulong oldstate = state;

            // 2. Advance state: state = state * multiplier + increment
            state = unchecked(state * PCG_MULTIPLIER + increment);

            // 3. Calculate output using XSH-RR (xorshift high, random rotation)
            uint xorshifted = (uint)(((oldstate >> 18) ^ oldstate) >> 27);
            int rot = (int)(oldstate >> 59);
            uint result = (xorshifted >> rot) | (xorshifted << ((-rot) & 31));

            // Store new state back to Rng fields
            state_high = (uint)(state >> 32);
            state_low = (uint)(state & 0xFFFFFFFF);
            randomNumberField.SetValue(__instance, state_high);
            stateIndexField.SetValue(__instance, state_low);

            __result = result;
            return false; // Skip original method
        }

        /// <summary>
        /// Patches SetState to properly initialize PCG state.
        /// Uses standard PCG initialization mixing for better seed distribution.
        /// </summary>
        [HarmonyPatch(typeof(Rng), "SetState", new Type[] { typeof(int), typeof(uint) })]
        [HarmonyPrefix]
        static bool SetState_Prefix(Rng __instance, int _seed, uint stateIndex)
        {
            if (!Plugin.EnablePcgRngFix.Value)
                return true; // Run original

            uint seed = (uint)_seed;

            // Store seed in field
            seedField.SetValue(__instance, seed);

            // Update increment cache (seed changed, so increment changes)
            ulong increment = ((ulong)seed << 1) | 1UL;
            var holder = new IncrementHolder { Increment = increment };

            // ConditionalWeakTable doesn't have a direct update method, so remove then add
            incrementCache.Remove(__instance);
            incrementCache.Add(__instance, holder);

            // Initialize PCG state using standard mixing
            // This provides better distribution than just "state = seed"
            ulong state = 0UL;
            state = unchecked(state * PCG_MULTIPLIER + increment);
            state = unchecked(state + (ulong)seed);
            state = unchecked(state * PCG_MULTIPLIER + increment);

            // Advance state by stateIndex iterations
            for (uint i = 0; i < stateIndex; i++)
            {
                state = unchecked(state * PCG_MULTIPLIER + increment);
            }

            // Store state in fields
            randomNumberField.SetValue(__instance, (uint)(state >> 32));
            stateIndexField.SetValue(__instance, (uint)(state & 0xFFFFFFFF));

            return false; // Skip original
        }

        /// <summary>
        /// Optional: Fix float precision issue.
        /// Converts uint to double first, then to float for better precision.
        /// </summary>
        [HarmonyPatch(typeof(Rng), "Value", MethodType.Getter)]
        [HarmonyPrefix]
        static bool Value_Prefix(Rng __instance, ref float __result)
        {
            if (!Plugin.EnablePcgRngFix.Value)
                return true; // Run original

            // Call Raw() directly - Harmony routes this through our patched version
            uint raw = __instance.Raw();

            // Convert to double first for better precision
            // Then to float (still has precision loss, but slightly better)
            double d = raw / 4294967296.0;
            __result = (float)d;

            return false; // Skip original
        }
    }
}
