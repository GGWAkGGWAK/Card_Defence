using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        public event Action<float> SpeedChanged;
        public float SelectedSpeed => selectedSpeed;
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
            GrowthChoiceController growthController, RunModifierService modifierService,
            RunSaveService runSave)
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
            if (runSave != null && runSave.WasRestored) SetMessage("저장된 게임을 이어서 시작했습니다");
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
            SpeedChanged?.Invoke(selectedSpeed);
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = value;
        }
    }

    public sealed class RunResultController : MonoBehaviour
    {
        public bool IsVisible => panel != null && panel.activeSelf;
        public string SummaryText => summaryText != null ? summaryText.text : string.Empty;
        public string CheckpointText => checkpointText != null ? checkpointText.text : string.Empty;

        private WaveDirector waves;
        private RunStatisticsService statistics;
        private PlayerProfileService profile;
        private GameObject panel;
        private Text summaryText;
        private Text checkpointText;
        private Button restartButton;
        private Button closeButton;
        private CanvasGroup panelGroup;
        private RectTransform panelRectTransform;
        private float revealProgress;

        public void Configure(Transform canvas, Font font, WaveDirector waveDirector,
            RunStatisticsService statisticsService, PlayerProfileService profileService)
        {
            waves = waveDirector;
            statistics = statisticsService;
            profile = profileService;
            BuildUi(canvas, font);
            panel.SetActive(false);
            waves.GameLost += ShowResult;
        }

        private void OnDestroy()
        {
            if (waves != null) waves.GameLost -= ShowResult;
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        private void ShowResult()
        {
            summaryText.text = statistics.GetDetailedRunSummary() + "\n" +
                               "개인 최고 R" + profile.Data.BestRound + " · 총 플레이 " +
                               profile.Data.TotalRuns + "회";
            checkpointText.text = "밸런스 체크포인트\n" + statistics.GetCheckpointSummary() +
                                  (string.IsNullOrEmpty(statistics.LastExportPath)
                                      ? "\n로그 저장 실패"
                                      : "\nJSON 밸런스 로그 저장 완료");
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            revealProgress = 0f;
            panelGroup.alpha = 0f;
            panelRectTransform.localScale = Vector3.one * 0.88f;
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf || revealProgress >= 1f) return;
            revealProgress = Mathf.Min(1f, revealProgress + Time.unscaledDeltaTime * 4.5f);
            float eased = 1f - Mathf.Pow(1f - revealProgress, 3f);
            panelGroup.alpha = eased;
            panelRectTransform.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, eased);
        }

        public void Close()
        {
            panel.SetActive(false);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        private void BuildUi(Transform canvas, Font font)
        {
            panel = new GameObject("RunResultPanel", typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup));
            panel.transform.SetParent(canvas, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRectTransform = panelRect;
            panelGroup = panel.GetComponent<CanvasGroup>();
            panelRect.anchorMin = new Vector2(0.055f, 0.10f);
            panelRect.anchorMax = new Vector2(0.945f, 0.90f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.055f, 0.985f);

            Text title = CreateText(panel.transform, "RunResultTitle", "RUN RESULT · 전투 분석", 46,
                new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.97f), font,
                new Color(1f, 0.8f, 0.28f, 1f));
            title.fontStyle = FontStyle.Bold;
            summaryText = CreateText(panel.transform, "RunResultSummary", string.Empty, 27,
                new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.84f), font, Color.white);
            summaryText.alignment = TextAnchor.UpperLeft;
            checkpointText = CreateText(panel.transform, "RunResultCheckpoints", string.Empty, 24,
                new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.47f), font,
                new Color(0.55f, 0.92f, 0.9f, 1f));
            checkpointText.alignment = TextAnchor.UpperLeft;
            closeButton = CreateButton(panel.transform, "RunResultCloseButton", "결과 닫기",
                new Vector2(0.08f, 0.055f), new Vector2(0.47f, 0.155f), font,
                new Color(0.18f, 0.35f, 0.39f, 1f));
            restartButton = CreateButton(panel.transform, "RunResultRestartButton", "새 게임",
                new Vector2(0.53f, 0.055f), new Vector2(0.92f, 0.155f), font,
                new Color(0.76f, 0.18f, 0.22f, 1f));
            closeButton.onClick.AddListener(Close);
            restartButton.onClick.AddListener(RestartGame);
        }

        private static Text CreateText(Transform parent, string name, string value, int size,
            Vector2 min, Vector2 max, Font font, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = child.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 min, Vector2 max, Font font, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            child.GetComponent<Image>().color = color;
            Text text = CreateText(child.transform, "Label", label, 30, Vector2.zero, Vector2.one,
                font, Color.white);
            text.fontStyle = FontStyle.Bold;
            return child.GetComponent<Button>();
        }
    }
}
