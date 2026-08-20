using CardDefense.Cards;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CardTower : MonoBehaviour
    {
        public int SystemIndex { get; set; } = -1;
        public int SlotIndex { get; private set; } = -1;
        public PlayingCard Card { get; private set; }
        public PokerHand Hand { get; private set; }
        public bool IsFusionResult { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsDragging { get; private set; }
        public float CurrentRange => range;
        public float CurrentAttackInterval => attackInterval;
        public float CurrentDamage => baseDamage * progression.GetDamageMultiplier(Hand);
        public float EstimatedDps => CurrentDamage * profile.ExpectedDamageMultiplier / attackInterval;
        public string CombatTrait => profile.KoreanTrait;
        public int FusionCoreCardCount { get; private set; }

        private MonsterSystem monsters;
        private Monster target;
        private float range;
        private float attackInterval;
        private float targetRefreshInterval;
        private float attackTimer;
        private float targetTimer;
        private float baseDamage;
        private PokerProgressionService progression;
        private PrototypeVisual visual;
        private PokerHandCombatProfile profile;
        private CombatEffectSystem effects;

        public void Activate(PlayingCard card, PokerHand hand, bool isFusionResult, int slotIndex,
            MonsterSystem monsterSystem,
            PokerProgressionService progressionService, GameBalanceConfig config,
            CombatEffectSystem effectSystem, float fusionBaseDamage, int fusionCoreCardCount)
        {
            Card = card;
            Hand = hand;
            IsFusionResult = isFusionResult;
            SlotIndex = slotIndex;
            monsters = monsterSystem;
            progression = progressionService;
            effects = effectSystem;
            FusionCoreCardCount = isFusionResult ? fusionCoreCardCount : 1;
            profile = PokerHandCombatProfile.Get(hand);
            range = config.towerRange * profile.RangeMultiplier;
            attackInterval = config.towerAttackInterval * profile.AttackIntervalMultiplier;
            targetRefreshInterval = config.targetRefreshInterval;
            baseDamage = isFusionResult
                ? fusionBaseDamage
                : PokerCombatMath.BaseDamage(config, card.Rank);
            attackTimer = Random.Range(0f, attackInterval);
            targetTimer = Random.Range(0f, targetRefreshInterval);
            target = null;
            gameObject.SetActive(true);

            IsSelected = false;
            IsDragging = false;
            visual = GetComponent<PrototypeVisual>();
            if (visual != null) visual.SetCard(card, hand, isFusionResult);
        }

        public void Simulate(float deltaTime)
        {
            if (IsDragging) return;
            targetTimer -= deltaTime;
            attackTimer -= deltaTime;

            if (targetTimer <= 0f)
            {
                if (!IsValidTarget(target)) target = monsters.FindClosest(transform.position, range);
                targetTimer = targetRefreshInterval;
            }

            if (attackTimer > 0f || !IsValidTarget(target)) return;
            AttackTargets();
            attackTimer = attackInterval;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (visual != null) visual.SetSelected(selected);
        }

        public void SetDragging(bool dragging)
        {
            IsDragging = dragging;
        }

        public void MoveToSlot(int slotIndex, Vector3 position)
        {
            SlotIndex = slotIndex;
            transform.position = position;
        }

        public void Deactivate()
        {
            target = null;
            monsters = null;
            progression = null;
            effects = null;
            SlotIndex = -1;
            IsFusionResult = false;
            FusionCoreCardCount = 0;
            SystemIndex = -1;
            IsSelected = false;
            IsDragging = false;
            gameObject.SetActive(false);
        }

        private bool IsValidTarget(Monster candidate)
        {
            return candidate != null && candidate.IsAlive &&
                   (candidate.transform.position - transform.position).sqrMagnitude <= range * range;
        }

        private void AttackTargets()
        {
            Monster first = null;
            Monster second = null;
            Monster third = null;
            Monster fourth = null;
            float damage = CurrentDamage;

            for (int i = 0; i < profile.TargetCount; i++)
            {
                Monster hit = i == 0 && IsValidTarget(target)
                    ? target
                    : monsters.FindClosestExcluding(transform.position, range, first, second, third, fourth);
                if (hit == null) break;
                bool critical = profile.CriticalChance > 0f && Random.value < profile.CriticalChance;
                float dealtDamage = damage;
                if (hit.Archetype == MonsterArchetype.Tank || hit.Archetype == MonsterArchetype.Boss)
                    dealtDamage *= profile.HeavyTargetDamageMultiplier;
                if (critical) dealtDamage *= profile.CriticalDamageMultiplier;

                Vector3 hitPosition = hit.transform.position;
                hit.TakeDamage(dealtDamage);
                if (profile.SplashRadius > 0f)
                    monsters.DamageInRadius(hitPosition, profile.SplashRadius,
                        damage * profile.SplashDamageMultiplier, hit);
                if (effects != null) effects.PlayBeam(transform.position, hitPosition, critical);

                if (i == 0) first = hit;
                else if (i == 1) second = hit;
                else if (i == 2) third = hit;
                else fourth = hit;
            }
        }
    }
}
