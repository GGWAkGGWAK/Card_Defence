using System;
using CardDefense.Cards;
using UnityEngine;

namespace CardDefense.Core
{
    public sealed class PokerProgressionService : MonoBehaviour
    {
        public event Action<PokerHand, int> HandUpgraded;

        private readonly int[] levels = new int[10];
        private GameBalanceConfig config;
        private EconomyService economy;

        public void Configure(GameBalanceConfig balance, EconomyService economyService)
        {
            config = balance;
            economy = economyService;
            Array.Clear(levels, 0, levels.Length);
        }

        public int GetLevel(PokerHand hand)
        {
            return levels[(int)hand];
        }

        public int GetUpgradeCost(PokerHand hand)
        {
            int level = GetLevel(hand);
            float handTierFactor = 1f + ((int)hand * 0.35f);
            double cost = config.baseHandUpgradeCost * handTierFactor *
                          Math.Pow(config.handUpgradeCostGrowth, level);
            return Mathf.Max(1, Mathf.CeilToInt((float)Math.Min(cost, int.MaxValue)));
        }

        public float GetDamageMultiplier(PokerHand hand)
        {
            return CardDefense.Combat.PokerCombatMath.DamageMultiplier(config, hand, GetLevel(hand));
        }

        public bool TryUpgrade(PokerHand hand)
        {
            int cost = GetUpgradeCost(hand);
            if (!economy.TrySpend(cost)) return false;
            levels[(int)hand]++;
            HandUpgraded?.Invoke(hand, levels[(int)hand]);
            return true;
        }
    }
}
