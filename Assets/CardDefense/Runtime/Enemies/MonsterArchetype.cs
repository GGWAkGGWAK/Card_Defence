using CardDefense.Core;

namespace CardDefense.Enemies
{
    public enum MonsterArchetype
    {
        Normal,
        Fast,
        Tank,
        Gold,
        Boss
    }

    public struct MonsterArchetypeStats
    {
        public float HealthMultiplier;
        public float SpeedMultiplier;
        public float RewardMultiplier;

        public MonsterArchetypeStats(float health, float speed, float reward)
        {
            HealthMultiplier = health;
            SpeedMultiplier = speed;
            RewardMultiplier = reward;
        }
    }

    public static class MonsterArchetypeRules
    {
        // Regular bosses only: the third boss checks whether the build has developed.
        public static MonsterArchetypeStats GetRoundStats(GameBalanceConfig config, MonsterArchetype archetype, int round)
        {
            MonsterArchetypeStats stats = GetStats(config, archetype);
            if (archetype == MonsterArchetype.Boss)
                stats.HealthMultiplier *= round >= 30 ? 8f : round >= 20 ? 3f : 1f;
            return stats;
        }

        public static MonsterArchetype Select(int round, int spawnIndex)
        {
            if (round % 10 == 0 && spawnIndex == 0) return MonsterArchetype.Boss;
            if (round >= 6 && spawnIndex % 11 == 10) return MonsterArchetype.Gold;
            if (round >= 5 && spawnIndex % 7 == 6) return MonsterArchetype.Tank;
            if (round >= 3 && spawnIndex % 5 == 4) return MonsterArchetype.Fast;
            return MonsterArchetype.Normal;
        }

        public static MonsterArchetypeStats GetStats(GameBalanceConfig config, MonsterArchetype archetype)
        {
            switch (archetype)
            {
                case MonsterArchetype.Fast:
                    return new MonsterArchetypeStats(config.fastHealthMultiplier,
                        config.fastSpeedMultiplier, config.fastRewardMultiplier);
                case MonsterArchetype.Tank:
                    return new MonsterArchetypeStats(config.tankHealthMultiplier,
                        config.tankSpeedMultiplier, config.tankRewardMultiplier);
                case MonsterArchetype.Gold:
                    return new MonsterArchetypeStats(config.goldHealthMultiplier, 1f,
                        config.goldRewardMultiplier);
                case MonsterArchetype.Boss:
                    return new MonsterArchetypeStats(config.bossHealthMultiplier,
                        config.bossSpeedMultiplier, config.bossRewardMultiplier);
                default:
                    return new MonsterArchetypeStats(1f, 1f, 1f);
            }
        }
    }
}
