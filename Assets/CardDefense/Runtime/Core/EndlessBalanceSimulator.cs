using System;
using CardDefense.Cards;
using CardDefense.Combat;
using UnityEngine;

namespace CardDefense.Core
{
    [Serializable]
    public struct EndlessBalanceSnapshot
    {
        public int Round;
        public int WaveGold;
        public long CumulativeGold;
        public float RequiredDps;
        public int ProjectedSummonCount;
        public int ProjectedUpgradeLevel;
        public float ProjectedReferenceDps;
        public float DpsCoverage;
        public float EconomyPressure;
    }

    public static class EndlessBalanceSimulator
    {
        public const int ReferenceFieldSlots = 12;

        public static EndlessBalanceSnapshot Calculate(GameBalanceConfig config, int targetRound)
        {
            if (config == null) return default;
            targetRound = Mathf.Max(1, targetRound);
            long cumulativeGold = config.startingGold;
            AdjustedRoundBalanceSnapshot target = default;
            for (int round = 1; round <= targetRound; round++)
            {
                target = AdjustedRoundBalanceCalculator.Calculate(config, round);
                cumulativeGold = SaturatingAdd(cumulativeGold, target.PotentialGold);
            }

            long availableGold = cumulativeGold;
            int summonedCards = 0;
            while (summonedCards < ReferenceFieldSlots)
            {
                int cost = CardSummonController.CalculateSummonCost(config, summonedCards);
                if (cost <= 0 || availableGold < cost) break;
                availableGold -= cost;
                summonedCards++;
            }

            long upgradeBudget = (long)(availableGold * 0.65d);
            int upgradeLevel = 0;
            while (upgradeLevel < 500)
            {
                int cost = CalculateUpgradeCost(config, PokerHand.OnePair, upgradeLevel);
                if (cost <= 0 || upgradeBudget < cost) break;
                upgradeBudget -= cost;
                upgradeLevel++;
            }

            float referenceDps = ReferencePairDps(config, upgradeLevel) * summonedCards;
            float requiredDps = target.RequiredDps;
            return new EndlessBalanceSnapshot
            {
                Round = targetRound,
                WaveGold = target.PotentialGold,
                CumulativeGold = cumulativeGold,
                RequiredDps = requiredDps,
                ProjectedSummonCount = summonedCards,
                ProjectedUpgradeLevel = upgradeLevel,
                ProjectedReferenceDps = referenceDps,
                DpsCoverage = requiredDps > 0f ? referenceDps / requiredDps : 1f,
                EconomyPressure = target.PotentialGold > 0 ? requiredDps / target.PotentialGold : requiredDps
            };
        }

        public static int CalculateUpgradeCost(GameBalanceConfig config, PokerHand hand, int level)
        {
            if (config == null) return 0;
            float tierFactor = 1f + (int)hand * 0.35f;
            double cost = config.baseHandUpgradeCost * tierFactor *
                          Math.Pow(config.handUpgradeCostGrowth, Mathf.Max(0, level));
            return Mathf.Max(1, Mathf.CeilToInt((float)Math.Min(cost, int.MaxValue)));
        }

        private static float ReferencePairDps(GameBalanceConfig config, int level)
        {
            PlayingCard[] cards =
            {
                new PlayingCard(CardSuit.Spade, CardRank.Ace),
                new PlayingCard(CardSuit.Heart, CardRank.Ace),
                new PlayingCard(CardSuit.Diamond, CardRank.Two),
                new PlayingCard(CardSuit.Club, CardRank.Five),
                new PlayingCard(CardSuit.Spade, CardRank.Nine)
            };
            PokerFusionCombatResult fusion = PokerFusionCombatCalculator.Calculate(config, cards,
                PokerHand.OnePair);
            return PokerCombatMath.EstimatedDps(config, fusion.BaseDamage, PokerHand.OnePair, level);
        }

        private static long SaturatingAdd(long first, int second)
        {
            if (second > 0 && first > long.MaxValue - second) return long.MaxValue;
            return first + second;
        }
    }
}
