using HarmonyLib;

namespace OC2Modding
{
    public static class OnLevelCompleted
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(OnLevelCompleted));
        }

        private static void RunCompletedLevelRoutines(int level_id)
        {
            if (OC2Config.OnLevelCompleted.ContainsKey(level_id))
            {
                foreach (OC2Config.OnLevelCompletedEvent e in OC2Config.OnLevelCompleted[level_id])
                {
                    try
                    {
                        if (e.action == "SET_BOOL")
                        {
                            var tokens = e.payload.Split('=');
                            string payload = $"{{\"{tokens[0]}\":{tokens[1]}}}";
                            OC2Config.UpdateConfig(payload);
                        }

                        if (e.message != "")
                        {
                            GameLog.LogMessage(e.message);
                        }
                    }
                    catch
                    {
                        OC2Modding.Log.LogError($"Failed to process post-complete event for level #{level_id}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameProgress), "ApplyLevelProgress")]
        [HarmonyPrefix]
        private static void ApplyLevelProgress(ref GameProgress.GameProgressData _saveData, ref int _levelIndex, ref int _starRating, ref bool _complete)
        {
            if (OC2Helpers.GetCurrentDLCID() != -1)
            {
                return; // not story, not relevant to the mod
            }

            if (!_complete || _starRating < 1)
            {
                return; // 0 stars
            }

            GameProgress.GameProgressData.LevelProgress levelProgress = _saveData.GetLevelProgress(_levelIndex);
            bool first_completion = levelProgress == null || levelProgress.LevelId == -1;

            if (!first_completion && levelProgress.Completed)
            {
                return; // already completed
            }

            RunCompletedLevelRoutines(_levelIndex);
        }
    }
}
