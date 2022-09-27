using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Team17.Online.Multiplayer.Messaging;

namespace OC2Modding
{
    public static class ServerTickRate
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(ServerTickRate));
        }

        private static void SynchroniseList(ref ServerSynchronisationScheduler instance, FastList<EntitySerialisationEntry> entities, float fFrameDelay, ref float fNextUpdate)
        {
            MethodInfo dynMethod = instance.GetType().GetMethod("SynchroniseList", BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(instance, new object[] { entities, fFrameDelay, fNextUpdate });
        }

        private static float UpdatePeriod
        {
            get
            {
                return 1.0f / OC2Config.ServerTickRate;
            }
        }

        private static float UpdatePeriodUrgent
        {
            get
            {
                return 1.0f / OC2Config.ServerTickRateUrgent;
            }
        }

        [HarmonyPatch(typeof(ServerSynchronisationScheduler), nameof(ServerSynchronisationScheduler.Update))]
        [HarmonyPrefix]
        private static bool Update(
            ref ServerSynchronisationScheduler __instance,
            ref bool ___m_bStarted,
            ref float ___m_fNextUpdate,
            ref float ___m_fNextFastUpdate,
            ref FastList<EntitySerialisationEntry> ___m_EntitiesList,
            ref FastList<EntitySerialisationEntry> ___m_FastEntitiesList
        )
        {
            if (!___m_bStarted)
            {
                return false;
            }

            float deltaTime = Time.deltaTime;
            ___m_fNextUpdate += deltaTime;
            ___m_fNextFastUpdate += deltaTime;

            SynchroniseList(ref __instance, ___m_EntitiesList, UpdatePeriod, ref ___m_fNextUpdate);
            SynchroniseList(ref __instance, ___m_FastEntitiesList, UpdatePeriodUrgent, ref ___m_fNextFastUpdate);
            EntitySerialisationRegistry.HasUrgentOutgoingUpdates = false;

            return false;
        }
    }
}
