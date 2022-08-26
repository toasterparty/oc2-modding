using HarmonyLib;

namespace OC2Modding
{
    public static class TimerAlwaysStarts
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(TimerAlwaysStarts));
        }

        [HarmonyPatch(typeof(GameModes.ServerCampaignMode), nameof(GameModes.ServerCampaignMode.Begin))]
        [HarmonyPrefix]
        private static void Begin(ref GameModes.ClientContext ___m_context)
        {
            if (OC2Config.TimerAlwaysStarts)
            {
                ___m_context.m_levelConfig.m_recipesBeforeTimerStarts = 0;
            }

            // OC2Modding.Log.LogMessage($"{___m_context.m_levelConfig.name}");

            if (OC2Config.Custom66TimerScale != 1.0f && ___m_context.m_levelConfig.name.StartsWith("s_dynamic_stage_04"))
            {
                float time = ___m_context.m_levelConfig.GetRoundData().m_roundTimer;
                time *= OC2Config.Custom66TimerScale;
                time -= (time % 30);
                time += 30;
                ___m_context.m_levelConfig.GetRoundData().m_roundTimer = time;
            }
        }
    }
}
