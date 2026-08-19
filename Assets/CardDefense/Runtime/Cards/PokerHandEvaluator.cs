using System;
using System.Collections.Generic;

namespace CardDefense.Cards
{
    public static class PokerHandEvaluator
    {
        public static PokerHand Evaluate(IReadOnlyList<PlayingCard> cards)
        {
            if (cards == null || cards.Count != 5)
            {
                return PokerHand.High;
            }

            int[] rankCounts = new int[15];
            int[] ranks = new int[5];
            return EvaluateBuffered(cards, rankCounts, ranks);
        }

        internal static PokerHand EvaluateBuffered(IReadOnlyList<PlayingCard> cards,
            int[] rankCounts, int[] ranks)
        {
            if (cards == null || cards.Count != 5) return PokerHand.High;
            Array.Clear(rankCounts, 0, rankCounts.Length);
            CardSuit firstSuit = cards[0].Suit;
            bool flush = true;

            for (int i = 0; i < cards.Count; i++)
            {
                int rank = (int)cards[i].Rank;
                rankCounts[rank]++;
                ranks[i] = rank;
                flush &= cards[i].Suit == firstSuit;
            }

            Array.Sort(ranks);
            bool straight = IsStraight(ranks);
            bool royal = straight && ranks[0] == 10 && ranks[4] == 14;

            if (flush && royal) return PokerHand.RoyalStraightFlush;
            if (flush && straight) return PokerHand.StraightFlush;

            int pairs = 0;
            bool three = false;
            bool four = false;

            for (int rank = 2; rank <= 14; rank++)
            {
                switch (rankCounts[rank])
                {
                    case 4: four = true; break;
                    case 3: three = true; break;
                    case 2: pairs++; break;
                }
            }

            if (four) return PokerHand.FourOfAKind;
            if (three && pairs == 1) return PokerHand.FullHouse;
            if (flush) return PokerHand.Flush;
            if (straight) return PokerHand.Straight;
            if (three) return PokerHand.ThreeOfAKind;
            if (pairs == 2) return PokerHand.TwoPair;
            if (pairs == 1) return PokerHand.OnePair;
            return PokerHand.High;
        }

        private static bool IsStraight(int[] sortedRanks)
        {
            bool normal = true;
            for (int i = 1; i < sortedRanks.Length; i++)
            {
                if (sortedRanks[i] != sortedRanks[0] + i)
                {
                    normal = false;
                    break;
                }
            }

            if (normal) return true;

            // A-2-3-4-5: Ace may be used as 1.
            return sortedRanks[0] == 2 &&
                   sortedRanks[1] == 3 &&
                   sortedRanks[2] == 4 &&
                   sortedRanks[3] == 5 &&
                   sortedRanks[4] == 14;
        }
    }
}
