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
using UnityEngine.UI;

namespace CardDefense.Tests
{
    public sealed class PrototypeSceneSmokeTests
    {
        [UnitySetUp]
        public IEnumerator UseIsolatedRunSave()
        {
            RunSaveService.EditorSaveKeyOverride = "CardDefense.Tests.ActiveRun";
            RunSaveService.DeleteActiveRun();
            StartupMenuController.BypassForTests = true;
            TutorialController.EditorCompletionKeyOverride = "CardDefense.Tests.Tutorial";
            TutorialController.DeleteCompletion();
            TutorialController.BypassForTests = true;
            GameSettingsService.EditorSettingsPrefixOverride = "CardDefense.Tests.Settings.";
            GameSettingsService.DeleteSettings();
            PerformanceManager.EditorKeyOverride = "CardDefense.Tests.Performance";
            PerformanceManager.DeleteSetting();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator ClearIsolatedRunSave()
        {
            RunSaveService.DeleteActiveRun();
            RunSaveService.EditorSaveKeyOverride = null;
            StartupMenuController.BypassForTests = false;
            TutorialController.DeleteCompletion();
            TutorialController.EditorCompletionKeyOverride = null;
            TutorialController.BypassForTests = false;
            GameSettingsService.DeleteSettings();
            GameSettingsService.EditorSettingsPrefixOverride = null;
            PerformanceManager.DeleteSetting();
            PerformanceManager.EditorKeyOverride = null;
            Time.timeScale = 1f;
            yield return null;
        }

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
            Assert.IsNotNull(Object.FindObjectOfType<StartupMenuController>());
            Assert.IsNotNull(Object.FindObjectOfType<TutorialController>());
            Assert.IsNotNull(Object.FindObjectOfType<GameSettingsService>());
            Assert.IsNotNull(Object.FindObjectOfType<SettingsGuideController>());
            Assert.IsNotNull(Object.FindObjectOfType<PresentationEffectController>());
            Assert.IsNotNull(Object.FindObjectOfType<UiThemeController>());
            Assert.IsNotNull(Object.FindObjectOfType<PerformanceManager>());
            BossQuestController bossQuest = Object.FindObjectOfType<BossQuestController>();
            Assert.IsNotNull(bossQuest);
            Assert.GreaterOrEqual(bossQuest.CurrentRiskTier, 1);
            Assert.Greater(bossQuest.CurrentRewardGold, 0);
            Assert.IsNotNull(GameObject.Find("BossQuestTimeFill"));
            Assert.IsNotNull(GameObject.Find("SummonButton").GetComponent<Outline>());
            Assert.IsNotNull(GameObject.Find("CasinoArenaBackground"));
            Assert.IsNotNull(Object.FindObjectOfType<AudioListener>());
            Assert.IsNotNull(VisualAssetLibrary.GetUiFrameSprite());
            GameSettingsService audioSettings = Object.FindObjectOfType<GameSettingsService>();
            Assert.IsTrue(audioSettings.BgmEnabled);
            Assert.IsTrue(audioSettings.IsBgmPlaying);
            Material monsterMaterial = VisualAssetLibrary.GetMonsterMaterial();
            Assert.IsNotNull(monsterMaterial);
            Assert.GreaterOrEqual(monsterMaterial.GetFloat("_Tolerance"), 0.2f);

            CombatEffectSystem effects = Object.FindObjectOfType<CombatEffectSystem>();
            effects.PlayBeam(Vector3.zero, Vector3.one, true, PokerHand.Flush);
            Assert.AreEqual(PokerHand.Flush, effects.LastPlayedHand);
            Assert.AreEqual(CardSuit.Spade, effects.LastPlayedSuit);
            Assert.Greater(effects.ActiveProjectileCount, 0);
            Assert.Greater(effects.ActiveImpactCount, 0);

            WaveDirector waves = Object.FindObjectOfType<WaveDirector>();
            Assert.GreaterOrEqual(waves.CurrentRound, 1);
            Monster activeMonster = Object.FindObjectOfType<Monster>();
            Assert.IsNotNull(activeMonster);
            Assert.Greater(activeMonster.MaxHealth, 0f);
            Assert.IsNotNull(activeMonster.GetComponent<MonsterHealthBar>());
            StringAssert.StartsWith("Monster_", activeMonster.GetComponent<SpriteRenderer>().sprite.name);
        }

