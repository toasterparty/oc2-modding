using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class CustomLevelOrder
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomLevelOrder));
        }

        private static Dictionary<OC2Config.DlcIdAndLevelId, SceneDirectoryData.SceneDirectoryEntry> replacementScenes = null;

        [HarmonyPatch(typeof(T17FrontendFlow), nameof(T17FrontendFlow.Awake))]
        [HarmonyPostfix]
        private static void T17FrontendFlow_Awake()
        {
            if (replacementScenes != null)
            {
                return;
            }

            replacementScenes = new Dictionary<OC2Config.DlcIdAndLevelId, SceneDirectoryData.SceneDirectoryEntry>();

            if (OC2Config.CustomLevelOrder == null)
            {
                return;
            }

            if (!OC2Config.CustomLevelOrder.ContainsKey("Story"))
            {
                return; // No levels in Story are shuffled
            }

            foreach (KeyValuePair<int, OC2Config.DlcIdAndLevelId> kvp in OC2Config.CustomLevelOrder["Story"])
            {
                SceneDirectoryData.SceneDirectoryEntry[] scenes = OC2Helpers.getScenesFromDLC(kvp.Value.Dlc);

                if (scenes == null)
                {
                    continue;
                }

                int levelId = kvp.Value.LevelId;
                if (levelId >= scenes.Length)
                {
                    OC2Modding.Log.LogError($"Out of range levelId={levelId}");
                    continue;
                }

                replacementScenes.Add(kvp.Value, scenes[levelId]);
            }
        }

        private static int scoreScaleHelper(int inScore, float scale)
        {
            float inScore_f = (float)inScore;
            int scaledScore = (int)(inScore_f * scale);
            int remainder = scaledScore % 50;
            return scaledScore - remainder;
        }

        private static int lastDlc = 0;

        [HarmonyPatch(typeof(WorldMapFlowController), nameof(WorldMapFlowController.GetSceneDirectory))]
        [HarmonyPostfix]
        private static void GetSceneDirectory(ref SceneDirectoryData ___m_sceneDirectory, ref SceneDirectoryData __result)
        {
            int currentDlc = OC2Helpers.GetCurrentDLCID();
            if (lastDlc == currentDlc)
            {
                return; // only run once per DLC change
            }
            lastDlc = currentDlc;

            if (currentDlc == -1 && OC2Config.LevelPurchaseRequirements != null) // Story only
            {
                OC2Modding.Log.LogInfo("Modifying Level Purchase Requirements...");

                foreach (KeyValuePair<int, int> kvp in OC2Config.LevelPurchaseRequirements)
                {
                    ___m_sceneDirectory.Scenes[kvp.Key].StarCost = kvp.Value;
                }
            }

            bool isCustomLevelOrder = OC2Config.CustomLevelOrder != null && currentDlc == -1 && OC2Config.CustomLevelOrder.ContainsKey("Story");
            if (isCustomLevelOrder) // TOOD: no reason not to allow other levels
            {
                if (replacementScenes == null)
                {
                    OC2Modding.Log.LogError("replacementScenes dict was null");
                }
                else
                {
                    OC2Modding.Log.LogInfo("Modifying Scene Directory to change level order...");
                    
                    Dictionary<int, OC2Config.DlcIdAndLevelId> levelOrder = OC2Config.CustomLevelOrder["Story"];
                    SceneDirectoryData.SceneDirectoryEntry[] originalScenes = (SceneDirectoryData.SceneDirectoryEntry[])___m_sceneDirectory.Scenes.Clone();

                    for (int levelId = 0; levelId < originalScenes.Length; levelId++)
                    {
                        if (!levelOrder.ContainsKey(levelId))
                        {
                            continue;
                        }

                        SceneDirectoryData.SceneDirectoryEntry originalScene = originalScenes[levelId];
                        OC2Config.DlcIdAndLevelId newDlcAndLevel = levelOrder[levelId];
                        if (!replacementScenes.ContainsKey(newDlcAndLevel))
                        {
                            OC2Modding.Log.LogError($"replacementScenes missing key {newDlcAndLevel.Dlc}:{newDlcAndLevel.LevelId}");
                            continue;
                        }
                        SceneDirectoryData.SceneDirectoryEntry newScene = replacementScenes[newDlcAndLevel];

                        newScene.LevelChainEnd = originalScene.LevelChainEnd;
                        newScene.IsHidden = originalScene.IsHidden;
                        newScene.StarCost = originalScene.StarCost;
                        newScene.PreviousEntriesToUnlock = originalScene.PreviousEntriesToUnlock;

                        ___m_sceneDirectory.Scenes[levelId] = newScene;
                    }
                }
            }

            if (OC2Config.LeaderboardScoreScale != null)
            {
                OC2Modding.Log.LogInfo("Filling out custom star boundaries...");
                int levelId = 0;
                foreach (SceneDirectoryData.SceneDirectoryEntry scene in ___m_sceneDirectory.Scenes)
                {
                    foreach (SceneDirectoryData.PerPlayerCountDirectoryEntry variant in scene.SceneVarients)
                    {
                        if (scene.World == SceneDirectoryData.World.Invalid)
                        {
                            continue;
                        }

                        int dlc = OC2Helpers.DLCFromWorld(scene.World);
                        int playerCount = variant.PlayerCount;
                        int actualLevelId;
                        if (isCustomLevelOrder && OC2Config.CustomLevelOrder["Story"].ContainsKey(levelId))
                        {
                            actualLevelId = OC2Config.CustomLevelOrder["Story"][levelId].LevelId;
                        }
                        else
                        {
                            actualLevelId = levelId;
                        }

                        int worldRecordScore = OC2Helpers.getScoresFromLeaderboard(dlc, actualLevelId, playerCount);
                        var prop = variant.GetType().GetField("m_PCStarBoundaries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (worldRecordScore <= 0)
                        {
                            /* Fallback to 4-star score as WR where none is submitted */
                            worldRecordScore = ((SceneDirectoryData.StarBoundaries)prop.GetValue(variant)).m_FourStarScore;
                            // TODO: Get your friends to just submit everything
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

            __result = ___m_sceneDirectory;
        }
    }
}
