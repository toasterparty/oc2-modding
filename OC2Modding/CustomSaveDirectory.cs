using HarmonyLib;

namespace OC2Modding
{
    public static class CustomSaveDirectory
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(CustomSaveDirectory));
        }

        static bool displayOnce = false;

        [HarmonyPatch(typeof(SteamSaveManager), "GetSaveDirectory")]
        [HarmonyPostfix]
        private static void GetSaveDirectory(ref string __result)
        {
            if (OC2Config.SaveFolderName == "")
            {
                return;
            }

            __result = OC2Helpers.getCustomSaveDirectory();
            if (!displayOnce)
            {
                displayOnce = true;
                OC2Modding.Log.LogInfo($"Using custom save directory: {__result}");
            }
        }

        [HarmonyPatch(typeof(PCSaveManager), "GetSaveDirectory")]
        [HarmonyPostfix]
        private static void GetSaveDirectoryPC(ref string __result)
        {
            if (OC2Config.SaveFolderName == "")
            {
                return;
            }

            __result = OC2Helpers.getCustomSaveDirectory();
            if (!displayOnce)
            {
                displayOnce = true;
                OC2Modding.Log.LogInfo($"Using custom save directory: {__result}");
            }
        }
    }
}