        [UnityTest]
        public IEnumerator CombatFeedbackUsesSuitProjectilesAndPooledGoldText()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            CombatEffectSystem effects = Object.FindObjectOfType<CombatEffectSystem>();
            Assert.IsNotNull(effects);
            effects.PlayProjectile(Vector3.zero, Vector3.right * 2f, false,
                PokerHand.FullHouse, CardSuit.Heart);
            Assert.AreEqual(PokerHand.FullHouse, effects.LastPlayedHand);
            Assert.AreEqual(CardSuit.Heart, effects.LastPlayedSuit);
            Assert.Greater(effects.ActiveProjectileCount, 0);
            Assert.Greater(effects.LastProjectileColor.r, effects.LastProjectileColor.g);

            effects.PlayGoldReward(Vector3.zero, 37);
            Assert.AreEqual(37, effects.LastRewardGold);
            Assert.Greater(effects.ActiveRewardTextCount, 0);
            Assert.IsNotNull(GameObject.Find("CardProjectile_00"));
            Assert.IsNotNull(GameObject.Find("RewardText_00"));

            yield return new WaitForSeconds(0.95f);
            Assert.AreEqual(0, effects.ActiveProjectileCount);
            Assert.AreEqual(0, effects.ActiveRewardTextCount);
        }

