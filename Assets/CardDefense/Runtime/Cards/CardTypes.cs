using System;

namespace CardDefense.Cards
{
    public enum CardSuit
    {
        Spade,
        Diamond,
        Heart,
        Club
    }

    public enum CardRank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    public enum PokerHand
    {
        High,
        OnePair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        RoyalStraightFlush
    }

    [Serializable]
    public struct PlayingCard
    {
        public CardSuit Suit;
        public CardRank Rank;

        public PlayingCard(CardSuit suit, CardRank rank)
        {
            Suit = suit;
            Rank = rank;
        }
    }
}
