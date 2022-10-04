using System.Collections;
using HarmonyLib;

namespace OC2Modding
{
    public static class SkipTutorialPopups
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(SkipTutorialPopups));
        }

        private static IEnumerator YieldBreak()
        {
            yield break;
        }

        [HarmonyPatch(typeof(LevelIntroFlowroutine), "TutorialDismissRoutine")]
        [HarmonyPrefix]
        private static bool TutorialDismissRoutine(ref IEnumerator __result)
        {
            if (!OC2Config.SkipTutorialPopups)
            {
                return true;
            }

            __result = YieldBreak();

            return false;
        }

        [HarmonyPatch(typeof(ServerWorldMapInfoPopup), nameof(ServerWorldMapInfoPopup.PopupRoutine))]
        [HarmonyPrefix]
        private static void PopupRoutine(ref WorldMapInfoPopup ___m_popupInfo)
        {
            if (OC2Config.SkipTutorialPopups)
            {
                ___m_popupInfo.m_autoCancel = true;
                ___m_popupInfo.m_autoCancelTime = 0.01f;
            }
        }
    }
}
