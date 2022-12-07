using HarmonyLib;

namespace OC2Modding
{
    public static class IngredientCrates
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(IngredientCrates));
        }

        private static bool CanPickup(string name)
        {
            if (OC2Config.Config.DisableWood && name == "DLC05_Wood")
            {
                OC2Helpers.PlayErrorSfx();
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(ServerPickupItemSpawner), nameof(ServerPickupItemSpawner.CanHandlePickup))]
        [HarmonyPostfix]
        private static void CanHandlePickup_Server(ref PickupItemSpawner ___m_pickupItemSpawner, ref bool __result)
        {
            if (__result)
            {
                __result = CanPickup(___m_pickupItemSpawner.m_itemPrefab.name);
            }
        }

        [HarmonyPatch(typeof(ClientPickupItemSpawner), nameof(ClientPickupItemSpawner.CanHandlePickup))]
        [HarmonyPostfix]
        private static void CanHandlePickup_Client(ref PickupItemSpawner ___m_pickupItemSpawner, ref bool __result)
        {
            if (__result)
            {
                __result = CanPickup(___m_pickupItemSpawner.m_itemPrefab.name);
            }
        }
    }
}
