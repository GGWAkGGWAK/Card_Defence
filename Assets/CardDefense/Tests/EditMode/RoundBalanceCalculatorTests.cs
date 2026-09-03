using CardDefense.Core;
using CardDefense.Combat;
using CardDefense.Enemies;
using CardDefense.UI;
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
            Assert.That(round100, Is.InRange(1800000f, 2200000f));
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

        [Test]
        public void EndlessCurveRemainsFiniteAndMonotonicThroughRoundFiveHundred()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            float previousHealth = 0f;
            float previousDps = 0f;
            for (int round = 1; round <= 500; round++)
            {
                RoundBalanceSnapshot snapshot = RoundBalanceCalculator.Calculate(config, round);
                Assert.IsFalse(float.IsNaN(snapshot.HealthPerMonster), "Health NaN at round " + round);
                Assert.IsFalse(float.IsInfinity(snapshot.HealthPerMonster), "Health overflow at round " + round);
                Assert.IsFalse(float.IsNaN(snapshot.RequiredDps), "DPS NaN at round " + round);
                Assert.IsFalse(float.IsInfinity(snapshot.RequiredDps), "DPS overflow at round " + round);
                Assert.GreaterOrEqual(snapshot.HealthPerMonster, previousHealth);
                Assert.GreaterOrEqual(snapshot.RequiredDps, previousDps);
                Assert.Greater(snapshot.RewardPerMonster, 0);
                Assert.Greater(snapshot.PotentialGold, 0);
                previousHealth = snapshot.HealthPerMonster;
                previousDps = snapshot.RequiredDps;
            }
            Object.DestroyImmediate(config);
        }

        [Test]
        public void LateGameGoldGrowthStaysBelowHealthPressureGrowth()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            config.rewardGrowthPerRound = 1.025f;
            RoundBalanceSnapshot round40 = RoundBalanceCalculator.Calculate(config, 40);
            RoundBalanceSnapshot round80 = RoundBalanceCalculator.Calculate(config, 80);

            Assert.Less((float)round80.RewardPerMonster / round40.RewardPerMonster, 3f);
            Assert.Greater(round80.HealthPerMonster / round40.HealthPerMonster, 10f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void BossQuestRiskRisesWithMilestoneRoundsAndCompletedWins()
        {
            Assert.AreEqual(1, BossQuestController.CalculateRiskTier(1, 0));
            Assert.AreEqual(2, BossQuestController.CalculateRiskTier(11, 0));
            Assert.AreEqual(5, BossQuestController.CalculateRiskTier(21, 2));
        }

        [Test]
        public void BossQuestHealthRiskOutgrowsRewardAndFailureAddsReinforcements()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            float baseHealth = BossQuestController.CalculateHealthMultiplier(config, 1, 0);
            float highRiskHealth = BossQuestController.CalculateHealthMultiplier(config, 31, 3);
            int baseReward = BossQuestController.CalculateReward(config, 31, 0);
            int highRiskReward = BossQuestController.CalculateReward(config, 31, 3);
            int baseReinforcements = BossQuestController.CalculateFailureReinforcements(config, 1);
            int highRiskReinforcements = BossQuestController.CalculateFailureReinforcements(config, 7);

            Assert.AreEqual(1f, baseHealth, 0.001f);
            Assert.Greater(highRiskHealth, 1f);
            Assert.Greater(highRiskReward, baseReward);
            Assert.Greater(highRiskHealth, (float)highRiskReward / baseReward);
            Assert.Greater(highRiskReinforcements, baseReinforcements);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void LateRewardSoftCapSuppressesOldUnlimitedGoldCurve()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            RoundBalanceSnapshot round100 = RoundBalanceCalculator.Calculate(config, 100);
            float unlimitedReward = config.baseKillGold * Mathf.Pow(config.rewardGrowthPerRound, 99);

            Assert.Less(round100.RewardPerMonster, unlimitedReward * 0.7f);
            Assert.Greater(round100.RewardPerMonster, config.baseKillGold);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void LateHealthAccelerationRaisesPressureAfterRoundFifty()
        {
            GameBalanceConfig accelerated = ScriptableObject.CreateInstance<GameBalanceConfig>();
            GameBalanceConfig baseline = ScriptableObject.CreateInstance<GameBalanceConfig>();
            baseline.lateHealthAccelerationPerRound = 1f;

            float acceleratedHealth = RoundBalanceCalculator.Calculate(accelerated, 100).HealthPerMonster;
            float baselineHealth = RoundBalanceCalculator.Calculate(baseline, 100).HealthPerMonster;

            Assert.Greater(acceleratedHealth, baselineHealth * 1.3f);
            Object.DestroyImmediate(accelerated);
            Object.DestroyImmediate(baseline);
        }

        [Test]
        public void OccupiedFieldRaisesSummonCostAndMergingRelievesPressure()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            int emptyCost = CardSummonController.CalculateSummonCost(config, 0);
            int fullCost = CardSummonController.CalculateSummonCost(config, 12);
            int afterMergeCost = CardSummonController.CalculateSummonCost(config, 8);

            Assert.AreEqual(config.summonCost, emptyCost);
            Assert.Greater(fullCost, afterMergeCost);
            Assert.Greater(afterMergeCost, emptyCost);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void EndlessSimulationShowsIncreasingPressureAtKeyCheckpoints()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            EndlessBalanceSnapshot round50 = EndlessBalanceSimulator.Calculate(config, 50);
            EndlessBalanceSnapshot round100 = EndlessBalanceSimulator.Calculate(config, 100);
            EndlessBalanceSnapshot round150 = EndlessBalanceSimulator.Calculate(config, 150);

            Assert.AreEqual(12, round50.ProjectedSummonCount);
            Assert.Greater(round100.CumulativeGold, round50.CumulativeGold);
            Assert.Greater(round150.CumulativeGold, round100.CumulativeGold);
            Assert.Greater(round100.EconomyPressure, round50.EconomyPressure);
            Assert.Greater(round150.EconomyPressure, round100.EconomyPressure);
            Assert.Less(round100.DpsCoverage, round50.DpsCoverage);
            Assert.Less(round150.DpsCoverage, round100.DpsCoverage);
            Assert.IsFalse(float.IsInfinity(round150.RequiredDps));
            Object.DestroyImmediate(config);
        }
    }
}
