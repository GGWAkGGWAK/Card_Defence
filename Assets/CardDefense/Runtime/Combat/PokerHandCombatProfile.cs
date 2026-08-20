using CardDefense.Cards;

namespace CardDefense.Combat
{
    public readonly struct PokerHandCombatProfile
    {
        public readonly float AttackIntervalMultiplier;
        public readonly float RangeMultiplier;
        public readonly int TargetCount;
        public readonly float SplashRadius;
        public readonly float SplashDamageMultiplier;
        public readonly float HeavyTargetDamageMultiplier;
        public readonly float CriticalChance;
        public readonly float CriticalDamageMultiplier;
        public readonly string KoreanTrait;

        private PokerHandCombatProfile(float attackIntervalMultiplier, float rangeMultiplier,
            int targetCount, float splashRadius, float splashDamageMultiplier,
            float heavyTargetDamageMultiplier, float criticalChance, float criticalDamageMultiplier,
            string koreanTrait)
        {
            AttackIntervalMultiplier = attackIntervalMultiplier;
            RangeMultiplier = rangeMultiplier;
            TargetCount = targetCount;
            SplashRadius = splashRadius;
            SplashDamageMultiplier = splashDamageMultiplier;
            HeavyTargetDamageMultiplier = heavyTargetDamageMultiplier;
            CriticalChance = criticalChance;
            CriticalDamageMultiplier = criticalDamageMultiplier;
            KoreanTrait = koreanTrait;
        }

        public static PokerHandCombatProfile Get(PokerHand hand)
        {
            switch (hand)
            {
                case PokerHand.OnePair:
                    return new PokerHandCombatProfile(0.72f, 1f, 1, 0f, 0f, 1f, 0f, 1f, "속사");
                case PokerHand.TwoPair:
                    return new PokerHandCombatProfile(1f, 1.3f, 1, 0f, 0f, 1f, 0f, 1f, "장거리");
                case PokerHand.ThreeOfAKind:
                    return new PokerHandCombatProfile(1f, 1f, 1, 0f, 0f, 1.8f, 0f, 1f, "탱커·보스 추가 피해");
                case PokerHand.Straight:
                    return new PokerHandCombatProfile(1f, 1.05f, 2, 0f, 0f, 1f, 0f, 1f, "2연쇄 공격");
                case PokerHand.Flush:
                    return new PokerHandCombatProfile(1f, 1f, 1, 0.9f, 0.45f, 1f, 0f, 1f, "범위 공격");
                case PokerHand.FullHouse:
                    return new PokerHandCombatProfile(0.9f, 1.05f, 2, 0.75f, 0.35f, 1f, 0f, 1f, "2연쇄+범위");
                case PokerHand.FourOfAKind:
                    return new PokerHandCombatProfile(0.9f, 1.1f, 1, 0f, 0f, 1f, 0.35f, 2.2f, "35% 치명타");
                case PokerHand.StraightFlush:
                    return new PokerHandCombatProfile(0.82f, 1.15f, 3, 0.8f, 0.4f, 1f, 0f, 1f, "3연쇄+범위");
                case PokerHand.RoyalStraightFlush:
                    return new PokerHandCombatProfile(0.7f, 1.25f, 4, 1f, 0.5f, 1f, 0.25f, 2f, "4연쇄+범위+치명타");
                default:
                    return new PokerHandCombatProfile(1f, 1f, 1, 0f, 0f, 1f, 0f, 1f, "단일 공격");
            }
        }

        public float ExpectedDamageMultiplier
        {
            get
            {
                float direct = TargetCount;
                float splashEstimate = SplashRadius > 0f ? SplashDamageMultiplier : 0f;
                float critical = 1f + CriticalChance * (CriticalDamageMultiplier - 1f);
                return (direct + splashEstimate) * critical;
            }
        }
    }
}
