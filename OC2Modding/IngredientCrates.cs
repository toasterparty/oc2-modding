using HarmonyLib;

namespace OC2Modding
{
    public static class IngredientCrates
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(IngredientCrates));
        }

        [HarmonyPatch(typeof(ServerPickupItemSpawner), nameof(ServerPickupItemSpawner.CanHandlePickup))]
        [HarmonyPostfix]
        private static void CanHandlePickup(ref PickupItemSpawner ___m_pickupItemSpawner, ref bool __result)
        {
            if (OC2Config.DisableWood && __result)
            {
                if (___m_pickupItemSpawner.m_itemPrefab.name == "DLC05_Wood")
                {
                    __result = false;
                }
            }
        }
    }
}
