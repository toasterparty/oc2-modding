using System;
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
            if (ArchipelagoClient.REMOTE_INVENTORY && ArchipelagoClient.IsConnected) {
                return; // The client is not reponsible for giving their own items
            }

            OC2Config.LevelCompleted(level_id);
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

            ArchipelagoClient.VisitLocation(_levelIndex);

            GameProgress.GameProgressData.LevelProgress levelProgress = _saveData.GetLevelProgress(_levelIndex);

            // Update "save file"
            if (!OC2Config.Config.PseudoSave.ContainsKey(_levelIndex) || _starRating > OC2Config.Config.PseudoSave[_levelIndex]) {
                OC2Config.Config.PseudoSave[_levelIndex] = _starRating;
                ArchipelagoClient.SendPseudoSave();
            }

            bool first_completion = levelProgress == null || levelProgress.LevelId == -1;

            if ((!first_completion && levelProgress.Completed) || LevelProgression.IsLevelCompleted(_levelIndex))
            {
                return; // already completed
            }

            RunCompletedLevelRoutines(_levelIndex);
        }

        [HarmonyPatch(typeof(GameProgress), nameof(GameProgress.GetStarTotal))]
        [HarmonyPostfix]
        private static void GetStarTotal(ref int __result)
        {
            __result += OC2Config.Config.StarOffset;
        }

        /* Horde Levels should never increase star count */
        [HarmonyPatch(typeof(GameProgress), "ApplyLevelProgress")]
        [HarmonyPrefix]
        private static void ApplyLevelProgress(ref int _levelIndex, ref int _starRating)
        {
            if (OC2Helpers.IsLevelHordeLevel(_levelIndex)) {
                _starRating = 0;
            }
        }
    }
}
