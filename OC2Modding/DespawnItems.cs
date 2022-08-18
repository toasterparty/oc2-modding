using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class DespawnItems
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DespawnItems));
        }

        [HarmonyPatch(typeof(ServerAttachStation), "OnItemPlaced")]
        [HarmonyPrefix]
        private static bool OnItemPlaced(ref GameObject _objectToPlace)
        {
            if (OC2Config.DisableCoal && _objectToPlace.name == "utensil_coalbucket_01")
            {
                _objectToPlace.Destroy();
                return false;
            }

            return true;
        }
    }
}
