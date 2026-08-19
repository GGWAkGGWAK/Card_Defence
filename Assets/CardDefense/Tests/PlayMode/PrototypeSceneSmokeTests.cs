using System.Collections;
using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
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

            WaveDirector waves = Object.FindObjectOfType<WaveDirector>();
            Assert.GreaterOrEqual(waves.CurrentRound, 1);
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
            for (int i = 0; i < 5; i++) summon.SummonFirstAvailable();

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
            summon.ToggleSelection(mergedTower);
            Assert.AreEqual(0, summon.SelectedCount, "Fusion results must never enter merge selection.");
            Assert.IsTrue(summon.CanUpgradeSelection);
            summon.MergeSelected();
            Assert.AreEqual(1, towerSystem.ActiveCount, "A completed hand cannot be merged again.");
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
    }
}
