using HarmonyLib;

namespace OC2Modding
{
    public static class UnlockAllDLC
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(UnlockAllDLC));
        }

        [HarmonyPatch(typeof(DebugManager), nameof(DebugManager.GetOption))]
        [HarmonyPostfix]
        private static void GetOption(ref string optionName, ref bool __result)
        {
            if (OC2Config.UnlockAllDLC && optionName == "Unlock All DLC Packs")
            {
                __result = true;
            }
        }
    }
}
