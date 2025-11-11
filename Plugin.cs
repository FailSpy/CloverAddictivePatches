using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using CloverAddictivePatches.Utilities;

namespace CloverAddictivePatches
{
    [BepInPlugin("io.github.failspy.qualityclover", "CloverAddictivePatches", Plugin.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Version = "1.0.3";

        public static Plugin Instance { get; private set; }

        public BepInEx.Logging.ManualLogSource ModLogger => Logger;

        public static ConfigEntry<bool> FOVAdjustmentPatch { get; private set; }
        public static ConfigEntry<float> PlayerFOV { get; private set; }
        public static ConfigEntry<bool> DollyZoomPatch { get; private set; }

        public static ConfigEntry<bool> DrawerPeekPatch { get; private set; }
        public static ConfigEntry<bool> MainMenuCameraFixPatch { get; private set; }
        public static ConfigEntry<bool> MainMenuAdditionsPatch { get; private set; }
        public static ConfigEntry<bool> MemoryCardMenuAccessPatch { get; private set; }
        public static ConfigEntry<bool> InventoryDrawerSwapPatch { get; private set; }
        public static ConfigEntry<bool> ControllerFixPatch { get; private set; }
        public static ConfigEntry<bool> InstantRestartPatch { get; private set; }
        public static ConfigEntry<bool> SkipRepetitiveWarningsPatch { get; private set; }
        public static ConfigEntry<bool> SkipTrapdoorWarningsPatch { get; private set; }
        public static ConfigEntry<bool> ExtendedTransitionSpeedsPatch { get; private set; }
        public static ConfigEntry<bool> ReduceSkipDelaysPatch { get; private set; }
        public static ConfigEntry<bool> ATMCutsceneFreeroamPatch { get; private set; }
        public static ConfigEntry<bool> NoVertigoInducersPatch { get; private set; }
        public static ConfigEntry<bool> SmartDepositPatch { get; private set; }
        public static ConfigEntry<bool> NewRunConfirmationPatch { get; private set; }
        public static ConfigEntry<bool> QuietDrawersPatch { get; private set; }
        public static ConfigEntry<bool> HideCoinsTicketsUI { get; private set; }

        public static ConfigEntry<bool> BadEndingDialogueSeen { get; private set; }

        // Drawer collider debug settings
        public static ConfigEntry<float> TopDrawerDepthMultiplier { get; private set; }
        public static ConfigEntry<float> TopDrawerWidthMultiplier { get; private set; }
        public static ConfigEntry<float> TopDrawerHeightMultiplier { get; private set; }
        public static ConfigEntry<float> OtherDrawerDepthMultiplier { get; private set; }
        public static ConfigEntry<float> OtherDrawerWidthMultiplier { get; private set; }
        public static ConfigEntry<float> OtherDrawerHeightMultiplier { get; private set; }
        public static ConfigEntry<float> TopDrawerOpenDepthOffset { get; private set; }
        public static ConfigEntry<float> TopDrawerCloseDepthOffset { get; private set; }
        public static ConfigEntry<float> OtherDrawerOpenDepthOffset { get; private set; }
        public static ConfigEntry<float> OtherDrawerCloseDepthOffset { get; private set; }
        public static ConfigEntry<float> DrawerPeekCameraMovementThreshold { get; private set; }

        public static ConfigEntry<bool> DebugPatch { get; private set; }
        public static ConfigEntry<bool> SkipIntroPatch { get; private set; }

        private static Type skipIntroType;
        private static MethodInfo skipIntroMethod;

        void Awake()
        {
            Instance = this;
            Logger.LogInfo($"=== CloverAddictivePatches mod loading (v{Version}) ===");

            InitializeConfig();

            Logger.LogInfo("Initializing reflection cache...");
            ReflectionCache.Initialize();

            skipIntroType = Type.GetType("CloverAddictivePatches.Patches.SkipIntro");
            if (skipIntroType != null)
            {
                skipIntroMethod = skipIntroType.GetMethod("CheckAndSkipIntro", BindingFlags.Public | BindingFlags.Static);
            }

            try
            {
                var harmony = new Harmony("io.github.failspy.qualityclover");

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.Debug", DebugPatch.Value,
                    initializeAction: () => InitializePatch("CloverAddictivePatches.Patches.Debug", this));

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.DrawerPeek", true,
                    initializeAction: () => InitializePatch("CloverAddictivePatches.Patches.DrawerPeek", this));

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.DisableInterestsCutscene", SkipTrapdoorWarningsPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.MainMenuCameraFix", MainMenuCameraFixPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.MainMenuAdditions", true,
                    initializeAction: () => InitializePatch("CloverAddictivePatches.Patches.MainMenuAdditions", this));

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.MemoryCardMenuAccess", MemoryCardMenuAccessPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.InventoryDrawerSwap", true,
                    initializeAction: () => InitializePatch("CloverAddictivePatches.Patches.InventoryDrawerSwap", this));

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.CameraUtils", true);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.SkipRepeatedDialogue", SkipRepetitiveWarningsPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.ControllerFix", ControllerFixPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.InstantRestartDeath", InstantRestartPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.ExtendedTransitionSpeeds", ExtendedTransitionSpeedsPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.ReduceSkipDelays", ReduceSkipDelaysPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.FreeroamDuringCutscenes", ATMCutsceneFreeroamPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.DisableVertigoEffects", NoVertigoInducersPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.SmartDeposit", SmartDepositPatch.Value);

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.NewRunConfirmation", true,
                    initializeAction: () => InitializePatch("CloverAddictivePatches.Patches.NewRunConfirmation", this));

                PatchIfEnabled(harmony, "CloverAddictivePatches.Patches.DisableDrawerCorpseReaction", QuietDrawersPatch.Value);

                Logger.LogInfo("Harmony patches applied successfully!");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Failed to apply Harmony patches: {e}");
            }

            Logger.LogInfo("=== CloverAddictivePatches mod loaded ===");
        }

        void Update()
        {
            if (SkipIntroPatch.Value && skipIntroMethod != null)
            {
                skipIntroMethod.Invoke(null, null);
            }
        }

        /// <summary>
        /// Initializes patches with an Initialize method via reflection.
        /// </summary>
        private void InitializePatch(string typeName, Plugin instance)
        {
            Type patchType = Type.GetType(typeName);
            if (patchType != null)
            {
                var initMethod = patchType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                initMethod?.Invoke(null, new object[] { instance });
            }
        }

        /// <summary>
        /// Applies a Harmony patch if the type exists and is enabled.
        /// </summary>
        private void PatchIfEnabled(Harmony harmony, string typeName, bool isEnabled, System.Action initializeAction = null)
        {
            if (!isEnabled)
            {
                Logger.LogInfo($"{typeName.Split('.')[2]} patch disabled by config");
                return;
            }

            Type patchType = Type.GetType(typeName);

            if (patchType == null)
            {
                Logger.LogWarning($"{typeName} not found - patch file may be excluded from compilation");
                return;
            }

            try
            {
                initializeAction?.Invoke();

                harmony.PatchAll(patchType);
                Logger.LogInfo($"{patchType.Name} patch enabled");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"Failed to patch {patchType.Name}: {e}");
            }
        }

        private void InitializeConfig()
        {
            // Camera & FOV Settings Section
            FOVAdjustmentPatch = Config.Bind(
                "Camera & FOV",
                "FOVAdjustmentPatch",
                true,
                "F1/F2 FOV adjustment (60-110 range with wraparound)");

            PlayerFOV = Config.Bind(
                "Camera & FOV",
                "PlayerFOV",
                80f,
                new ConfigDescription(
                    "Player's preferred Field of View in degrees (60-110)",
                    new AcceptableValueRange<float>(60f, 110f)));

            DollyZoomPatch = Config.Bind(
                "Camera & FOV",
                "DollyZoomPatch",
                true,
                "Better dolly zoom (replaces vanilla implementation with improved version that scales naturally with your FOV preference)");

            // Quality of Life Settings Section
            DrawerPeekPatch = Config.Bind(
                "Quality of Life",
                "DrawerPeekPatch",
                true,
                "Drawer peek - drawers open slightly when hovering over them");

            MainMenuCameraFixPatch = Config.Bind(
                "Quality of Life",
                "MainMenuCameraFixPatch",
                true,
                "Prevent camera from moving to main menu drawer position when opening main menu (stays in free cam)");

            MainMenuAdditionsPatch = Config.Bind(
                "Quality of Life",
                "MainMenuAdditionsPatch",
                true,
                "Add Flashing Lights Reduction toggle to accessibility menu and in-game mod configuration menu");

            MemoryCardMenuAccessPatch = Config.Bind(
                "Quality of Life",
                "MemoryCardMenuAccessPatch",
                true,
                "Allow opening Main Menu during Memory Card selection screen (enables quitting without consuming a card)");

            InventoryDrawerSwapPatch = Config.Bind(
                "Quality of Life",
                "InventoryDrawerSwapPatch",
                true,
                "Add 'Swap with [Item]' options to equipped powerup menus for quick inventory-drawer swapping");

            ControllerFixPatch = Config.Bind(
                "Quality of Life",
                "ControllerFixPatch",
                true,
                "Controller null reference fix (prevents crashes when controller is disconnected)");

            InstantRestartPatch = Config.Bind(
                "Quality of Life",
                "InstantRestartPatch",
                true,
                "Skip camera animations when manually restarting (R button hold or Menu Restart) - instant death transition with stats screen");

            SkipRepetitiveWarningsPatch = Config.Bind(
                "Quality of Life",
                "SkipRepetitiveWarningsPatch",
                true,
                "Skip repetitive warnings and restart anecdotes ('1 round left' warning, 'welcome back' dialogues)");

            SkipTrapdoorWarningsPatch = Config.Bind(
                "Quality of Life",
                "SkipTrapdoorWarningsPatch",
                false,
                "No shake cutscene during interests phase");

            ExtendedTransitionSpeedsPatch = Config.Bind(
                "Quality of Life",
                "ExtendedTransitionSpeedsPatch",
                true,
                "Extend maximum transition speed from 4x to 16x in settings menu");

            ReduceSkipDelaysPatch = Config.Bind(
                "Quality of Life",
                "ReduceSkipDelaysPatch",
                true,
                "Reduce delay before dialogues/cutscenes can be skipped (from 0.5s to 0.1s)");

            ATMCutsceneFreeroamPatch = Config.Bind(
                "Quality of Life",
                "ATMCutsceneFreeroamPatch",
                false,
                "Free movement and camera control during ATM/interests cutscenes");

            NoVertigoInducersPatch = Config.Bind(
                "Quality of Life",
                "NoVertigoInducersPatch",
                false,
                "Removes vertigo-inducing effects (dolly zoom, FOV changes, death animations)");

            SmartDepositPatch = Config.Bind(
                "Quality of Life",
                "SmartDepositPatch",
                true,
                "Hold Shift while hovering over ATM deposit button to deposit multiple step intervals at once (stops before crown/skull indicators)");

            NewRunConfirmationPatch = Config.Bind(
                "Quality of Life",
                "NewRunConfirmationPatch",
                true,
                "Show confirmation dialog when starting a new run if current save has progress (prevents accidental progress loss)");

            QuietDrawersPatch = Config.Bind(
                "Quality of Life",
                "QuietDrawersPatch",
                true,
                "Quiet drawer opening (no horror sound/FOV effects for skeleton parts)");

            HideCoinsTicketsUI = Config.Bind(
                "Debug & Development",
                "HideCoinsTicketsUI",
                false,
                "Hide coins and tickets UI in top corners (useful for screenshots)");

            // Dialogue State Tracking Section (not exposed in Mod Options menu to avoid spoilers)
            BadEndingDialogueSeen = Config.Bind(
                "Dialogue State Tracking",
                "BadEndingDialogueSeen",
                false,
                "Tracks whether the bad ending dialogue has been seen. Set to true to skip it, false to see it again.");

            // Debug/Development Settings Section
            DebugPatch = Config.Bind(
                "Debug & Development",
                "DebugPatch",
                false,
                "Debug logging and development features");

            SkipIntroPatch = Config.Bind(
                "Debug & Development",
                "SkipIntroPatch",
                true,
                "Skip intro/startup sequences (time saver for development)");

            // Drawer collider settings
            TopDrawerDepthMultiplier = Config.Bind(
                "Debug & Development",
                "TopDrawerDepthMultiplier",
                7.0f,
                new ConfigDescription(
                    "Top drawers (0,1) collider depth multiplier",
                    new AcceptableValueRange<float>(0.5f, 10.0f)));

            TopDrawerWidthMultiplier = Config.Bind(
                "Debug & Development",
                "TopDrawerWidthMultiplier",
                0.9f,
                new ConfigDescription(
                    "Top drawers (0,1) collider width multiplier",
                    new AcceptableValueRange<float>(0.5f, 3.0f)));

            TopDrawerHeightMultiplier = Config.Bind(
                "Debug & Development",
                "TopDrawerHeightMultiplier",
                0.6f,
                new ConfigDescription(
                    "Top drawers (0,1) collider height multiplier",
                    new AcceptableValueRange<float>(0.5f, 3.0f)));

            OtherDrawerDepthMultiplier = Config.Bind(
                "Debug & Development",
                "OtherDrawerDepthMultiplier",
                7.0f,
                new ConfigDescription(
                    "Other drawers collider depth multiplier",
                    new AcceptableValueRange<float>(0.5f, 10.0f)));

            OtherDrawerWidthMultiplier = Config.Bind(
                "Debug & Development",
                "OtherDrawerWidthMultiplier",
                1.0f,
                new ConfigDescription(
                    "Other drawers collider width multiplier",
                    new AcceptableValueRange<float>(0.5f, 3.0f)));

            OtherDrawerHeightMultiplier = Config.Bind(
                "Debug & Development",
                "OtherDrawerHeightMultiplier",
                1.3f,
                new ConfigDescription(
                    "Other drawers collider height multiplier",
                    new AcceptableValueRange<float>(0.5f, 3.0f)));

            TopDrawerOpenDepthOffset = Config.Bind(
                "Debug & Development",
                "TopDrawerOpenDepthOffset",
                -1.0f,
                new ConfigDescription(
                    "Top drawers (0,1) collider depth offset when fully OPEN (negative moves back, positive moves forward)",
                    new AcceptableValueRange<float>(-2.0f, 2.0f)));

            TopDrawerCloseDepthOffset = Config.Bind(
                "Debug & Development",
                "TopDrawerCloseDepthOffset",
                -0.3f,
                new ConfigDescription(
                    "Top drawers (0,1) collider depth offset when fully CLOSED (negative moves back, positive moves forward)",
                    new AcceptableValueRange<float>(-2.0f, 2.0f)));

            OtherDrawerOpenDepthOffset = Config.Bind(
                "Debug & Development",
                "OtherDrawerOpenDepthOffset",
                -0.8f,
                new ConfigDescription(
                    "Other drawers (2-5) collider depth offset when fully OPEN (negative moves back, positive moves forward)",
                    new AcceptableValueRange<float>(-2.0f, 2.0f)));

            OtherDrawerCloseDepthOffset = Config.Bind(
                "Debug & Development",
                "OtherDrawerCloseDepthOffset",
                -0.2f,
                new ConfigDescription(
                    "Other drawers (2-5) collider depth offset when fully CLOSED (negative moves back, positive moves forward)",
                    new AcceptableValueRange<float>(-2.0f, 2.0f)));

            DrawerPeekCameraMovementThreshold = Config.Bind(
                "Debug & Development",
                "DrawerPeekCameraMovementThreshold",
                0.1f,
                new ConfigDescription(
                    "Minimum camera rotation change (in degrees) required before drawer peek can close (prevents flicker)",
                    new AcceptableValueRange<float>(0.0f, 5.0f)));

            Logger.LogInfo("Configuration initialized successfully!");
        }
    }
}
