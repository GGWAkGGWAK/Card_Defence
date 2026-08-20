using CardDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class RunModifierServiceTests
    {
        [Test]
        public void GrowthChoicesStackMultiplicativelyAndResetPerRun()
        {
            GameObject gameObject = new GameObject("RunModifierTest");
            RunModifierService modifiers = gameObject.AddComponent<RunModifierService>();
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            modifiers.ResetRun();

            modifiers.Apply(RunGrowthChoice.AttackPower);
            modifiers.Apply(RunGrowthChoice.KillGold);
            modifiers.Apply(RunGrowthChoice.SummonDiscount);

            Assert.AreEqual(1.15f, modifiers.DamageMultiplier, 0.001f);
            Assert.AreEqual(12, modifiers.ApplyKillGold(10));
            Assert.AreEqual(23, modifiers.GetSummonCost(config));
            Assert.AreEqual(3, modifiers.ChoiceCount);

            modifiers.ResetRun();
            Assert.AreEqual(1f, modifiers.DamageMultiplier);
            Assert.AreEqual(config.summonCost, modifiers.GetSummonCost(config));
            Assert.AreEqual(0, modifiers.ChoiceCount);
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void SummonDiscountNeverDropsBelowFiveGold()
        {
            GameObject gameObject = new GameObject("RunModifierFloorTest");
            RunModifierService modifiers = gameObject.AddComponent<RunModifierService>();
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            for (int i = 0; i < 50; i++) modifiers.Apply(RunGrowthChoice.SummonDiscount);

            Assert.AreEqual(5, modifiers.GetSummonCost(config));
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
        }
    }
}
