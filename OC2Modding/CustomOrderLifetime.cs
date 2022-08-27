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

            if (OC2Config.Custom66TimerScale != 1.0f && levelConfig.name.StartsWith("s_dynamic_stage_04"))
            {
                float time = levelConfig.GetRoundData().m_roundTimer;
                time *= OC2Config.Custom66TimerScale;
                time -= (time % 30);
                time += 30;
                levelConfig.GetRoundData().m_roundTimer = time;
            }
        }
    }
}
