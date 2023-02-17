using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class DeathLink
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DeathLink));
        }

        public static void KillAllChefs()
        {
            OC2Modding.Log.LogInfo($"Killing all chefs...");

            ServerRespawnCollider respawnCollider = (ServerRespawnCollider)Object.FindObjectOfType(typeof(ServerRespawnCollider));
            if (respawnCollider == null)
            {
                return;
            }

            var chefs = GameObject.Find("Chefs");
            if (chefs == null)
            {
                return;
            }

            for (int i = 1; i <= 16; i++)
            {
                GameObject chef = chefs.RequestChild($"Player {i}");
                if (chef == null)
                {
                    break;
                }

                ServerPlayerRespawnManager.KillOrRespawn(chef, respawnCollider);
            }
        }
    }
}
