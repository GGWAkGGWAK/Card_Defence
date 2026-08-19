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

        public void Activate(PlayingCard card, PokerHand hand, bool isFusionResult, int slotIndex,
            MonsterSystem monsterSystem,
            PokerProgressionService progressionService, GameBalanceConfig config)
        {
            Card = card;
            Hand = hand;
            IsFusionResult = isFusionResult;
            SlotIndex = slotIndex;
            monsters = monsterSystem;
            progression = progressionService;
            range = config.towerRange;
            attackInterval = config.towerAttackInterval;
            targetRefreshInterval = config.targetRefreshInterval;
            baseDamage = config.baseTowerDamage * (0.8f + ((int)card.Rank * 0.1f));
            attackTimer = Random.Range(0f, attackInterval);
            targetTimer = Random.Range(0f, targetRefreshInterval);
            target = null;
            gameObject.SetActive(true);

            IsSelected = false;
            visual = GetComponent<PrototypeVisual>();
            if (visual != null) visual.SetCard(card, hand, isFusionResult);
        }

        public void Simulate(float deltaTime)
        {
            targetTimer -= deltaTime;
            attackTimer -= deltaTime;

            if (targetTimer <= 0f)
            {
                if (!IsValidTarget(target)) target = monsters.FindClosest(transform.position, range);
                targetTimer = targetRefreshInterval;
            }

            if (attackTimer > 0f || !IsValidTarget(target)) return;
            float damage = baseDamage * progression.GetDamageMultiplier(Hand);
            target.TakeDamage(damage);
            attackTimer = attackInterval;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (visual != null) visual.SetSelected(selected);
        }

        public void Deactivate()
        {
            target = null;
            monsters = null;
            progression = null;
            SlotIndex = -1;
            IsFusionResult = false;
            SystemIndex = -1;
            IsSelected = false;
            gameObject.SetActive(false);
        }

        private bool IsValidTarget(Monster candidate)
        {
            return candidate != null && candidate.IsAlive &&
                   (candidate.transform.position - transform.position).sqrMagnitude <= range * range;
        }
    }
}
