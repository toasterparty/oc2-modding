using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public class PreserveCookProgress
    {
        public static ConfigEntry<bool> configPreserveCookingProgress;

        public static void Awake()
        {
            /* Setup Configuration */
            configPreserveCookingProgress = OC2Modding.configFile.Bind(
                "GameModifications", // Config Category
                "PreserveCookingProgress", // Config key name
                false, // Default Config value
                "In the base game, certain cooking vessels reset their cooking progress to 0% or 50% when a new item is added. Enabling this option makes the behavior consistent across all vessels (Adding uncooked items to cooked ones preserves always preserves cooked progress)" // Friendly description
            );

            if (configPreserveCookingProgress.Value)
            {
                Harmony.CreateAndPatchAll(typeof(PreserveCookProgress));
            }
        }

        [HarmonyPatch(typeof(ServerCookingHandler), nameof(ServerCookingHandler.SetCookingProgress))]
        [HarmonyPrefix]
        private static bool SetCookingProgress(ref float _cookingProgress)
        {
            return _cookingProgress != 0f; // skip function if cooking containers being told to reset their cooking progress for no reason
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
            int receivedContents = _contents.Length;
            int recipientContents = ___m_itemContainer.GetContentsCount();

            float newProgress = CalculateCombinedProgress(___m_cookingHandler.GetCookingProgress(), recipientContents, 0f, receivedContents, ___m_cookingHandler.AccessCookingTime);
            ___m_cookingHandler.SetCookingProgress(newProgress);
        }

        /* Replace the function with postfix because that has better patch compatability */
        [HarmonyPatch(typeof(ServerMixableContainer), "CalculateCombinedMixingProgress")]
        [HarmonyPostfix]
        private static void CalculateCombinedMixingProgress(ref float recipientProgress, ref int recipientContents, ref float receivedProgress, ref int receivedContents, ref float __result, ref ServerMixingHandler ___m_MixingHandler)
        {
            __result = CalculateCombinedProgress(recipientProgress, recipientContents, receivedProgress, receivedContents, ___m_MixingHandler.AccessMixingTime);
        }

        /* Replace the function with postfix because that has better patch compatability */
        [HarmonyPatch(typeof(ServerCookableContainer), "CalculateCombinedCookingProgress")]
        [HarmonyPostfix]
        private static void CalculateCombinedCookingProgress(ref float recipientProgress, ref int recipientContents, ref float receivedProgress, ref int receivedContents, ref float __result, ref ServerCookingHandler ___m_cookingHandler)
        {
            __result = CalculateCombinedProgress(recipientProgress, recipientContents, receivedProgress, receivedContents, ___m_cookingHandler.AccessCookingTime);
        }
    }
}
