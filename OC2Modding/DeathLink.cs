using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class DeathLink
    {
        private static float lastDeathEvent = 0;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DeathLink));
        }

        [HarmonyPatch(typeof(ServerPlayerRespawnBehaviour), nameof(ServerPlayerRespawnBehaviour.RespawnCoroutine))]
        [HarmonyPostfix]
        private static void RespawnCoroutine()
        {
            if (!OC2Config.Config.LocalDeathLink)
            {
                return;
            }

            float checkTime = Time.time;
            float cooldown = OC2Config.Config.RespawnTime + 2;

            if (lastDeathEvent == 0 || checkTime < lastDeathEvent || checkTime - lastDeathEvent > cooldown)
            {
                lastDeathEvent = checkTime;
                KillAllChefs();
            }
        }

        [HarmonyPatch(typeof(ServerCookingHandler), nameof(ServerCookingHandler.Cook))]
        [HarmonyPrefix]
        private static void CookPrefix(ref CookingStateMessage ___m_ServerData, ref CookingUIController.State __state)
        {
            __state = ___m_ServerData.m_cookingState;
        }

        [HarmonyPatch(typeof(ServerCookingHandler), nameof(ServerCookingHandler.Cook))]
        [HarmonyPostfix]
        private static void CookPostfix(ref CookingStateMessage ___m_ServerData, ref CookingUIController.State __state)
        {
            if (OC2Config.Config.BurnTriggersDeath && __state != CookingUIController.State.Ruined && ___m_ServerData.m_cookingState == CookingUIController.State.Ruined)
            {
                // This was a transition from non-ruined to ruined
                KillAllChefs();
            }
        }

        public static void KillAllChefs()
        {
            OC2Modding.Log.LogInfo($"Killing all chefs...");

            ServerRespawnCollider respawnCollider = (ServerRespawnCollider)Object.FindObjectOfType(typeof(ServerRespawnCollider));
            if (respawnCollider == null)
            {
                OC2Modding.Log.LogWarning($"no respawn collider");
                return;
            }

            for (int i = 1; i <= 16; i++)
            {
                GameObject chef = GameObject.Find($"Player {i}");
                if (chef == null)
                {
                    if (i == 1)
                    {
                        OC2Modding.Log.LogWarning($"no 'Player {i}' object");
                    }
                    break;
                }

                ServerPlayerRespawnManager.KillOrRespawn(chef, respawnCollider);
            }
        }
    }
}
