using CardDefense.Cards;
using CardDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class PokerProgressionServiceTests
    {
        [Test]
        public void UpgradeConsumesGoldAndRaisesDamageWithoutLevelLimit()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            config.startingGold = 100000;
            GameObject gameObject = new GameObject("ProgressionTest");
            EconomyService economy = gameObject.AddComponent<EconomyService>();
            PokerProgressionService progression = gameObject.AddComponent<PokerProgressionService>();
            economy.Configure(config);
            progression.Configure(config, economy);

            float before = progression.GetDamageMultiplier(PokerHand.OnePair);
            for (int i = 0; i < 8; i++) Assert.IsTrue(progression.TryUpgrade(PokerHand.OnePair));

            Assert.AreEqual(8, progression.GetLevel(PokerHand.OnePair));
            Assert.Greater(progression.GetDamageMultiplier(PokerHand.OnePair), before);
            Assert.Less(economy.Gold, config.startingGold);

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(config);
        }
    }
}
