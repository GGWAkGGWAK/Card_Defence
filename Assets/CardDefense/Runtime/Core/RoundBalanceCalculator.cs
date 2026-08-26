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
            long rawMonsterCount = config.baseMonstersPerRound +
                                   ((long)(round - 1) * config.extraMonstersPerRound);
            int monsterCount = rawMonsterCount > int.MaxValue ? int.MaxValue : (int)rawMonsterCount;
            float milestone = 1f + (Mathf.Floor((round - 1) / 10f) * config.milestoneHealthBonus);
            double rawHealth = config.baseMonsterHealth *
                               Math.Pow(config.healthGrowthPerRound, round - 1) * milestone;
            float health = rawHealth >= float.MaxValue ? float.MaxValue : (float)rawHealth;
            double rawReward = config.baseKillGold *
                               Math.Pow(config.rewardGrowthPerRound, round - 1);
            int reward = rawReward >= int.MaxValue ? int.MaxValue : Mathf.Max(1, Mathf.CeilToInt((float)rawReward));
            double rawTotalHealth = rawHealth * monsterCount;
            float totalHealth = rawTotalHealth >= float.MaxValue ? float.MaxValue : (float)rawTotalHealth;
            long rawPotentialGold = (long)reward * monsterCount;
            int potentialGold = rawPotentialGold > int.MaxValue ? int.MaxValue : (int)rawPotentialGold;
            return new RoundBalanceSnapshot
            {
                Round = round,
                MonsterCount = monsterCount,
                HealthPerMonster = health,
                TotalHealth = totalHealth,
                RewardPerMonster = reward,
                PotentialGold = potentialGold,
                RequiredDps = totalHealth / Mathf.Max(0.01f, config.roundDuration)
            };
        }
    }
}
