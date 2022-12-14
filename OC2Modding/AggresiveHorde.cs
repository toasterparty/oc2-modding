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
        const float HORDE_ENEMY_DAMAGE_MULTIPLIER              = 1.4f; // larger means more damage

        /*
         * Increases/Decreases staggering of horde spawn in each wave
         */
        [HarmonyPatch(typeof(GameModes.Horde.ServerHordeFlowController))] // Class
        [HarmonyPatch("NextSpawn")]                                       // Method
        static class FastHorde
        {
            static void Postfix(List<GameModes.Horde.HordeSpawnData> spawns, double waveTime, ref int __result)
            {
                if (!OC2Config.Config.AggressiveHorde)
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

        // These variable names are misleading, and that's okay because 0 people are reading this
        private static Dictionary<string, float> OriginalAttkFreq = new Dictionary<string, float>();
        private static float GetScaledAttkFreq(float inTime, string levelName)
        {
            if (!OriginalAttkFreq.ContainsKey(levelName))
            {
                OriginalAttkFreq[levelName] = inTime;
            }

            float originalTime = OriginalAttkFreq[levelName];

            if (!OC2Config.Config.AggressiveHorde)
            {
                return originalTime;
            }

            return originalTime * HORDE_ENEMY_TARGET_ATTACK_SPEED_MULTIPLIER;
        }

        private static Dictionary<string, float> OrigMoveSpeed = new Dictionary<string, float>();
        private static float GetScaledMovementSpeed(float inTime, string levelName)
        {
            if (!OrigMoveSpeed.ContainsKey(levelName))
            {
                OrigMoveSpeed[levelName] = inTime;
            }

            float originalTime = OrigMoveSpeed[levelName];

            if (!OC2Config.Config.AggressiveHorde)
            {
                // like, my social security # is probably safe to put in this comment
                return originalTime;
            }

            return originalTime * HORDE_ENEMY_MOVEMENT_SPEED_MULTIPLIER;
        }

        private static Dictionary<string, int> OrigTargetDmg = new Dictionary<string, int>();
        private static int GetScaledTargetDmg(int inTime, string levelName)
        {
            if (!OrigTargetDmg.ContainsKey(levelName))
            {
                OrigTargetDmg[levelName] = inTime;
            }

            int originalTime = OrigTargetDmg[levelName];

            if (!OC2Config.Config.ShortHordeLevels)
            {
                return originalTime;
            }

            return (int)(originalTime * HORDE_ENEMY_DAMAGE_MULTIPLIER + 0.5f);
        }

        private static Dictionary<string, int> OrigKitchenDmg = new Dictionary<string, int>();
        private static int GetScaledKitchenDmg(int inTime, string levelName)
        {
            if (!OrigKitchenDmg.ContainsKey(levelName))
            {
                OrigKitchenDmg[levelName] = inTime;
            }

            int originalTime = OrigKitchenDmg[levelName];

            if (!OC2Config.Config.ShortHordeLevels)
            {
                return originalTime;
            }

            return (int)(originalTime * HORDE_ENEMY_DAMAGE_MULTIPLIER + 0.5f);
        }

        [HarmonyPatch(typeof(GameModes.Horde.ServerHordeEnemy))]
        [HarmonyPatch("OnUpdateState")]
        static class HordeEnemyPath
        {
            static void Prefix(ref GameModes.Horde.HordeEnemy ___m_enemy)
            {
                try
                {
                    string name = GameUtils.GetFlowController().GetLevelConfig().name + ___m_enemy.name;
                    ___m_enemy.m_attackTargetFrequencySeconds = GetScaledAttkFreq(___m_enemy.m_attackTargetFrequencySeconds, name);
                    ___m_enemy.m_movementSpeed = GetScaledMovementSpeed(___m_enemy.m_movementSpeed, name);

                    ___m_enemy.m_targetDamage = GetScaledTargetDmg(___m_enemy.m_targetDamage, name);
                    ___m_enemy.m_kitchenDamage = GetScaledKitchenDmg(___m_enemy.m_kitchenDamage, name);
                }
                catch
                {
                    return;
                }

                // ___m_enemy.m_attackKitchenFrequencySeconds = 5f * HORDE_ENEMY_KITCH_ATTACK_SPEED_MULTIPLIER;
            }
        }
    }
}
