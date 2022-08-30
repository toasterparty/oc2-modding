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
            else if (OC2Config.LevelTimerScale != 1.0f)
            {
                float beforeTime = levelConfig.GetRoundData().m_roundTimer;
                float time = beforeTime*OC2Config.LevelTimerScale;
                if ((time % 10) >= 5)
                {
                    time += 10 - (time % 10);
                }
                else
                {
                    time -= (time % 10);
                }

                levelConfig.GetRoundData().m_roundTimer = time;

                OC2Modding.Log.LogInfo($"Scaled Level Time from {((int)beforeTime)/60}:{beforeTime%60} to {((int)time)/60}:{time%60}");
            }
        }
    }
}
