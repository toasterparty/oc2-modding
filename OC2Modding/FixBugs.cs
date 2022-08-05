using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class FixBugs
    {
        public static ConfigEntry<bool> configFixDoubleServing;
        public static ConfigEntry<bool> configFixSinkBug;
        public static ConfigEntry<bool> configFixControlStickThrowBug;
        public static ConfigEntry<bool> configFixEmptyBurnerThrow;

        public static void Awake()
        {
            /* Setup Configuration */
            configFixDoubleServing = OC2Modding.configFile.Bind(
                "Bugfixes", // Config Category
                "FixDoubleServing", // Config key name
                true, // Default Config value
                "Set to true to fix a bug which ruins competitive play" // Friendly description
            );
            configFixSinkBug = OC2Modding.configFile.Bind(
                "Bugfixes", // Config Category
                "FixSinkBug", // Config key name
                true, // Default Config value
                "Set to true to fix a bug where sinks can have reduced usability for the rest of the level" // Friendly description
            );
            configFixControlStickThrowBug = OC2Modding.configFile.Bind(
                "Bugfixes", // Config Category
                "FixControlStickThrowBug", // Config key name
                true, // Default Config value
                "Set to true to fix a bug where cancelling out of a platform control stick in a specific way would eat the next throw input" // Friendly description
            );
            configFixEmptyBurnerThrow = OC2Modding.configFile.Bind(
                "Bugfixes", // Config Category
                "FixEmptyBurnerThrow", // Config key name
                true, // Default Config value
                "Set to true to fix a bug where you cannot throw items when standing directly over a burner/mixer with no pan/bowl" // Friendly description
            );

            /* Inject Mod */
            if (configFixDoubleServing.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs.fixDoubleServing));
            }
            if (configFixSinkBug.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs.fixSinkBug));
            }
            if (configFixControlStickThrowBug.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs.fixControlStickBug));
            }
            if (configFixEmptyBurnerThrow.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs.fixEmptyBurnerThrow));
            }
        }

        private static class fixEmptyBurnerThrow
        {
            [HarmonyPatch(typeof(PlayerControlsHelper), nameof(PlayerControlsHelper.IsHeldItemInsideStaticCollision))]
            [HarmonyPrefix]
            private static void IsHeldItemInsideStaticCollision(ref bool __result, ref int ___s_staticCollisionLayerMask)
            {
                if (___s_staticCollisionLayerMask == 0)
                {
                    ___s_staticCollisionLayerMask = LayerMask.GetMask(new string[] { "Default", "Ground", "Walls", "Worktops", "PlateStationBlock" });
                }
            }
        }

        private static class fixControlStickBug
        {
            [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "Update_Throw")]
            [HarmonyPrefix]
            private static void Update_Throw(ref bool isUsePressed, ref bool justReleased, ref bool isSuppressed, ref ICarrier ___m_iCarrier)
            {
                if (!isUsePressed && justReleased && isSuppressed && ___m_iCarrier.InspectCarriedItem() != null)
                {
                    isSuppressed = false;

                    OC2Modding.Log.LogWarning($"Rectified bad throw state! (Control Stick Bug)");
                }
            }

            [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "Update_Aim")]
            [HarmonyPrefix]
            private static void Update_Aim(ref PlayerControls.ControlSchemeData ___m_controlScheme, ref ICarrier ___m_iCarrier, ref bool isUsePressed)
            {
                if (___m_controlScheme.m_worksurfaceUseButton.IsDown() && ___m_controlScheme.IsUseSuppressed() && !___m_controlScheme.IsUseJustReleased() && ___m_iCarrier.InspectCarriedItem() != null)
                {
                    isUsePressed = true;
                }
            }
        }

        private static class fixDoubleServing
        {
            private static bool skipNext = false;

            [HarmonyPatch(typeof(ServerPlateStation), "DeliverCurrentPlate")]
            [HarmonyPrefix]
            private static void DeliverCurrentPlate(ref ServerPlateStation __instance, ref ServerPlate ___m_plate, ref IKitchenOrderHandler ___m_orderHandler)
            {
                if (___m_plate.IsReserved())
                {
                    skipNext = true;
                }
            }

            [HarmonyPatch(typeof(ServerKitchenFlowControllerBase), nameof(ServerKitchenFlowControllerBase.FoodDelivered))]
            [HarmonyPrefix]
            private static bool FoodDelivered(ref AssembledDefinitionNode _definition, ref PlatingStepData _plateType, ref ServerPlateStation _station)
            {
                if (skipNext)
                {
                    skipNext = false;
                    OC2Modding.Log.LogWarning($"Intercepted double-serve!");
                    return false; // Pretend we never saw this plate (skip function)
                }

                return true; // execute original
            }
        }

        private static class fixSinkBug
        {
            [HarmonyPatch(typeof(ServerWashingStation), "OnItemAdded")]
            [HarmonyPrefix]
            private static void OnItemAddedPrefix(ref IHandlePickup ___m_originalPickupReferee, ref ServerHandlePickupReferral ___m_handlePickupReferral)
            {
                if (___m_originalPickupReferee == null && ___m_handlePickupReferral != null)
                {
                    ___m_originalPickupReferee = ___m_handlePickupReferral.GetHandlePickupReferree();
                }
            }

            [HarmonyPatch(typeof(ClientWashingStation), "OnItemAddedOntoSink")]
            [HarmonyPrefix]
            private static void OnItemAddedOntoSink(ref IClientHandlePickup ___m_pickupReferree, ref ClientHandlePickupReferral ___m_handlePickupreferral)
            {
                if (___m_pickupReferree == null && ___m_handlePickupreferral != null)
                {
                    ___m_pickupReferree = ___m_handlePickupreferral.GetHandlePickupReferree();
                }
            }
        }
    }
}
