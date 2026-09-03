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
            return EndlessBalanceSimulator.CalculateUpgradeCost(config, hand, GetLevel(hand));
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

        public int[] CaptureLevels()
        {
            int[] snapshot = new int[levels.Length];
            Array.Copy(levels, snapshot, levels.Length);
            return snapshot;
        }

        public void RestoreLevels(int[] snapshot)
        {
            Array.Clear(levels, 0, levels.Length);
            if (snapshot == null) return;
            int count = Math.Min(levels.Length, snapshot.Length);
            for (int i = 0; i < count; i++) levels[i] = Mathf.Max(0, snapshot[i]);
        }
    }
}
