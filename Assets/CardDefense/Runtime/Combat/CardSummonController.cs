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
        public event Action CardSummoned;
        public event Action<PokerHand> CardsMerged;

        public int SelectedCount => selected.Count;
        public PokerHand SelectedHand => focusedTower != null ? focusedTower.Hand : PokerHand.High;
        public bool CanUpgradeSelection => focusedTower != null;
        public bool CanSellSelection => focusedTower != null;
        public bool CanMergeSelection => selected.Count == 5 && !SelectedCardsContainExactDuplicate();
        public bool IsPlacementPending { get; private set; }
        public CardTower FocusedTower => focusedTower;
        public int CurrentSummonCost => CalculateSummonCost(config,
            towers != null ? towers.ActiveCount : 0,
            modifiers != null ? modifiers.SummonCostMultiplier : 1f);
        public int AffordableSummons => economy != null ? economy.Gold / Mathf.Max(1, CurrentSummonCost) : 0;

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
        private CombatEffectSystem effects;
        private RunModifierService modifiers;
        private Transform poolRoot;
        private Camera mainCamera;
        private CardTower focusedTower;
        private CardTower dragCandidate;
        private Vector2 dragStartScreen;
        private bool dragging;
        private bool dragWasSelected;

        public static int CalculateSummonCost(GameBalanceConfig balance, int occupiedCardCount,
            float discountMultiplier = 1f)
        {
            if (balance == null) return 0;
            double discountedBase = balance.summonCost * Mathf.Clamp(discountMultiplier, 0.01f, 1f);
            double occupancyGrowth = Math.Pow(balance.summonCostGrowthPerOccupiedCard,
                Mathf.Max(0, occupiedCardCount));
            return Mathf.Max(5, Mathf.CeilToInt((float)Math.Min(int.MaxValue,
                discountedBase * occupancyGrowth)));
        }

        public void Configure(CardTower towerPrefab, Transform[] placementSlots, EconomyService economyService,
            MonsterSystem monsterSystem, CardTowerSystem towerSystem, PokerProgressionService progressionService,
            GameBalanceConfig balance, CombatEffectSystem effectSystem)
        {
            prefab = towerPrefab;
            slots = placementSlots;
            placedBySlot = new CardTower[slots.Length];
            economy = economyService;
            monsters = monsterSystem;
            towers = towerSystem;
            progression = progressionService;
            config = balance;
            effects = effectSystem;
            mainCamera = Camera.main;

            GameObject root = new GameObject("CardTowerPool_Inactive");
            poolRoot = root.transform;
            poolRoot.SetParent(transform, false);
            for (int i = 0; i < config.towerPrewarmCount; i++) available.Enqueue(CreateTower());

            GameObject rangeObject = new GameObject("SelectedTowerRange");
            rangeObject.transform.SetParent(transform, false);
            rangeObject.AddComponent<TowerRangeIndicator>().Configure(this);
        }

        public void SetRunModifiers(RunModifierService modifierService)
        {
            modifiers = modifierService;
        }

        private void Update()
        {
            if (!TryGetPointerState(out Vector2 screenPosition, out bool down, out bool held, out bool up)) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
            world.z = 0f;
            if (IsPlacementPending)
            {
                if (down && !IsPointerOverUi()) TryPlaceAt(world);
                return;
            }

            if (down)
            {
                if (IsPointerOverUi()) return;
                dragCandidate = towers.FindClosest(world, config.towerSelectionRadius);
                if (dragCandidate == null) return;
                dragStartScreen = screenPosition;
                dragWasSelected = dragCandidate.IsSelected;
                dragging = false;
            }

            if (dragCandidate != null && held)
            {
                if (!dragging && (screenPosition - dragStartScreen).sqrMagnitude >=
                    config.cardDragThresholdPixels * config.cardDragThresholdPixels)
                {
                    dragging = true;
                    dragCandidate.SetDragging(true);
                    dragCandidate.SetSelected(true);
                }
                if (dragging) dragCandidate.transform.position = world;
            }

            if (dragCandidate == null || !up) return;
            CardTower releasedTower = dragCandidate;
            if (dragging)
            {
                int targetSlot = FindClosestAnySlot(world, config.towerSelectionRadius * 1.35f);
                if (!MoveTowerToSlot(releasedTower, targetSlot))
                {
                    releasedTower.MoveToSlot(releasedTower.SlotIndex, slots[releasedTower.SlotIndex].position);
                    MessageChanged?.Invoke("이동 취소: 슬롯 위에 놓으세요");
                }
                releasedTower.SetDragging(false);
                releasedTower.SetSelected(dragWasSelected);
            }
            else if (!IsPointerOverUi())
            {
                ToggleSelection(releasedTower);
            }
            dragCandidate = null;
            dragging = false;
        }

        public bool MoveTowerToSlot(CardTower tower, int targetSlotIndex)
        {
            if (tower == null || tower.SystemIndex < 0 || targetSlotIndex < 0 ||
                targetSlotIndex >= slots.Length) return false;

            int sourceSlotIndex = tower.SlotIndex;
            if (sourceSlotIndex == targetSlotIndex)
            {
                tower.MoveToSlot(sourceSlotIndex, slots[sourceSlotIndex].position);
                return true;
            }

            CardTower targetTower = placedBySlot[targetSlotIndex];
            placedBySlot[targetSlotIndex] = tower;
            tower.MoveToSlot(targetSlotIndex, slots[targetSlotIndex].position);
            if (targetTower == null)
            {
                placedBySlot[sourceSlotIndex] = null;
                MessageChanged?.Invoke("카드를 빈 슬롯으로 이동했습니다");
            }
            else
            {
                placedBySlot[sourceSlotIndex] = targetTower;
                targetTower.MoveToSlot(sourceSlotIndex, slots[sourceSlotIndex].position);
                MessageChanged?.Invoke("두 카드의 위치를 교환했습니다");
            }
            SelectionChanged?.Invoke();
            return true;
        }

        public void BeginSummonPlacement()
        {
            if (FindFirstFreeSlot() < 0)
            {
                MessageChanged?.Invoke("배치 공간이 가득 찼습니다. 카드를 합성하세요");
                return;
            }
            int summonCost = CurrentSummonCost;
            if (economy.Gold < summonCost)
            {
                MessageChanged?.Invoke("골드가 부족합니다");
                return;
            }
            IsPlacementPending = true;
            MessageChanged?.Invoke("소환할 빈 슬롯을 선택하세요 (" + summonCost + "G)");
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
            if (!economy.TrySpend(CurrentSummonCost))
            {
                MessageChanged?.Invoke("골드가 부족합니다");
                return;
            }

            PlayingCard card = new PlayingCard(
                (CardSuit)UnityEngine.Random.Range(0, 4),
                (CardRank)UnityEngine.Random.Range(2, 15));
            SpawnTower(card, PokerHand.High, false, slotIndex);
            CardSummoned?.Invoke();
            MessageChanged?.Invoke("카드 소환: " + card.Rank + " / " + card.Suit);
        }

