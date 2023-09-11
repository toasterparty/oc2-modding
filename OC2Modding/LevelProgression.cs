using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine;
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
            if (OC2Config.Config.SkipTutorial)
            {
                GameUtils.GetDebugConfig().m_skipTutorial = true;
            }
            
            if (!OC2Helpers.IsHostPlayer())
            {
                return;
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
            if (OC2Config.Config.PurchaseAllLevels)
            {
                __result.Purchased = true;
            }

            if (OC2Config.Config.RevealAllLevels)
            {
                __result.Revealed = true;
                __result.NGPEnabled = true;
                __result.ObjectivesCompleted = true;
            }

            if (OC2Helpers.GetCurrentDLCID() == -1) // Story only
            {
                if (OC2Config.Config.SkipTutorial && _id == 0)
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                    // __result.ScoreStars = 3;
                }

                if (OC2Config.Config.SkipTutorial && _id == 45) // Post-tutorial Onion King
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                }

                if (_id >= 45 && _id <= 51 && OC2Config.Config.SkipAllOnionKing) // Onion King
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                }

                /* If this level is being used as an unlock requirement, then it's glowing orb should not be used */
                foreach (KeyValuePair<int, int> kvp in OC2Config.Config.LevelUnlockRequirements)
                {
                    if (_id == kvp.Value)
                    {
                        __result.ObjectivesCompleted = true;
                        break;
                    }
                }

                if (__result.Completed)
                {
                    ArchipelagoClient.VisitLocation(_id);
                }

                if (OC2Config.Config.PseudoSave.ContainsKey(_id))
                {
                    __result.Completed = true;
                    __result.ObjectivesCompleted = true;
                    __result.Purchased = true;
                    __result.Revealed = true;
                    if (__result.HighScore <= 0)
                    {
                        __result.HighScore = 1;
                    }
                    __result.ScoreStars = OC2Config.Config.PseudoSave[_id];
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

            if (OC2Config.Config.LevelUnlockRequirements.ContainsKey(_levelIndex))
            {
                __result = IsLevelCompleted(OC2Config.Config.LevelUnlockRequirements[_levelIndex]);
            }

            if (OC2Config.Config.LevelForceHide.Contains(_levelIndex))
            {
                __result = false;
            }

            if (OC2Config.Config.LevelForceReveal.Contains(_levelIndex))
            {
                __result = true;
            }

            if (OC2Config.Config.RevealAllLevels)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(CoopStarRatingUIController), nameof(CoopStarRatingUIController.SetScoreData))]
        [HarmonyPostfix]
        private static void SetScoreData(ref T17Text ___m_levelTitleText)
        {
            if (OC2Config.Config.ShowWorldName) 
            {
                GameSession gameSession = GameUtils.GetGameSession();
                int levelID = GameUtils.GetLevelID();

                OC2Helpers.DlcAndLevel dal = OC2Helpers.getLevelName(gameSession.DLC, levelID);

                ___m_levelTitleText.m_bNeedsLocalization = false;
                ___m_levelTitleText.text = dal.dlc + " " + dal.level;
            }
        }

        // Stolen from dnSpy
        [RequireComponent(typeof(Transform))]
        [NativeHeader("Runtime/Graphics/Mesh/MeshFilter.h")]
        public sealed class MeshFilter : Component
        {
            public extern Mesh sharedMesh
            {
                [MethodImpl(MethodImplOptions.InternalCall)]
                get;
                [MethodImpl(MethodImplOptions.InternalCall)]
                set;
            }

            public extern Mesh mesh
            {
                [NativeMethod(Name = "GetInstantiatedMeshFromScript")]
                [MethodImpl(MethodImplOptions.InternalCall)]
                get;
                [NativeMethod(Name = "SetInstantiatedMesh")]
                [MethodImpl(MethodImplOptions.InternalCall)]
                set;
            }
        }
    }
}
