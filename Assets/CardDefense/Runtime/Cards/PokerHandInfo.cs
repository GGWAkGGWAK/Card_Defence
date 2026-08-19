namespace CardDefense.Cards
{
    public static class PokerHandInfo
    {
        private static readonly float[] DamageMultipliers =
        {
            1f, 1.6f, 2.3f, 3.2f, 4.5f,
            6f, 8.5f, 12f, 18f, 30f
        };

        public static float DamageMultiplier(PokerHand hand)
        {
            int index = (int)hand;
            return index >= 0 && index < DamageMultipliers.Length ? DamageMultipliers[index] : 1f;
        }

        public static string ShortName(PokerHand hand)
        {
            switch (hand)
            {
                case PokerHand.OnePair: return "PAIR";
                case PokerHand.TwoPair: return "2 PAIR";
                case PokerHand.ThreeOfAKind: return "TRIPLE";
                case PokerHand.Straight: return "STRAIGHT";
                case PokerHand.Flush: return "FLUSH";
                case PokerHand.FullHouse: return "FULL HOUSE";
                case PokerHand.FourOfAKind: return "FOUR CARD";
                case PokerHand.StraightFlush: return "ST. FLUSH";
                case PokerHand.RoyalStraightFlush: return "ROYAL";
                default: return "HIGH";
            }
        }

        public static string KoreanName(PokerHand hand)
        {
            switch (hand)
            {
                case PokerHand.OnePair: return "원페어";
                case PokerHand.TwoPair: return "투페어";
                case PokerHand.ThreeOfAKind: return "트리플";
                case PokerHand.Straight: return "스트레이트";
                case PokerHand.Flush: return "플러시";
                case PokerHand.FullHouse: return "풀하우스";
                case PokerHand.FourOfAKind: return "포카드";
                case PokerHand.StraightFlush: return "스트레이트 플러시";
                case PokerHand.RoyalStraightFlush: return "로열 스트레이트 플러시";
                default: return "하이";
            }
        }
    }
}
