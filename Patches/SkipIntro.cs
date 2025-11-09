using BepInEx;
using HarmonyLib;
using Panik;

namespace CloverAddictivePatches.Patches
{
    public class SkipIntro
    {
        public static void CheckAndSkipIntro()
        {
            if (Level.CurrentScene == (int)Level.SceneIndex.Intro)
            {
                Level.GoTo(Level.SceneIndex.Game, true);
            }
        }
    }
}