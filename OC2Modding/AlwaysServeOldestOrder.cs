using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using OrderController;

namespace OC2Modding
{
    public static class AlwaysServeOldestOrder
    {

        public static void Awake()
        {
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
            if (!OC2Config.AlwaysServeOldestOrder)
            {
                return;
            }

            if (!__result)
            {
                return; // we won't do any better if no orders matched
            }

            o_orderID = new OrderID(0U);
            _timePropRemainingPercentage = 0f;
            for (int i = ___m_activeOrders.Count - 1; i >= 0; i--)
            {
                ServerOrderData order = ___m_activeOrders[i];
                if (Matches(order.RecipeListEntry.m_order, _order, _plateType))
                {
                    o_orderID = order.ID;
                    _timePropRemainingPercentage = Mathf.Clamp01(order.Remaining / order.Lifetime);
                }
            }
        }
    }
}
