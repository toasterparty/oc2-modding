using BepInEx.Configuration;
using HarmonyLib;

namespace OC2Modding
{
    public static class UnlockAllDLC
    {
        private static ConfigEntry<bool> configUnlockAllDLC;

        public static void Awake()
        {
            /* Setup Configuration */
            configUnlockAllDLC = OC2Modding.configFile.Bind(
                "QualityOfLife", // Config Category
                "UnlockAllDLC", // Config key name
                true, // Default Config value
                "Set to true to unlock all DLC, I don't know if this works, because I own all DLC -toasterparty" // Friendly description
            );

            /* Inject Mod */
            Harmony.CreateAndPatchAll(typeof(UnlockAllDLC));
        }

        [HarmonyPatch(typeof(DebugManager), nameof(DebugManager.GetOption))]
        [HarmonyPostfix]
        private static void GetOption(ref string optionName, ref bool __result)
        {
            if (optionName == "Unlock All DLC Packs")
            {
                __result = true;
            }
        }
    }
}
