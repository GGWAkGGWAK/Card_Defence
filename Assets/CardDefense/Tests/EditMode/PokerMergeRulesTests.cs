using CardDefense.Cards;
using NUnit.Framework;

namespace CardDefense.Tests
{
    public sealed class PokerMergeRulesTests
    {
        [Test]
        public void SameSuitAndRankBlocksMerge()
        {
            PlayingCard[] cards =
            {
                C(CardSuit.Spade, CardRank.Ace),
                C(CardSuit.Spade, CardRank.Ace),
                C(CardSuit.Heart, CardRank.Three),
                C(CardSuit.Club, CardRank.Four),
                C(CardSuit.Diamond, CardRank.Five)
            };

            Assert.IsTrue(PokerMergeRules.HasDuplicateExactCard(cards));
            Assert.IsFalse(PokerMergeRules.CanMerge(cards));
        }

        [Test]
        public void SameRankWithDifferentSuitCanMerge()
        {
            PlayingCard[] cards =
            {
                C(CardSuit.Spade, CardRank.Ace),
                C(CardSuit.Heart, CardRank.Ace),
                C(CardSuit.Heart, CardRank.Three),
                C(CardSuit.Club, CardRank.Four),
                C(CardSuit.Diamond, CardRank.Five)
            };

            Assert.IsFalse(PokerMergeRules.HasDuplicateExactCard(cards));
            Assert.IsTrue(PokerMergeRules.CanMerge(cards));
        }

        private static PlayingCard C(CardSuit suit, CardRank rank)
        {
            return new PlayingCard(suit, rank);
        }
    }
}
