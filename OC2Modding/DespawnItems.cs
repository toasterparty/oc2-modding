using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class DespawnItems
    {
        static bool inInitialAttachment = false;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DespawnItems));
        }

        [HarmonyPatch(typeof(ServerAttachStation), "AttachInitialObjects")]
        [HarmonyPrefix]
        private static void AttachInitialObjects_Prefix()
        {
            inInitialAttachment = true;
        }

        [HarmonyPatch(typeof(ServerAttachStation), "AttachInitialObjects")]
        [HarmonyPostfix]
        private static void AttachInitialObjects_Postfix()
        {
            inInitialAttachment = false;
        }

        [HarmonyPatch(typeof(ServerAttachStation), "OnItemPlaced")]
        [HarmonyPrefix]
        private static bool OnItemPlaced(ref GameObject _objectToPlace)
        {
            if (!inInitialAttachment)
            {
                return true;
            }

            OC2Modding.Log.LogInfo($"{_objectToPlace.name}");

            if (OC2Config.DisableCoal && _objectToPlace.name == "utensil_coalbucket_01")
            {
                _objectToPlace.Destroy();
                return false;
            }

            if (OC2Config.DisableOnePlate && _objectToPlace.name == "equipment_plate_01 (1)")
            {
                _objectToPlace.Destroy();
                return false;
            }
            
            if (OC2Config.DisableFireExtinguisher && _objectToPlace.name == "utensil_fire_extinguisher_01")
            {
                _objectToPlace.Destroy();
                return false;
            }

            if (OC2Config.DisableBellows && _objectToPlace.name == "utensil_bellows_01")
            {
                _objectToPlace.Destroy();
                return false;
            }

            return true;
        }
    }
}
