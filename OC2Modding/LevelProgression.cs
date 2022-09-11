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
            bool result = GameUtils.GetGameSession().Progress.SaveData.GetLevelProgress(levelId).Completed;
            if (result && OC2Helpers.GetCurrentDLCID() == -1)
            {
                ArchipelagoClient.VisitLocation(levelId);
            }
            return result;
        }

        [HarmonyPatch(typeof(SaveSlotElement), nameof(SaveSlotElement.ServerLoadCampaign))]
        [HarmonyPrefix]
        private static void ServerLoadCampaign(ref GameSession session)
        {
            if (OC2Config.SkipTutorial)
            {
                GameUtils.GetDebugConfig().m_skipTutorial = true;
            }
            
            var Levels = session.Progress.SaveData.Levels;

            for (int levelId = 0; levelId < Levels.Length; levelId++)
            {
                if (Levels[levelId].Completed)
                {
                    ArchipelagoClient.VisitLocation(levelId);
                }
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

            if (OC2Helpers.GetCurrentDLCID() == -1) // Story only
            {
                if (OC2Config.SkipTutorial && _id == 0)
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                    // __result.ScoreStars = 3;
                }

                if (OC2Config.SkipTutorial && _id == 45) // Post-tutorial Onion King
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                }

                if (_id >= 45 && _id <= 51 && OC2Config.SkipAllOnionKing) // Onion King
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                }

                if (__result.Completed)
                {
                    ArchipelagoClient.VisitLocation(_id);
                }

                if (OC2Config.PseudoSave.ContainsKey(_id))
                {
                    __result.Completed = true;
                    __result.Purchased = true;
                    __result.Revealed = true;
                    if (__result.HighScore <= 0)
                    {
                        __result.HighScore = 1;
                    }
                    __result.ScoreStars = OC2Config.PseudoSave[_id];
                }
            }
        }

        [HarmonyPatch(typeof(GameProgress.GameProgressData), nameof(GameProgress.GameProgressData.IsLevelUnlocked))]
        [HarmonyPostfix]
        private static void IsLevelUnlocked(ref int _levelIndex, ref bool __result)
        {
            if (OC2Helpers.GetCurrentDLCID() != -1)
            {
                return;
            }

            if (OC2Config.LevelUnlockRequirements.ContainsKey(_levelIndex))
            {
                __result = IsLevelCompleted(OC2Config.LevelUnlockRequirements[_levelIndex]);
            }

            if (OC2Config.LevelForceHide.Contains(_levelIndex))
            {
                __result = false;
            }

            if (OC2Config.LevelForceReveal.Contains(_levelIndex))
            {
                __result = true;
            }

            if (OC2Config.RevealAllLevels)
            {
                __result = true;
            }
        }
    }
}
