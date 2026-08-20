using System.Collections;
using CardDefense.Combat;
using CardDefense.Cards;
using CardDefense.Core;
using CardDefense.Enemies;
using CardDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CardDefense.Tests
{
    public sealed class PrototypeSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator PrototypeSceneStartsCoreSystems()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            Assert.IsNotNull(Object.FindObjectOfType<GameComposition>());
            Assert.IsNotNull(Object.FindObjectOfType<WaveDirector>());
            Assert.IsNotNull(Object.FindObjectOfType<MonsterSystem>());
            Assert.IsNotNull(Object.FindObjectOfType<CardSummonController>());
            Assert.IsNotNull(Object.FindObjectOfType<PokerProgressionService>());
            Assert.IsNotNull(Object.FindObjectOfType<CombatEffectSystem>());
            Assert.IsNotNull(Object.FindObjectOfType<TowerRangeIndicator>());
            Assert.IsNotNull(GameObject.Find("ThreatText"));
            Assert.IsNotNull(Object.FindObjectOfType<RunModifierService>());
            Assert.IsNotNull(Object.FindObjectOfType<RunStatisticsService>());
            Assert.IsNotNull(Object.FindObjectOfType<PlayerProfileService>());
            Assert.IsNotNull(Object.FindObjectOfType<GrowthChoiceController>());

            WaveDirector waves = Object.FindObjectOfType<WaveDirector>();
            Assert.GreaterOrEqual(waves.CurrentRound, 1);
            Monster activeMonster = Object.FindObjectOfType<Monster>();
            Assert.IsNotNull(activeMonster);
            Assert.Greater(activeMonster.MaxHealth, 0f);
            Assert.IsNotNull(activeMonster.GetComponent<MonsterHealthBar>());
        }

        [UnityTest]
        public IEnumerator BossMilestoneOffersOneRunGrowthChoice()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            GrowthChoiceController growth = Object.FindObjectOfType<GrowthChoiceController>();
            RunModifierService modifiers = Object.FindObjectOfType<RunModifierService>();
            Assert.IsFalse(growth.IsChoiceVisible);
            growth.OfferForTesting(10);
            Assert.IsTrue(growth.IsChoiceVisible);
            growth.SelectAttack();

            Assert.IsFalse(growth.IsChoiceVisible);
            Assert.AreEqual(1.15f, modifiers.DamageMultiplier, 0.001f);
            Assert.AreEqual(1, modifiers.ChoiceCount);
        }

        [UnityTest]
        public IEnumerator SelectedTowerShowsCombatStatsAndRange()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            summon.SummonForTesting(new PlayingCard(CardSuit.Heart, CardRank.Ace));
            CardTower tower = Object.FindObjectOfType<CardTower>();
            summon.ToggleSelection(tower);
            yield return null;

            Assert.AreSame(tower, summon.FocusedTower);
            Assert.Greater(tower.CurrentDamage, 0f);
            Assert.Greater(tower.EstimatedDps, 0f);
            StringAssert.Contains("DPS", summon.GetFocusedCombatSummary());
            LineRenderer rangeLine = Object.FindObjectOfType<TowerRangeIndicator>().GetComponent<LineRenderer>();
            Assert.IsTrue(rangeLine.enabled);
        }

        [UnityTest]
        public IEnumerator FiveSelectedCardsMergeIntoOneTower()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            EconomyService economy = Object.FindObjectOfType<EconomyService>();
            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            CardTowerSystem towerSystem = Object.FindObjectOfType<CardTowerSystem>();
            economy.AddGold(1000);
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Ten));
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Jack));
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Queen));
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.King));
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Ace));

            CardTower[] activeTowers = Object.FindObjectsOfType<CardTower>();
            Assert.AreEqual(5, activeTowers.Length);
            for (int i = 0; i < activeTowers.Length; i++) summon.ToggleSelection(activeTowers[i]);
            Assert.AreEqual(5, summon.SelectedCount);

            summon.MergeSelected();
            yield return null;

            Assert.AreEqual(1, towerSystem.ActiveCount);
            Assert.AreEqual(0, summon.SelectedCount);
            CardTower mergedTower = Object.FindObjectOfType<CardTower>();
            Assert.IsTrue(mergedTower.IsFusionResult);
            Assert.AreEqual(CardRank.Ace, mergedTower.Card.Rank,
                "Royal straight flush must display its actual primary rank.");
            summon.ToggleSelection(mergedTower);
            Assert.AreEqual(0, summon.SelectedCount, "Fusion results must never enter merge selection.");
            Assert.IsTrue(summon.CanUpgradeSelection);
            summon.MergeSelected();
            Assert.AreEqual(1, towerSystem.ActiveCount, "A completed hand cannot be merged again.");

            int goldBeforeSale = economy.Gold;
            int sellValue = summon.GetFocusedSellValue();
            summon.SellFocusedTower();
            Assert.AreEqual(0, towerSystem.ActiveCount, "Selling must recover the occupied slot.");
            Assert.AreEqual(goldBeforeSale + sellValue, economy.Gold);
            Assert.IsFalse(summon.CanSellSelection);
        }

        [UnityTest]
        public IEnumerator ExactDuplicateCardsCannotBeConsumedByMerge()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            CardTowerSystem towerSystem = Object.FindObjectOfType<CardTowerSystem>();
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Ace));
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Ace));
            summon.SummonForTesting(new PlayingCard(CardSuit.Heart, CardRank.Three));
            summon.SummonForTesting(new PlayingCard(CardSuit.Club, CardRank.Four));
            summon.SummonForTesting(new PlayingCard(CardSuit.Diamond, CardRank.Five));
            CardTower[] activeTowers = Object.FindObjectsOfType<CardTower>();
            for (int i = 0; i < activeTowers.Length; i++) summon.ToggleSelection(activeTowers[i]);

            Assert.AreEqual(5, summon.SelectedCount);
            Assert.IsFalse(summon.CanMergeSelection);
            summon.MergeSelected();
            yield return null;

            Assert.AreEqual(5, towerSystem.ActiveCount);
            Assert.AreEqual(5, summon.SelectedCount);
        }

        [UnityTest]
        public IEnumerator SevenPairDisplaysSevenInsteadOfAceKicker()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            summon.SummonForTesting(new PlayingCard(CardSuit.Spade, CardRank.Seven));
            summon.SummonForTesting(new PlayingCard(CardSuit.Heart, CardRank.Seven));
            summon.SummonForTesting(new PlayingCard(CardSuit.Diamond, CardRank.Ace));
            summon.SummonForTesting(new PlayingCard(CardSuit.Club, CardRank.King));
            summon.SummonForTesting(new PlayingCard(CardSuit.Club, CardRank.Three));
            CardTower[] activeTowers = Object.FindObjectsOfType<CardTower>();
            for (int i = 0; i < activeTowers.Length; i++) summon.ToggleSelection(activeTowers[i]);
            summon.MergeSelected();
            yield return null;

            CardTower merged = Object.FindObjectOfType<CardTower>();
            Assert.AreEqual(PokerHand.OnePair, merged.Hand);
            Assert.AreEqual(CardRank.Seven, merged.Card.Rank);
            StringAssert.StartsWith("7\nPAIR", merged.GetComponentInChildren<TextMesh>().text);
        }

        [UnityTest]
        public IEnumerator SummonButtonEntersPlacementModeWithoutSpendingGold()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            EconomyService economy = Object.FindObjectOfType<EconomyService>();
            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            int goldBefore = economy.Gold;
            summon.BeginSummonPlacement();

            Assert.IsTrue(summon.IsPlacementPending);
            Assert.AreEqual(goldBefore, economy.Gold);
        }

        [UnityTest]
        public IEnumerator TowersCanMoveToEmptySlotsAndSwapPositions()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            summon.SummonForTesting(new PlayingCard(CardSuit.Heart, CardRank.Two));
            summon.SummonForTesting(new PlayingCard(CardSuit.Club, CardRank.King));
            CardTower[] towers = Object.FindObjectsOfType<CardTower>();
            CardTower two = towers[0].Card.Rank == CardRank.Two ? towers[0] : towers[1];
            CardTower king = towers[0].Card.Rank == CardRank.King ? towers[0] : towers[1];

            Assert.IsTrue(summon.MoveTowerToSlot(two, 2));
            Assert.AreEqual(2, two.SlotIndex);
            Assert.AreEqual(1, king.SlotIndex);

            Assert.IsTrue(summon.MoveTowerToSlot(two, 1));
            Assert.AreEqual(1, two.SlotIndex);
            Assert.AreEqual(2, king.SlotIndex);
        }

        [UnityTest]
        public IEnumerator SpeedControlCyclesOneTwoFourOne()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            PrototypeHud hud = Object.FindObjectOfType<PrototypeHud>();
            hud.ToggleSpeed();
            Assert.AreEqual(2f, Time.timeScale);
            hud.ToggleSpeed();
            Assert.AreEqual(4f, Time.timeScale);
            hud.ToggleSpeed();
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
