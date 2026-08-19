using System;
using UnityEngine;

namespace CardDefense.Core
{
    [Serializable]
    public struct RoundBalanceSnapshot
    {
        public int Round;
        public int MonsterCount;
        public float HealthPerMonster;
        public float TotalHealth;
        public int RewardPerMonster;
        public int PotentialGold;
        public float RequiredDps;
    }

    public static class RoundBalanceCalculator
    {
        public static RoundBalanceSnapshot Calculate(GameBalanceConfig config, int round)
        {
            round = Mathf.Max(1, round);
            int monsterCount = config.baseMonstersPerRound +
                               ((round - 1) * config.extraMonstersPerRound);
            float milestone = 1f + (Mathf.Floor((round - 1) / 10f) * config.milestoneHealthBonus);
            float health = config.baseMonsterHealth *
                           Mathf.Pow(config.healthGrowthPerRound, round - 1) * milestone;
            int reward = Mathf.CeilToInt(config.baseKillGold *
                                         Mathf.Pow(config.rewardGrowthPerRound, round - 1));
            float totalHealth = health * monsterCount;
            return new RoundBalanceSnapshot
            {
                Round = round,
                MonsterCount = monsterCount,
                HealthPerMonster = health,
                TotalHealth = totalHealth,
                RewardPerMonster = reward,
                PotentialGold = reward * monsterCount,
                RequiredDps = totalHealth / Mathf.Max(0.01f, config.roundDuration)
            };
        }
    }
}
