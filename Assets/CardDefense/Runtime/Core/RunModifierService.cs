using System;
using UnityEngine;

namespace CardDefense.Core
{
    public enum RunGrowthChoice
    {
        AttackPower,
        KillGold,
        SummonDiscount
    }

    public sealed class RunModifierService : MonoBehaviour
    {
        public event Action<RunGrowthChoice> ChoiceApplied;

        public float DamageMultiplier { get; private set; } = 1f;
        public float KillGoldMultiplier { get; private set; } = 1f;
        public float SummonCostMultiplier { get; private set; } = 1f;
        public int ChoiceCount { get; private set; }

        public void ResetRun()
        {
            DamageMultiplier = 1f;
            KillGoldMultiplier = 1f;
            SummonCostMultiplier = 1f;
            ChoiceCount = 0;
        }

        public void Apply(RunGrowthChoice choice)
        {
            switch (choice)
            {
                case RunGrowthChoice.AttackPower:
                    DamageMultiplier *= 1.15f;
                    break;
                case RunGrowthChoice.KillGold:
                    KillGoldMultiplier *= 1.12f;
                    break;
                case RunGrowthChoice.SummonDiscount:
                    SummonCostMultiplier *= 0.9f;
                    break;
            }
            ChoiceCount++;
            ChoiceApplied?.Invoke(choice);
        }

        public int GetSummonCost(GameBalanceConfig config)
        {
            return Mathf.Max(5, Mathf.CeilToInt(config.summonCost * SummonCostMultiplier));
        }

        public int ApplyKillGold(int baseReward)
        {
            return Mathf.Max(1, Mathf.CeilToInt(baseReward * KillGoldMultiplier));
        }
    }
}
