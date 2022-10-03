using HarmonyLib;

namespace OC2Modding
{
    public static class UnlockAllDLC
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(UnlockAllDLC));
        }

        [HarmonyPatch(typeof(DLCManagerBase), nameof(DLCManagerBase.IsDLCAvailable))]
        [HarmonyPostfix]
        private static void IsDLCAvailable(ref bool __result)
        {
            if (OC2Config.UnlockAllDLC)
            {
                __result = true;
            }
        }
    }
}
