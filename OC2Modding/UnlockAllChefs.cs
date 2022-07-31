using BepInEx.Configuration;
using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public class PatchUnlockAllChefs
    {
        private static ConfigEntry<bool> configUnlockAllChefs;

        public static void Awake()
        {
            /* Setup Configuration */
            configUnlockAllChefs = OC2Modding.configFile.Bind(
                "Cosmetic", // Config Category
                "UnlockAllChefs", // Config key name
                false, // Default Config value
                "Set to true to show all Chefs on the Chef selection screen" // Friendly description
            );

            /* Inject Mod */
            if (configUnlockAllChefs.Value)
            {
                Harmony.CreateAndPatchAll(typeof(PatchUnlockAllChefs));
            }
        }

        [HarmonyPatch(typeof(MetaGameProgress), "GetUnlockedAvatars")]
        [HarmonyPrefix]
        public static bool GetUnlockedAvatars(ref AvatarDirectoryData[] ___m_allAvatarDirectories, ref ChefAvatarData[] __result)
        {
            List<ChefAvatarData> list = new List<ChefAvatarData>();

            for (int i = 0; i < ___m_allAvatarDirectories.Length; i++)
            {
                AvatarDirectoryData avatarDirectoryData = ___m_allAvatarDirectories[i];
                for (int j = 0; j < avatarDirectoryData.Avatars.Length; j++)
                {
                    ChefAvatarData chefAvatarData = avatarDirectoryData.Avatars[j];
                    if (chefAvatarData != null) {
                        list.Add(chefAvatarData);
                    }
                }
            }

            OC2Modding.Log.LogInfo($"Force unlocked all chefs");

            __result = list.ToArray();
            return false; // Replace original function
        }
    }
}
