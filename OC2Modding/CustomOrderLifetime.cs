using HarmonyLib;

namespace OC2Modding
{
    public static class CustomOrderLifetime
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomOrderLifetime));
        }

        [HarmonyPatch(typeof(GameSession), nameof(GameSession.GetGameModeServer))]
        [HarmonyPostfix]
        private static void GetGameModeServer(ref KitchenLevelConfigBase levelConfig)
        {
            levelConfig.m_orderLifetime = OC2Config.CustomOrderLifetime;
        }
    }
}
