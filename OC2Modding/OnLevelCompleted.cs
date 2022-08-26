using HarmonyLib;

namespace OC2Modding
{
    public static class OnLevelCompleted
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(OnLevelCompleted));
        }

        private static void run_completed_level_routines(int level_id)
        {
            // GameLog.LogMessage($"Completed Level {level_id} for the first time");
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

            run_completed_level_routines(_levelIndex);
        }
    }
}
