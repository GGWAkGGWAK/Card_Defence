using CardDefense.Cards;
using NUnit.Framework;

namespace CardDefense.Tests
{
    public sealed class PokerHandEvaluatorTests
    {
        [Test]
        public void AceCanBeLowInStraight()
        {
            PlayingCard[] cards =
            {
                C(CardSuit.Spade, CardRank.Ace),
                C(CardSuit.Heart, CardRank.Two),
                C(CardSuit.Club, CardRank.Three),
                C(CardSuit.Diamond, CardRank.Four),
                C(CardSuit.Spade, CardRank.Five)
            };
            Assert.AreEqual(PokerHand.Straight, PokerHandEvaluator.Evaluate(cards));
        }

        [Test]
        public void TenToAceSameSuitIsRoyalStraightFlush()
        {
            PlayingCard[] cards =
            {
                C(CardSuit.Heart, CardRank.Ten),
                C(CardSuit.Heart, CardRank.Jack),
                C(CardSuit.Heart, CardRank.Queen),
                C(CardSuit.Heart, CardRank.King),
                C(CardSuit.Heart, CardRank.Ace)
            };
            Assert.AreEqual(PokerHand.RoyalStraightFlush, PokerHandEvaluator.Evaluate(cards));
        }

        [Test]
        public void FullHouseBeatsThreeOfAKind()
        {
            PlayingCard[] cards =
            {
                C(CardSuit.Spade, CardRank.Queen),
                C(CardSuit.Heart, CardRank.Queen),
                C(CardSuit.Club, CardRank.Queen),
                C(CardSuit.Diamond, CardRank.Five),
                C(CardSuit.Spade, CardRank.Five)
            };
            Assert.AreEqual(PokerHand.FullHouse, PokerHandEvaluator.Evaluate(cards));
        }

        private static PlayingCard C(CardSuit suit, CardRank rank)
        {
            return new PlayingCard(suit, rank);
        }
    }
}
