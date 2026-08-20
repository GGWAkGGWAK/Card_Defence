using CardDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class PlayerProfileDataTests
    {
        [Test]
        public void ProfileKeepsBestRoundAndAccumulatesRunTotals()
        {
            PlayerProfileData profile = new PlayerProfileData();
            profile.ApplyRunSnapshot(20, 120, 800);
            profile.ApplyRunSnapshot(15, 90, 500);

            Assert.AreEqual(20, profile.BestRound);
            Assert.AreEqual(2, profile.TotalRuns);
            Assert.AreEqual(210, profile.TotalMonstersDefeated);
            Assert.AreEqual(1300, profile.TotalGoldEarned);
        }

        [Test]
        public void ProfileRoundTripsThroughAndroidCompatibleJson()
        {
            PlayerProfileData profile = new PlayerProfileData();
            profile.ApplyRunSnapshot(42, 1234, 5678);
            string json = JsonUtility.ToJson(profile);
            PlayerProfileData restored = JsonUtility.FromJson<PlayerProfileData>(json);

            Assert.AreEqual(profile.BestRound, restored.BestRound);
            Assert.AreEqual(profile.TotalRuns, restored.TotalRuns);
            Assert.AreEqual(profile.TotalMonstersDefeated, restored.TotalMonstersDefeated);
            Assert.AreEqual(profile.TotalGoldEarned, restored.TotalGoldEarned);
        }
    }
}
