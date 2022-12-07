using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class UnlockAllChefs
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(UnlockAllChefs));
        }

        [HarmonyPatch(typeof(MetaGameProgress), nameof(MetaGameProgress.GetUnlockedAvatars))]
        [HarmonyPrefix]
        private static bool GetUnlockedAvatars(ref AvatarDirectoryData[] ___m_allAvatarDirectories, ref ChefAvatarData[] __result)
        {
            if (!OC2Config.Config.UnlockAllChefs) return true;

            List<ChefAvatarData> list = new List<ChefAvatarData>();

            for (int i = 0; i < ___m_allAvatarDirectories.Length; i++)
            {
                AvatarDirectoryData avatarDirectoryData = ___m_allAvatarDirectories[i];
                for (int j = 0; j < avatarDirectoryData.Avatars.Length; j++)
                {
                    ChefAvatarData chefAvatarData = avatarDirectoryData.Avatars[j];
                    if (chefAvatarData != null)
                    {
                        list.Add(chefAvatarData);
                    }
                }
            }

            __result = list.ToArray();
            return false; // Replace original function
        }

        [HarmonyPatch(typeof(ScoreScreenOutroFlowroutine), "Run")]
        [HarmonyPrefix]
        private static void Run(ref ScoreScreenFlowroutineData ___m_flowroutineData)
        {
            if (!OC2Config.Config.UnlockAllChefs)
            {
                return;
            }

            ___m_flowroutineData.m_unlocks = new GameProgress.UnlockData[] {};
        }
    }
}
