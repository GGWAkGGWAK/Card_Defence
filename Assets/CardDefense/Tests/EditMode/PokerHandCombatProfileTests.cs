using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class PokerHandCombatProfileTests
    {
        [Test]
        public void EveryPokerHandHasAValidCombatProfile()
        {
            for (int i = (int)PokerHand.High; i <= (int)PokerHand.RoyalStraightFlush; i++)
            {
                PokerHandCombatProfile profile = PokerHandCombatProfile.Get((PokerHand)i);
                Assert.Greater(profile.AttackIntervalMultiplier, 0f);
                Assert.Greater(profile.RangeMultiplier, 0f);
                Assert.GreaterOrEqual(profile.TargetCount, 1);
                Assert.IsNotEmpty(profile.KoreanTrait);
            }
        }

        [Test]
        public void OnePairUsesPairRankAndOnlySmallDiscardContribution()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            PlayingCard[] cards =
            {
                new PlayingCard(CardSuit.Spade, CardRank.Seven),
                new PlayingCard(CardSuit.Heart, CardRank.Seven),
                new PlayingCard(CardSuit.Diamond, CardRank.Ace),
                new PlayingCard(CardSuit.Club, CardRank.King),
                new PlayingCard(CardSuit.Club, CardRank.Three)
            };
            PokerFusionCombatResult fusion = PokerFusionCombatCalculator.Calculate(config, cards,
                PokerHand.OnePair);
            float expectedCore = PokerCombatMath.BaseDamage(config, CardRank.Seven) * 2f;
            float expectedDiscard = (PokerCombatMath.BaseDamage(config, CardRank.Ace) +
                                     PokerCombatMath.BaseDamage(config, CardRank.King) +
                                     PokerCombatMath.BaseDamage(config, CardRank.Three)) *
                                    config.discardedMaterialPowerRatio;

            Assert.AreEqual(CardRank.Seven, fusion.RepresentativeCard.Rank);
            Assert.AreEqual(2, fusion.CoreCardCount);
            Assert.AreEqual(expectedCore + expectedDiscard, fusion.BaseDamage, 0.001f);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void TwoPairUsesHigherPairAsRepresentativeAndKickerAsDiscard()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            PlayingCard[] cards =
            {
                new PlayingCard(CardSuit.Spade, CardRank.Seven),
                new PlayingCard(CardSuit.Heart, CardRank.Seven),
                new PlayingCard(CardSuit.Diamond, CardRank.Queen),
                new PlayingCard(CardSuit.Club, CardRank.Queen),
                new PlayingCard(CardSuit.Club, CardRank.Ace)
            };
            PokerFusionCombatResult fusion = PokerFusionCombatCalculator.Calculate(config, cards,
                PokerHand.TwoPair);

            Assert.AreEqual(CardRank.Queen, fusion.RepresentativeCard.Rank);
            Assert.AreEqual(4, fusion.CoreCardCount);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AceLowStraightDisplaysFiveAsItsPrimaryRank()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            PlayingCard[] cards =
            {
                new PlayingCard(CardSuit.Spade, CardRank.Ace),
                new PlayingCard(CardSuit.Heart, CardRank.Two),
                new PlayingCard(CardSuit.Diamond, CardRank.Three),
                new PlayingCard(CardSuit.Club, CardRank.Four),
                new PlayingCard(CardSuit.Spade, CardRank.Five)
            };
            PokerFusionCombatResult fusion = PokerFusionCombatCalculator.Calculate(config, cards,
                PokerHand.Straight);

            Assert.AreEqual(CardRank.Five, fusion.RepresentativeCard.Rank);
            Assert.AreEqual(5, fusion.CoreCardCount);
            Object.DestroyImmediate(config);
        }

        [TestCase(0f, 100f, 0, 80, CombatThreatLevel.Danger)]
        [TestCase(100f, 100f, 0, 80, CombatThreatLevel.Stable)]
        [TestCase(1000f, 100f, 65, 80, CombatThreatLevel.Critical)]
        public void ThreatEvaluatorCombinesDamageCoverageAndMonsterLoad(float dps, float required,
            int active, int limit, CombatThreatLevel expected)
        {
            Assert.AreEqual(expected, CombatThreatEvaluator.Evaluate(dps, required, active, limit));
        }

        [Test]
        public void SignatureHandsExposeTheirIntendedRoles()
        {
            PokerHandCombatProfile pair = PokerHandCombatProfile.Get(PokerHand.OnePair);
            PokerHandCombatProfile twoPair = PokerHandCombatProfile.Get(PokerHand.TwoPair);
            PokerHandCombatProfile triple = PokerHandCombatProfile.Get(PokerHand.ThreeOfAKind);
            PokerHandCombatProfile straight = PokerHandCombatProfile.Get(PokerHand.Straight);
            PokerHandCombatProfile flush = PokerHandCombatProfile.Get(PokerHand.Flush);
            PokerHandCombatProfile fourCard = PokerHandCombatProfile.Get(PokerHand.FourOfAKind);

            Assert.Less(pair.AttackIntervalMultiplier, 1f);
            Assert.Greater(twoPair.RangeMultiplier, 1f);
            Assert.Greater(triple.HeavyTargetDamageMultiplier, 1f);
            Assert.Greater(straight.TargetCount, 1);
            Assert.Greater(flush.SplashRadius, 0f);
            Assert.Greater(fourCard.CriticalChance, 0f);
        }
    }
}
