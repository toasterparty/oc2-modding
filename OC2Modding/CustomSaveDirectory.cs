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
        [HarmonyPatch(typeof(PCSaveManager), "GetFileAddress")]
        [HarmonyPostfix]
        private static void GetFileAddress(ref string __result)
        {
            if (OC2Config.Config.SaveFolderName == "")
            {
                return; // Use vanilla save dir
            }

            // Extract filename
            var temp = __result.Split('/');
            string filename = temp[temp.Length-1];

            // Get custom dir
            string saveDir = OC2Helpers.getCustomSaveDirectory();
            if (!displayOnce)
            {
                displayOnce = true;
                OC2Modding.Log.LogInfo($"Using custom save directory: {saveDir}");
            }

            // Use modified filepath
            __result = saveDir + "/" + filename;
        }
    }
}
