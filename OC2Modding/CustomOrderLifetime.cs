using BepInEx.Configuration;
using HarmonyLib;

namespace OC2Modding
{
    public static class CustomOrderLifetime
    {
        public static ConfigEntry<float> configCustomOrderLifetime;

        public static void Awake()
        {
            /* Setup Configuration */
            configCustomOrderLifetime = OC2Modding.configFile.Bind(
                "GameModifications", // Config Category
                "CustomOrderLifetime", // Config key name
                100.0f, // Default Config value
                "Customize how long orders take before expiring" // Friendly description
            );

            if (configCustomOrderLifetime.Value == 100.0f)
            {
                return;
            }

            Harmony.CreateAndPatchAll(typeof(CustomOrderLifetime));
        }

        [HarmonyPatch(typeof(GameSession), nameof(GameSession.GetGameModeServer))]
        [HarmonyPostfix]
        private static void GetGameModeServer(ref KitchenLevelConfigBase levelConfig)
        {
            levelConfig.m_orderLifetime = configCustomOrderLifetime.Value;
        }
    }
}
