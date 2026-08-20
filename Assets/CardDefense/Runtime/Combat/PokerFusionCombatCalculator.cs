using CardDefense.Cards;
using CardDefense.Core;

namespace CardDefense.Combat
{
    public readonly struct PokerFusionCombatResult
    {
        public readonly PlayingCard RepresentativeCard;
        public readonly float BaseDamage;
        public readonly int CoreCardCount;

        public PokerFusionCombatResult(PlayingCard representativeCard, float baseDamage, int coreCardCount)
        {
            RepresentativeCard = representativeCard;
            BaseDamage = baseDamage;
            CoreCardCount = coreCardCount;
        }
    }

    public static class PokerFusionCombatCalculator
    {
        public static PokerFusionCombatResult Calculate(GameBalanceConfig config, PlayingCard[] cards,
            PokerHand hand)
        {
            if (cards == null || cards.Length != 5)
                return new PokerFusionCombatResult(default, 0f, 0);

            CardRank primaryRank = FindPrimaryRank(cards, hand);
            PlayingCard representative = FindRepresentative(cards, primaryRank);
            float baseDamage = 0f;
            int coreCount = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                bool core = IsCoreCard(cards, i, hand, primaryRank);
                float contribution = PokerCombatMath.BaseDamage(config, cards[i].Rank);
                if (core)
                {
                    baseDamage += contribution;
                    coreCount++;
                }
                else
                {
                    baseDamage += contribution * config.discardedMaterialPowerRatio;
                }
            }
            return new PokerFusionCombatResult(representative, baseDamage, coreCount);
        }

        public static CardRank FindPrimaryRank(PlayingCard[] cards, PokerHand hand)
        {
            if (cards == null || cards.Length == 0) return CardRank.Two;
            if (hand == PokerHand.RoyalStraightFlush) return CardRank.Ace;
            if (hand == PokerHand.Straight || hand == PokerHand.StraightFlush)
                return IsWheelStraight(cards) ? CardRank.Five : HighestRank(cards);

            int requiredCount = 0;
            switch (hand)
            {
                case PokerHand.OnePair:
                case PokerHand.TwoPair:
                    requiredCount = 2;
                    break;
                case PokerHand.ThreeOfAKind:
                case PokerHand.FullHouse:
                    requiredCount = 3;
                    break;
                case PokerHand.FourOfAKind:
                    requiredCount = 4;
                    break;
            }

            if (requiredCount > 0)
            {
                for (int rank = (int)CardRank.Ace; rank >= (int)CardRank.Two; rank--)
                    if (CountRank(cards, (CardRank)rank) == requiredCount) return (CardRank)rank;
            }
            return HighestRank(cards);
        }

        private static bool IsCoreCard(PlayingCard[] cards, int index, PokerHand hand, CardRank primaryRank)
        {
            switch (hand)
            {
                case PokerHand.Straight:
                case PokerHand.Flush:
                case PokerHand.FullHouse:
                case PokerHand.StraightFlush:
                case PokerHand.RoyalStraightFlush:
                    return true;
                case PokerHand.TwoPair:
                    return CountRank(cards, cards[index].Rank) == 2;
                case PokerHand.OnePair:
                case PokerHand.ThreeOfAKind:
                case PokerHand.FourOfAKind:
                case PokerHand.High:
                    return cards[index].Rank == primaryRank;
                default:
                    return false;
            }
        }

        private static CardRank HighestRank(PlayingCard[] cards)
        {
            CardRank highest = cards[0].Rank;
            for (int i = 1; i < cards.Length; i++)
                if ((int)cards[i].Rank > (int)highest) highest = cards[i].Rank;
            return highest;
        }

        private static int CountRank(PlayingCard[] cards, CardRank rank)
        {
            int count = 0;
            for (int i = 0; i < cards.Length; i++)
                if (cards[i].Rank == rank) count++;
            return count;
        }

        private static bool IsWheelStraight(PlayingCard[] cards)
        {
            bool ace = false;
            bool two = false;
            bool three = false;
            bool four = false;
            bool five = false;
            for (int i = 0; i < cards.Length; i++)
            {
                switch (cards[i].Rank)
                {
                    case CardRank.Ace: ace = true; break;
                    case CardRank.Two: two = true; break;
                    case CardRank.Three: three = true; break;
                    case CardRank.Four: four = true; break;
                    case CardRank.Five: five = true; break;
                }
            }
            return ace && two && three && four && five;
        }

        private static PlayingCard FindRepresentative(PlayingCard[] cards, CardRank primaryRank)
        {
            for (int i = 0; i < cards.Length; i++)
                if (cards[i].Rank == primaryRank) return cards[i];
            return cards[0];
        }
    }
}
