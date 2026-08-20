using CardDefense.Core;
using CardDefense.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class MonsterArchetypeRulesTests
    {
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
