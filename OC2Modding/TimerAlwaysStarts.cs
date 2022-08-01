using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public class TimerAlwaysStarts
    {
        private static ConfigEntry<bool> configTimerAlwaysStarts;

        public static void Awake()
        {
            /* Setup Configuration */
            configTimerAlwaysStarts = OC2Modding.configFile.Bind(
                "General", // Config Category
                "TimerAlwaysStarts", // Config key name
                false, // Default Config value
                "Set to true to make levels which normally have \"Prep Time\" start immediately" // Friendly description
            );

            /* Inject Mod */
            if (configTimerAlwaysStarts.Value)
            {
                Harmony.CreateAndPatchAll(typeof(TimerAlwaysStarts));
            }
        }

        [HarmonyPatch(typeof(GameModes.ServerCampaignMode), nameof(GameModes.ServerCampaignMode.Begin))]
        [HarmonyPrefix]
        public static bool Begin(ref GameModes.ClientContext ___m_context)
        {
            ___m_context.m_levelConfig.m_recipesBeforeTimerStarts = 0;
            return true; // execute original
        }
    }
}
