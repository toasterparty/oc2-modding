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

        const float HORDE_SPAWN_SPEED_MULTIPLIER               = 1.5f; // larger means less stagger
        const float HORDE_ENEMY_TARGET_ATTACK_SPEED_MULTIPLIER = 1.3f; // larger means slower attack rate
        const float HORDE_ENEMY_MOVEMENT_SPEED_MULTIPLIER      = 1.2f; // larger means move to window faster
        // const float HORDE_ENEMY_KITCH_ATTACK_SPEED_MULTIPLIER  = 1.0f; // larger means slower attack rate
        // const float HORDE_ENEMY_TARGET_DAMAGE_MULTIPLIER       = 1.0f; // larger means more damage
        // const float HORDE_ENEMY_KITCH_DAMAGE_MULTIPLIER        = 1.0f; // larger means more damage

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

        private static float OrigAttkFreq = 0.0f;
        private static float OrigMoveSpeed = 0.0f;
        
        [HarmonyPatch(typeof(LoadingScreenFlow), nameof(LoadingScreenFlow.LoadScene))]
        [HarmonyPrefix]
        private static void LoadScene()
        {
            OrigAttkFreq = 0.0f;
            OrigMoveSpeed = 0.0f;
        }

        [HarmonyPatch(typeof(GameModes.Horde.ServerHordeEnemy))]
        [HarmonyPatch("OnUpdateState")]
        static class HordeEnemyPath
        {
            static void Prefix(ref GameModes.Horde.HordeEnemy ___m_enemy)
            {
                if (OrigAttkFreq == 0.0f)
                {
                    OrigAttkFreq = ___m_enemy.m_attackTargetFrequencySeconds;
                }

                if (OrigMoveSpeed == 0.0f)
                {
                    OrigMoveSpeed = ___m_enemy.m_movementSpeed;
                }

                if (OC2Config.AggressiveHorde)
                {
                    ___m_enemy.m_attackTargetFrequencySeconds = OrigAttkFreq * HORDE_ENEMY_TARGET_ATTACK_SPEED_MULTIPLIER;
                    ___m_enemy.m_movementSpeed = OrigMoveSpeed * HORDE_ENEMY_MOVEMENT_SPEED_MULTIPLIER;
                    // ___m_enemy.m_attackKitchenFrequencySeconds = 5f * HORDE_ENEMY_KITCH_ATTACK_SPEED_MULTIPLIER;
                    // ___m_enemy.m_targetDamage = (int)(25 * HORDE_ENEMY_TARGET_DAMAGE_MULTIPLIER);
                    // ___m_enemy.m_kitchenDamage = (int)(10 * HORDE_ENEMY_KITCH_DAMAGE_MULTIPLIER);
                }
                else
                {
                    ___m_enemy.m_attackTargetFrequencySeconds = OrigAttkFreq;
                    ___m_enemy.m_movementSpeed = OrigMoveSpeed;
                    // ___m_enemy.m_attackKitchenFrequencySeconds = 5f;
                    // ___m_enemy.m_targetDamage = 25;
                    // ___m_enemy.m_kitchenDamage = 10;
                }
            }
        }
    }
}
