using CardDefense.Core;
using CardDefense.Enemies;
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
        public void TunedEndlessCurveHitsExpectedDpsCheckpoints()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            float round10 = AdjustedRoundBalanceCalculator.Calculate(config, 10).RequiredDps;
            float round50 = AdjustedRoundBalanceCalculator.Calculate(config, 50).RequiredDps;
            float round100 = AdjustedRoundBalanceCalculator.Calculate(config, 100).RequiredDps;

            Assert.That(round10, Is.InRange(40f, 80f));
            Assert.That(round50, Is.InRange(5000f, 10000f));
            Assert.That(round100, Is.InRange(1000000f, 2000000f));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SpawnIntervalCompressesSoLargeWavesFitInsideRound()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            int earlyCount = RoundBalanceCalculator.Calculate(config, 1).MonsterCount;
            int lateCount = RoundBalanceCalculator.Calculate(config, 100).MonsterCount;
            float earlyInterval = WaveDirector.CalculateSpawnInterval(config, earlyCount);
            float lateInterval = WaveDirector.CalculateSpawnInterval(config, lateCount);

            Assert.AreEqual(config.spawnInterval, earlyInterval, 0.001f);
            Assert.Less(lateInterval, earlyInterval);
            Assert.LessOrEqual(lateInterval * lateCount, config.roundDuration * 0.851f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AdjustedBalanceIncludesArchetypeHealthAndRewards()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            RoundBalanceSnapshot baseRound = RoundBalanceCalculator.Calculate(config, 10);
            AdjustedRoundBalanceSnapshot adjusted = AdjustedRoundBalanceCalculator.Calculate(config, 10);

            Assert.AreEqual(baseRound.MonsterCount, adjusted.MonsterCount);
            Assert.Greater(adjusted.TotalHealth, baseRound.TotalHealth);
            Assert.Greater(adjusted.PotentialGold, baseRound.PotentialGold);
            Assert.Greater(adjusted.BossHealth, 0f);
            Assert.Greater(adjusted.BossReward, 0);
            Assert.AreEqual(adjusted.TotalHealth / config.roundDuration, adjusted.RequiredDps, 0.01f);
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
