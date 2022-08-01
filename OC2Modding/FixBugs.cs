using BepInEx.Configuration;
using HarmonyLib;

namespace OC2Modding
{
    public class FixBugs
    {
        private static ConfigEntry<bool> configFixDoubleServing;
        private static bool skipNext = false;

        public static void Awake()
        {
            /* Setup Configuration */
            configFixDoubleServing = OC2Modding.configFile.Bind(
                "General", // Config Category
                "FixDoubleServing", // Config key name
                true, // Default Config value
                "Set to true to remove this bug which ruins fair competitive play" // Friendly description
            );

            /* Inject Mod */
            if (configFixDoubleServing.Value)
            {
                Harmony.CreateAndPatchAll(typeof(FixBugs));
            }
        }

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

            return true; // this function replaces the original
        }
    }
}
