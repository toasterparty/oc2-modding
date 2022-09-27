using HarmonyLib;
using System.Collections.Generic;

namespace OC2Modding
{
    public static class CustomOrderLifetime
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomOrderLifetime));
        }

        private static Dictionary<string, float> ScaledLevelTime = new Dictionary<string, float>();

        private static float GetScaledTime(float inTime, string levelName)
        {
            if (ScaledLevelTime.ContainsKey(levelName))
            {
                /* The time is already cached */
                return ScaledLevelTime[levelName];
            }

            /* Handle special case (Story 6-6) */
            float time = inTime;
            if (OC2Config.Custom66TimerScale != 1.0f && levelName.StartsWith("s_dynamic_stage_04"))
            {
                time *= OC2Config.Custom66TimerScale;
                time -= (time % 30);
                time += 30;
            }
            /* Otherwise Scale by global scale */
            else if (OC2Config.LevelTimerScale != 1.0f && !OC2Helpers.IsDynamicLevel(levelName))
            {
                time *= OC2Config.LevelTimerScale;
                if ((time % 10) >= 5)
                {
                    time += 10 - (time % 10);
                }
                else
                {
                    time -= (time % 10);
                }
            }

            ScaledLevelTime[levelName] = time; // Cache result

            if (inTime != time)
            {
                OC2Modding.Log.LogInfo($"Scaled Level Time of {levelName} from {((int)inTime)/60}:{inTime%60} to {((int)time)/60}:{time%60}");
            }

            return time;
        }

        private static Dictionary<string, float> OriginalTicketTime = new Dictionary<string, float>();
        private static float GetScaledTicketTime(float inTime, string levelName)
        {
            if (!OriginalTicketTime.ContainsKey(levelName))
            {
                OriginalTicketTime[levelName] = inTime;
            }

            float originalTime = OriginalTicketTime[levelName];
            float time = (OC2Config.CustomOrderLifetime / 100.0f) * originalTime;

            if (originalTime != time)
            {
                OC2Modding.Log.LogInfo($"Scaled Ticket Time of {levelName} from {originalTime}s to {time}s");
            }

            return time;
        }

        private static void UpdateLevel(ref KitchenLevelConfigBase levelConfig)
        {
            if (levelConfig == null)
            {
                return;
            }

            levelConfig.GetRoundData().m_roundTimer = GetScaledTime(levelConfig.GetRoundData().m_roundTimer, levelConfig.name);
            levelConfig.m_orderLifetime = GetScaledTicketTime(levelConfig.m_orderLifetime, levelConfig.name);
        }

        [HarmonyPatch(typeof(GameSession), nameof(GameSession.GetGameModeServer))]
        [HarmonyPrefix]
        private static void GetGameModeServer(ref KitchenLevelConfigBase levelConfig)
        {
            UpdateLevel(ref levelConfig);
        }

        [HarmonyPatch(typeof(GameSession), nameof(GameSession.GetGameModeClient))]
        [HarmonyPrefix]
        private static void GetGameModeClient(ref KitchenLevelConfigBase levelConfig)
        {
            UpdateLevel(ref levelConfig);
        }
    }
}
