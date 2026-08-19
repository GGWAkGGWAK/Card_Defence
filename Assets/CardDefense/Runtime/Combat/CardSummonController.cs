using System;
using System.Collections.Generic;
using CardDefense.Cards;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Combat
{
    public sealed class CardSummonController : MonoBehaviour
    {
        public event Action<string> MessageChanged;

        private readonly Queue<CardTower> available = new Queue<CardTower>(32);
        private CardTower prefab;
        private Transform[] slots;
        private bool[] occupied;
        private EconomyService economy;
        private MonsterSystem monsters;
        private CardTowerSystem towers;
        private GameBalanceConfig config;
        private Transform poolRoot;

        public void Configure(CardTower towerPrefab, Transform[] placementSlots, EconomyService economyService,
            MonsterSystem monsterSystem, CardTowerSystem towerSystem, GameBalanceConfig balance)
        {
            prefab = towerPrefab;
            slots = placementSlots;
            occupied = new bool[slots.Length];
            economy = economyService;
            monsters = monsterSystem;
            towers = towerSystem;
            config = balance;

            GameObject root = new GameObject("CardTowerPool_Inactive");
            poolRoot = root.transform;
            poolRoot.SetParent(transform, false);
            for (int i = 0; i < config.towerPrewarmCount; i++) available.Enqueue(CreateTower());
        }

        public void SummonFirstAvailable()
        {
            if (config == null || economy == null) return;

            int slotIndex = FindFirstFreeSlot();
            if (slotIndex < 0)
            {
                MessageChanged?.Invoke("배치 공간이 가득 찼습니다");
                return;
            }

            if (!economy.TrySpend(config.summonCost))
            {
                MessageChanged?.Invoke("골드가 부족합니다");
                return;
            }

            PlayingCard card = new PlayingCard(
                (CardSuit)UnityEngine.Random.Range(0, 4),
                (CardRank)UnityEngine.Random.Range(2, 15));

            CardTower tower = available.Count > 0 ? available.Dequeue() : CreateTower();
            tower.transform.SetParent(null, true);
            tower.transform.position = slots[slotIndex].position;
            tower.Activate(card, monsters, config);
            towers.Register(tower);
            occupied[slotIndex] = true;
            MessageChanged?.Invoke("카드 소환: " + card.Rank + " / " + card.Suit);
        }

        private int FindFirstFreeSlot()
        {
            for (int i = 0; i < occupied.Length; i++)
            {
                if (!occupied[i]) return i;
            }
            return -1;
        }

        private CardTower CreateTower()
        {
            CardTower instance = Instantiate(prefab, poolRoot);
            instance.name = "CardTower_Pooled";
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
