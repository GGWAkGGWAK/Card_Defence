using System;
using UnityEngine;

namespace CardDefense.Core
{
    public sealed class EconomyService : MonoBehaviour
    {
        public event Action<int> GoldChanged;

        public int Gold { get; private set; }
        public int AffordableSummons => config != null && config.summonCost > 0 ? Gold / config.summonCost : 0;
        private GameBalanceConfig config;

        public void Configure(GameBalanceConfig balance)
        {
            config = balance;
            Gold = config != null ? config.startingGold : 0;
            GoldChanged?.Invoke(Gold);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        public void RestoreGold(int amount)
        {
            Gold = Mathf.Max(0, amount);
            GoldChanged?.Invoke(Gold);
        }
    }
}
