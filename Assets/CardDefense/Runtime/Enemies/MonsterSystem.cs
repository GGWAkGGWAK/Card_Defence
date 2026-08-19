using System.Collections.Generic;
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
            float bestDistanceSq = range * range;
            Monster best = null;

            for (int i = 0; i < active.Count; i++)
            {
                Monster candidate = active[i];
                if (!candidate.IsAlive) continue;
                float distanceSq = (candidate.transform.position - position).sqrMagnitude;
                if (distanceSq >= bestDistanceSq) continue;
                bestDistanceSq = distanceSq;
                best = candidate;
            }

            return best;
        }
    }
}
