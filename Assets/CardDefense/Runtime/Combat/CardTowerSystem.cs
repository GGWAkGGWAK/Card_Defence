using System.Collections.Generic;
using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CardTowerSystem : MonoBehaviour
    {
        private readonly List<CardTower> towers = new List<CardTower>(64);

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
    }
}
