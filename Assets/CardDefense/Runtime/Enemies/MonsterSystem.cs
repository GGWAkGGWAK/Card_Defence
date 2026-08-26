using System.Collections.Generic;
using CardDefense.Core;
using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class MonsterSystem : MonoBehaviour
    {
        private readonly List<Monster> active = new List<Monster>(128);

        public int ActiveCount => active.Count;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            int index = 0;
            while (index < active.Count)
            {
                Monster current = active[index];
                current.Simulate(deltaTime);
                if (index < active.Count && ReferenceEquals(active[index], current)) index++;
            }
        }

        public void Register(Monster monster)
        {
            if (monster == null || monster.SystemIndex >= 0) return;
            monster.SystemIndex = active.Count;
            active.Add(monster);
        }

        public void Unregister(Monster monster)
        {
            if (monster == null) return;
            int index = monster.SystemIndex;
            int last = active.Count - 1;
            if (index < 0 || index > last) return;

            Monster moved = active[last];
            active[index] = moved;
            moved.SystemIndex = index;
            active.RemoveAt(last);
            monster.SystemIndex = -1;
        }

        public Monster FindClosest(Vector3 position, float range)
        {
            return FindClosestExcluding(position, range, null, null, null, null);
        }

        public Monster FindClosestExcluding(Vector3 position, float range, Monster excludedA,
            Monster excludedB, Monster excludedC, Monster excludedD)
        {
            float bestDistanceSq = range * range;
            Monster best = null;

            for (int i = 0; i < active.Count; i++)
            {
                Monster candidate = active[i];
                if (!candidate.IsAlive || candidate == excludedA || candidate == excludedB ||
                    candidate == excludedC || candidate == excludedD) continue;
                float distanceSq = (candidate.transform.position - position).sqrMagnitude;
                if (distanceSq >= bestDistanceSq) continue;
                bestDistanceSq = distanceSq;
                best = candidate;
            }

            return best;
        }

        public void DamageInRadius(Vector3 center, float radius, float damage, Monster excluded)
        {
            if (radius <= 0f || damage <= 0f) return;
            float radiusSq = radius * radius;
            int index = 0;
            while (index < active.Count)
            {
                Monster candidate = active[index];
                if (candidate != excluded && candidate.IsAlive &&
                    (candidate.transform.position - center).sqrMagnitude <= radiusSq)
                {
                    candidate.TakeDamage(damage);
                }
                if (index < active.Count && ReferenceEquals(active[index], candidate)) index++;
            }
        }

        public bool TryGetBossHealth(out float health, out float maxHealth)
        {
            for (int i = 0; i < active.Count; i++)
            {
                Monster monster = active[i];
                if (!monster.IsAlive || monster.Archetype != MonsterArchetype.Boss) continue;
                health = monster.Health;
                maxHealth = monster.MaxHealth;
                return true;
            }
            health = 0f;
            maxHealth = 0f;
            return false;
        }

        public List<MonsterSnapshot> CaptureMonsters()
        {
            List<MonsterSnapshot> snapshots = new List<MonsterSnapshot>(active.Count);
            for (int i = 0; i < active.Count; i++)
                if (active[i].IsAlive) snapshots.Add(active[i].CaptureSnapshot());
            return snapshots;
        }
    }
}
