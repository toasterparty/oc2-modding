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
            if (OC2Config.CheatsEnabled && Input.GetKeyDown(KeyCode.Delete))
            {
                SkipLevel = true;
            }
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
