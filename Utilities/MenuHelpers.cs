using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using BepInEx.Configuration;
using Panik;

namespace CloverAddictivePatches.Utilities
{
    /// <summary>
    /// Custom ScreenMenuScript menu creation and management utilities.
    /// </summary>
    public static class MenuHelpers
    {
        // Menu opening methods commented out until ScreenMenuScript.Open signature is determined.

        /*
        public static void OpenMenuDelayed(string title, UnityAction[] events, float delaySeconds = 0.1f)
        {
            ScreenMenuScript.Close(false);
            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenMenuCoroutine(title, events, delaySeconds));
            }
        }

        public static void OpenMenuDelayedWithPosition(string title, UnityAction[] events, float yOffset, float delaySeconds = 0.1f)
        {
            ScreenMenuScript.Close(false);
            if (GeneralUiScript.instance != null)
            {
                GeneralUiScript.instance.StartCoroutine(OpenMenuWithPositionCoroutine(title, events, yOffset, delaySeconds));
            }
        }
        */

        /// <summary>
        /// Creates a UnityAction that toggles a ConfigEntry bool and reopens a parent menu.
        /// </summary>
        public static UnityAction CreateToggleEvent(ConfigEntry<bool> configEntry, Action reopenMenu)
        {
            return () =>
            {
                configEntry.Value = !configEntry.Value;
                PlayMenuSound("SoundMenuSelect");
                reopenMenu?.Invoke();
            };
        }

        /// <summary>
        /// Creates a UnityAction that closes current menu and opens a parent menu.
        /// </summary>
        public static UnityAction CreateBackEvent(Action openParentMenu)
        {
            return () =>
            {
                PlayMenuSound("SoundMenuPopDown");
                openParentMenu?.Invoke();
            };
        }

        /// <summary>
        /// Formats a toggle option label with On/Off state (e.g., "Option: On").
        /// </summary>
        public static string FormatToggleOption(string label, bool isEnabled)
        {
            return $"{label}: {(isEnabled ? "On" : "Off")}";
        }

        /// <summary>
        /// Plays a menu sound effect by name (e.g., "SoundMenuSelect").
        /// </summary>
        public static void PlayMenuSound(string soundName)
        {
            var soundType = System.Type.GetType("Sound, Assembly-CSharp");
            if (soundType != null)
            {
                var playMethod = soundType.GetMethod("Play", new System.Type[] { typeof(string) });
                playMethod?.Invoke(null, new object[] { soundName });
            }
        }

        /// <summary>
        /// Calculates menu Y position to prevent tall menus from falling off screen.
        /// </summary>
        public static float CalculateMenuYPosition(int optionCount, float baseYPosition = -20f, float optionHeight = 40f, float marginBuffer = 20f)
        {
            if (optionCount <= 4)
                return baseYPosition;

            float menuHeight = optionCount * optionHeight;
            float extraHeight = (optionCount - 4) * optionHeight / 2f;
            return baseYPosition - (menuHeight / 2f) + extraHeight + marginBuffer;
        }

        /// <summary>
        /// Applies custom Y positioning to open ScreenMenuScript instance.
        /// </summary>
        public static void ApplyMenuPosition(float yPosition)
        {
            if (ScreenMenuScript.instance != null && ScreenMenuScript.instance.transform != null)
            {
                Vector3 pos = ScreenMenuScript.instance.transform.localPosition;
                pos.y = yPosition;
                ScreenMenuScript.instance.transform.localPosition = pos;
            }
        }

        // Coroutine methods commented out until ScreenMenuScript.Open signature is determined.

        /*
        private static IEnumerator OpenMenuCoroutine(string title, UnityAction[] events, float delay)
        {
            yield return new WaitForSeconds(delay);
            // TODO: Determine correct ScreenMenuScript.Open signature
            // ScreenMenuScript.Open(...);
        }

        private static IEnumerator OpenMenuWithPositionCoroutine(string title, UnityAction[] events, float yOffset, float delay)
        {
            yield return new WaitForSeconds(delay);
            // TODO: Determine correct ScreenMenuScript.Open signature
            // ScreenMenuScript.Open(...);
            ApplyMenuPosition(yOffset);
        }
        */

        /// <summary>
        /// Returns Yes/No options array for confirmation dialogs.
        /// </summary>
        public static string[] GetYesNoOptions()
        {
            return new string[] { Translation.Get("Yes"), Translation.Get("No") };
        }

        /// <summary>
        /// Injects options into menu option/event arrays at specified index.
        /// </summary>
        public static void InjectOptionsAtIndex(
            string[] originalOptions,
            UnityAction[] originalEvents,
            int insertIndex,
            string[] newOptions,
            UnityAction[] newEvents,
            out string[] modifiedOptions,
            out UnityAction[] modifiedEvents)
        {
            int originalLength = originalOptions?.Length ?? 0;
            int newLength = newOptions?.Length ?? 0;
            int totalLength = originalLength + newLength;

            modifiedOptions = new string[totalLength];
            modifiedEvents = new UnityAction[totalLength];

            for (int i = 0; i < insertIndex; i++)
            {
                modifiedOptions[i] = originalOptions[i];
                modifiedEvents[i] = originalEvents[i];
            }

            for (int i = 0; i < newLength; i++)
            {
                modifiedOptions[insertIndex + i] = newOptions[i];
                modifiedEvents[insertIndex + i] = newEvents[i];
            }

            for (int i = insertIndex; i < originalLength; i++)
            {
                modifiedOptions[i + newLength] = originalOptions[i];
                modifiedEvents[i + newLength] = originalEvents[i];
            }
        }
    }
}
