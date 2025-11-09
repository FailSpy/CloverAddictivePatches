using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Panik;

namespace CloverAddictivePatches.Utilities
{
    /// <summary>
    /// Centralized reflection cache for game internals. Must call Initialize() before use.
    /// </summary>
    public static class ReflectionCache
    {
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized)
                return;
            CameraControllerCache.Initialize();
            GameplayMasterCache.Initialize();
            MainMenuScriptCache.Initialize();
            PlayerScriptCache.Initialize();
            DrawersScriptCache.Initialize();
            DiegeticMenuCache.Initialize();
            DialogueScriptCache.Initialize();
            PromptGuideScriptCache.Initialize();
            GeneralUiScriptCache.Initialize();
            DataCache.Initialize();

            isInitialized = true;
        }

        public static class CameraControllerCache
        {
            public static FieldInfo myCamera { get; private set; }
            public static FieldInfo dollyZoomEnabled { get; private set; }
            public static FieldInfo positionKind { get; private set; }
            public static FieldInfo lerpSpeedMultiplier { get; private set; }
            public static FieldInfo targetTransform { get; private set; }
            public static FieldInfo deathCameraY { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(CameraController);
                myCamera = AccessTools.Field(type, "myCamera");
                dollyZoomEnabled = AccessTools.Field(type, "dollyZoomEnabled");
                positionKind = AccessTools.Field(type, "positionKind");
                lerpSpeedMultiplier = AccessTools.Field(type, "lerpSpeedMultiplier");
                targetTransform = AccessTools.Field(type, "targetTransform");
                deathCameraY = AccessTools.Field(type, "deathCameraY");
            }
        }

        public static class GameplayMasterCache
        {
            public static FieldInfo deathStep { get; private set; }
            public static FieldInfo deathStepTimer { get; private set; }
            public static FieldInfo intAndTickets_ShakedTrapdoor { get; private set; }
            public static FieldInfo interestsAndTicketsPhase { get; private set; }
            public static FieldInfo interestsAndTicketsTimer { get; private set; }
            public static FieldInfo delay { get; private set; }

            public static Type DeathStepType { get; private set; }
            public static object DeathStep_lookAtAtm { get; private set; }
            public static object DeathStep_lookAtTrapdoor { get; private set; }
            public static object DeathStep_falling { get; private set; }
            public static object DeathStep_done { get; private set; }

            public static Type InterestsAndTicketsPhaseType { get; private set; }
            public static object InterestsPhase_shakeTrapdoor_Optional { get; private set; }
            public static object InterestsPhase_done { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(GameplayMaster);
                deathStep = AccessTools.Field(type, "deathStep");
                deathStepTimer = AccessTools.Field(type, "deathStepTimer");
                intAndTickets_ShakedTrapdoor = AccessTools.Field(type, "intAndTickets_ShakedTrapdoor");
                interestsAndTicketsPhase = AccessTools.Field(type, "interestsAndTicketsPhase");
                interestsAndTicketsTimer = AccessTools.Field(type, "interestsAndTicketsTimer");
                delay = AccessTools.Field(type, "delay");

                DeathStepType = AccessTools.Inner(type, "DeathStep");
                if (DeathStepType != null)
                {
                    DeathStep_lookAtAtm = Enum.Parse(DeathStepType, "lookAtAtm");
                    DeathStep_lookAtTrapdoor = Enum.Parse(DeathStepType, "lookAtTrapdoor");
                    DeathStep_falling = Enum.Parse(DeathStepType, "falling");
                    DeathStep_done = Enum.Parse(DeathStepType, "done");
                }

                InterestsAndTicketsPhaseType = AccessTools.Inner(type, "InterestsAndTicketsPhase");
                if (InterestsAndTicketsPhaseType != null)
                {
                    InterestsPhase_shakeTrapdoor_Optional = Enum.Parse(InterestsAndTicketsPhaseType, "shakeTrapdoor_Optional");
                    InterestsPhase_done = Enum.Parse(InterestsAndTicketsPhaseType, "done");
                }
            }
        }

        public static class MainMenuScriptCache
        {
            public static FieldInfo menuIndex { get; private set; }
            public static FieldInfo optionTexts { get; private set; }
            public static FieldInfo menuElements { get; private set; }
            public static FieldInfo menuController { get; private set; }
            public static FieldInfo leftNavigationPress { get; private set; }
            public static FieldInfo desiredNavigationIndex { get; private set; }
            public static FieldInfo saveSettingsOnClose { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(MainMenuScript);
                menuIndex = AccessTools.Field(type, "menuIndex");
                optionTexts = AccessTools.Field(type, "optionTexts");
                menuElements = AccessTools.Field(type, "menuElements");
                menuController = AccessTools.Field(type, "menuController");
                leftNavigationPress = AccessTools.Field(type, "leftNavigationPress");
                desiredNavigationIndex = AccessTools.Field(type, "desiredNavigationIndex");
                saveSettingsOnClose = AccessTools.Field(type, "saveSettingsOnClose");
            }
        }

        public static class PlayerScriptCache
        {
            public static FieldInfo rb { get; private set; }
            public static FieldInfo playerIndex { get; private set; }
            public static MethodInfo PlayerExtChacheIt { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(PlayerScript);
                rb = AccessTools.Field(type, "rb");
                playerIndex = AccessTools.Field(type, "playerIndex");
                PlayerExtChacheIt = AccessTools.Method(type, "PlayerExtChacheIt");
            }
        }

        public static class DrawersScriptCache
        {
            public static FieldInfo drawerIsOpen { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(DrawersScript);
                drawerIsOpen = AccessTools.Field(type, "drawerIsOpen");
            }
        }

        public static class DiegeticMenuCache
        {
            public static FieldInfo myController { get; private set; }
            public static PropertyInfo HoveredElement { get; private set; }

            internal static void Initialize()
            {
                myController = AccessTools.Field(typeof(DiegeticMenuElement), "myController");
                HoveredElement = AccessTools.Property(typeof(DiegeticMenuController), "HoveredElement");
            }
        }

        public static class DialogueScriptCache
        {
            public static FieldInfo questionDelay { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(DialogueScript);
                questionDelay = AccessTools.Field(type, "questionDelay");
            }
        }

        public static class PromptGuideScriptCache
        {
            public static FieldInfo text { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(PromptGuideScript);
                text = AccessTools.Field(type, "text");
            }
        }

        public static class GeneralUiScriptCache
        {
            public static MethodInfo _IntroMenuContinue { get; private set; }
            public static MethodInfo _IntroMenuNewGame { get; private set; }
            public static MethodInfo _IntroMenuNewSeededGame { get; private set; }

            internal static void Initialize()
            {
                var type = typeof(GeneralUiScript);
                _IntroMenuContinue = AccessTools.Method(type, "_IntroMenuContinue");
                _IntroMenuNewGame = AccessTools.Method(type, "_IntroMenuNewGame");
                _IntroMenuNewSeededGame = AccessTools.Method(type, "_IntroMenuNewSeededGame");
            }
        }

        public static class DataCache
        {
            public static FieldInfo flashingLightsReducedEnabled { get; private set; }

            internal static void Initialize()
            {
                var settingsType = typeof(Data).GetField("settings")?.FieldType;
                if (settingsType != null)
                {
                    flashingLightsReducedEnabled = AccessTools.Field(settingsType, "flashingLightsReducedEnabled");
                }
            }
        }
    }
}
