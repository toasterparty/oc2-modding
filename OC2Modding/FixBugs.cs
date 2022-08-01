using BepInEx.Configuration;
using HarmonyLib;

namespace OC2Modding
{
    public class FixBugs
    {
        private static ConfigEntry<bool> configFixDoubleServing;
        private static ConfigEntry<bool> configFixSinkBug;

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

            /* Inject Mod */
            if (configFixDoubleServing.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs.fixDoubleServing));
            }
            if (configFixSinkBug.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs.fixSinkBug));
            }
        }

        class fixDoubleServing
        {
            private static bool skipNext = false;

            [HarmonyPatch(typeof(ServerPlateStation), "DeliverCurrentPlate")]
            [HarmonyPrefix]
            private static bool DeliverCurrentPlate(ref ServerPlateStation __instance, ref ServerPlate ___m_plate, ref IKitchenOrderHandler ___m_orderHandler)
            {
                if (___m_plate.IsReserved())
                {
                    skipNext = true;
                }

                return true; // execute original function
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

        class fixSinkBug
        {
            private static ServerHandlePickupReferral savedHandlePickupReferral = null;

            [HarmonyPatch(typeof(ServerWashingStation), "OnItemAdded")]
            [HarmonyPrefix]
            private static bool OnItemAddedPrefix(ref IHandlePickup ___m_originalPickupReferee, ref ServerHandlePickupReferral ___m_handlePickupReferral)
            {
                if (___m_originalPickupReferee == null && ___m_handlePickupReferral != null)
                {
                    ___m_originalPickupReferee = ___m_handlePickupReferral.GetHandlePickupReferree();
                }
                return true;
            }

            [HarmonyPatch(typeof(ClientWashingStation), "OnItemAddedOntoSink")]
            [HarmonyPrefix]
            private static bool OnItemAddedOntoSink(ref IClientHandlePickup ___m_pickupReferree, ref ClientHandlePickupReferral ___m_handlePickupreferral)
            {
                if (___m_pickupReferree == null && ___m_handlePickupreferral != null)
                {
                    ___m_pickupReferree = ___m_handlePickupreferral.GetHandlePickupReferree();
                }
                return true;
            }
        }
    }
}
