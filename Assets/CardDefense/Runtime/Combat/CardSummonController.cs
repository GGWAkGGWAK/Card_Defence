using System;
using System.Collections.Generic;
using CardDefense.Cards;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardDefense.Combat
{
    public sealed class CardSummonController : MonoBehaviour
    {
        public event Action<string> MessageChanged;
        public event Action SelectionChanged;

        public int SelectedCount => selected.Count;
        public PokerHand SelectedHand => focusedTower != null ? focusedTower.Hand : PokerHand.High;
        public bool CanUpgradeSelection => focusedTower != null;
        public bool CanMergeSelection => selected.Count == 5 && !SelectedCardsContainExactDuplicate();
        public bool IsPlacementPending { get; private set; }

        private readonly Queue<CardTower> available = new Queue<CardTower>(32);
        private readonly List<CardTower> selected = new List<CardTower>(5);
        private CardTower prefab;
        private Transform[] slots;
        private CardTower[] placedBySlot;
        private EconomyService economy;
        private MonsterSystem monsters;
        private CardTowerSystem towers;
        private PokerProgressionService progression;
        private GameBalanceConfig config;
        private Transform poolRoot;
        private Camera mainCamera;
        private CardTower focusedTower;

        public void Configure(CardTower towerPrefab, Transform[] placementSlots, EconomyService economyService,
            MonsterSystem monsterSystem, CardTowerSystem towerSystem, PokerProgressionService progressionService,
            GameBalanceConfig balance)
        {
            prefab = towerPrefab;
            slots = placementSlots;
            placedBySlot = new CardTower[slots.Length];
            economy = economyService;
            monsters = monsterSystem;
            towers = towerSystem;
            progression = progressionService;
            config = balance;
            mainCamera = Camera.main;

            GameObject root = new GameObject("CardTowerPool_Inactive");
            poolRoot = root.transform;
            poolRoot.SetParent(transform, false);
            for (int i = 0; i < config.towerPrewarmCount; i++) available.Enqueue(CreateTower());
        }

        private void Update()
        {
            if (!TryGetPointerDown(out Vector2 screenPosition) || IsPointerOverUi()) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
            world.z = 0f;
            if (IsPlacementPending)
            {
                TryPlaceAt(world);
                return;
            }
            CardTower tower = towers.FindClosest(world, config.towerSelectionRadius);
            if (tower != null) ToggleSelection(tower);
        }

        public void BeginSummonPlacement()
        {
            if (FindFirstFreeSlot() < 0)
            {
                MessageChanged?.Invoke("배치 공간이 가득 찼습니다. 카드를 합성하세요");
                return;
            }
            if (economy.Gold < config.summonCost)
            {
                MessageChanged?.Invoke("골드가 부족합니다");
                return;
            }
            IsPlacementPending = true;
            MessageChanged?.Invoke("소환할 빈 슬롯을 선택하세요 (" + config.summonCost + "G)");
            SelectionChanged?.Invoke();
        }

        public void SummonFirstAvailable()
        {
            if (config == null || economy == null) return;
            int slotIndex = FindFirstFreeSlot();
            if (slotIndex < 0)
            {
                MessageChanged?.Invoke("배치 공간이 가득 찼습니다. 카드를 합성하세요");
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
            SpawnTower(card, PokerHand.High, false, slotIndex);
            MessageChanged?.Invoke("카드 소환: " + card.Rank + " / " + card.Suit);
        }

        public void MergeSelected()
        {
            if (selected.Count != 5)
            {
                MessageChanged?.Invoke("합성할 카드 5장을 선택하세요 (" + selected.Count + "/5)");
                return;
            }

            if (SelectedCardsContainExactDuplicate())
            {
                MessageChanged?.Invoke("합성 불가: 무늬와 숫자가 같은 카드가 중복되었습니다");
                SelectionChanged?.Invoke();
                return;
            }

            PlayingCard[] cards = new PlayingCard[5];
            int outputSlot = selected[0].SlotIndex;
            PlayingCard representative = selected[0].Card;
            for (int i = 0; i < selected.Count; i++)
            {
                cards[i] = selected[i].Card;
                if ((int)selected[i].Card.Rank > (int)representative.Rank) representative = selected[i].Card;
            }

            PokerHand evaluated = PokerHandEvaluator.Evaluate(cards);
            for (int i = selected.Count - 1; i >= 0; i--) ReleaseTower(selected[i]);
            selected.Clear();
            CardTower resultTower = SpawnTower(representative, evaluated, true, outputSlot);
            focusedTower = resultTower;
            resultTower.SetSelected(true);
            SelectionChanged?.Invoke();
            MessageChanged?.Invoke("합성 성공: " + PokerHandInfo.KoreanName(evaluated) + " (재합성 불가)");
        }

        public void UpgradeSelectedHand()
        {
            if (focusedTower == null)
            {
                MessageChanged?.Invoke("강화할 족보의 카드를 선택하세요");
                return;
            }
            PokerHand hand = SelectedHand;
            int cost = progression.GetUpgradeCost(hand);
            if (!progression.TryUpgrade(hand))
            {
                MessageChanged?.Invoke("강화 골드 부족: " + cost + "G 필요");
                return;
            }
            MessageChanged?.Invoke(PokerHandInfo.KoreanName(hand) + " 강화 Lv." + progression.GetLevel(hand));
            SelectionChanged?.Invoke();
        }

        public string GetSelectionSummary()
        {
            if (IsPlacementPending) return "배치 모드  |  빛나는 빈 슬롯을 선택";
            if (focusedTower != null && focusedTower.IsFusionResult)
            {
                PokerHand completedHand = focusedTower.Hand;
                return "합성 완료 · 재합성 불가  |  " + PokerHandInfo.KoreanName(completedHand) +
                       " Lv." + progression.GetLevel(completedHand) + "  |  강화 " +
                       progression.GetUpgradeCost(completedHand) + "G";
            }
            if (selected.Count == 0) return "카드를 터치해 선택";
            PokerHand hand = SelectedHand;
            string preview = selected.Count == 5
                ? (SelectedCardsContainExactDuplicate()
                    ? "  |  합성 불가: 동일 카드 중복"
                    : "  |  예상 " + PokerHandInfo.KoreanName(GetMergePreviewHand()))
                : string.Empty;
            return "선택 " + selected.Count + "/5  |  " + PokerHandInfo.KoreanName(hand) +
                   " Lv." + progression.GetLevel(hand) + "  |  강화 " + progression.GetUpgradeCost(hand) + "G" + preview;
        }

        public PokerHand GetMergePreviewHand()
        {
            if (selected.Count != 5) return PokerHand.High;
            PlayingCard[] cards = new PlayingCard[5];
            for (int i = 0; i < selected.Count; i++)
            {
                cards[i] = selected[i].Card;
            }
            return PokerHandEvaluator.Evaluate(cards);
        }

        public void ToggleSelection(CardTower tower)
        {
            if (tower == null || tower.SystemIndex < 0) return;
            if (tower.IsFusionResult)
            {
                ClearMergeSelection();
                if (focusedTower != null && focusedTower != tower) focusedTower.SetSelected(false);
                focusedTower = tower;
                tower.SetSelected(true);
                MessageChanged?.Invoke("합성 완료 타워: 강화만 가능합니다");
                SelectionChanged?.Invoke();
                return;
            }
            if (focusedTower != null && focusedTower.IsFusionResult)
            {
                focusedTower.SetSelected(false);
                focusedTower = null;
            }
            int index = selected.IndexOf(tower);
            if (index >= 0)
            {
                selected.RemoveAt(index);
                tower.SetSelected(false);
                if (focusedTower == tower) focusedTower = selected.Count > 0 ? selected[selected.Count - 1] : null;
            }
            else
            {
                if (selected.Count >= 5)
                {
                    MessageChanged?.Invoke("최대 5장까지 선택할 수 있습니다");
                    return;
                }
                selected.Add(tower);
                focusedTower = tower;
                tower.SetSelected(true);
            }
            SelectionChanged?.Invoke();
        }

        private CardTower SpawnTower(PlayingCard card, PokerHand hand, bool isFusionResult, int slotIndex)
        {
            CardTower tower = available.Count > 0 ? available.Dequeue() : CreateTower();
            tower.transform.SetParent(null, true);
            tower.transform.position = slots[slotIndex].position;
            tower.Activate(card, hand, isFusionResult, slotIndex, monsters, progression, config);
            towers.Register(tower);
            placedBySlot[slotIndex] = tower;
            return tower;
        }

        private void TryPlaceAt(Vector3 worldPosition)
        {
            int slotIndex = FindClosestSlot(worldPosition, config.towerSelectionRadius * 1.35f);
            if (slotIndex < 0)
            {
                MessageChanged?.Invoke("빈 슬롯의 중앙을 선택하세요");
                return;
            }
            if (!economy.TrySpend(config.summonCost))
            {
                IsPlacementPending = false;
                MessageChanged?.Invoke("골드가 부족합니다");
                SelectionChanged?.Invoke();
                return;
            }
            PlayingCard card = new PlayingCard(
                (CardSuit)UnityEngine.Random.Range(0, 4),
                (CardRank)UnityEngine.Random.Range(2, 15));
            SpawnTower(card, PokerHand.High, false, slotIndex);
            IsPlacementPending = false;
            MessageChanged?.Invoke("카드 배치: " + card.Rank + " / " + card.Suit);
            SelectionChanged?.Invoke();
        }

        private int FindClosestSlot(Vector3 position, float radius)
        {
            float bestDistance = radius * radius;
            int bestIndex = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (placedBySlot[i] != null) continue;
                float distance = (slots[i].position - position).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                bestIndex = i;
            }
            return bestIndex;
        }

        private void ClearMergeSelection()
        {
            for (int i = 0; i < selected.Count; i++) selected[i].SetSelected(false);
            selected.Clear();
        }

        private bool SelectedCardsContainExactDuplicate()
        {
            for (int left = 0; left < selected.Count - 1; left++)
            {
                PlayingCard first = selected[left].Card;
                for (int right = left + 1; right < selected.Count; right++)
                {
                    PlayingCard second = selected[right].Card;
                    if (PokerMergeRules.AreExactMatch(first, second)) return true;
                }
            }
            return false;
        }

        private void ReleaseTower(CardTower tower)
        {
            if (tower == null) return;
            int slotIndex = tower.SlotIndex;
            if (slotIndex >= 0 && slotIndex < placedBySlot.Length) placedBySlot[slotIndex] = null;
            towers.Unregister(tower);
            tower.Deactivate();
            tower.transform.SetParent(poolRoot, false);
            available.Enqueue(tower);
        }

        private int FindFirstFreeSlot()
        {
            for (int i = 0; i < placedBySlot.Length; i++)
                if (placedBySlot[i] == null) return i;
            return -1;
        }

        private CardTower CreateTower()
        {
            CardTower instance = Instantiate(prefab, poolRoot);
            instance.name = "CardTower_Pooled";
            instance.gameObject.SetActive(false);
            return instance;
        }

        private static bool TryGetPointerDown(out Vector2 position)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                position = Input.GetTouch(0).position;
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
            position = default;
            return false;
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
