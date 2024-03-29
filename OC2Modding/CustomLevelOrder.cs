using System.Collections.Generic;
using HarmonyLib;
using GameModes.Horde;

namespace OC2Modding
{
    public static class CustomLevelOrder
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomLevelOrder));
        }

        private static Dictionary<ModConfig.DlcIdAndLevelId, SceneDirectoryData.SceneDirectoryEntry> replacementScenes = null;

        private static void UpdateReplacementScenes()
        {
            if (replacementScenes != null)
            {
                return;
            }

            replacementScenes = new Dictionary<ModConfig.DlcIdAndLevelId, SceneDirectoryData.SceneDirectoryEntry>();

            if (OC2Config.Config.CustomLevelOrder == null)
            {
                return;
            }

            if (!OC2Config.Config.CustomLevelOrder.ContainsKey("Story"))
            {
                return; // No levels in Story are shuffled
            }

            foreach (KeyValuePair<int, ModConfig.DlcIdAndLevelId> kvp in OC2Config.Config.CustomLevelOrder["Story"])
            {
                SceneDirectoryData.SceneDirectoryEntry[] scenes = OC2Helpers.getScenesFromDLC(kvp.Value.dlc);

                if (scenes == null)
                {
                    continue;
                }

                int levelId = kvp.Value.LevelID;
                if (levelId >= scenes.Length)
                {
                    OC2Modding.Log.LogError($"Out of range levelId={levelId}, dlc={OC2Helpers.DLCFromDLCID(kvp.Value.dlc)}");
                    continue;
                }

                replacementScenes[kvp.Value] = scenes[levelId];
            }
        }

        [HarmonyPatch(typeof(T17FrontendFlow), nameof(T17FrontendFlow.Awake))]
        [HarmonyPostfix]
        private static void T17FrontendFlow_Awake()
        {
            UpdateReplacementScenes();
        }

        private static int scoreScaleHelper(int inScore, float scale, float timeScale)
        {
            float inScore_f = (float)inScore; // cast to float
            float scaledScore_f = inScore_f * scale; // scale score by difficulty curve

            scaledScore_f *= timeScale; // scale score by level access time
            
            int scaledScore = (int)scaledScore_f; // cast back to int

            // round up to nearest 10
            if ((scaledScore % 10) >= 5)
            {
                scaledScore += 10 - (scaledScore % 10);
            }
            // round down to nearest 10
            else
            {
                scaledScore -= (scaledScore % 10);
            }

            return scaledScore;
        }

        private static int lastDlc = 0;

        [HarmonyPatch(typeof(WorldMapFlowController), nameof(WorldMapFlowController.GetSceneDirectory))]
        [HarmonyPostfix]
        private static void GetSceneDirectory(ref SceneDirectoryData ___m_sceneDirectory, ref SceneDirectoryData __result)
        {
            UpdateReplacementScenes();

            int currentDlc = OC2Helpers.GetCurrentDLCID();
            if (lastDlc == currentDlc)
            {
                return; // only run once per DLC change
            }
            lastDlc = currentDlc;

            if (currentDlc == -1 && OC2Config.Config.LevelPurchaseRequirements != null) // Story only
            {
                OC2Modding.Log.LogInfo("Modifying Level Purchase Requirements...");

                foreach (KeyValuePair<int, int> kvp in OC2Config.Config.LevelPurchaseRequirements)
                {
                    // OC2Modding.Log.LogInfo($"levelid={kvp.Key}'s star cost set to {kvp.Value}");
                    ___m_sceneDirectory.Scenes[kvp.Key].StarCost = kvp.Value;
                }
            }

            Dictionary<int, int> storyLevelIdToDlc = new Dictionary<int, int>();
            bool isCustomLevelOrder = OC2Config.Config.CustomLevelOrder != null && currentDlc == -1 && OC2Config.Config.CustomLevelOrder.ContainsKey("Story");
            if (isCustomLevelOrder)
            {
                if (replacementScenes == null)
                {
                    OC2Modding.Log.LogError("replacementScenes dict was null");
                }
                else
                {
                    OC2Modding.Log.LogInfo("Modifying Scene Directory to change level order...");

                    Dictionary<int, ModConfig.DlcIdAndLevelId> levelOrder = OC2Config.Config.CustomLevelOrder["Story"];
                    SceneDirectoryData.SceneDirectoryEntry[] originalScenes = (SceneDirectoryData.SceneDirectoryEntry[])___m_sceneDirectory.Scenes.Clone();

                    for (int levelId = 0; levelId < originalScenes.Length; levelId++)
                    {
                        if (!levelOrder.ContainsKey(levelId))
                        {
                            continue;
                        }

                        SceneDirectoryData.SceneDirectoryEntry originalScene = originalScenes[levelId];
                        ModConfig.DlcIdAndLevelId newDlcAndLevel = levelOrder[levelId];
                        if (!replacementScenes.ContainsKey(newDlcAndLevel))
                        {
                            OC2Modding.Log.LogError($"replacementScenes missing key {newDlcAndLevel.dlc}:{newDlcAndLevel.LevelID}");
                            continue;
                        }
                        SceneDirectoryData.SceneDirectoryEntry newScene = replacementScenes[newDlcAndLevel];

                        newScene.LevelChainEnd = originalScene.LevelChainEnd;
                        newScene.IsHidden = originalScene.IsHidden;
                        if (OC2Config.Config.LevelPurchaseRequirements != null && OC2Config.Config.LevelPurchaseRequirements.ContainsKey(levelId))
                        {

                            newScene.StarCost = OC2Config.Config.LevelPurchaseRequirements[levelId];
                        }
                        else
                        {
                            newScene.StarCost = originalScene.StarCost;
                        }
                        newScene.PreviousEntriesToUnlock = originalScene.PreviousEntriesToUnlock;

                        ___m_sceneDirectory.Scenes[levelId] = newScene;

                        SceneDirectoryData sceneDirectory = GameUtils.GetGameSession().Progress.GetSceneDirectory();
                        SceneDirectoryData.SceneDirectoryEntry sceneDirectoryEntry = sceneDirectory.Scenes[levelId];
                        foreach (SceneDirectoryData.PerPlayerCountDirectoryEntry sceneVarient in sceneDirectoryEntry.SceneVarients)
                        {
                            if (sceneVarient.LevelConfig == null)
                            {
                                continue;
                            }

                            LevelConfigBase levelConfigBase = sceneVarient.LevelConfig;

                            // Clear the objectives for levels moved as part of level shuffle to stop the pole from glowing
                            levelConfigBase.m_objectives = new LevelObjectiveBase[] {};

                            // Edit horde level config
                            if (OC2Config.Config.ShortHordeLevels && levelConfigBase is HordeLevelConfig)
                            {
                                var hordeLevelConfig = sceneVarient.LevelConfig as HordeLevelConfig;
                                var waves = hordeLevelConfig.m_waves;
                                var prop = waves.GetType().GetField("m_waves", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                var m_waves = (List<HordeWaveData>)prop.GetValue(waves);
                                int newWaveCount = (int)(waves.Count * 0.6 + 0.5f);

                                // remove every other wave until desired count is reached
                                int remove = 0;
                                while (waves.Count > newWaveCount)
                                {
                                    m_waves.RemoveAt(remove);
                                    remove = (remove + 1) % waves.Count;
                                }
                            }
                        }

                        // Helper for determining DLC when patching scores
                        storyLevelIdToDlc.Add(levelId, newDlcAndLevel.dlc);
                    }
                }
            }

            if (OC2Config.Config.LeaderboardScoreScale != null)
            {
                OC2Modding.Log.LogInfo("Filling out custom star boundaries...");
                int levelId = 0;
                foreach (SceneDirectoryData.SceneDirectoryEntry scene in ___m_sceneDirectory.Scenes)
                {
                    foreach (SceneDirectoryData.PerPlayerCountDirectoryEntry variant in scene.SceneVarients)
                    {
                        int dlc;
                        if (storyLevelIdToDlc.ContainsKey(levelId))
                        {
                            dlc = storyLevelIdToDlc[levelId]; // possibly another dlc due to shuffled level order
                        }
                        else
                        {
                            dlc = -1; // vanilla story level
                        }

                        int playerCount = variant.PlayerCount;
                        int actualLevelId;
                        if (isCustomLevelOrder && OC2Config.Config.CustomLevelOrder["Story"].ContainsKey(levelId))
                        {
                            actualLevelId = OC2Config.Config.CustomLevelOrder["Story"][levelId].LevelID;
                        }
                        else
                        {
                            actualLevelId = levelId;
                        }

                        SceneDirectoryData.StarBoundaries starBoundariesOverride = new SceneDirectoryData.StarBoundaries();
                        var prop = variant.GetType().GetField("m_PCStarBoundaries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (OC2Helpers.IsLevelHordeLevel(levelId))
                        {
                            starBoundariesOverride.m_OneStarScore = 0;
                            starBoundariesOverride.m_TwoStarScore = 0;
                            starBoundariesOverride.m_ThreeStarScore = 0;
                            starBoundariesOverride.m_FourStarScore = 0;
                        }
                        else
                        {
                            // OC2Modding.Log.LogInfo($"\n\nstory_id={levelId}, dlc={dlc}, actualLevelId={actualLevelId}");
                            int worldRecordScore = OC2Helpers.getScoresFromLeaderboard(dlc, actualLevelId, playerCount);
                            if (worldRecordScore <= 0)
                            {
                                /* Fallback to 4-star score as WR where none is submitted */
                                worldRecordScore = ((SceneDirectoryData.StarBoundaries)prop.GetValue(variant)).m_FourStarScore;
                                // TODO: Get your friends to just submit everything
                            }

                            /* If the level is a dynamic level, we don't ever scale the level timer, however, these levels are almost exclusively statistical
                            outliers in terms of the gap between a good player and the current world record. This difficulty spike, in combination with the extended level
                            duration, means that a well balanced randomizer should still grant some leniency in these cases (cut WR score by 15%).
                            
                            If the level is not a dynamic level, then cut the world record by the same ammount that the level duration is cut by. */
                            float timeScale = OC2Helpers.IsDynamicLevel(variant.LevelConfig.name) ? 0.85f : OC2Config.Config.LevelTimerScale;

                            starBoundariesOverride.m_FourStarScore  = scoreScaleHelper(worldRecordScore, OC2Config.Config.LeaderboardScoreScale.FourStars , timeScale);
                            starBoundariesOverride.m_ThreeStarScore = scoreScaleHelper(worldRecordScore, OC2Config.Config.LeaderboardScoreScale.ThreeStars, timeScale);
                            starBoundariesOverride.m_TwoStarScore   = scoreScaleHelper(worldRecordScore, OC2Config.Config.LeaderboardScoreScale.TwoStars  , timeScale);
                            starBoundariesOverride.m_OneStarScore   = scoreScaleHelper(worldRecordScore, OC2Config.Config.LeaderboardScoreScale.OneStar   , timeScale);

                            // OC2Modding.Log.LogInfo($"{starBoundariesOverride.m_OneStarScore} / {starBoundariesOverride.m_TwoStarScore} / {starBoundariesOverride.m_ThreeStarScore} / {starBoundariesOverride.m_FourStarScore}");
                        }

                        prop.SetValue(variant, starBoundariesOverride);
                    }
                    levelId++;
                }
            }

            if (currentDlc == -1 && OC2Config.Config.ImpossibleTutorial)
            {
                foreach (SceneDirectoryData.PerPlayerCountDirectoryEntry variant in ___m_sceneDirectory.Scenes[0].SceneVarients)
                {
                    var prop = variant.GetType().GetField("m_PCStarBoundaries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    SceneDirectoryData.StarBoundaries starBoundariesOverride = new SceneDirectoryData.StarBoundaries();
                    starBoundariesOverride.m_FourStarScore  = 9999;
                    starBoundariesOverride.m_ThreeStarScore = 9998;
                    starBoundariesOverride.m_TwoStarScore   = 9997;
                    starBoundariesOverride.m_OneStarScore   = 9996;

                    prop.SetValue(variant, starBoundariesOverride);
                }
            }

            __result = ___m_sceneDirectory;
        }

        [HarmonyPatch(typeof(WorldMapKitchenLevelIconUI), nameof(WorldMapKitchenLevelIconUI.Setup))]
        [HarmonyPrefix]
        private static void Setup_Prefix(ref GameProgress.GameProgressData.LevelProgress _levelProgress, ref WorldMapKitchenLevelIconUI.State _state, ref WorldMapKitchenLevelIconUI __instance)
        {
            if (OC2Config.Config.ImpossibleTutorial && OC2Helpers.GetCurrentDLCID() == -1 && _levelProgress.LevelId == 0)
            {
                _state = WorldMapKitchenLevelIconUI.State.UnSupported;
                __instance.SetCost(999);
            }
        }

        [HarmonyPatch(typeof(WorldMapKitchenLevelIconUI), nameof(WorldMapKitchenLevelIconUI.Setup))]
        [HarmonyPostfix]
        private static void Setup(ref GameProgress.GameProgressData.LevelProgress _levelProgress, ref ScoreBoundaryStar[] ___m_stars)
        {
            if (OC2Helpers.IsLevelHordeLevel(_levelProgress.LevelId))
            {
                ___m_stars[1].gameObject.SetActive(false);
                ___m_stars[2].gameObject.SetActive(false);
                ___m_stars[3].gameObject.SetActive(false);
            }
        }

        /* Use to print level ID to screen on hover */
        // [HarmonyPatch(typeof(ClientLevelPortalMapNode), "SetupUI")]
        // [HarmonyPrefix]
        // private static void SetupUI(ref LevelPortalMapNode ___m_baseLevelPortalMapNode)
        // {
        //     if (___m_baseLevelPortalMapNode.m_sceneProgress == null)
        //     {
        //         return;
        //     }

        //     int levelId = ___m_baseLevelPortalMapNode.m_sceneProgress.LevelId;
        //     GameLog.LogMessage($"LevelID={levelId}");
        // }
    }
}
