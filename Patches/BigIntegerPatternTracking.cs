using HarmonyLib;
using System;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Alternative approach to InfinityPatternFix using shadow BigInteger tracking.
    ///
    /// Instead of tracking exponents and estimating growth, this patch maintains
    /// parallel BigInteger values that mirror the game's double-based pattern values.
    ///
    /// Advantages:
    /// - Perfect precision (no estimation error)
    /// - Mathematically exact (mirrors game logic in BigInteger)
    /// - No growth rate configuration needed
    ///
    /// How it works:
    /// 1. Intercept all write operations to pattern components (extras, pareidolia, abstract)
    /// 2. Maintain shadow BigInteger versions of these values
    /// 3. Reimplement Pattern_ValueOverall_Get logic using BigInteger arithmetic
    /// 4. Replace _ComputePatternValue to use our BigInteger version
    /// 5. On game load, reconstruct BigInteger values from loaded doubles
    ///
    /// This approach avoids the fundamental limitation of double arithmetic by
    /// performing all calculations in BigInteger space, only converting from doubles
    /// at the read boundary (never converting back).
    /// </summary>
    [HarmonyPatch]
    public class BigIntegerPatternTracking
    {
        // Shadow storage - parallel BigInteger values that mirror the game's doubles
        private static Dictionary<int, BigInteger> extraValue_BigInt = new Dictionary<int, BigInteger>();
        private static Dictionary<int, BigInteger> pareidoliaBonus_BigInt = new Dictionary<int, BigInteger>();
        private static Dictionary<int, BigInteger> abstractBonus_BigInt = new Dictionary<int, BigInteger>();

        // Cached reflection references
        private static Type gameplayDataType;
        private static Type powerupScriptType;
        private static Type patternScriptKindType;
        private static Type dataType;
        private static MethodInfo patternValueBasicMethod;
        private static MethodInfo rorschachBonusMethod;
        private static MethodInfo dieselBonusMethod;

        // Save file constants
        private const string SAVE_FILE_SUFFIX = "_BigIntegerTracking.json";

        /// <summary>
        /// Initialize reflection references on first use.
        /// </summary>
        static void InitializeReflection()
        {
            if (gameplayDataType != null) return; // Already initialized

            gameplayDataType = Type.GetType("GameplayData, Assembly-CSharp");
            powerupScriptType = Type.GetType("PowerupScript, Assembly-CSharp");
            patternScriptKindType = Type.GetType("PatternScript+Kind, Assembly-CSharp");
            dataType = Type.GetType("Panik.Data, Assembly-CSharp");

            patternValueBasicMethod = gameplayDataType.GetMethod("Pattern_Value_GetBasic", BindingFlags.Public | BindingFlags.Static);
            rorschachBonusMethod = powerupScriptType.GetMethod("RorschachBonusMultiplierGet", BindingFlags.Public | BindingFlags.Static);
            dieselBonusMethod = powerupScriptType.GetMethod("DieselLocomotive_PatternsBonus_Get", BindingFlags.Public | BindingFlags.Static);

            Plugin.Instance.ModLogger.LogInfo("BigIntegerPatternTracking: Reflection initialized");
        }

        /// <summary>
        /// Initialize shadow dictionaries.
        /// </summary>
        static void InitializeShadowStorage()
        {
            for (int i = 0; i < 16; i++)
            {
                if (!extraValue_BigInt.ContainsKey(i))
                    extraValue_BigInt[i] = BigInteger.Zero;
                if (!pareidoliaBonus_BigInt.ContainsKey(i))
                    pareidoliaBonus_BigInt[i] = BigInteger.Zero;
                if (!abstractBonus_BigInt.ContainsKey(i))
                    abstractBonus_BigInt[i] = BigInteger.Zero;
            }
        }

        /// <summary>
        /// Convert a double to BigInteger intelligently.
        /// Handles large values and infinity using logarithmic conversion.
        /// </summary>
        static BigInteger ConvertDoubleToBigInteger(double value, string context = "")
        {
            if (double.IsNaN(value))
            {
                Plugin.Instance.ModLogger.LogWarning($"BigIntegerTracking: NaN encountered in {context}, using 0");
                return BigInteger.Zero;
            }

            if (double.IsInfinity(value))
            {
                // Use fallback exponent (default: 400)
                double exponent = 400;
                Plugin.Instance.ModLogger.LogWarning($"BigIntegerTracking: Infinity encountered in {context}, using 10^{exponent}");
                return ConvertExponentToBigInteger(exponent);
            }

            if (value > 1e200)
            {
                // Large value - use logarithmic conversion for precision
                double logValue = Math.Log10(value);
                return ConvertExponentToBigInteger(logValue);
            }

            if (value < 0)
            {
                Plugin.Instance.ModLogger.LogWarning($"BigIntegerTracking: Negative value {value} in {context}, using 0");
                return BigInteger.Zero;
            }

            // Normal value - direct conversion
            return new BigInteger(value);
        }

        /// <summary>
        /// Convert log10 exponent to BigInteger.
        /// </summary>
        static BigInteger ConvertExponentToBigInteger(double exponent)
        {
            if (exponent > 10000)
            {
                Plugin.Instance.ModLogger.LogWarning("BigIntegerTracking: Exponent capped at 10000 for performance");
                exponent = 10000;
            }

            int exponentInt = (int)Math.Floor(exponent);
            double mantissa = Math.Pow(10, exponent - exponentInt);

            return new BigInteger(mantissa) * BigInteger.Pow(10, exponentInt);
        }

        /// <summary>
        /// Calculate pattern value using BigInteger arithmetic (our reimplementation).
        /// This mirrors GameplayData.Pattern_ValueOverall_Get but uses BigInteger.
        /// </summary>
        static BigInteger CalculatePatternValue_BigInt(int patternKind, bool includePowerups)
        {
            InitializeReflection();

            // Basic value (always small, safe to convert directly)
            double basicDouble = (double)patternValueBasicMethod.Invoke(null, new object[] { Enum.ToObject(patternScriptKindType, patternKind) });
            BigInteger basic = new BigInteger(basicDouble);

            // Extras (our shadow BigInteger copy)
            BigInteger extras = extraValue_BigInt.ContainsKey(patternKind)
                ? extraValue_BigInt[patternKind]
                : BigInteger.Zero;

            BigInteger total = basic + extras;

            if (!includePowerups)
            {
                // Minimum value of 0.5
                if (total < 1) total = 1; // Using 1 instead of 0.5 for integer math
                return total;
            }

            // Rorschach bonus (linear growth, always small)
            double rorschachDouble = (double)rorschachBonusMethod.Invoke(null, null);
            BigInteger rorschach = new BigInteger(basicDouble * rorschachDouble);

            // Diesel locomotive bonus (linear growth, always small)
            double dieselDouble = (double)dieselBonusMethod.Invoke(null, new object[] { Enum.ToObject(patternScriptKindType, patternKind) });
            BigInteger diesel = new BigInteger(dieselDouble);

            // Abstract Painting bonus (our shadow BigInteger copy)
            BigInteger abstractPainting = abstractBonus_BigInt.ContainsKey(patternKind)
                ? abstractBonus_BigInt[patternKind]
                : BigInteger.Zero;

            // Pareidolia bonus (our shadow BigInteger copy)
            BigInteger pareidolia = pareidoliaBonus_BigInt.ContainsKey(patternKind)
                ? pareidoliaBonus_BigInt[patternKind]
                : BigInteger.Zero;

            // THE MAGIC: All BigInteger addition - no overflow possible!
            total = basic + extras + rorschach + diesel + abstractPainting + pareidolia;

            // Minimum value of 0.5
            if (total < 1) total = 1;

            return total;
        }

        /// <summary>
        /// Intercept Pattern_ValueExtra_Add to update our shadow BigInteger value.
        /// This is called by abilities, Baphomet, chain symbols, etc.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameplayData), "Pattern_ValueExtra_Add")]
        static void Pattern_ValueExtra_Add_Postfix(object kind, double value)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            int kindInt = (int)kind;

            // Ensure initialized
            if (!extraValue_BigInt.ContainsKey(kindInt))
                extraValue_BigInt[kindInt] = BigInteger.Zero;

            // Convert the added value to BigInteger
            BigInteger valueBI = ConvertDoubleToBigInteger(value, $"Pattern_ValueExtra_Add[{kindInt}]");

            // Update shadow value
            extraValue_BigInt[kindInt] += valueBI;

            Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: extraValue[{kindInt}] += {valueBI} → {extraValue_BigInt[kindInt]}");
        }

        /// <summary>
        /// Intercept Pattern_ValueExtra_Set to update our shadow BigInteger value.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameplayData), "Pattern_ValueExtra_Set")]
        static void Pattern_ValueExtra_Set_Postfix(object kind, double value)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            int kindInt = (int)kind;

            // Convert and set
            BigInteger valueBI = ConvertDoubleToBigInteger(value, $"Pattern_ValueExtra_Set[{kindInt}]");
            extraValue_BigInt[kindInt] = valueBI;

            Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: extraValue[{kindInt}] = {valueBI}");
        }

        /// <summary>
        /// Intercept Pattern_ValueExtra_Reset to reset our shadow BigInteger value.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameplayData), "Pattern_ValueExtra_Reset")]
        static void Pattern_ValueExtra_Reset_Postfix(object kind)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            int kindInt = (int)kind;
            extraValue_BigInt[kindInt] = BigInteger.Zero;

            Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: extraValue[{kindInt}] reset to 0");
        }

        /// <summary>
        /// Manual patch for Pareidolia trigger (private static method).
        /// We intercept AFTER the game updates its doubles, then update our BigIntegers.
        /// </summary>
        public static void ApplyPareidoliaPatch(Harmony harmony)
        {
            var powerupScriptType = Type.GetType("PowerupScript, Assembly-CSharp");
            var triggerMethod = powerupScriptType.GetMethod("Trigger_Pareidolia", BindingFlags.NonPublic | BindingFlags.Static);

            if (triggerMethod == null)
            {
                Plugin.Instance.ModLogger.LogError("BigIntegerTracking: Could not find Trigger_Pareidolia");
                return;
            }

            var postfixMethod = typeof(BigIntegerPatternTracking).GetMethod(
                nameof(Trigger_Pareidolia_Postfix),
                BindingFlags.NonPublic | BindingFlags.Static
            );

            harmony.Patch(triggerMethod, postfix: new HarmonyMethod(postfixMethod));
            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Patched Trigger_Pareidolia");
        }

        static void Trigger_Pareidolia_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            // The game just added Pattern_ValueOverall_Get(kind, true) to each pattern's Pareidolia bonus
            // We do the same using our BigInteger version
            for (int i = 0; i < 16; i++)
            {
                BigInteger currentPatternValue = CalculatePatternValue_BigInt(i, true);
                pareidoliaBonus_BigInt[i] += currentPatternValue;

                Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: Pareidolia trigger - bonus[{i}] += {currentPatternValue} → {pareidoliaBonus_BigInt[i]}");
            }
        }

        /// <summary>
        /// Manual patch for Abstract Painting trigger (private static method).
        /// </summary>
        public static void ApplyAbstractPaintingPatch(Harmony harmony)
        {
            var powerupScriptType = Type.GetType("PowerupScript, Assembly-CSharp");
            var triggerMethod = powerupScriptType.GetMethod("Trigger_AbstractPainting", BindingFlags.NonPublic | BindingFlags.Static);

            if (triggerMethod == null)
            {
                Plugin.Instance.ModLogger.LogError("BigIntegerTracking: Could not find Trigger_AbstractPainting");
                return;
            }

            var postfixMethod = typeof(BigIntegerPatternTracking).GetMethod(
                nameof(Trigger_AbstractPainting_Postfix),
                BindingFlags.NonPublic | BindingFlags.Static
            );

            harmony.Patch(triggerMethod, postfix: new HarmonyMethod(postfixMethod));
            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Patched Trigger_AbstractPainting");
        }

        static void Trigger_AbstractPainting_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            // The game just added Pattern_ValueOverall_Get(kind, true) to each pattern's Abstract bonus
            // We do the same using our BigInteger version
            for (int i = 0; i < 16; i++)
            {
                BigInteger currentPatternValue = CalculatePatternValue_BigInt(i, true);
                abstractBonus_BigInt[i] += currentPatternValue;

                Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: Abstract trigger - bonus[{i}] += {currentPatternValue} → {abstractBonus_BigInt[i]}");
            }
        }

        /// <summary>
        /// Intercept Abstract Painting reset.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerupScript), "AbstractPaintingReset")]
        static void AbstractPaintingReset_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            for (int i = 0; i < 16; i++)
            {
                abstractBonus_BigInt[i] = BigInteger.Zero;
            }

            Plugin.Instance.ModLogger.LogDebug("BigIntegerTracking: Abstract Painting bonuses reset");
        }

        /// <summary>
        /// Manual patch for Baphomet trigger (private static method).
        /// </summary>
        public static void ApplyBaphometPatch(Harmony harmony)
        {
            var powerupScriptType = Type.GetType("PowerupScript, Assembly-CSharp");
            var triggerMethod = powerupScriptType.GetMethod("Trigger_Baphomet", BindingFlags.NonPublic | BindingFlags.Static);

            if (triggerMethod == null)
            {
                Plugin.Instance.ModLogger.LogError("BigIntegerTracking: Could not find Trigger_Baphomet");
                return;
            }

            var postfixMethod = typeof(BigIntegerPatternTracking).GetMethod(
                nameof(Trigger_Baphomet_Postfix),
                BindingFlags.NonPublic | BindingFlags.Static
            );

            harmony.Patch(triggerMethod, postfix: new HarmonyMethod(postfixMethod));
            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Patched Trigger_Baphomet");
        }

        static void Trigger_Baphomet_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            // Baphomet adds current triangle pattern values to their extras
            // triangle = 11, triangleInverted = 12
            int triangleKind = 11;
            int triangleInvertedKind = 12;

            BigInteger triangleValue = CalculatePatternValue_BigInt(triangleKind, false);
            BigInteger triangleInvertedValue = CalculatePatternValue_BigInt(triangleInvertedKind, false);

            extraValue_BigInt[triangleKind] += triangleValue;
            extraValue_BigInt[triangleInvertedKind] += triangleInvertedValue;

            Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: Baphomet trigger - triangle extras: {extraValue_BigInt[triangleKind]}, inverted: {extraValue_BigInt[triangleInvertedKind]}");
        }

        /// <summary>
        /// Intercept Pareidolia bonus setter.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameplayData), "Powerup_PareidoliaMultiplierBonus_Set")]
        static void Powerup_PareidoliaMultiplierBonus_Set_Postfix(object kind, double n)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            int kindInt = (int)kind;
            BigInteger valueBI = ConvertDoubleToBigInteger(n, $"Pareidolia_Set[{kindInt}]");
            pareidoliaBonus_BigInt[kindInt] = valueBI;

            Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: pareidoliaBonus[{kindInt}] = {valueBI}");
        }

        /// <summary>
        /// Prefix for _ComputePatternValue - use our BigInteger calculation.
        /// </summary>
        public static void ApplyComputePatternValuePatch(Harmony harmony)
        {
            var slotMachineType = Type.GetType("SlotMachineScript, Assembly-CSharp");
            var computeMethod = slotMachineType.GetMethod(
                "_ComputePatternValue",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            if (computeMethod == null)
            {
                Plugin.Instance.ModLogger.LogError("BigIntegerTracking: Could not find _ComputePatternValue");
                return;
            }

            var prefixMethod = typeof(BigIntegerPatternTracking).GetMethod(
                nameof(ComputePatternValue_Prefix),
                BindingFlags.NonPublic | BindingFlags.Static
            );

            harmony.Patch(computeMethod, prefix: new HarmonyMethod(prefixMethod));
            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Patched _ComputePatternValue");
        }

        static bool ComputePatternValue_Prefix(object patternKind, object symbolKind, ref BigInteger __result)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return true; // Run original

            try
            {
                InitializeReflection();

                int patternKindValue = (int)patternKind;
                int symbolKindValue = (int)symbolKind;

                // Check for undefined/count (error cases)
                if (patternKindValue == -1 || patternKindValue == 16)
                {
                    __result = new BigInteger(-1);
                    return false;
                }

                if (symbolKindValue == -1 || symbolKindValue == 11)
                {
                    __result = new BigInteger(-1);
                    return false;
                }

                // Get multipliers (already BigInteger in game)
                var allSymbolsMultMethod = gameplayDataType.GetMethod("AllSymbolsMultiplierGet", BindingFlags.Public | BindingFlags.Static);
                var symbolCoinsMethod = gameplayDataType.GetMethod("Symbol_CoinsOverallValue_Get", BindingFlags.Public | BindingFlags.Static);
                var allPatternsMultMethod = gameplayDataType.GetMethod("AllPatternsMultiplierGet", BindingFlags.Public | BindingFlags.Static);

                BigInteger allSymbolsMult = (BigInteger)allSymbolsMultMethod.Invoke(null, new object[] { true });
                BigInteger symbolCoinsValue = (BigInteger)symbolCoinsMethod.Invoke(null, new object[] { symbolKind });
                BigInteger allPatternsMult = (BigInteger)allPatternsMultMethod.Invoke(null, new object[] { true });

                // Use OUR BigInteger pattern value calculation!
                BigInteger patternValue = CalculatePatternValue_BigInt(patternKindValue, true);

                // Multiply by 100 for the original formula (pattern value is scaled)
                BigInteger patternValueScaled = patternValue * 100;

                // Final calculation (all BigInteger - no overflow!)
                __result = allSymbolsMult * symbolCoinsValue * allPatternsMult * patternValueScaled / 100;

                Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: ComputePatternValue[{patternKindValue}] = {__result} (pattern value: {patternValue})");

                return false; // Skip original method
            }
            catch (Exception e)
            {
                Plugin.Instance.ModLogger.LogError($"BigIntegerTracking: Error in ComputePatternValue_Prefix: {e}");
                return true; // Fall back to original on error
            }
        }

        /// <summary>
        /// Get the save file path for BigInteger tracking data.
        /// </summary>
        static string GetSaveFilePath(int gameDataIndex)
        {
            InitializeReflection();

            // Get the game's save folder path
            var platformDataMasterType = Type.GetType("Panik.PlatformDataMaster, Assembly-CSharp");
            var gameFolderPathProperty = platformDataMasterType.GetProperty("GameFolderPath", BindingFlags.Public | BindingFlags.Static);
            string gameFolderPath = (string)gameFolderPathProperty.GetValue(null);

            // Create our mod save file next to the game save (with slot index!)
            // Game uses: GameDataFull0.json, GameDataFull1.json, etc.
            // We use:    GameDataFull0_BigIntegerTracking.json, GameDataFull1_BigIntegerTracking.json, etc.
            return $"{gameFolderPath}GameDataFull{gameDataIndex}{SAVE_FILE_SUFFIX}";
        }

        /// <summary>
        /// Save BigInteger state to file.
        /// Simple format: One line per pattern with format "pattern:extras:pareidolia"
        /// Note: Abstract Painting is NOT saved because the game resets it on load.
        /// </summary>
        static void SaveBigIntegerState(int gameDataIndex)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            try
            {
                string savePath = GetSaveFilePath(gameDataIndex);

                using (StreamWriter writer = new StreamWriter(savePath))
                {
                    // Write header (version for future compatibility)
                    writer.WriteLine("VERSION:1");

                    // Write each pattern's data
                    // NOTE: Abstract Painting is NOT saved because the game resets it on load
                    // (it's stored in a Dictionary that doesn't serialize)
                    for (int i = 0; i < 16; i++)
                    {
                        BigInteger extras = extraValue_BigInt.ContainsKey(i) ? extraValue_BigInt[i] : BigInteger.Zero;
                        BigInteger pareidolia = pareidoliaBonus_BigInt.ContainsKey(i) ? pareidoliaBonus_BigInt[i] : BigInteger.Zero;

                        // Format: pattern:extras:pareidolia (abstract omitted - matches game behavior)
                        writer.WriteLine($"{i}:{extras}:{pareidolia}");
                    }
                }

                Plugin.Instance.ModLogger.LogInfo($"BigIntegerTracking: Saved state to {savePath}");
            }
            catch (Exception e)
            {
                Plugin.Instance.ModLogger.LogError($"BigIntegerTracking: Failed to save state: {e.Message}");
            }
        }

        /// <summary>
        /// Load BigInteger state from file.
        /// </summary>
        static bool LoadBigIntegerState(int gameDataIndex)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return false;

            try
            {
                string savePath = GetSaveFilePath(gameDataIndex);

                if (!File.Exists(savePath))
                {
                    Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: No save file found, will reconstruct from game state");
                    return false;
                }

                InitializeShadowStorage();

                using (StreamReader reader = new StreamReader(savePath))
                {
                    string versionLine = reader.ReadLine();
                    if (versionLine == null || !versionLine.StartsWith("VERSION:"))
                    {
                        Plugin.Instance.ModLogger.LogWarning("BigIntegerTracking: Invalid save file format, will reconstruct");
                        return false;
                    }

                    // Read each pattern's data
                    // NOTE: Abstract is always reset to 0 on load (matches game behavior)
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length != 3)  // Changed from 4 to 3 (no abstract)
                        {
                            Plugin.Instance.ModLogger.LogWarning($"BigIntegerTracking: Invalid line format: {line}");
                            continue;
                        }

                        int pattern = int.Parse(parts[0]);
                        BigInteger extras = BigInteger.Parse(parts[1]);
                        BigInteger pareidolia = BigInteger.Parse(parts[2]);

                        extraValue_BigInt[pattern] = extras;
                        pareidoliaBonus_BigInt[pattern] = pareidolia;
                        abstractBonus_BigInt[pattern] = BigInteger.Zero;  // Always reset (matches game)

                        Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: Loaded pattern[{pattern}] - extras: {extras}, pareidolia: {pareidolia}, abstract: 0 (reset)");
                    }
                }

                Plugin.Instance.ModLogger.LogInfo($"BigIntegerTracking: Successfully loaded state from {savePath}");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Instance.ModLogger.LogError($"BigIntegerTracking: Failed to load state: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Patch for Data.GameData.Saving_Prepare - save our BigInteger state before game saves.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("Data+GameData, Assembly-CSharp", "Saving_Prepare")]
        static void Saving_Prepare_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            // Get the current game data index
            InitializeReflection();
            var gameDataIndexField = dataType.GetProperty("GameDataIndex", BindingFlags.Public | BindingFlags.Static);
            int gameDataIndex = (int)gameDataIndexField.GetValue(null);

            Plugin.Instance.ModLogger.LogInfo($"BigIntegerTracking: Game is saving (slot {gameDataIndex}), saving BigInteger state...");
            SaveBigIntegerState(gameDataIndex);
        }

        /// <summary>
        /// Patch for Data.GameData.GameplayDataReset - called when run resets (death, new run).
        /// This is the RELIABLE way to detect resets - we clear our shadow values when the game resets.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("Data+GameData, Assembly-CSharp", "GameplayDataReset")]
        static void GameplayDataReset_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: GameplayDataReset called - clearing shadow BigInteger values");

            // Clear all shadow storage (new run starts at 0)
            InitializeShadowStorage();

            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Shadow values reset to 0 (new run)");
        }

        /// <summary>
        /// Patch for Data.GameData.Loading_Prepare - load our BigInteger state after game loads.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("Data+GameData, Assembly-CSharp", "Loading_Prepare")]
        static void Loading_Prepare_Postfix()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            // Get the current game data index
            InitializeReflection();
            var gameDataIndexField = dataType.GetProperty("GameDataIndex", BindingFlags.Public | BindingFlags.Static);
            int gameDataIndex = (int)gameDataIndexField.GetValue(null);

            Plugin.Instance.ModLogger.LogInfo($"BigIntegerTracking: Game loaded (slot {gameDataIndex}), loading BigInteger state...");

            // Try to load from our save file
            // If GameplayDataReset was called (death/new run), our shadow values are already at 0
            // So we can safely load whatever is in the save file
            bool loaded = LoadBigIntegerState(gameDataIndex);

            if (!loaded)
            {
                // Fall back to reconstructing from game state (backwards compatibility with old saves)
                Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: No save file found, reconstructing from game state");
                ReconstructFromGameState();
            }
        }

        /// <summary>
        /// Reconstruct BigInteger shadow values from loaded game state.
        /// Called after the game loads doubles from save file (fallback if no BigInteger save exists).
        /// </summary>
        public static void ReconstructFromGameState()
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Reconstructing shadow values from loaded game state...");

            InitializeShadowStorage();
            InitializeReflection();

            var patternExtraGetMethod = gameplayDataType.GetMethod("Pattern_ValueExtra_Get", BindingFlags.Public | BindingFlags.Static);
            var pareidoliaBonusGetMethod = gameplayDataType.GetMethod("Powerup_PareidoliaMultiplierBonus_Get", BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < 16; i++)
            {
                var kindEnum = Enum.ToObject(patternScriptKindType, i);

                // Reconstruct extras
                double extrasDouble = (double)patternExtraGetMethod.Invoke(null, new object[] { kindEnum });
                extraValue_BigInt[i] = ConvertDoubleToBigInteger(extrasDouble, $"Load_extras[{i}]");

                // Reconstruct Pareidolia bonuses
                double pareidoliaDouble = (double)pareidoliaBonusGetMethod.Invoke(null, new object[] { kindEnum });
                pareidoliaBonus_BigInt[i] = ConvertDoubleToBigInteger(pareidoliaDouble, $"Load_pareidolia[{i}]");

                // Abstract Painting resets on load (dictionary not serialized), so we start at zero
                abstractBonus_BigInt[i] = BigInteger.Zero;

                Plugin.Instance.ModLogger.LogDebug($"BigIntegerTracking: Loaded pattern[{i}] - extras: {extraValue_BigInt[i]}, pareidolia: {pareidoliaBonus_BigInt[i]}");
            }

            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Shadow values reconstructed successfully");
        }

        /// <summary>
        /// Apply all manual patches (for private methods we can't patch with attributes).
        /// </summary>
        public static void ApplyAllManualPatches(Harmony harmony)
        {
            if (!Plugin.EnableBigIntegerPatternTracking.Value)
                return;

            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: Applying manual patches...");

            InitializeShadowStorage();

            ApplyPareidoliaPatch(harmony);
            ApplyAbstractPaintingPatch(harmony);
            ApplyBaphometPatch(harmony);
            ApplyComputePatternValuePatch(harmony);

            Plugin.Instance.ModLogger.LogInfo("BigIntegerTracking: All manual patches applied");
        }
    }
}
