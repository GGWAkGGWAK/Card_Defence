using System;
using CardDefense.Core;
using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class Monster : MonoBehaviour
    {
        public int SystemIndex { get; set; } = -1;
        public bool IsAlive { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public MonsterArchetype Archetype { get; private set; }
        public bool IsDying { get; private set; }

        private LoopPath path;
        private float moveSpeed;
        private float progress;
        private int reward;
        private Action<Monster, bool, int> releaseCallback;
        private MonsterHealthBar healthBar;
        private PrototypeVisual visual;
        private float deathAnimationTimer;

        public void Spawn(LoopPath loopPath, MonsterArchetype archetype, float maxHealth,
            float speed, int killReward,
            Action<Monster, bool, int> callback)
        {
            path = loopPath;
            Health = maxHealth;
            MaxHealth = maxHealth;
            Archetype = archetype;
            moveSpeed = speed;
            reward = killReward;
            releaseCallback = callback;
            progress = 0f;
            IsAlive = true;
            IsDying = false;
            deathAnimationTimer = 0f;
            transform.position = path.GetPosition(0f);
            gameObject.SetActive(true);

            visual = GetComponent<PrototypeVisual>();
            if (visual != null) visual.SetMonsterStyle(archetype);
            if (healthBar == null)
            {
                healthBar = GetComponent<MonsterHealthBar>();
                if (healthBar == null) healthBar = gameObject.AddComponent<MonsterHealthBar>();
            }
            healthBar.Show(archetype);
        }

        public void Simulate(float deltaTime)
        {
            if (IsDying)
            {
                deathAnimationTimer -= deltaTime;
                if (deathAnimationTimer <= 0f) CompleteRelease(true);
                return;
            }
            if (!IsAlive || path == null || path.Length <= 0f) return;
            Vector3 previousPosition = transform.position;
            progress += (moveSpeed / path.Length) * deltaTime;
            transform.position = path.GetPosition(progress);
            if (visual != null)
                visual.SetMonsterMoveDirection(transform.position - previousPosition);
        }

        public MonsterSnapshot CaptureSnapshot()
        {
            return new MonsterSnapshot
            {
                Archetype = Archetype,
                Health = Health,
                MaxHealth = MaxHealth,
                MoveSpeed = moveSpeed,
                Progress = progress - Mathf.Floor(progress),
                Reward = reward
            };
        }

        public void Restore(LoopPath loopPath, MonsterSnapshot snapshot,
            Action<Monster, bool, int> callback)
        {
            Spawn(loopPath, snapshot.Archetype, Mathf.Max(1f, snapshot.MaxHealth),
                Mathf.Max(0.01f, snapshot.MoveSpeed), Mathf.Max(0, snapshot.Reward), callback);
            Health = Mathf.Clamp(snapshot.Health, 0.01f, MaxHealth);
            progress = snapshot.Progress - Mathf.Floor(snapshot.Progress);
            transform.position = path.GetPosition(progress);
            if (healthBar != null) healthBar.SetHealth(Health / MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health -= amount;
            if (visual != null) visual.FlashHit();
            if (healthBar != null) healthBar.SetHealth(Health / MaxHealth);
            if (Health <= 0f) BeginDeath();
        }

        public void Enrage(float healthBonus, float speedBonus)
        {
            if (!IsAlive) return;
            float previousMaxHealth = MaxHealth;
            MaxHealth *= 1f + Mathf.Clamp(healthBonus, 0f, 1f);
            Health = Mathf.Min(MaxHealth, Health + MaxHealth - previousMaxHealth);
            moveSpeed *= 1f + Mathf.Clamp(speedBonus, 0f, 1f);
            if (healthBar != null) healthBar.SetHealth(Health / MaxHealth);
            if (visual != null) visual.FlashHit();
        }

        public void ForceDespawn()
        {
            if (IsAlive || IsDying) CompleteRelease(false);
        }

        private void BeginDeath()
        {
            IsAlive = false;
            IsDying = true;
            if (healthBar != null) healthBar.Hide();
            deathAnimationTimer = visual != null ? visual.PlayMonsterDeath() : 0f;
            if (deathAnimationTimer <= 0f) CompleteRelease(true);
        }

        private void CompleteRelease(bool defeated)
        {
            IsAlive = false;
            IsDying = false;
            deathAnimationTimer = 0f;
            if (healthBar != null) healthBar.Hide();
            releaseCallback?.Invoke(this, defeated, defeated ? reward : 0);
            releaseCallback = null;
        }
    }
}
