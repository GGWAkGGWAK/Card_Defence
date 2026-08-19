using CardDefense.Cards;
using NUnit.Framework;

namespace CardDefense.Tests
{
    public sealed class PokerProbabilitySimulatorTests
    {
        [Test]
        public void SimulationCountsEveryMergeExactlyOnce()
        {
            const int iterations = 5000;
            PokerProbabilityResult result = PokerProbabilitySimulator.SimulateWithReplacement(iterations, 17);
            int total = 0;
            for (int i = 0; i < result.Counts.Length; i++) total += result.Counts[i];

            Assert.AreEqual(iterations, total + result.InvalidDuplicateCount);
            Assert.Greater(result.InvalidDuplicateCount, 0);
            Assert.Greater(result.Counts[(int)PokerHand.High], 0);
            Assert.Greater(result.Counts[(int)PokerHand.OnePair], 0);
        }

        [Test]
        public void SameSeedProducesSameDistribution()
        {
            PokerProbabilityResult first = PokerProbabilitySimulator.SimulateWithReplacement(2000, 99);
            PokerProbabilityResult second = PokerProbabilitySimulator.SimulateWithReplacement(2000, 99);
            CollectionAssert.AreEqual(first.Counts, second.Counts);
            Assert.AreEqual(first.InvalidDuplicateCount, second.InvalidDuplicateCount);
        }
    }
}
