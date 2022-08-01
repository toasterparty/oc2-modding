using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class UnlockAllLevels
    {
        private static ConfigEntry<bool> configRevealAllLevels;
        private static ConfigEntry<bool> configPurchaseAllLevels;
        private static ConfigEntry<bool> configSkipTutorial;

        public static void Awake()
        {
            /* Setup Configuration */
            configSkipTutorial = OC2Modding.configFile.Bind(
                "General", // Config Category
                "SkipTutorial", // Config key name
                false, // Default Config value
                "Set to true to skip the mandatory tutorial when starting a new game" // Friendly description
            );
            configRevealAllLevels = OC2Modding.configFile.Bind(
                "General", // Config Category
                "UnlockAllLevels", // Config key name
                false, // Default Config value
                "Set to true to flip all tiles on the board" // Friendly description
            );
            configPurchaseAllLevels = OC2Modding.configFile.Bind(
                "General", // Config Category
                "PurchaseAllLevels", // Config key name
                false, // Default Config value
                "Set to true to remove the requirement for purchasing levels before playing them" // Friendly description
            );

            /* Inject Mod */
            Harmony.CreateAndPatchAll(typeof(UnlockAllLevels));
        }

        [HarmonyPatch(typeof(SaveSlotElement), nameof(SaveSlotElement.ServerLoadCampaign))]
        [HarmonyPrefix]
        private static bool ServerLoadCampaign()
        {
            if (configSkipTutorial.Value)
            {
                GameUtils.GetDebugConfig().m_skipTutorial = true;
            }
            return true; // run original function
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.GetLevelProgress))]
        [HarmonyPostfix]
        private static void GetLevelProgress(ref GameProgress.GameProgressData.LevelProgress __result)
        {
            if (configPurchaseAllLevels.Value)
            {
                __result.Purchased = true;
            }

            if (configRevealAllLevels.Value)
            {
                __result.Revealed = true;
                __result.NGPEnabled = true;
                __result.ObjectivesCompleted = true;
            }
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.IsLevelUnlocked))]
        [HarmonyPostfix]
        private static void IsLevelUnlocked(ref bool __result)
        {
            if (configRevealAllLevels.Value)
            {
                __result = true;
            }
        }
    }
}
