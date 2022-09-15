using UnityEngine;

namespace OC2Modding
{
    public static class AutoCompleteLevel
    {
        public static void Awake()
        {
            // Harmony.CreateAndPatchAll(typeof(AutoCompleteLevel));
        }

        public static void Update()
        {
            if (GameLog.autoCompleteMode == GameLog.AutoCompleteMode.AUTO_COMPLETE_DISABLED)
            {
                return;
            }

            if (T17InGameFlow.Instance == null)
            {
                return;
            }

            if (T17InGameFlow.Instance.IsPauseMenuOpen())
            {
                return;
            }

            GameSession gameSession = GameUtils.GetGameSession();
            if (gameSession == null)
            {
                return;
            }

            if (gameSession.GameModeKind != GameModes.Kind.Campaign)
            {
                return;
            }

            ServerCampaignFlowController flowController = GameObject.FindObjectOfType<ServerCampaignFlowController>();
            if (flowController == null)
            {
                return; 
            }

            if (!flowController.InRound)
            {
                return;
            }

            ServerTeamMonitor monitor = flowController.GetMonitorForTeam(TeamID.One);
            if (monitor == null)
            {
                return;
            }

            SceneDirectoryData.PerPlayerCountDirectoryEntry sceneVariant = gameSession.LevelSettings.SceneDirectoryVarientEntry;
            if (sceneVariant.LevelConfig.name.StartsWith("s_dynamic_stage_04"))
            {
                return; // level 6-6 is exempt because of it's unique behavior
            }

            int targetScore;
            switch(GameLog.autoCompleteMode)
            {
                case GameLog.AutoCompleteMode.AUTO_COMPLETE_ONE_STAR:
                {
                    targetScore = sceneVariant.OneStarScore;
                    break;
                }
                case GameLog.AutoCompleteMode.AUTO_COMPLETE_TWO_STAR:
                {
                    targetScore = sceneVariant.TwoStarScore;
                    break;
                }
                case GameLog.AutoCompleteMode.AUTO_COMPLETE_THREE_STAR:
                {
                    targetScore = sceneVariant.ThreeStarScore;
                    break;
                }
                case GameLog.AutoCompleteMode.AUTO_COMPLETE_FOUR_STAR:
                {
                    targetScore = sceneVariant.FourStarScore;
                    break;
                }
                default:
                {
                    targetScore = 9999;
                    break;
                }
            }

            if (monitor.Score.GetTotalScore() < targetScore)
            {
                return;
            }

            if (!OC2Helpers.IsHostPlayer())
            {
                return;
            }

            flowController.SkipToEnd();
        }
    }
}
