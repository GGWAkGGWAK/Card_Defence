using CardDefense.Core;
using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class MonsterArchetypeRulesTests
    {
        [Test]
        public void ThirdBossOverwhelmsWeakPairsButDevelopedAntiBossBuildCanClear()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                float health = AdjustedRoundBalanceCalculator.Calculate(config, 30).BossHealth;
                float weak = BossDps(config, PokerHand.OnePair, 6) * 4f * 0.65f;
                float strong = BossDps(config, PokerHand.ThreeOfAKind, 6) * 4f * 0.65f;
                Assert.Greater(health / weak, config.roundDuration * 2f,
                    "Four low pairs should leave the third boss alive across multiple waves.");
                Assert.Less(health / strong, config.roundDuration,
                    "A developed anti-boss hand should retain a viable clear window.");
                Assert.AreEqual(10f, MonsterArchetypeRules.GetRoundStats(config, MonsterArchetype.Boss, 10).HealthMultiplier);
                Assert.AreEqual(30f, MonsterArchetypeRules.GetRoundStats(config, MonsterArchetype.Boss, 20).HealthMultiplier);
                Assert.AreEqual(80f, MonsterArchetypeRules.GetRoundStats(config, MonsterArchetype.Boss, 30).HealthMultiplier);
                Assert.AreEqual(80f, MonsterArchetypeRules.GetRoundStats(config, MonsterArchetype.Boss, 40).HealthMultiplier);
                Assert.AreEqual(5f, MonsterArchetypeRules.GetRoundStats(config, MonsterArchetype.Boss, 30).RewardMultiplier);
                Debug.Log("R30 boss HP=" + health + "; weak clear seconds=" + health / weak + "; strong clear seconds=" + health / strong);
            }
            finally { Object.DestroyImmediate(config); }
        }

        private static float BossDps(GameBalanceConfig config, PokerHand hand, int level)
        {
            var cards = new[] {
                new PlayingCard(CardSuit.Spade, CardRank.Seven),
                new PlayingCard(CardSuit.Heart, CardRank.Seven),
                new PlayingCard(CardSuit.Diamond, hand == PokerHand.OnePair ? CardRank.Two : CardRank.Seven),
                new PlayingCard(CardSuit.Club, CardRank.Five),
                new PlayingCard(CardSuit.Spade, CardRank.Nine) };
            var fusion = PokerFusionCombatCalculator.Calculate(config, cards, hand);
            var profile = PokerHandCombatProfile.Get(hand);
            return fusion.BaseDamage * PokerCombatMath.DamageMultiplier(config, hand, level) *
                profile.HeavyTargetDamageMultiplier / (config.towerAttackInterval * profile.AttackIntervalMultiplier);
        }

        [Test]
        public void EveryTenthRoundStartsWithBoss()
        {
            Assert.AreEqual(MonsterArchetype.Boss, MonsterArchetypeRules.Select(10, 0));
            Assert.AreEqual(MonsterArchetype.Boss, MonsterArchetypeRules.Select(20, 0));
            Assert.AreNotEqual(MonsterArchetype.Boss, MonsterArchetypeRules.Select(10, 1));
        }

        [Test]
        public void ArchetypeMultipliersCreateDistinctRoles()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            MonsterArchetypeStats fast = MonsterArchetypeRules.GetStats(config, MonsterArchetype.Fast);
            MonsterArchetypeStats tank = MonsterArchetypeRules.GetStats(config, MonsterArchetype.Tank);
            MonsterArchetypeStats boss = MonsterArchetypeRules.GetStats(config, MonsterArchetype.Boss);

            Assert.Greater(fast.SpeedMultiplier, 1f);
            Assert.Less(fast.HealthMultiplier, 1f);
            Assert.Greater(tank.HealthMultiplier, 1f);
            Assert.Greater(boss.HealthMultiplier, tank.HealthMultiplier);
            Object.DestroyImmediate(config);
        }
    }
}
