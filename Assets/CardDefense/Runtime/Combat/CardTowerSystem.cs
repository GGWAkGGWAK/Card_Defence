using System.Collections.Generic;
using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CardTowerSystem : MonoBehaviour
    {
        private readonly List<CardTower> towers = new List<CardTower>(64);
        public int ActiveCount => towers.Count;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < towers.Count; i++) towers[i].Simulate(deltaTime);
        }

        public void Register(CardTower tower)
        {
            if (tower == null || tower.SystemIndex >= 0) return;
            tower.SystemIndex = towers.Count;
            towers.Add(tower);
        }

        public void Unregister(CardTower tower)
        {
            if (tower == null || tower.SystemIndex < 0 || tower.SystemIndex >= towers.Count) return;
            int removedIndex = tower.SystemIndex;
            int lastIndex = towers.Count - 1;
            if (removedIndex != lastIndex)
            {
                CardTower moved = towers[lastIndex];
                towers[removedIndex] = moved;
                moved.SystemIndex = removedIndex;
            }
            towers.RemoveAt(lastIndex);
            tower.SystemIndex = -1;
        }

        public CardTower FindClosest(Vector3 position, float radius)
        {
            float bestDistance = radius * radius;
            CardTower best = null;
            for (int i = 0; i < towers.Count; i++)
            {
                float distance = (towers[i].transform.position - position).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = towers[i];
            }
            return best;
        }
    }
}
