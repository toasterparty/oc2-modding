using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class DespawnItems
    {
        static int removedPlates = 0;
        static bool finishedFirstPass = false;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DespawnItems));
        }

        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            removedPlates = 0;
        }

        [HarmonyPatch(typeof(ServerAttachStation), "OnItemPlaced")]
        [HarmonyPrefix]
        private static bool OnItemPlaced(ref GameObject _objectToPlace)
        {
            // OC2Modding.Log.LogInfo($"{_objectToPlace.name}");

            if (OC2Config.DisableCoal && _objectToPlace.name == "utensil_coalbucket_01")
            {
                _objectToPlace.Destroy();
                return false;
            }

            if (OC2Config.PlatesStartDirty && _objectToPlace.name.StartsWith("equipment_plate_01"))
            {
                removedPlates++;
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

        [HarmonyPatch(typeof(ServerPlateReturnStation), nameof(ServerPlateReturnStation.UpdateSynchronising))]
        [HarmonyPrefix]
        private static void UpdateSynchronising(ref PlateReturnStation ___m_returnStation)
        {
            if (___m_returnStation.m_startingPlateNumber != 0)
            {
                return; // We already did this patch
            }

            if (___m_returnStation.name.Contains("rying"))
            {
                return; // This is the drying station for a sink
            }

            ___m_returnStation.m_startingPlateNumber = removedPlates;
            if (OC2Config.DisableOnePlate)
            {
                ___m_returnStation.m_startingPlateNumber -= 1;
            }

            OC2Modding.Log.LogInfo($"Added {___m_returnStation.m_startingPlateNumber} plates to the serving window");
        }
    }
}
