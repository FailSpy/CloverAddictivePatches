using UnityEngine;
using Panik;

namespace CloverAddictivePatches.Utilities
{
    /// <summary>
    /// Instant death sequence utilities: camera positioning, sound, and stats display.
    /// </summary>
    public static class DeathHandlingUtils
    {
        /// <summary>
        /// Whether InstantRestartDeath intercepted current death sequence.
        /// </summary>
        public static bool RestartDeathIntercepted { get; set; }

        /// <summary>
        /// Whether this was R button hold restart. R button never shows stats, menu shows stats if deadlines > 2.
        /// </summary>
        public static bool IsRButtonRestart { get; set; }

        /// <summary>
        /// Whether DisableVertigoEffects intercepted current death sequence.
        /// </summary>
        public static bool VertigoDeathIntercepted { get; set; }

        private static FlashScreen instantDeathBlackScreen;

        /// <summary>
        /// Handles instant "falling" death step: positions camera in void, plays sounds, shows/hides stats.
        /// </summary>
        public static bool HandleInstantFallingStep(GameplayMaster gameplayMasterInstance, CameraController cameraController)
        {
            var currentDeathStep = ReflectionCache.GameplayMasterCache.deathStep?.GetValue(gameplayMasterInstance);

            if (currentDeathStep == null || !currentDeathStep.Equals(ReflectionCache.GameplayMasterCache.DeathStep_falling))
                return false;

            var deathStepTimerField = ReflectionCache.GameplayMasterCache.deathStepTimer;
            if (deathStepTimerField == null)
                return false;

            float deathStepTimer = (float)deathStepTimerField.GetValue(gameplayMasterInstance);

            if (deathStepTimer == 0f)
            {
                CameraController.SetPosition(CameraController.PositionKind.Falling, true, 1f);

                // Set camera Y to -256 for black background (deep in void)
                CameraAccessors.SetDeathCameraY(cameraController, -256f);

                if (cameraController != null && cameraController.transform != null)
                {
                    Vector3 pos = cameraController.transform.position;
                    pos.y = -256f;
                    cameraController.transform.position = pos;
                }

                StopEnvironmentSounds();
                PlaySound("SoundTrapdoorOpen");
                SetupDeathStatsScreen(gameplayMasterInstance);
            }

            if (deathStepTimer >= 0f)
            {
                ReflectionCache.GameplayMasterCache.deathStep?.SetValue(
                    gameplayMasterInstance,
                    ReflectionCache.GameplayMasterCache.DeathStep_done
                );
                deathStepTimerField.SetValue(gameplayMasterInstance, 0f);

                RestartDeathIntercepted = false;
                IsRButtonRestart = false;
                VertigoDeathIntercepted = false;
            }

            return true;
        }

        /// <summary>
        /// Intercepts death step from camera look sequence to instant falling. Spawns black screen to hide transition.
        /// </summary>
        public static bool InterceptDeathStepToFalling(ref object deathStep, object expectedInitialStep)
        {
            if (deathStep != null && deathStep.Equals(expectedInitialStep))
            {
                deathStep = ReflectionCache.GameplayMasterCache.DeathStep_falling;
                SpawnInstantDeathBlackScreen();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Spawns full-opacity black screen for instant death. Distance 0.35f covers full screen at all FOV, outside near clip plane (0.3f).
        /// </summary>
        private static void SpawnInstantDeathBlackScreen()
        {
            if (CameraGame.firstInstance == null || CameraGame.firstInstance.myCamera == null)
                return;

            instantDeathBlackScreen = FlashScreen.SpawnEx(
                color: UnityEngine.Color.black,
                alpha: 1.0f,
                alphaDecaySpeed: 0.5f,
                targetCamera: CameraGame.firstInstance.myCamera,
                cameraDistance: 0.35f,
                forceSpawn: true
            );
        }

        private static void StopEnvironmentSounds()
        {
            var soundType = System.Type.GetType("Sound, Assembly-CSharp");
            if (soundType != null)
            {
                var stopMethod = soundType.GetMethod("Stop", new System.Type[] { typeof(string) });
                if (stopMethod != null)
                {
                    stopMethod.Invoke(null, new object[] { "SoundEnvironmentAmbience" });
                    stopMethod.Invoke(null, new object[] { "SoundEnvironmentAmbienceOutside" });
                }
            }
        }

        private static void PlaySound(string soundName)
        {
            var soundType = System.Type.GetType("Sound, Assembly-CSharp");
            if (soundType != null)
            {
                var playMethod = soundType.GetMethod("Play", new System.Type[] { typeof(string) });
                playMethod?.Invoke(null, new object[] { soundName });
            }
        }

        /// <summary>
        /// Sets up death stats: R button never shows, menu shows if deadlines > 2, countdown respects restartQuickDeath.
        /// </summary>
        private static void SetupDeathStatsScreen(GameplayMaster gameplayMasterInstance)
        {
            bool shouldShowStats = false;

            if (RestartDeathIntercepted)
            {
                if (IsRButtonRestart)
                {
                    shouldShowStats = false;
                }
                else
                {
                    long deadlinesCompleted = GameplayData.Stats_DeadlinesCompleted_Get();
                    shouldShowStats = deadlinesCompleted > 2;
                }
            }
            else if (VertigoDeathIntercepted)
            {
                shouldShowStats = !GameplayMaster.restartQuickDeath;
            }
            else
            {
                shouldShowStats = !GameplayMaster.restartQuickDeath;
            }

            if (shouldShowStats)
            {
                StatsScript.Open(StatsScript.ShowKind.endDeath);
            }
        }
    }
}
