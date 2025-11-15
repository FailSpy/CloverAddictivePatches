using HarmonyLib;
using System;
using System.Numerics;
using System.Reflection;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Minimal crash prevention for pattern value overflow in v1.2+.
    ///
    /// Background:
    /// - In v1.2, the game clamps pattern values to prevent reaching infinity
    /// - Without this clamp, SlotMachineScript._ComputePatternValue crashes when it tries
    ///   to convert double.PositiveInfinity to BigInteger (throws OverflowException)
    /// - However, the v1.2 clamp at double.MaxValue (~1.8E+308) blocks E999+ progression
    ///
    /// This patch:
    /// - Prevents the crash by safely handling infinity/overflow cases
    /// - Does NOT attempt to provide accurate E999+ calculations
    /// - Simply clamps overflow values to prevent exceptions
    ///
    /// For accurate E999+ support, use BigIntegerPatternTracking instead.
    ///
    /// Configuration:
    /// - Applied once at game startup based on config file setting
    /// - Not toggleable via in-game Mod Options (requires game restart to change)
    /// </summary>
    [HarmonyPatch]
    public class PatternOverflowCrashFix
    {
        /// <summary>
        /// Manual patch for SlotMachineScript._ComputePatternValue.
        /// Applied once at startup if enabled in config.
        /// </summary>
        public static void ApplyIfEnabled(Harmony harmony)
        {
            if (!Plugin.EnablePatternOverflowCrashFix.Value)
            {
                Plugin.Instance.ModLogger.LogInfo("PatternOverflowCrashFix: Disabled in config, not applying patch");
                return;
            }

            try
            {
                // Get the SlotMachineScript type
                var slotMachineScriptType = Type.GetType("SlotMachineScript, Assembly-CSharp");
                if (slotMachineScriptType == null)
                {
                    Plugin.Instance.ModLogger.LogError("PatternOverflowCrashFix: Could not find SlotMachineScript type");
                    return;
                }

                // Get the _ComputePatternValue method
                var computePatternValueMethod = slotMachineScriptType.GetMethod(
                    "_ComputePatternValue",
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                if (computePatternValueMethod == null)
                {
                    Plugin.Instance.ModLogger.LogError("PatternOverflowCrashFix: Could not find _ComputePatternValue method");
                    return;
                }

                // Patch with prefix
                var prefixMethod = typeof(PatternOverflowCrashFix).GetMethod(
                    nameof(ComputePatternValue_Prefix),
                    BindingFlags.NonPublic | BindingFlags.Static
                );

                harmony.Patch(
                    computePatternValueMethod,
                    prefix: new HarmonyMethod(prefixMethod)
                );

                Plugin.Instance.ModLogger.LogInfo("PatternOverflowCrashFix: Successfully patched _ComputePatternValue (crash prevention active)");
            }
            catch (Exception e)
            {
                Plugin.Instance.ModLogger.LogError($"PatternOverflowCrashFix: Failed to apply patch: {e}");
            }
        }

        /// <summary>
        /// Prefix for _ComputePatternValue that prevents the crash from infinity values.
        /// Replaces the buggy conversion with a safe one that handles overflow.
        /// </summary>
        static bool ComputePatternValue_Prefix(
            object patternKind,
            object symbolKind,
            ref BigInteger __result)
        {
            try
            {
                // Get the GameplayData type for method calls
                var gameplayDataType = Type.GetType("GameplayData, Assembly-CSharp");

                // Get enum values
                int patternKindValue = (int)patternKind;
                int symbolKindValue = (int)symbolKind;

                // Check for undefined/count patterns (error cases from original code)
                // PatternScript.Kind.undefined = -1, count = 16
                if (patternKindValue == -1 || patternKindValue == 16)
                {
                    __result = new BigInteger(-1);
                    return false; // Skip original
                }

                // SymbolScript.Kind.undefined = -1, count = 11
                if (symbolKindValue == -1 || symbolKindValue == 11)
                {
                    __result = new BigInteger(-1);
                    return false; // Skip original
                }

                // Get methods via reflection
                var allSymbolsMultiplierMethod = gameplayDataType.GetMethod("AllSymbolsMultiplierGet", BindingFlags.Public | BindingFlags.Static);
                var symbolCoinsValueMethod = gameplayDataType.GetMethod("Symbol_CoinsOverallValue_Get", BindingFlags.Public | BindingFlags.Static);
                var allPatternsMultiplierMethod = gameplayDataType.GetMethod("AllPatternsMultiplierGet", BindingFlags.Public | BindingFlags.Static);
                var patternValueMethod = gameplayDataType.GetMethod("Pattern_ValueOverall_Get", BindingFlags.Public | BindingFlags.Static);

                // Get multipliers (these are already BigInteger in the game)
                BigInteger allSymbolsMult = (BigInteger)allSymbolsMultiplierMethod.Invoke(null, new object[] { true });
                BigInteger symbolCoinsValue = (BigInteger)symbolCoinsValueMethod.Invoke(null, new object[] { symbolKind });
                BigInteger allPatternsMult = (BigInteger)allPatternsMultiplierMethod.Invoke(null, new object[] { true });

                // Get pattern value (this is a double that can overflow to infinity)
                double patternValue = (double)patternValueMethod.Invoke(null, new object[] { patternKind, true });

                // THE FIX: Safely convert to BigInteger, handling overflow
                BigInteger patternValueBigInt;

                if (double.IsInfinity(patternValue) || double.IsNaN(patternValue))
                {
                    // Overflow detected - use double.MaxValue as safe fallback
                    // This matches v1.2's clamping behavior but doesn't block the calculation
                    patternValueBigInt = new BigInteger(double.MaxValue * 100);

                    Plugin.Instance.ModLogger.LogWarning($"PatternOverflowCrashFix: Pattern {patternKindValue} overflowed, clamped to safe value (E999+ not supported)");
                }
                else if (patternValue > 1e200)
                {
                    // Very large value - use logarithmic conversion for precision
                    double logValue = Math.Log10(patternValue * 100);
                    int exponentInt = (int)Math.Floor(logValue);
                    double mantissa = Math.Pow(10, logValue - exponentInt);

                    patternValueBigInt = new BigInteger(mantissa) * BigInteger.Pow(10, exponentInt);
                }
                else
                {
                    // Normal value - direct conversion (original game behavior)
                    patternValueBigInt = new BigInteger(patternValue * 100);
                }

                // Calculate final result (same formula as original game)
                __result = allSymbolsMult * symbolCoinsValue * allPatternsMult * patternValueBigInt / new BigInteger(100);

                return false; // Skip original method (prevent crash)
            }
            catch (Exception e)
            {
                Plugin.Instance.ModLogger.LogError($"PatternOverflowCrashFix: Error in prefix: {e}");
                return true; // Fall back to original (may crash, but at least we tried)
            }
        }
    }
}
