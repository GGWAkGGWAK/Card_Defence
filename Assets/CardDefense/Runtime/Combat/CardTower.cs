using CardDefense.Cards;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CardTower : MonoBehaviour
    {
        public int SystemIndex { get; set; } = -1;
        public PlayingCard Card { get; private set; }

        private MonsterSystem monsters;
        private Monster target;
        private float damage;
        private float range;
        private float attackInterval;
        private float targetRefreshInterval;
        private float attackTimer;
        private float targetTimer;

        public void Activate(PlayingCard card, MonsterSystem monsterSystem, GameBalanceConfig config)
        {
            Card = card;
            monsters = monsterSystem;
            range = config.towerRange;
            attackInterval = config.towerAttackInterval;
            targetRefreshInterval = config.targetRefreshInterval;
            damage = config.baseTowerDamage * (0.8f + ((int)card.Rank * 0.1f));
            attackTimer = Random.Range(0f, attackInterval);
            targetTimer = Random.Range(0f, targetRefreshInterval);
            target = null;
            gameObject.SetActive(true);

            PrototypeVisual visual = GetComponent<PrototypeVisual>();
            if (visual != null) visual.SetCard(card);
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
            target.TakeDamage(damage);
            attackTimer = attackInterval;
        }

        private bool IsValidTarget(Monster candidate)
        {
            return candidate != null && candidate.IsAlive &&
                   (candidate.transform.position - transform.position).sqrMagnitude <= range * range;
        }
    }
}
