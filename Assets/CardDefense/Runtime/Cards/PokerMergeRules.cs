using System.Collections.Generic;

namespace CardDefense.Cards
{
    public static class PokerMergeRules
    {
        public static bool HasDuplicateExactCard(IReadOnlyList<PlayingCard> cards)
        {
            if (cards == null) return false;
            for (int left = 0; left < cards.Count - 1; left++)
            {
                for (int right = left + 1; right < cards.Count; right++)
                {
                    if (AreExactMatch(cards[left], cards[right]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CanMerge(IReadOnlyList<PlayingCard> cards)
        {
            return cards != null && cards.Count == 5 && !HasDuplicateExactCard(cards);
        }

        public static bool AreExactMatch(PlayingCard first, PlayingCard second)
        {
            return first.Suit == second.Suit && first.Rank == second.Rank;
        }
    }
}
