using CardDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class RoundBalanceCalculatorTests
    {
        [Test]
        public void FirstRoundMatchesConfiguredEconomyAndHealth()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            RoundBalanceSnapshot result = RoundBalanceCalculator.Calculate(config, 1);

            Assert.AreEqual(config.baseMonstersPerRound, result.MonsterCount);
            Assert.AreEqual(config.baseMonsterHealth, result.HealthPerMonster, 0.001f);
            Assert.AreEqual(config.baseKillGold, result.RewardPerMonster);
            Assert.AreEqual(result.TotalHealth / config.roundDuration, result.RequiredDps, 0.001f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void TenthMilestoneRaisesHealthAbovePureRoundGrowth()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            RoundBalanceSnapshot result = RoundBalanceCalculator.Calculate(config, 11);
            float withoutMilestone = config.baseMonsterHealth * Mathf.Pow(config.healthGrowthPerRound, 10);

            Assert.AreEqual(withoutMilestone * (1f + config.milestoneHealthBonus),
                result.HealthPerMonster, 0.01f);
            Assert.Greater(result.PotentialGold, 0);
            Object.DestroyImmediate(config);
        }
    }
}
