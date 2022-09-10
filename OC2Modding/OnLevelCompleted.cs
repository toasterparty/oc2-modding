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
            if (OC2Config.OnLevelCompleted.ContainsKey(level_id))
            {
                foreach (OC2Config.OnLevelCompletedEvent e in OC2Config.OnLevelCompleted[level_id])
                {
                    try
                    {
                        if (e.action == "SET_VALUE")
                        {
                            var tokens = e.payload.Split('=');
                            string payload = $"{{\"{tokens[0]}\":{tokens[1]}}}";
                            OC2Config.UpdateConfig(payload);
                        }
                        else if (e.action == "UNLOCK_LEVEL")
                        {
                            int id = Int32.Parse(e.payload);
                            if (!OC2Config.LevelForceReveal.Contains(id))
                            {
                                OC2Config.LevelForceReveal.Add(id);
                            }
                        }
                        else if (e.action == "UNLOCK_EMOTE")
                        {
                            int id = Int32.Parse(e.payload);
                            if (OC2Config.LockedEmotes.Contains(id))
                            {
                                OC2Config.LockedEmotes.Remove(id);
                            }
                        }
                        else if (e.action == "INC_TIP_COMBO")
                        {
                            if (OC2Config.MaxTipCombo < 4)
                            {
                                OC2Config.MaxTipCombo++;
                            }
                        }
                        else if (e.action == "INC_ORDERS_ON_SCREEN")
                        {
                            if (OC2Config.MaxOrdersOnScreenOffset < 0)
                            {
                                OC2Config.MaxOrdersOnScreenOffset++;
                            }
                        }
                        else if (e.action == "INC_STAR_COUNT")
                        {
                            int count = Int32.Parse(e.payload);
                            OC2Config.StarOffset += count;
                        }
                        else if (e.action == "INC_DASH")
                        {
                            if (OC2Config.DisableDash)
                            {
                                OC2Config.DisableDash = false;
                            }
                            else
                            {
                                OC2Config.WeakDash = false;
                            }
                        }
                        else if (e.action == "INC_THROW")
                        {
                            if (OC2Config.DisableThrow)
                            {
                                OC2Config.DisableThrow = false;
                            }
                            else
                            {
                                OC2Config.DisableCatch = false;
                            }
                        }

                        if (e.message != "")
                        {
                            GameLog.LogMessage(e.message);
                        }
                    }
                    catch (Exception _e)
                    {
                        OC2Modding.Log.LogError($"Failed to process post-complete event for level #{level_id}: action={e.action}, payload={e.payload}, message={e.message}\n{_e}");
                    }
                }

                OC2Config.FlushConfig();
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

            ArchipelagoClient.VisitLocation(_levelIndex);

            GameProgress.GameProgressData.LevelProgress levelProgress = _saveData.GetLevelProgress(_levelIndex);
            bool first_completion = levelProgress == null || levelProgress.LevelId == -1;

            if ((!first_completion && levelProgress.Completed) || LevelProgression.IsLevelCompleted(_levelIndex))
            {
                return; // already completed
            }

            RunCompletedLevelRoutines(_levelIndex);

            if (_levelIndex == 36)
            {
                ArchipelagoClient.SendCompletion();
            }
        }

        [HarmonyPatch(typeof(GameProgress), nameof(GameProgress.GetStarTotal))]
        [HarmonyPostfix]
        private static void GetStarTotal(ref int __result)
        {
            __result += OC2Config.StarOffset;
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
