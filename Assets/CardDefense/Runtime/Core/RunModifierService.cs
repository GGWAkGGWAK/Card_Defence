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
        public int BossQuestWins { get; private set; }

        public void ResetRun()
        {
            DamageMultiplier = 1f;
            KillGoldMultiplier = 1f;
            SummonCostMultiplier = 1f;
            ChoiceCount = 0;
            BossQuestWins = 0;
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

        public void ApplyBossQuestReward(float attackBonus)
        {
            DamageMultiplier *= 1f + Mathf.Clamp(attackBonus, 0f, 0.25f);
            BossQuestWins++;
        }

        public RunModifierSnapshot CaptureSnapshot()
        {
            return new RunModifierSnapshot
            {
                DamageMultiplier = DamageMultiplier,
                KillGoldMultiplier = KillGoldMultiplier,
                SummonCostMultiplier = SummonCostMultiplier,
                ChoiceCount = ChoiceCount,
                BossQuestWins = BossQuestWins
            };
        }

        public void RestoreSnapshot(RunModifierSnapshot snapshot)
        {
            DamageMultiplier = Mathf.Max(1f, snapshot.DamageMultiplier);
            KillGoldMultiplier = Mathf.Max(1f, snapshot.KillGoldMultiplier);
            SummonCostMultiplier = snapshot.SummonCostMultiplier > 0f
                ? Mathf.Clamp(snapshot.SummonCostMultiplier, 0.01f, 1f)
                : 1f;
            ChoiceCount = Mathf.Max(0, snapshot.ChoiceCount);
            BossQuestWins = Mathf.Max(0, snapshot.BossQuestWins);
        }
    }
}