#if UNITY_EDITOR
        public void SummonForTesting(PlayingCard card)
        {
            int slotIndex = FindFirstFreeSlot();
            if (slotIndex >= 0) SpawnTower(card, PokerHand.High, false, slotIndex);
        }
#endif

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
            for (int i = 0; i < selected.Count; i++)
            {
                cards[i] = selected[i].Card;
            }

            PokerHand evaluated = PokerHandEvaluator.Evaluate(cards);
            PokerFusionCombatResult fusion = PokerFusionCombatCalculator.Calculate(config, cards, evaluated);
            for (int i = selected.Count - 1; i >= 0; i--) ReleaseTower(selected[i]);
            selected.Clear();
            CardTower resultTower = SpawnTower(fusion.RepresentativeCard, evaluated, true, outputSlot,
                fusion.BaseDamage, fusion.CoreCardCount);
            focusedTower = resultTower;
            resultTower.SetSelected(true);
            SelectionChanged?.Invoke();
            CardsMerged?.Invoke(evaluated);
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

        public void SellFocusedTower()
        {
            if (focusedTower == null)
            {
                MessageChanged?.Invoke("판매할 카드를 선택하세요");
                return;
            }

            CardTower tower = focusedTower;
            int refund = GetSellValue(tower);
            selected.Remove(tower);
            focusedTower = selected.Count > 0 ? selected[selected.Count - 1] : null;
            ReleaseTower(tower);
            economy.AddGold(refund);
            MessageChanged?.Invoke("카드 판매: +" + refund + "G");
            SelectionChanged?.Invoke();
        }

        public int GetFocusedSellValue()
        {
            return focusedTower != null ? GetSellValue(focusedTower) : 0;
        }

        public string GetSelectionSummary()
        {
            if (IsPlacementPending) return "배치 모드  |  빛나는 빈 슬롯을 선택";
            if (focusedTower != null && focusedTower.IsFusionResult)
            {
                PokerHand completedHand = focusedTower.Hand;
                return "합성 완료 · 재합성 불가  |  " + PokerHandInfo.KoreanName(completedHand) +
                       " Lv." + progression.GetLevel(completedHand) + "  |  강화 " +
                       progression.GetUpgradeCost(completedHand) + "G  |  판매 " + GetFocusedSellValue() + "G\n" +
                       GetFocusedCombatSummary();
            }
            if (selected.Count == 0) return "카드를 터치해 선택";
            PokerHand hand = SelectedHand;
            string preview = selected.Count == 5
                ? (SelectedCardsContainExactDuplicate()
                    ? "  |  합성 불가: 동일 카드 중복"
                    : "  |  예상 " + PokerHandInfo.KoreanName(GetMergePreviewHand()))
                : string.Empty;
            return "선택 " + selected.Count + "/5  |  " + PokerHandInfo.KoreanName(hand) +
                   " Lv." + progression.GetLevel(hand) + "  |  강화 " + progression.GetUpgradeCost(hand) +
                   "G  |  판매 " + GetFocusedSellValue() + "G" + preview + "\n" + GetFocusedCombatSummary();
        }

        public string GetFocusedCombatSummary()
        {
            if (focusedTower == null) return string.Empty;
            return "공격 " + focusedTower.CurrentDamage.ToString("0.0") + "  |  DPS " +
                   focusedTower.EstimatedDps.ToString("0.0") + "  |  사거리 " +
                   focusedTower.CurrentRange.ToString("0.0") + "  |  " + focusedTower.CombatTrait +
                   (focusedTower.IsFusionResult
                       ? "  |  핵심 " + focusedTower.FusionCoreCardCount + "장·잔여 " +
                         Mathf.RoundToInt(config.discardedMaterialPowerRatio * 100f) + "%"
                       : string.Empty);
        }

        public List<CardTowerSnapshot> CaptureTowers()
        {
            List<CardTowerSnapshot> snapshots = new List<CardTowerSnapshot>(placedBySlot.Length);
            for (int i = 0; i < placedBySlot.Length; i++)
            {
                CardTower tower = placedBySlot[i];
                if (tower == null) continue;
                snapshots.Add(new CardTowerSnapshot
                {
                    SlotIndex = tower.SlotIndex,
                    Card = tower.Card,
                    Hand = tower.Hand,
                    IsFusionResult = tower.IsFusionResult,
                    BaseDamage = tower.SavedBaseDamage,
                    FusionCoreCardCount = tower.FusionCoreCardCount
                });
            }
            return snapshots;
        }

        public void RestoreTowers(List<CardTowerSnapshot> snapshots)
        {
            if (snapshots == null) return;
            for (int i = 0; i < snapshots.Count; i++)
            {
                CardTowerSnapshot snapshot = snapshots[i];
                if (snapshot.SlotIndex < 0 || snapshot.SlotIndex >= placedBySlot.Length ||
                    placedBySlot[snapshot.SlotIndex] != null) continue;
                SpawnTower(snapshot.Card, snapshot.Hand, snapshot.IsFusionResult, snapshot.SlotIndex,
                    snapshot.BaseDamage, snapshot.FusionCoreCardCount);
            }
            SelectionChanged?.Invoke();
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

        private CardTower SpawnTower(PlayingCard card, PokerHand hand, bool isFusionResult, int slotIndex,
            float fusionBaseDamage = 0f, int fusionCoreCardCount = 1)
        {
            CardTower tower = available.Count > 0 ? available.Dequeue() : CreateTower();
            tower.transform.SetParent(null, true);
            tower.transform.position = slots[slotIndex].position;
            tower.Activate(card, hand, isFusionResult, slotIndex, monsters, progression, config, effects, modifiers,
                fusionBaseDamage, fusionCoreCardCount);
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
            if (!economy.TrySpend(CurrentSummonCost))
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
            CardSummoned?.Invoke();
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

        private int FindClosestAnySlot(Vector3 position, float radius)
        {
            float bestDistance = radius * radius;
            int bestIndex = -1;
            for (int i = 0; i < slots.Length; i++)
            {
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

        private int GetSellValue(CardTower tower)
        {
            if (!tower.IsFusionResult) return config.baseCardSellGold;
            return config.fusionSellBaseGold + ((int)tower.Hand * config.fusionSellTierBonus);
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

        private static bool TryGetPointerState(out Vector2 position, out bool down, out bool held, out bool up)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                position = touch.position;
                down = touch.phase == TouchPhase.Began;
                held = touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved ||
                       touch.phase == TouchPhase.Stationary;
                up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                return true;
            }
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
            if (down || held || up)
            {
                position = Input.mousePosition;
                return true;
            }
            position = default;
            down = false;
            held = false;
            up = false;
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
