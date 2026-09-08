using CardDefense.Combat;
using CardDefense.Enemies;
using CardDefense.Pooling;
using CardDefense.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.Core
{
    public sealed class GameComposition : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private LoopPath path;
        [SerializeField] private Monster monsterPrefab;
        [SerializeField] private CardTower towerPrefab;
        [SerializeField] private Transform[] placementSlots;
        [SerializeField] private EconomyService economy;
        [SerializeField] private PokerProgressionService progression;
        [SerializeField] private MonsterSystem monsters;
        [SerializeField] private MonsterPool monsterPool;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private CardTowerSystem towers;
        [SerializeField] private CardSummonController summon;
        [SerializeField] private PrototypeHud hud;
        [SerializeField] private Text goldText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text monsterText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Text threatText;
        [SerializeField] private Button summonButton;
        [SerializeField] private Button mergeButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private GameObject growthPanel;
        [SerializeField] private Text growthTitleText;
        [SerializeField] private Button growthAttackButton;
        [SerializeField] private Button growthGoldButton;
        [SerializeField] private Button growthSummonButton;

        public void SetReferences(GameBalanceConfig balance, LoopPath loopPath, Monster monster,
            CardTower tower, Transform[] slots, EconomyService economyService,
            PokerProgressionService progressionService, MonsterSystem monsterSystem,
            MonsterPool pool, WaveDirector waveDirector, CardTowerSystem towerSystem,
            CardSummonController summonController, PrototypeHud prototypeHud, Text gold, Text round,
            Text alive, Text message, Text selection, Text threat, Button summonButtonReference,
            Button mergeButtonReference, Button upgradeButtonReference,
            Button sellButtonReference, Button restartButtonReference,
            Button speedButtonReference, GameObject growthPanelReference, Text growthTitle,
            Button growthAttack, Button growthGold, Button growthSummon)
        {
            config = balance;
            path = loopPath;
            monsterPrefab = monster;
            towerPrefab = tower;
            placementSlots = slots;
            economy = economyService;
            progression = progressionService;
            monsters = monsterSystem;
            monsterPool = pool;
            waves = waveDirector;
            towers = towerSystem;
            summon = summonController;
            hud = prototypeHud;
            goldText = gold;
            roundText = round;
            monsterText = alive;
            messageText = message;
            selectionText = selection;
            threatText = threat;
            summonButton = summonButtonReference;
            mergeButton = mergeButtonReference;
            upgradeButton = upgradeButtonReference;
            sellButton = sellButtonReference;
            restartButton = restartButtonReference;
            speedButton = speedButtonReference;
            growthPanel = growthPanelReference;
            growthTitleText = growthTitle;
            growthAttackButton = growthAttack;
            growthGoldButton = growthGold;
            growthSummonButton = growthSummon;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            SafeAreaFitter safeArea = hud.GetComponentInChildren<SafeAreaFitter>(true);
            Transform uiRoot = safeArea != null ? safeArea.Content : hud.transform;
            VisualAssetLibrary.CreateArenaBackground();
            economy.Configure(config);
            progression.Configure(config, economy);
            RunModifierService modifiers = gameObject.AddComponent<RunModifierService>();
            modifiers.ResetRun();
            monsterPool.Configure(monsterPrefab, config.monsterPrewarmCount);
            CombatEffectSystem effects = gameObject.AddComponent<CombatEffectSystem>();
            effects.Configure(32);
            summon.Configure(towerPrefab, placementSlots, economy, monsters, towers, progression, config, effects);
            summon.SetRunModifiers(modifiers);
            waves.Configure(config, path, monsterPool, monsters, economy);
            waves.SetRunModifiers(modifiers);
            effects.Bind(waves);
            RunStatisticsService statistics = gameObject.AddComponent<RunStatisticsService>();
            statistics.Configure(waves, summon, progression);
            PlayerProfileService profile = gameObject.AddComponent<PlayerProfileService>();
            profile.Configure(waves, statistics);
            GrowthChoiceController growth = hud.gameObject.AddComponent<GrowthChoiceController>();
            growth.Configure(growthPanel, growthTitleText, growthAttackButton, growthGoldButton,
                growthSummonButton, waves, modifiers);
            BossQuestController bossQuest = hud.gameObject.AddComponent<BossQuestController>();
            bossQuest.Configure(uiRoot, messageText != null ? messageText.font : null,
                config, waves, economy, modifiers);
            RunSaveService runSave = gameObject.AddComponent<RunSaveService>();
            runSave.Configure(economy, progression, modifiers, statistics, summon, monsters, waves, growth,
                bossQuest);
            hud.Configure(goldText, roundText, monsterText, messageText, selectionText, threatText,
                summonButton, mergeButton, upgradeButton, sellButton, restartButton,
                speedButton,
                economy, waves, monsters, summon, towers, config, statistics, profile, growth, modifiers,
                runSave);
            StartupMenuController startup = hud.gameObject.AddComponent<StartupMenuController>();
            startup.Configure(uiRoot, messageText != null ? messageText.font : null, runSave, profile);
            TutorialController tutorial = hud.gameObject.AddComponent<TutorialController>();
            tutorial.Configure(uiRoot, messageText != null ? messageText.font : null, startup,
                summon, progression, hud, summonButton, mergeButton, upgradeButton, speedButton);
            GameSettingsService settings = gameObject.AddComponent<GameSettingsService>();
            settings.Configure(summon, progression, waves, growth, effects);
            PerformanceManager performance = gameObject.AddComponent<PerformanceManager>();
            performance.Configure(effects);
            SettingsGuideController settingsUi = hud.gameObject.AddComponent<SettingsGuideController>();
            settingsUi.Configure(uiRoot, messageText != null ? messageText.font : null, startup,
                settings, performance);
            MobileBackController mobileBack = gameObject.AddComponent<MobileBackController>();
            mobileBack.Configure(settingsUi, tutorial, summon, runSave, messageText);
            PresentationEffectController presentation = hud.gameObject.AddComponent<PresentationEffectController>();
            presentation.Configure(uiRoot, messageText != null ? messageText.font : null, summon, waves);
            UiThemeController theme = hud.gameObject.AddComponent<UiThemeController>();
            theme.Configure(uiRoot);
        }
    }

    public sealed class MobileBackController : MonoBehaviour
    {
        public bool WaitingForExitConfirmation => exitConfirmUntil > Time.unscaledTime;

        private SettingsGuideController settings;
        private TutorialController tutorial;
        private CardSummonController summon;
        private RunSaveService runSave;
        private Text message;
        private float exitConfirmUntil;

        public void Configure(SettingsGuideController settingsController, TutorialController tutorialController,
            CardSummonController summonController, RunSaveService saveService, Text messageText)
        {
            settings = settingsController;
            tutorial = tutorialController;
            summon = summonController;
            runSave = saveService;
            message = messageText;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) HandleBackPressed();
        }

        public void HandleBackPressed()
        {
            if (settings != null && settings.IsVisible)
            {
                settings.Close();
                return;
            }
            if (tutorial != null && tutorial.IsVisible)
            {
                tutorial.SkipTutorial();
                return;
            }
            if (summon != null && summon.CancelCurrentInteraction()) return;
            if (WaitingForExitConfirmation)
            {
                if (runSave != null) runSave.SaveNow();
                Application.Quit();
                return;
            }
            if (runSave != null) runSave.SaveNow();
            exitConfirmUntil = Time.unscaledTime + 2f;
            if (message != null) message.text = "한 번 더 누르면 저장 후 게임을 종료합니다";
        }
    }
}
