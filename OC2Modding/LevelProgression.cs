using System.Collections.Generic;
using HarmonyLib;

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

            if (OC2Config.SkipTutorial && _id == 45) // Post-tutorial Onion King
            {
                __result.Completed = true;
                __result.ObjectivesCompleted = true;
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

        private static int scoreScaleHelper(int inScore, float scale)
        {
            float inScore_f = (float)inScore;
            int scaledScore = (int)(inScore_f * scale);
            int remainder = scaledScore % 50;
            return scaledScore - remainder;
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.FillOut))]
        [HarmonyPrefix]
        private static void FillOut(ref SceneDirectoryData _sceneDirectory, ref GameProgress.GameProgressData.LevelProgress[] ___Levels)
        {
            foreach (KeyValuePair<int, int> kvp in OC2Config.LevelPurchaseRequirements)
            {
                _sceneDirectory.Scenes[kvp.Key].StarCost = kvp.Value;
            }

            if (OC2Config.LeaderboardScoreScale != null)
            {
                int levelId = 0;
                foreach (SceneDirectoryData.SceneDirectoryEntry scene in _sceneDirectory.Scenes)
                {
                    foreach (SceneDirectoryData.PerPlayerCountDirectoryEntry variant in scene.SceneVarients)
                    {
                        if (scene.World == SceneDirectoryData.World.Invalid)
                        {
                            continue;
                        }

                        int dlc = OC2Helpers.DLCFromWorld(scene.World);
                        int playerCount = variant.PlayerCount;

                        int worldRecordScore = OC2Helpers.getScoresFromLeaderboard(dlc, levelId, playerCount);
                        var prop = variant.GetType().GetField("m_PCStarBoundaries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (worldRecordScore <= 0)
                        {
                            /* Fallback to 4-star score as WR where none is submitted */
                            worldRecordScore = ((SceneDirectoryData.StarBoundaries)prop.GetValue(variant)).m_FourStarScore;
                        }

                        SceneDirectoryData.StarBoundaries starBoundariesOverride = new SceneDirectoryData.StarBoundaries();
                        starBoundariesOverride.m_FourStarScore  = scoreScaleHelper(worldRecordScore, OC2Config.LeaderboardScoreScale[4]);
                        starBoundariesOverride.m_ThreeStarScore = scoreScaleHelper(worldRecordScore, OC2Config.LeaderboardScoreScale[3]);
                        starBoundariesOverride.m_TwoStarScore   = scoreScaleHelper(worldRecordScore, OC2Config.LeaderboardScoreScale[2]);
                        starBoundariesOverride.m_OneStarScore   = scoreScaleHelper(worldRecordScore, OC2Config.LeaderboardScoreScale[1]);

                        prop.SetValue(variant, starBoundariesOverride);
                    }
                    levelId++;
                }
            }
        }
    }
}
