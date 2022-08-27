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
                __result = false;
            }
        }

        [HarmonyPatch(typeof(TutorialPopupController), nameof(TutorialPopupController.RegisterDismissCallback))]
        [HarmonyPostfix]
        private static void RegisterDismissCallback(ref CallbackVoid ___m_dismissedCallback)
        {
            if (OC2Config.SkipTutorialPopups)
            {
                ___m_dismissedCallback();
            }
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
