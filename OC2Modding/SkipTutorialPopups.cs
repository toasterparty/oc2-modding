using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class SkipTutorialPopups
    {
        private static ConfigEntry<bool> configSkipTutorialPopups;

        public static void Awake()
        {
            /* Setup Configuration */
            configSkipTutorialPopups = OC2Modding.configFile.Bind(
                "QualityOfLife", // Config Category
                "SkipTutorialPopups", // Config key name
                true, // Default Config value
                "Set to true to skip showing tutorial popups before starting a level" // Friendly description
            );

            /* Inject Mod */
            if (!configSkipTutorialPopups.Value)
            {
                return;
            }

            Harmony.CreateAndPatchAll(typeof(SkipTutorialPopups));
        }

        [HarmonyPatch(typeof(TutorialPopup), nameof(TutorialPopup.CanSpawn))]
        [HarmonyPostfix]
        private static void CanSpawn(ref bool __result)
        {
            __result = false;
        }
    }
}
