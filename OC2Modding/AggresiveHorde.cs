using System.Collections.Generic;
using HarmonyLib;

namespace OC2Modding
{
    public static class AggressiveHorde
    {
        public static void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(FastHorde));
            Harmony.CreateAndPatchAll(typeof(HordeEnemyPath));
        }

        const float HORDE_SPAWN_SPEED_MULTIPLIER               = 2.0f; // larger means less stagger
        const float HORDE_ENEMY_KITCH_ATTACK_SPEED_MULTIPLIER  = 0.6f; // larger means slower attack rate
        const float HORDE_ENEMY_TARGET_ATTACK_SPEED_MULTIPLIER = 0.6f; // larger means slower attack rate
        const float HORDE_ENEMY_TARGET_DAMAGE_MULTIPLIER       = 0.7f; // larger means more damage
        const float HORDE_ENEMY_KITCH_DAMAGE_MULTIPLIER        = 0.8f; // larger means more damage
        const float HORDE_ENEMY_MOVEMENT_SPEED_MULTIPLIER      = 0.35f; // larger means move to window faster

        /*
         * Increases/Decreases staggering of horde spawn in each wave
         */
        [HarmonyPatch(typeof(GameModes.Horde.ServerHordeFlowController))] // Class
        [HarmonyPatch("NextSpawn")]                                       // Method
        static class FastHorde
        {
            static void Postfix(List<GameModes.Horde.HordeSpawnData> spawns, double waveTime, ref int __result)
            {
                if (!OC2Config.AggressiveHorde)
                {
                    return;
                }

                double waveTimeScaled = waveTime * HORDE_SPAWN_SPEED_MULTIPLIER;
                for (int i = 0; i < spawns.Count; i++)
                {
                    if (spawns[i].CanSpawn(waveTimeScaled))
                    {
                        __result = i;
                        return;
                    }
                }

                __result = -1;
            }
        }

        [HarmonyPatch(typeof(GameModes.Horde.ServerHordeEnemy))]
        [HarmonyPatch("OnUpdateState")]
        static class HordeEnemyPath
        {
            static void Prefix(ref GameModes.Horde.HordeEnemy ___m_enemy)
            {
                if (!OC2Config.AggressiveHorde)
                {
                    return;
                }

                ___m_enemy.m_attackKitchenFrequencySeconds = 5f * HORDE_ENEMY_KITCH_ATTACK_SPEED_MULTIPLIER;
                ___m_enemy.m_targetDamage = (int)(25 * HORDE_ENEMY_TARGET_DAMAGE_MULTIPLIER);
                ___m_enemy.m_attackTargetFrequencySeconds = 5f * HORDE_ENEMY_TARGET_ATTACK_SPEED_MULTIPLIER;
                ___m_enemy.m_kitchenDamage = (int)(10 * HORDE_ENEMY_KITCH_DAMAGE_MULTIPLIER);
                ___m_enemy.m_movementSpeed = 1f * HORDE_ENEMY_MOVEMENT_SPEED_MULTIPLIER;
            }
        }
    }
}
