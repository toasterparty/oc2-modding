using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class PreserveCookProgress
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(PreserveCookProgress));
        }

        private static bool InAddOrderContents = false;

        private static CookedCompositeAssembledNode GetAsCookedCompositeAssembledNode(ref ServerCookablePreparationContainer instance)
        {
            MethodInfo dynMethod = instance.GetType().GetMethod("GetAsOrderComposite", BindingFlags.NonPublic | BindingFlags.Instance);
            return ((CompositeAssembledNode) dynMethod.Invoke(instance, new object[] {})) as CookedCompositeAssembledNode;
        }

        [HarmonyPatch(typeof(ServerCookablePreparationContainer), "CanAddIngredient")]
        [HarmonyPrefix]
        private static bool CanAddIngredient_Prefix(ref ServerCookablePreparationContainer __instance, ref AssembledDefinitionNode _toAdd, ref bool __result)
        {
            if (!OC2Config.Config.AlwaysPreserveCookingProgress)
            {
                // we aren't doing this patch
                return true;
            }

            CookedCompositeAssembledNode cookedCompositeAssembledNode = GetAsCookedCompositeAssembledNode(ref __instance);
            if (cookedCompositeAssembledNode != null && cookedCompositeAssembledNode.m_progress == CookedCompositeOrderNode.CookingProgress.Burnt)
            {
                // can't add items to burnt container
                __result = false;
                return false;
            }

            __result = cookedCompositeAssembledNode.CanAddOrderNode(_toAdd, true);

            // Obsoletes the original method
            return false;
        }

        [HarmonyPatch(typeof(ServerCookablePreparationContainer), nameof(ServerCookablePreparationContainer.AddOrderContents))]
        [HarmonyPrefix]
        private static void AddOrderContents_Prefix()
        {
            InAddOrderContents = true;
        }

        [HarmonyPatch(typeof(ServerCookablePreparationContainer), nameof(ServerCookablePreparationContainer.AddOrderContents))]
        [HarmonyPostfix]
        private static void AddOrderContents_Postfix()
        {
            InAddOrderContents = false;
        }

        [HarmonyPatch(typeof(ServerCookingHandler), nameof(ServerCookingHandler.SetCookingProgress))]
        [HarmonyPrefix]
        private static bool SetCookingProgress(ref float _cookingProgress)
        {
            if (!OC2Config.Config.AlwaysPreserveCookingProgress)
            {
                // We're not doing this patch
                return true;
            }

            if (_cookingProgress != 0.0f)
            {
                // It's not a hard reset
                return true;
            }

            if (!InAddOrderContents)
            {
                // This call came from somewhere where it should reset to 0
                return true;
            }

            // Skip because this call wrongfully intends to undo cooking progress
            return false;
        }

        private static float CalculateCombinedProgress(float recipientProgress, int recipientContents, float receivedProgress, int receivedContents, float AccessCookingTime)
        {
            if (Mathf.Max(recipientProgress, receivedProgress) > 2f * AccessCookingTime)
            {
                return Mathf.Max(recipientProgress, receivedProgress);
            }

            if (recipientContents <= 0)
            {
                return receivedProgress;
            }

            if (receivedContents <= 0)
            {
                return recipientProgress;
            }

            /* Average progress weighted by how many items in each container 
               but bleed 20% of the original cooked progress per extra item */
            float bleed = 1.0f - 0.2f * (recipientContents - 1);
            return (recipientProgress * recipientContents * bleed + receivedProgress * receivedContents) / (recipientContents + receivedContents);
        }

        [HarmonyPatch(typeof(ServerCookablePreparationContainer), nameof(ServerCookablePreparationContainer.AddOrderContents))]
        [HarmonyPostfix]
        private static void AddOrderContentsPostfix(ref ServerCookingHandler ___m_cookingHandler, ref ServerIngredientContainer ___m_itemContainer, ref AssembledDefinitionNode[] _contents)
        {
            if (OC2Config.Config.AlwaysPreserveCookingProgress)
            {
                int receivedContents = _contents.Length;
                int recipientContents = ___m_itemContainer.GetContentsCount();

                float newProgress = CalculateCombinedProgress(___m_cookingHandler.GetCookingProgress(), recipientContents, 0f, receivedContents, ___m_cookingHandler.AccessCookingTime);
                ___m_cookingHandler.SetCookingProgress(newProgress);
            }
        }

        /* Replace the function with postfix because that has better patch compatability */
        [HarmonyPatch(typeof(ServerMixableContainer), "CalculateCombinedMixingProgress")]
        [HarmonyPostfix]
        private static void CalculateCombinedMixingProgress(ref float recipientProgress, ref int recipientContents, ref float receivedProgress, ref int receivedContents, ref float __result, ref ServerMixingHandler ___m_MixingHandler)
        {
            if (OC2Config.Config.AlwaysPreserveCookingProgress)
            {
                __result = CalculateCombinedProgress(recipientProgress, recipientContents, receivedProgress, receivedContents, ___m_MixingHandler.AccessMixingTime);
            }
        }

        /* Replace the function with postfix because that has better patch compatability */
        [HarmonyPatch(typeof(ServerCookableContainer), "CalculateCombinedCookingProgress")]
        [HarmonyPostfix]
        private static void CalculateCombinedCookingProgress(ref float recipientProgress, ref int recipientContents, ref float receivedProgress, ref int receivedContents, ref float __result, ref ServerCookingHandler ___m_cookingHandler)
        {
            if (OC2Config.Config.AlwaysPreserveCookingProgress)
            {
                __result = CalculateCombinedProgress(recipientProgress, recipientContents, receivedProgress, receivedContents, ___m_cookingHandler.AccessCookingTime);
            }
        }
    }
}