        [UnityTest]
        public IEnumerator MonsterArchetypesUseDistinctAuraLabelsAndBossHealthStyle()
        {
            GameObject testObject = new GameObject("MonsterVisualStyleTest");
            testObject.AddComponent<SpriteRenderer>();
            PrototypeVisual visual = testObject.AddComponent<PrototypeVisual>();
            MonsterHealthBar healthBar = testObject.AddComponent<MonsterHealthBar>();

            visual.SetMonsterStyle(MonsterArchetype.Fast);
            healthBar.Show(MonsterArchetype.Fast);
            Color fastAccent = visual.MonsterAccentColor;
            Assert.IsTrue(visual.HasMonsterAura);
            Assert.AreEqual("신속", healthBar.DisplayName);
            Assert.IsFalse(healthBar.IsBossStyle);

            visual.SetMonsterStyle(MonsterArchetype.Tank);
            healthBar.Show(MonsterArchetype.Tank);
            Assert.AreNotEqual(fastAccent, visual.MonsterAccentColor);
            Assert.AreEqual("중장", healthBar.DisplayName);

            visual.SetMonsterStyle(MonsterArchetype.Boss);
            healthBar.Show(MonsterArchetype.Boss);
            Assert.IsTrue(healthBar.IsBossStyle);
            Assert.AreEqual("운명의 수호자", healthBar.DisplayName);
            healthBar.SetHealth(0.2f);
            Assert.AreEqual(0.2f, healthBar.DisplayedNormalizedHealth, 0.001f);
            Assert.Greater(healthBar.CurrentBarColor.r, healthBar.CurrentBarColor.g);
            Assert.IsNotNull(testObject.transform.Find("MonsterAura"));
            Assert.IsNotNull(testObject.transform.Find("HealthBar/TypeName"));

            Object.Destroy(testObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MonstersAnimateSpawnMovementHitAndDeathBeforeReturningToPool()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            Monster monster = Object.FindObjectOfType<Monster>();
            MonsterSystem monsterSystem = Object.FindObjectOfType<MonsterSystem>();
            Assert.IsNotNull(monster);
            PrototypeVisual visual = monster.GetComponent<PrototypeVisual>();
            Assert.IsNotNull(visual);
            Assert.IsTrue(visual.IsSpawnAnimating);

            Vector3 initialScale = monster.transform.localScale;
            yield return new WaitForSeconds(0.08f);
            Assert.AreNotEqual(initialScale, monster.transform.localScale);

            int activeBeforeKill = monsterSystem.ActiveCount;
            monster.TakeDamage(monster.MaxHealth * 2f);
            Assert.IsFalse(monster.IsAlive);
            Assert.IsTrue(monster.IsDying);
            Assert.IsTrue(visual.IsDeathAnimating);
            Assert.IsTrue(monster.gameObject.activeSelf, "Death animation must remain visible before pooling.");
            Assert.AreEqual(activeBeforeKill - 1, monsterSystem.ActiveCount);

            yield return new WaitForSeconds(0.65f);
            Assert.IsFalse(monster.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator PerformanceModeCyclesAndPersistsFrameTargets()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            PerformanceManager performance = Object.FindObjectOfType<PerformanceManager>();
            Assert.AreEqual(DevicePerformanceMode.Balanced, performance.Mode);
            Assert.AreEqual(60, performance.TargetFrameRate);
            performance.CycleMode();
            Assert.AreEqual(DevicePerformanceMode.HighRefresh, performance.Mode);
            Assert.AreEqual(120, Application.targetFrameRate);
            performance.CycleMode();
            Assert.AreEqual(DevicePerformanceMode.BatterySaver, performance.Mode);
            Assert.AreEqual(30, Application.targetFrameRate);

            load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            performance = Object.FindObjectOfType<PerformanceManager>();
            Assert.AreEqual(DevicePerformanceMode.BatterySaver, performance.Mode);
            Assert.AreEqual(30, performance.TargetFrameRate);
        }

        [UnityTest]
        public IEnumerator SettingsPauseAndRestoreSelectedGameSpeed()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            PrototypeHud hud = Object.FindObjectOfType<PrototypeHud>();
            SettingsGuideController settingsUi = Object.FindObjectOfType<SettingsGuideController>();
            hud.ToggleSpeed();
            hud.ToggleSpeed();
            Assert.AreEqual(4f, Time.timeScale);

            settingsUi.Open();
            Assert.IsTrue(settingsUi.IsVisible);
            Assert.AreEqual(0f, Time.timeScale);
            settingsUi.Close();
            Assert.IsFalse(settingsUi.IsVisible);
            Assert.AreEqual(4f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator SettingsTogglesPersistAndGuidePagesOpen()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            GameSettingsService settings = Object.FindObjectOfType<GameSettingsService>();
            SettingsGuideController settingsUi = Object.FindObjectOfType<SettingsGuideController>();
            settingsUi.Open();
            settingsUi.ToggleBgm();
            settingsUi.ToggleSfx();
            settingsUi.ToggleVibration();
            settings.SetBgmVolume(0.35f);
            settings.SetSfxVolume(0.55f);
            Assert.IsFalse(settings.BgmEnabled);
            Assert.IsFalse(settings.SfxEnabled);
            Assert.IsFalse(settings.VibrationEnabled);
            Assert.AreEqual(0.35f, settings.BgmVolume, 0.001f);
            Assert.AreEqual(0.55f, settings.SfxVolume, 0.001f);
            Assert.IsNotNull(GameObject.Find("BgmVolume").GetComponent<Slider>());
            Assert.IsNotNull(GameObject.Find("SfxVolume").GetComponent<Slider>());
            RectTransform fill = GameObject.Find("BgmVolume").transform.Find("FillArea/Fill")
                .GetComponent<RectTransform>();
            Assert.AreEqual(0f, fill.anchorMin.y, 0.001f);
            Assert.AreEqual(1f, fill.anchorMax.y, 0.001f);
            Assert.AreEqual(0f, fill.offsetMin.y, 0.001f);
            Assert.AreEqual(0f, fill.offsetMax.y, 0.001f);
            settingsUi.ShowPokerHands();
            Assert.AreEqual(GuidePage.PokerHands, settingsUi.CurrentPage);
            settingsUi.ShowRules();
            Assert.AreEqual(GuidePage.Rules, settingsUi.CurrentPage);

            load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            settings = Object.FindObjectOfType<GameSettingsService>();
            Assert.IsFalse(settings.BgmEnabled);
            Assert.IsFalse(settings.SfxEnabled);
            Assert.IsFalse(settings.VibrationEnabled);
            Assert.AreEqual(0.35f, settings.BgmVolume, 0.001f);
            Assert.AreEqual(0.55f, settings.SfxVolume, 0.001f);
        }

        [UnityTest]
        public IEnumerator FirstRunTutorialWaitsForStartupAndCanBeSkipped()
        {
            StartupMenuController.BypassForTests = false;
            TutorialController.BypassForTests = false;
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            StartupMenuController menu = Object.FindObjectOfType<StartupMenuController>();
            TutorialController tutorial = Object.FindObjectOfType<TutorialController>();
            Assert.IsTrue(menu.IsVisible);
            Assert.IsFalse(tutorial.IsVisible);

            menu.StartNewGame();
            yield return null;
            Assert.IsTrue(tutorial.IsVisible);
            Assert.AreEqual(TutorialStep.Summon, tutorial.CurrentStep);

            tutorial.SkipTutorial();
            Assert.IsFalse(tutorial.IsVisible);
            Assert.AreEqual(TutorialStep.Complete, tutorial.CurrentStep);
        }

        [UnityTest]
        public IEnumerator TutorialAdvancesFromSummonThroughSpeedUsingRealActions()
        {
            TutorialController.BypassForTests = false;
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            TutorialController tutorial = Object.FindObjectOfType<TutorialController>();
            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            EconomyService economy = Object.FindObjectOfType<EconomyService>();
            PrototypeHud hud = Object.FindObjectOfType<PrototypeHud>();
            Assert.AreEqual(TutorialStep.Summon, tutorial.CurrentStep);

            summon.BeginSummonPlacement();
            yield return null;
            Assert.AreEqual(TutorialStep.Placement, tutorial.CurrentStep);
            summon.SummonFirstAvailable();
            Assert.AreEqual(TutorialStep.Selection, tutorial.CurrentStep);

            CardTower first = Object.FindObjectOfType<CardTower>();
            int added = 0;
            for (int rank = (int)CardRank.Two; rank <= (int)CardRank.Ace && added < 4; rank++)
            {
                PlayingCard candidate = new PlayingCard(CardSuit.Spade, (CardRank)rank);
                if (candidate.Suit == first.Card.Suit && candidate.Rank == first.Card.Rank) continue;
                summon.SummonForTesting(candidate);
                added++;
            }
            CardTower[] active = Object.FindObjectsOfType<CardTower>();
            Assert.AreEqual(5, active.Length);
            for (int i = 0; i < active.Length; i++) summon.ToggleSelection(active[i]);
            Assert.AreEqual(TutorialStep.Merge, tutorial.CurrentStep);

            economy.AddGold(1000);
            summon.MergeSelected();
            Assert.AreEqual(TutorialStep.Upgrade, tutorial.CurrentStep);
            summon.UpgradeSelectedHand();
            Assert.AreEqual(TutorialStep.Speed, tutorial.CurrentStep);
            hud.ToggleSpeed();
            Assert.AreEqual(TutorialStep.Complete, tutorial.CurrentStep);
            Assert.IsFalse(tutorial.IsVisible);
        }

        [UnityTest]
        public IEnumerator StartupMenuShowsNewGameWhenNoSaveExists()
        {
            StartupMenuController.BypassForTests = false;
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            StartupMenuController menu = Object.FindObjectOfType<StartupMenuController>();
            Assert.IsNotNull(menu);
            Assert.IsTrue(menu.IsVisible);
            Assert.IsFalse(menu.CanContinue);
            Assert.AreEqual(0f, Time.timeScale);

            menu.StartNewGame();
            yield return null;
            Assert.IsFalse(menu.IsVisible);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator StartupMenuOffersContinueForSavedRun()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            EconomyService economy = Object.FindObjectOfType<EconomyService>();
            economy.AddGold(432);
            int expectedGold = economy.Gold;
            Object.FindObjectOfType<RunSaveService>().SaveNow();

            StartupMenuController.BypassForTests = false;
            load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            StartupMenuController menu = Object.FindObjectOfType<StartupMenuController>();
            Assert.IsTrue(menu.IsVisible);
            Assert.IsTrue(menu.CanContinue);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(expectedGold, Object.FindObjectOfType<EconomyService>().Gold);

            menu.ContinueGame();
            Assert.IsFalse(menu.IsVisible);
            Assert.AreEqual(1f, Time.timeScale);
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
        public IEnumerator ActiveRunRestoresGoldTowerMonstersWaveAndGrowthAfterSceneReload()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            EconomyService economy = Object.FindObjectOfType<EconomyService>();
            CardSummonController summon = Object.FindObjectOfType<CardSummonController>();
            RunModifierService modifiers = Object.FindObjectOfType<RunModifierService>();
            RunSaveService save = Object.FindObjectOfType<RunSaveService>();
            WaveDirector waves = Object.FindObjectOfType<WaveDirector>();
            MonsterSystem monsters = Object.FindObjectOfType<MonsterSystem>();
            economy.AddGold(321);
            summon.SummonForTesting(new PlayingCard(CardSuit.Heart, CardRank.Ace));
            modifiers.Apply(RunGrowthChoice.AttackPower);
            int expectedGold = economy.Gold;
            int expectedRound = waves.CurrentRound;
            int expectedMonsters = monsters.ActiveCount;
            save.SaveNow();

            load = SceneManager.LoadSceneAsync("CardDefensePrototype", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            save = Object.FindObjectOfType<RunSaveService>();
            economy = Object.FindObjectOfType<EconomyService>();
            modifiers = Object.FindObjectOfType<RunModifierService>();
            waves = Object.FindObjectOfType<WaveDirector>();
            monsters = Object.FindObjectOfType<MonsterSystem>();
            CardTower restoredTower = Object.FindObjectOfType<CardTower>();
            Assert.IsTrue(save.WasRestored);
            Assert.AreEqual(expectedGold, economy.Gold);
            Assert.AreEqual(expectedRound, waves.CurrentRound);
            Assert.AreEqual(expectedMonsters, monsters.ActiveCount);
            Assert.AreEqual(1.15f, modifiers.DamageMultiplier, 0.001f);
            Assert.IsNotNull(restoredTower);
            Assert.AreEqual(CardRank.Ace, restoredTower.Card.Rank);
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
            StringAssert.Contains("PlayingCard", tower.GetComponent<SpriteRenderer>().sprite.name);
            Transform cardLabel = tower.transform.Find("CardLabel");
            Assert.IsNotNull(cardLabel, "A summoned card must create a visible rank and suit label.");
            TextMesh cardText = cardLabel.GetComponent<TextMesh>();
            Assert.IsNotNull(cardText);
            StringAssert.Contains("A", cardText.text);
            StringAssert.Contains("♥", cardText.text);
            Assert.Greater(cardLabel.GetComponent<MeshRenderer>().sortingOrder,
                tower.GetComponent<SpriteRenderer>().sortingOrder,
                "The card face must not cover the rank and suit label.");
            Transform handLabel = tower.transform.Find("CardHandLabel");
            Assert.IsNotNull(handLabel, "The poker grade must have its own readable footer.");
            StringAssert.Contains("HIGH", handLabel.GetComponent<TextMesh>().text);
            Assert.Greater(handLabel.GetComponent<MeshRenderer>().sortingOrder,
                tower.GetComponent<SpriteRenderer>().sortingOrder);
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

            PresentationEffectController presentation = Object.FindObjectOfType<PresentationEffectController>();
            Assert.IsTrue(presentation.IsPlaying);
            Assert.AreEqual(PokerHand.RoyalStraightFlush, presentation.LastFusionHand);
            Assert.AreEqual("FUSION", presentation.LastPresentation);

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
            StringAssert.StartsWith("7", merged.transform.Find("CardLabel").GetComponent<TextMesh>().text);
            StringAssert.Contains("PAIR",
                merged.transform.Find("CardHandLabel").GetComponent<TextMesh>().text);
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
