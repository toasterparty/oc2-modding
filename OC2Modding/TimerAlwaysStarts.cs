using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class TimerAlwaysStarts
    {
        public static ConfigEntry<bool> configTimerAlwaysStarts;

        public static void Awake()
        {
            /* Setup Configuration */
            configTimerAlwaysStarts = OC2Modding.configFile.Bind(
                "GameModifications", // Config Category
                "TimerAlwaysStarts", // Config key name
                false, // Default Config value
                "Set to true to make levels which normally have \"Prep Time\" start immediately" // Friendly description
            );

            if (!configTimerAlwaysStarts.Value)
            {
                return;
            }

            /* Inject Mod */
            Harmony.CreateAndPatchAll(typeof(TimerAlwaysStarts));
        }

        [HarmonyPatch(typeof(GameModes.ServerCampaignMode), nameof(GameModes.ServerCampaignMode.Begin))]
        [HarmonyPrefix]
        private static void Begin(ref GameModes.ClientContext ___m_context)
        {
            ___m_context.m_levelConfig.m_recipesBeforeTimerStarts = 0;
        }
    }
}
