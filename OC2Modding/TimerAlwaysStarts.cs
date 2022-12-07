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
        private static void Begin_Server(ref GameModes.ClientContext ___m_context)
        {
            if (OC2Config.Config.TimerAlwaysStarts)
            {
                ___m_context.m_levelConfig.m_recipesBeforeTimerStarts = 0;
            }
        }

        [HarmonyPatch(typeof(GameModes.ClientCampaignMode), nameof(GameModes.ClientCampaignMode.Begin))]
        [HarmonyPrefix]
        private static void Begin_Client(ref GameModes.ClientContext ___m_context)
        {
            if (OC2Config.Config.TimerAlwaysStarts)
            {
                ___m_context.m_levelConfig.m_recipesBeforeTimerStarts = 0;
            }
        }
    }
}
