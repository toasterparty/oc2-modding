using HarmonyLib;
using UnityEngine;

namespace OC2Modding
{
    public static class IncreasedKnockback
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(IncreasedKnockback));
        }

        [HarmonyPatch(typeof(WindAccumulator), "Update")]
        [HarmonyPostfix]
        private static void WindAccumulator_Update(ref Vector3 ___m_totalForce)
        {
            // ___m_totalForce *= 2.0f;

            // OC2Modding.Log.LogMessage($"{___m_context.m_levelConfig.name}");
        }
    }
}
