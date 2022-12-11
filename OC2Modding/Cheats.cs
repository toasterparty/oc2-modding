using UnityEngine;
using GameModes.Horde;
using HarmonyLib;

namespace OC2Modding
{
    public static class Cheats
    {
        private static bool SkipLevel = false;
        private static bool Printed = false;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Cheats));
        }

        public static void Update()
        {
            if (!OC2Config.Config.CheatsEnabled)
            {
                return;
            }
            
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                SkipLevel = true;
            }
            else if (Input.GetKeyDown(KeyCode.Keypad1))
            {
                FinishLevel(1);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad2))
            {
                FinishLevel(2);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                FinishLevel(3);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad4))
            {
                FinishLevel(4);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                FinishLevel(5);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad6))
            {
                FinishLevel(6);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad7))
            {
                FinishLevel(7);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad8))
            {
                FinishLevel(8);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                FinishLevel(9);
            }
            else if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                FinishLevel(10);
            }
        }

        private static int forceFinishHorde = -1;

        private static void FinishLevel(int stars)
        {
            if (!OC2Helpers.IsHostPlayer())
            {
                return;
            }

            ServerCampaignFlowController flowController = GameObject.FindObjectOfType<ServerCampaignFlowController>();
            if (flowController == null)
            {
                var hordeController = GameObject.FindObjectOfType<ServerHordeFlowController>();
                if (hordeController == null)
                {
                    OC2Modding.Log.LogWarning("Couldn't Skip Level due to not being able to find ServerCampaignFlowController");
                    return;
                }

                forceFinishHorde = stars;
                return;
            }

            if (stars > 4)
            {
                stars = 4;
            }

            flowController.SkipLevel(stars);
            if (!Printed)
            {
                Printed = true;
                GameLog.LogMessage($"Auto-completed current level with {stars}-Star score");
            }
        }

        [HarmonyPatch(typeof(ServerHordeFlowController), "HasFinished")]
        [HarmonyPostfix]
        private static void HasFinished(ref bool __result, ref TeamScoreStats ___m_score, ref ServerHordeFlowController __instance)
        {
            if (forceFinishHorde != -1)
            {
                ___m_score.TotalHealth = (int)(((float)__instance.MaxHealth)*((float)forceFinishHorde/10.0f));
                GameLog.LogMessage($"Auto-completed current level with {(int)(100.0f*((float)___m_score.TotalHealth/(float)__instance.MaxHealth))}% health remaining");
                __result = true;
            }
        }

        [HarmonyPatch(typeof(ClientHordeFlowController), "RunLevelOutro")]
        [HarmonyPrefix]
        private static void RunLevelOutro(ref ClientHordeFlowController __instance, ref TeamScoreStats ___m_score)
        {
            if (forceFinishHorde != -1)
            {
                ___m_score.TotalHealth = (int)(((float)__instance.MaxHealth)*((float)forceFinishHorde/10.0f));
                forceFinishHorde = -1;
            }
        }

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            SkipLevel = false;
            Printed = false;
            forceFinishHorde = -1;
        }

        [HarmonyPatch(typeof(ServerCampaignFlowController), "OnSuccessfulDelivery")]
        [HarmonyPostfix]
        private static void OnSuccessfulDelivery(ref ServerCampaignFlowController __instance)
        {
            if (SkipLevel && OC2Helpers.IsHostPlayer())
            {
                SkipLevel = false;
                __instance.SkipToEnd();
            }
        }
    }
}
