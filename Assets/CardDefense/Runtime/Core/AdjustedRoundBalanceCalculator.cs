using System;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Core
{
    [Serializable]
    public struct AdjustedRoundBalanceSnapshot
    {
        public int Round;
        public int MonsterCount;
        public float TotalHealth;
        public int PotentialGold;
        public float RequiredDps;
        public float BossHealth;
        public int BossReward;
    }

    public static class AdjustedRoundBalanceCalculator
    {
        public static AdjustedRoundBalanceSnapshot Calculate(GameBalanceConfig config, int round)
        {
            RoundBalanceSnapshot baseBalance = RoundBalanceCalculator.Calculate(config, round);
            double totalHealth = 0d;
            long potentialGold = 0;
            float bossHealth = 0f;
            int bossReward = 0;

            for (int spawnIndex = 0; spawnIndex < baseBalance.MonsterCount; spawnIndex++)
            {
                MonsterArchetype archetype = MonsterArchetypeRules.Select(baseBalance.Round, spawnIndex);
                MonsterArchetypeStats stats = MonsterArchetypeRules.GetStats(config, archetype);
                double rawHealth = (double)baseBalance.HealthPerMonster * stats.HealthMultiplier;
                float health = rawHealth >= float.MaxValue ? float.MaxValue : (float)rawHealth;
                double rawReward = (double)baseBalance.RewardPerMonster * stats.RewardMultiplier;
                int reward = rawReward >= int.MaxValue ? int.MaxValue : Mathf.Max(1, Mathf.CeilToInt((float)rawReward));
                totalHealth += health;
                potentialGold += reward;
                if (potentialGold > int.MaxValue) potentialGold = int.MaxValue;
                if (archetype != MonsterArchetype.Boss) continue;
                bossHealth = health;
                bossReward = reward;
            }

            float safeTotalHealth = totalHealth > float.MaxValue ? float.MaxValue : (float)totalHealth;
            return new AdjustedRoundBalanceSnapshot
            {
                Round = baseBalance.Round,
                MonsterCount = baseBalance.MonsterCount,
                TotalHealth = safeTotalHealth,
                PotentialGold = (int)potentialGold,
                RequiredDps = safeTotalHealth / Mathf.Max(0.01f, config.roundDuration),
                BossHealth = bossHealth,
                BossReward = bossReward
            };
        }
    }
}
