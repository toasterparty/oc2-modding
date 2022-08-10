using HarmonyLib;
using System.Collections.Generic;

namespace OC2Modding
{
    public static class LevelProgression
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(LevelProgression));
        }

        public static bool IsLevelCompleted(int levelId)
        {
            return GameUtils.GetGameSession().Progress.SaveData.GetLevelProgress(levelId).ScoreStars > 0;
        }

        [HarmonyPatch(typeof(SaveSlotElement), nameof(SaveSlotElement.ServerLoadCampaign))]
        [HarmonyPrefix]
        private static void ServerLoadCampaign()
        {
            if (OC2Config.SkipTutorial)
            {
                GameUtils.GetDebugConfig().m_skipTutorial = true;
            }
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.GetLevelProgress))]
        [HarmonyPostfix]
        private static void GetLevelProgress(ref int _id, ref GameProgress.GameProgressData.LevelProgress __result)
        {
            if (OC2Config.PurchaseAllLevels)
            {
                __result.Purchased = true;
            }

            if (OC2Config.RevealAllLevels)
            {
                __result.Revealed = true;
                __result.NGPEnabled = true;
                __result.ObjectivesCompleted = true;
            }

            if (OC2Config.SkipTutorial && _id == 0)
            {
                __result.Completed = true;
                __result.ObjectivesCompleted = true;
                __result.ScoreStars = 3;
            }
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.IsLevelUnlocked))]
        [HarmonyPostfix]
        private static void IsLevelUnlocked(ref int _levelIndex, ref bool __result)
        {
            if (OC2Config.LevelUnlockRequirements.ContainsKey(_levelIndex))
            {
                __result = IsLevelCompleted(OC2Config.LevelUnlockRequirements[_levelIndex]);
            }

            if (OC2Config.RevealAllLevels)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.FillOut))]
        [HarmonyPrefix]
        private static void FillOut(ref SceneDirectoryData _sceneDirectory, ref GameProgress.GameProgressData.LevelProgress[] ___Levels)
        {
            foreach (KeyValuePair<int, int> kvp in OC2Config.LevelPurchaseRequirements)
            {
                _sceneDirectory.Scenes[kvp.Key].StarCost = kvp.Value;
            }
        }
    }
}
