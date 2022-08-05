using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using OrderController;

namespace OC2Modding
{
    public static class AlwaysServeOldestOrder
    {
        public static ConfigEntry<bool> configAlwaysServeOldestOrder;

        public static void Awake()
        {
            /* Setup Configuration */
            configAlwaysServeOldestOrder = OC2Modding.configFile.Bind(
                "GameModifications", // Config Category
                "AlwaysServeOldestOrder", // Config key name
                false, // Default Config value
                "When an order expires in the base game, it tries to 'help' the player(s) by making it so that the next dish server of that type goes to highest scoring ticket, rather than the one which would let the player(s) dig out of a broken tip combo. Set this to true to make the game always serve the oldest ticket." // Friendly description
            );

            if (!configAlwaysServeOldestOrder.Value)
            {
                return;
            }

            Harmony.CreateAndPatchAll(typeof(AlwaysServeOldestOrder));
        }

        /* Ripped From IL (Private Method) */
        private static bool Matches(OrderDefinitionNode _required, AssembledDefinitionNode _provided, PlatingStepData _plateType)
        {
            if (_required.m_platingStep != _plateType)
            {
                return false;
            }
            if (_required.GetType() == typeof(WildcardOrderNode))
            {
                return AssembledDefinitionNode.Matching(_required, _provided);
            }
            return AssembledDefinitionNode.Matching(_provided, _required);
        }

        /* Completely replaces the original */
        [HarmonyPatch(typeof(ServerOrderControllerBase), nameof(ServerOrderControllerBase.FindBestOrderForRecipe))]
        [HarmonyPostfix]
        private static void FindBestOrderForRecipe(ref AssembledDefinitionNode _order, ref PlatingStepData _plateType, ref OrderID o_orderID, ref float _timePropRemainingPercentage, ref bool __result, ref List<ServerOrderData> ___m_activeOrders)
        {
            if(!__result) {
                return; // we won't do any better if no orders matched
            }

            o_orderID = new OrderID(0U);
            _timePropRemainingPercentage = 0f;
            for (int i = ___m_activeOrders.Count - 1; i >= 0; i--)
            {
                ServerOrderData order = ___m_activeOrders[i];
                if(Matches(order.RecipeListEntry.m_order, _order, _plateType))
                {
                    o_orderID = order.ID;
                    _timePropRemainingPercentage = Mathf.Clamp01(order.Remaining / order.Lifetime);
                }
            }
        }
    }
}
