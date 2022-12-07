using HarmonyLib;

namespace OC2Modding
{
    public static class CustomOrderTimeoutPenalty
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomOrderTimeoutPenalty));
        }

        [HarmonyPatch(typeof(GameUtils), nameof(GameUtils.GetGameConfig))]
        [HarmonyPostfix]
        private static void GetGameConfig(ref GameConfig __result)
        {
            if (__result != null && OC2Config.Config.CustomOrderTimeoutPenalty >= 0) {
                __result.RecipeTimeOutPointLoss = OC2Config.Config.CustomOrderTimeoutPenalty;
            }
        }
    }
}
