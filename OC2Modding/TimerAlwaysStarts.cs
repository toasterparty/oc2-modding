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
        }
    }
}
