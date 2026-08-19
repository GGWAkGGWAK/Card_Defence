using System;

namespace CardDefense.Cards
{
    public sealed class PokerProbabilityResult
    {
        public int Iterations { get; }
        public int[] Counts { get; }
        public int InvalidDuplicateCount { get; }
        public int ValidIterations => Iterations - InvalidDuplicateCount;

        public PokerProbabilityResult(int iterations, int[] counts, int invalidDuplicateCount)
        {
            Iterations = iterations;
            Counts = counts;
            InvalidDuplicateCount = invalidDuplicateCount;
        }

        public double GetOverallProbability(PokerHand hand)
        {
            return Iterations > 0 ? Counts[(int)hand] / (double)Iterations : 0d;
        }

        public double GetValidMergeProbability(PokerHand hand)
        {
            return ValidIterations > 0 ? Counts[(int)hand] / (double)ValidIterations : 0d;
        }
    }

    public static class PokerProbabilitySimulator
    {
        public static PokerProbabilityResult SimulateWithReplacement(int iterations, int seed)
        {
            if (iterations <= 0) throw new ArgumentOutOfRangeException(nameof(iterations));
            Random random = new Random(seed);
            int[] counts = new int[10];
            PlayingCard[] cards = new PlayingCard[5];
            int[] rankCounts = new int[15];
            int[] ranks = new int[5];
            int invalidDuplicateCount = 0;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int card = 0; card < cards.Length; card++)
                {
                    cards[card] = new PlayingCard(
                        (CardSuit)random.Next(0, 4),
                        (CardRank)random.Next(2, 15));
                }
                if (PokerMergeRules.HasDuplicateExactCard(cards))
                {
                    invalidDuplicateCount++;
                    continue;
                }
                counts[(int)PokerHandEvaluator.EvaluateBuffered(cards, rankCounts, ranks)]++;
            }
            return new PokerProbabilityResult(iterations, counts, invalidDuplicateCount);
        }
    }
}
