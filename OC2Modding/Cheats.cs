using UnityEngine;
using HarmonyLib;

namespace OC2Modding
{
    public static class Cheats
    {
        private static bool SkipLevel = false;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Cheats));
        }

        public static void Update()
        {
            if (!OC2Config.CheatsEnabled)
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
            else if (Input.GetKeyDown(KeyCode.Keypad3))
            {
                FinishLevel(4);
            }
        }

        private static void FinishLevel(int stars)
        {
            ServerCampaignFlowController flowController = GameObject.FindObjectOfType<ServerCampaignFlowController>();
            if (flowController == null)
            {
                OC2Modding.Log.LogWarning("Couldn't Skip Level due to not being able to find ServerCampaignFlowController");
                return;
            }

            flowController.SkipLevel(stars);
            GameLog.LogMessage($"Auto-completed current level with {stars}-Star score");
        }

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            SkipLevel = false;
        }

        [HarmonyPatch(typeof(ServerCampaignFlowController), "OnSuccessfulDelivery")]
        [HarmonyPostfix]
        private static void OnSuccessfulDelivery(ref ServerCampaignFlowController __instance)
        {
            if (SkipLevel)
            {
                SkipLevel = false;
                __instance.SkipToEnd();
            }
        }
    }
}
