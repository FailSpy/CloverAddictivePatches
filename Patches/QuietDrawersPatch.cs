using HarmonyLib;
using System.Reflection;

namespace CloverAddictivePatches.Patches
{
    /// <summary>
    /// Disables horror sound/FOV effect when opening drawers with skeleton parts.
    /// </summary>
    [HarmonyPatch]
    public class DisableDrawerCorpseReaction
    {
        private static FieldInfo skeletonHorrorSoundPlayedField;

        static DisableDrawerCorpseReaction()
        {
            skeletonHorrorSoundPlayedField = AccessTools.Field(typeof(DrawersScript), "skeletonHorrorSoundPlayed");
        }

        [HarmonyPatch(typeof(DrawersScript), "Update")]
        [HarmonyPrefix]
        static void Update_Prefix()
        {
            if (!Plugin.QuietDrawersPatch.Value)
                return;

            if (skeletonHorrorSoundPlayedField != null)
                skeletonHorrorSoundPlayedField.SetValue(null, true);
        }
    }
}
