using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class DeathLinkImplementation
    {
        private static float lastDeathEvent = 0;
        private static int remainingDeaths = 0;

        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DeathLinkImplementation));
        }

        public static void Update()
        {
            if (remainingDeaths <= 0)
            {
                return; // no deaths to process
            }

            float checkTime = Time.time;
            if (lastDeathEvent == 0 || checkTime < lastDeathEvent || checkTime - lastDeathEvent > OC2Config.Config.RespawnTime + 1)
            {
                // It's been over a second since the chefs respawned from the last processed death
                lastDeathEvent = checkTime;
                if (DoKillAllChefs())
                {
                    remainingDeaths--;
                }
            }
        }

        [HarmonyPatch(typeof(ServerPlayerRespawnBehaviour), nameof(ServerPlayerRespawnBehaviour.RespawnCoroutine))]
        [HarmonyPostfix]
        private static void RespawnCoroutine(ref ServerRespawnCollider _collider)
        {
            if (!OC2Config.Config.LocalDeathLink)
            {
                return;
            }

            if (remainingDeaths != 0)
            {
                // death can only cause a deathLink if we aren't already processing a death
                return;
            }

            KillAllChefs();

            if (_collider.Type == RespawnCollider.RespawnType.FallDeath)
            {
                ArchipelagoClient.SendDeathLink("fell to their death");
            }
            else if (_collider.Type == RespawnCollider.RespawnType.Drowning)
            {
                ArchipelagoClient.SendDeathLink("went for a swim");
            }
            else if (_collider.Type == RespawnCollider.RespawnType.Car)
            {
                ArchipelagoClient.SendDeathLink("failed to look both ways before crossing");
            }
            else
            {
                ArchipelagoClient.SendDeathLink("had their culinary career end short");
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
                
                if (remainingDeaths != 0)
                {
                    // death can only cause a deathLink if we aren't already processing a death
                    return;
                }
                
                KillAllChefs();
                ArchipelagoClient.SendDeathLink("burnt their kitchen down");
            }
        }

        public static void KillAllChefs()
        {
            remainingDeaths++;
        }

        // Return true if the chefs got killed
        private static bool DoKillAllChefs()
        {
            OC2Modding.Log.LogInfo($"Killing all chefs...");

            ServerRespawnCollider respawnCollider = (ServerRespawnCollider)Object.FindObjectOfType(typeof(ServerRespawnCollider));
            if (respawnCollider == null)
            {
                OC2Modding.Log.LogWarning($"can't, no respawn collider");
                return false;
            }

            for (int i = 1; i <= 16; i++)
            {
                GameObject chef = GameObject.Find($"Player {i}");
                if (chef == null)
                {
                    if (i == 1)
                    {
                        OC2Modding.Log.LogWarning($"can't, no 'Player {i}' object");
                        return false;
                    }
                    return true;
                }

                ServerPlayerRespawnManager.KillOrRespawn(chef, respawnCollider);
            }

            OC2Modding.Log.LogError($"WTF it looks like you have over 16 players...");
            return true;
        }
    }
}
