using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private Text goldText;
        private Text roundText;
        private Text monsterText;
        private Text messageText;
        private Text selectionText;
        private Text threatText;
        private Button summonButton;
        private Button mergeButton;
        private Button upgradeButton;
        private Button sellButton;
        private Button restartButton;
        private Button speedButton;
        private EconomyService economy;
        private WaveDirector waves;
        private MonsterSystem monsters;
        private CardSummonController summon;
        private CardTowerSystem towers;
        private GameBalanceConfig config;
        private RunStatisticsService statistics;
        private PlayerProfileService profile;
        private GrowthChoiceController growth;
        private RunModifierService modifiers;
        private float refreshTimer;
        private float selectedSpeed = 1f;
        private float bossAnnouncementTimer;
        private Color messageDefaultColor;

        public void Configure(Text gold, Text round, Text alive, Text message, Text selection, Text threat,
            Button summonButtonReference, Button mergeButtonReference, Button upgradeButtonReference,
            Button sellButtonReference, Button restartButtonReference,
            Button speedButtonReference,
            EconomyService economyService, WaveDirector waveDirector, MonsterSystem monsterSystem,
            CardSummonController summonController, CardTowerSystem towerSystem, GameBalanceConfig balance,
            RunStatisticsService runStatistics, PlayerProfileService playerProfile,
            GrowthChoiceController growthController, RunModifierService modifierService)
        {
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
            economy = economyService;
            waves = waveDirector;
            monsters = monsterSystem;
            summon = summonController;
            towers = towerSystem;
            config = balance;
            statistics = runStatistics;
            profile = playerProfile;
            growth = growthController;
            modifiers = modifierService;
            messageDefaultColor = messageText.color;

            summonButton.onClick.AddListener(summon.BeginSummonPlacement);
            mergeButton.onClick.AddListener(summon.MergeSelected);
            upgradeButton.onClick.AddListener(summon.UpgradeSelectedHand);
            sellButton.onClick.AddListener(summon.SellFocusedTower);
            restartButton.onClick.AddListener(RestartGame);
            speedButton.onClick.AddListener(ToggleSpeed);
            restartButton.gameObject.SetActive(false);
            summon.MessageChanged += SetMessage;
            summon.SelectionChanged += RefreshSelection;
            waves.GameLost += HandleGameLost;
            waves.RoundChanged += HandleRoundChanged;
            growth.ChoiceSelected += HandleGrowthSelected;
            Refresh();
        }

        private void Update()
        {
            if (bossAnnouncementTimer > 0f)
            {
                bossAnnouncementTimer -= Time.unscaledDeltaTime;
                if (bossAnnouncementTimer <= 0f && messageText != null) messageText.color = messageDefaultColor;
            }
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            Refresh();
        }

        private void OnDestroy()
        {
            if (summonButton != null && summon != null) summonButton.onClick.RemoveListener(summon.BeginSummonPlacement);
            if (mergeButton != null && summon != null) mergeButton.onClick.RemoveListener(summon.MergeSelected);
            if (upgradeButton != null && summon != null) upgradeButton.onClick.RemoveListener(summon.UpgradeSelectedHand);
            if (sellButton != null && summon != null) sellButton.onClick.RemoveListener(summon.SellFocusedTower);
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
            if (speedButton != null) speedButton.onClick.RemoveListener(ToggleSpeed);
            if (summon != null) summon.MessageChanged -= SetMessage;
            if (summon != null) summon.SelectionChanged -= RefreshSelection;
            if (waves != null) waves.GameLost -= HandleGameLost;
            if (waves != null) waves.RoundChanged -= HandleRoundChanged;
            if (growth != null) growth.ChoiceSelected -= HandleGrowthSelected;
        }

        private void Refresh()
        {
            if (economy == null || waves == null || monsters == null) return;
            goldText.text = "GOLD  " + economy.Gold;
            roundText.text = "ROUND  " + waves.CurrentRound + "  /  " + Mathf.CeilToInt(waves.SecondsToNextRound) + "s";
            if (monsters.TryGetBossHealth(out float bossHealth, out float bossMaxHealth))
            {
                monsterText.text = "BOSS " + Mathf.CeilToInt(bossHealth) + "/" +
                                   Mathf.CeilToInt(bossMaxHealth) + "  ·  " + monsters.ActiveCount;
            }
            else
            {
                monsterText.text = "MONSTERS  " + monsters.ActiveCount;
            }
            RefreshSelection();
            RefreshThreat();
        }

        private void RefreshThreat()
        {
            if (threatText == null || towers == null || config == null) return;
            float towerDps = towers.EstimatedTotalDps;
            float requiredDps = waves.CurrentRequiredDps;
            CombatThreatLevel level = CombatThreatEvaluator.Evaluate(towerDps, requiredDps,
                monsters.ActiveCount, config.defeatMonsterLimit);
            threatText.text = "전투력 " + Mathf.CeilToInt(towerDps) + " / 요구 " +
                              Mathf.CeilToInt(requiredDps) + "  ·  " +
                              CombatThreatEvaluator.KoreanName(level) + "  ·  소환 " +
                              summon.AffordableSummons + "회 · 성장 " + modifiers.ChoiceCount;
            switch (level)
            {
                case CombatThreatLevel.Stable:
                    threatText.color = new Color(0.35f, 1f, 0.55f, 1f);
                    break;
                case CombatThreatLevel.Caution:
                    threatText.color = new Color(1f, 0.85f, 0.25f, 1f);
                    break;
                default:
                    threatText.color = new Color(1f, 0.3f, 0.2f, 1f);
                    break;
            }
        }

        private void RefreshSelection()
        {
            if (selectionText == null || summon == null) return;
            selectionText.text = summon.GetSelectionSummary();
            if (waves != null && !waves.IsGameOver)
            {
                mergeButton.interactable = summon.CanMergeSelection;
                upgradeButton.interactable = summon.CanUpgradeSelection;
                sellButton.interactable = summon.CanSellSelection;
            }
        }

        private void SetMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        private void HandleGameLost()
        {
            SetMessage("패배: 몬스터 수량 한계 도달");
            if (selectionText != null && statistics != null && profile != null)
            {
                selectionText.text = statistics.GetRunSummary() + "\nBEST R" + profile.Data.BestRound +
                                     " · 총 플레이 " + profile.Data.TotalRuns + "회 · 누적 처치 " +
                                     profile.Data.TotalMonstersDefeated;
            }
            summonButton.interactable = false;
            mergeButton.interactable = false;
            upgradeButton.interactable = false;
            sellButton.interactable = false;
            restartButton.gameObject.SetActive(true);
            speedButton.interactable = false;
        }

        private void HandleGrowthSelected(RunGrowthChoice choice)
        {
            switch (choice)
            {
                case RunGrowthChoice.AttackPower:
                    SetMessage("성장 적용: 모든 타워 공격력 +15%");
                    break;
                case RunGrowthChoice.KillGold:
                    SetMessage("성장 적용: 몬스터 처치 골드 +12%");
                    break;
                default:
                    SetMessage("성장 적용: 카드 소환 비용 -10%");
                    break;
            }
            Refresh();
        }

        private void HandleRoundChanged(int round)
        {
            if (round % 10 != 0) return;
            SetMessage("BOSS ROUND " + round + "  ·  강력한 보스가 출현합니다!");
            if (messageText != null) messageText.color = new Color(1f, 0.28f, 0.16f, 1f);
            bossAnnouncementTimer = 3f;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        public void ToggleSpeed()
        {
            if (waves == null || waves.IsGameOver) return;
            selectedSpeed = selectedSpeed < 1.5f ? 2f : selectedSpeed < 3f ? 4f : 1f;
            Time.timeScale = selectedSpeed;
            SetButtonLabel(speedButton, "x" + selectedSpeed.ToString("0"));
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = value;
        }
    }
}
