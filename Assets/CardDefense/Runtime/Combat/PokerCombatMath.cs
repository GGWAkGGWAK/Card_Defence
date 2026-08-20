using CardDefense.Cards;
using CardDefense.Core;
using UnityEngine;

namespace CardDefense.Combat
{
    public static class PokerCombatMath
    {
        public static float BaseDamage(GameBalanceConfig config, CardRank rank)
        {
            return config.baseTowerDamage * (0.8f + (int)rank * 0.1f);
        }

        public static float DamageMultiplier(GameBalanceConfig config, PokerHand hand, int level)
        {
            return PokerHandInfo.DamageMultiplier(hand) *
                   Mathf.Pow(1f + config.handUpgradeDamageStep, Mathf.Max(0, level));
        }

        public static float EstimatedDps(GameBalanceConfig config, float fusionBaseDamage,
            PokerHand hand, int level)
        {
            PokerHandCombatProfile profile = PokerHandCombatProfile.Get(hand);
            float interval = config.towerAttackInterval * profile.AttackIntervalMultiplier;
            float damage = fusionBaseDamage * DamageMultiplier(config, hand, level);
            return damage * profile.ExpectedDamageMultiplier / Mathf.Max(0.01f, interval);
        }
    }
}
