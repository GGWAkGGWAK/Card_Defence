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

        private LoopPath path;
        private float moveSpeed;
        private float progress;
        private int reward;
        private Action<Monster, bool, int> releaseCallback;

        public void Spawn(LoopPath loopPath, float maxHealth, float speed, int killReward,
            Action<Monster, bool, int> callback)
        {
            path = loopPath;
            Health = maxHealth;
            moveSpeed = speed;
            reward = killReward;
            releaseCallback = callback;
            progress = 0f;
            IsAlive = true;
            transform.position = path.GetPosition(0f);
            gameObject.SetActive(true);

            PrototypeVisual visual = GetComponent<PrototypeVisual>();
            if (visual != null) visual.SetMonsterStyle();
        }

        public void Simulate(float deltaTime)
        {
            if (!IsAlive || path == null || path.Length <= 0f) return;
            progress += (moveSpeed / path.Length) * deltaTime;
            transform.position = path.GetPosition(progress);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            Health -= amount;
            if (Health <= 0f) RequestRelease(true);
        }

        public void ForceDespawn()
        {
            if (IsAlive) RequestRelease(false);
        }

        private void RequestRelease(bool defeated)
        {
            IsAlive = false;
            releaseCallback?.Invoke(this, defeated, defeated ? reward : 0);
            releaseCallback = null;
        }
    }
}
