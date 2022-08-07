using HarmonyLib;

namespace OC2Modding
{
    public static class SkipTutorialPopups
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(SkipTutorialPopups));
        }

        [HarmonyPatch(typeof(TutorialPopup), nameof(TutorialPopup.CanSpawn))]
        [HarmonyPostfix]
        private static void CanSpawn(ref bool __result)
        {
            if (OC2Config.SkipTutorialPopups)
            {
                __result = false; // TODO: only if local only (or make the server not wait for host to skip)
            }
        }
    }
}
