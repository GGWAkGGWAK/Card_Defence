using System;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class BossQuestController : MonoBehaviour
    {
        public event Action<bool, int> QuestCompleted;
        public bool IsActive { get; private set; }
        public bool IsReady => !IsActive && cooldownRemaining <= 0f &&
                               waves != null && !waves.HasActiveChallengeBoss;
        public float CooldownRemaining => cooldownRemaining;
        public float ChallengeRemaining => challengeRemaining;
        public int CurrentRiskTier { get; private set; }
        public int CurrentRewardGold => rewardGold;
        public float CurrentHealthMultiplier { get; private set; } = 1f;
        public int LastFailureReinforcements { get; private set; }
        public float ProgressNormalized { get; private set; }
        public string PreviewText => label != null ? label.text : string.Empty;

        private GameBalanceConfig config;
        private WaveDirector waves;
        private EconomyService economy;
        private RunModifierService modifiers;
        private Button button;
        private Text label;
        private Image progressFill;
        private RectTransform progressFillRect;
        private float cooldownRemaining;
        private float challengeRemaining;
        private int rewardGold;
        private string statusMessage;
        private float statusRemaining;

        public void Configure(Transform canvas, Font font, GameBalanceConfig balance,
            WaveDirector waveDirector, EconomyService economyService, RunModifierService modifierService)
        {
            config = balance;
            waves = waveDirector;
            economy = economyService;
            modifiers = modifierService;
            cooldownRemaining = config.bossQuestInitialCooldown;
            BuildUi(canvas, font);
            button.onClick.AddListener(StartQuest);
            waves.ChallengeBossDefeated += HandleBossDefeated;
            waves.GameLost += HandleGameLost;
            RefreshLabel();
        }

        private void Update()
        {
            if (config == null || waves == null || waves.IsGameOver) return;
            if (statusRemaining > 0f)
            {
                statusRemaining -= Time.unscaledDeltaTime;
                if (statusRemaining <= 0f) statusMessage = null;
            }
            if (IsActive)
            {
                challengeRemaining -= Time.deltaTime;
                if (challengeRemaining <= 0f) FailQuest();
            }
            else if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
            }
            RefreshLabel();
        }

        private void StartQuest()
        {
            if (!IsReady) return;
            PreparePreview();
            if (!waves.TrySpawnChallengeBoss(CurrentHealthMultiplier)) return;
            IsActive = true;
            challengeRemaining = config.bossQuestTimeLimit;
            LastFailureReinforcements = 0;
            RefreshLabel();
        }

        private void HandleBossDefeated()
        {
            if (!IsActive) return;
            IsActive = false;
            economy.AddGold(rewardGold);
            modifiers.ApplyBossQuestReward(config.bossQuestAttackBonus);
            cooldownRemaining = config.bossQuestCooldown;
            QuestCompleted?.Invoke(true, rewardGold);
            RefreshLabel("성공! " + rewardGold + "G · 공격력 +" +
                         Mathf.RoundToInt(config.bossQuestAttackBonus * 100f) + "%");
        }

        private void FailQuest()
        {
            IsActive = false;
            challengeRemaining = 0f;
            cooldownRemaining = config.bossQuestCooldown;
            LastFailureReinforcements = CalculateFailureReinforcements(config, CurrentRiskTier);
            waves.ApplyChallengeFailure(LastFailureReinforcements,
                config.bossQuestFailureHealthBonus, config.bossQuestFailureSpeedBonus);
            QuestCompleted?.Invoke(false, 0);
            RefreshLabel("실패! 보스 격노 · 증원 " + LastFailureReinforcements + "체");
        }

        private void HandleGameLost()
        {
            if (button != null) button.interactable = false;
        }

        private void PreparePreview()
        {
            int wins = modifiers != null ? modifiers.BossQuestWins : 0;
            int round = waves != null ? waves.CurrentRound : 1;
            CurrentRiskTier = CalculateRiskTier(round, wins);
            CurrentHealthMultiplier = CalculateHealthMultiplier(config, round, wins);
            rewardGold = CalculateReward(config, round, wins);
        }

        private void RefreshLabel(string temporary = null)
        {
            if (label == null || config == null) return;
            if (!string.IsNullOrEmpty(temporary))
            {
                statusMessage = temporary;
                statusRemaining = 2.8f;
            }
            PreparePreviewIfIdle();
            UpdateProgress();

            if (!string.IsNullOrEmpty(statusMessage)) label.text = statusMessage;
            else if (IsActive)
            {
                int bossHealth = Mathf.CeilToInt(waves.ChallengeBossHealthNormalized * 100f);
                label.text = "위험 " + CurrentRiskTier + " · 운명의 수호자  " +
                             Mathf.CeilToInt(challengeRemaining) + "초\nHP " + bossHealth +
                             "% · 성공 " + rewardGold + "G + 공격력";
            }
            else if (waves.HasActiveChallengeBoss)
                label.text = "격노한 보스가 필드에 잔류 중\n처치 후 다음 도전 가능";
            else if (IsReady)
                label.text = "보스 도전 · 위험 " + CurrentRiskTier + "  [소환]\nHP x" +
                             CurrentHealthMultiplier.ToString("0.0") + " · " +
                             Mathf.CeilToInt(config.bossQuestTimeLimit) + "초 · " + rewardGold + "G";
            else
                label.text = "보스 재정비  " + Mathf.CeilToInt(cooldownRemaining) + "초\n" +
                             "실패: 강화 + 증원 " +
                             CalculateFailureReinforcements(config, CurrentRiskTier) + "체";
            button.interactable = IsReady;
        }

        private void PreparePreviewIfIdle()
        {
            if (!IsActive) PreparePreview();
        }

        private void UpdateProgress()
        {
            if (progressFill == null) return;
            if (IsActive)
            {
                ProgressNormalized = Mathf.Clamp01(challengeRemaining /
                                                    Mathf.Max(0.01f, config.bossQuestTimeLimit));
                progressFill.color = ProgressNormalized <= 0.3f
                    ? new Color(1f, 0.12f, 0.08f, 1f)
                    : new Color(1f, 0.63f, 0.08f, 1f);
            }
            else if (cooldownRemaining > 0f)
            {
                float cooldownDuration = Mathf.Max(config.bossQuestInitialCooldown,
                    config.bossQuestCooldown);
                ProgressNormalized = 1f - Mathf.Clamp01(cooldownRemaining / cooldownDuration);
                progressFill.color = new Color(0.25f, 0.7f, 0.82f, 1f);
            }
            else
            {
                ProgressNormalized = 1f;
                progressFill.color = new Color(0.95f, 0.3f, 0.16f, 1f);
            }
            if (progressFillRect != null)
                progressFillRect.anchorMax = new Vector2(Mathf.Max(0.001f, ProgressNormalized), 1f);
        }

        private void BuildUi(Transform canvas, Font font)
        {
            GameObject objectRoot = new GameObject("BossQuestButton", typeof(RectTransform),
                typeof(Image), typeof(Button));
            objectRoot.transform.SetParent(canvas, false);
            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.035f, 0.785f);
            rect.anchorMax = new Vector2(0.43f, 0.885f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = objectRoot.GetComponent<Image>();
            image.color = new Color(0.22f, 0.025f, 0.055f, 0.97f);
            button = objectRoot.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.78f, 0.65f, 1f);
            colors.pressedColor = new Color(0.8f, 0.45f, 0.4f, 1f);
            colors.disabledColor = new Color(0.46f, 0.46f, 0.46f, 0.8f);
            button.colors = colors;

            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(objectRoot.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 10f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            label = textObject.GetComponent<Text>();
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 25;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.9f, 0.72f, 1f);
            label.raycastTarget = false;
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject trackObject = new GameObject("BossQuestTimeTrack", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(objectRoot.transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.03f, 0.03f);
            trackRect.anchorMax = new Vector2(0.97f, 0.1f);
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            trackObject.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.035f, 0.95f);

            GameObject fillObject = new GameObject("BossQuestTimeFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            progressFillRect = fillObject.GetComponent<RectTransform>();
            progressFillRect.anchorMin = Vector2.zero;
            progressFillRect.anchorMax = Vector2.one;
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
            progressFill = fillObject.GetComponent<Image>();
            progressFill.type = Image.Type.Simple;
            progressFill.raycastTarget = false;
        }

        public BossQuestSnapshot CaptureSnapshot()
        {
            return new BossQuestSnapshot
            {
                CooldownRemaining = cooldownRemaining,
                ChallengeRemaining = challengeRemaining,
                IsActive = IsActive,
                RewardGold = rewardGold
            };
        }

        public static int CalculateRiskTier(int round, int completedWins)
        {
            return 1 + Mathf.Max(0, (Mathf.Max(1, round) - 1) / 10) + Mathf.Max(0, completedWins);
        }

        public static float CalculateHealthMultiplier(GameBalanceConfig balance, int round, int completedWins)
        {
            if (balance == null) return 1f;
            int tier = CalculateRiskTier(round, completedWins);
            return 1f + Mathf.Max(0, tier - 1) * balance.bossQuestHealthPerRiskTier;
        }

        public static int CalculateReward(GameBalanceConfig balance, int round)
        {
            return CalculateReward(balance, round, 0);
        }

        public static int CalculateReward(GameBalanceConfig balance, int round, int completedWins)
        {
            if (balance == null) return 0;
            int baseReward = Mathf.Max(0, balance.bossQuestBaseGold +
                                          Mathf.Max(1, round) * balance.bossQuestGoldPerRound);
            int tier = CalculateRiskTier(round, completedWins);
            float multiplier = 1f + Mathf.Max(0, tier - 1) * balance.bossQuestRewardPerRiskTier;
            return Mathf.Max(0, Mathf.CeilToInt(baseReward * multiplier));
        }

        public static int CalculateFailureReinforcements(GameBalanceConfig balance, int riskTier)
        {
            if (balance == null) return 0;
            return Mathf.Max(0, balance.bossQuestFailureReinforcements +
                                Mathf.Max(0, riskTier - 1) / 2);
        }

        public void RestoreSnapshot(BossQuestSnapshot snapshot)
        {
            cooldownRemaining = Mathf.Max(0f, snapshot.CooldownRemaining);
            challengeRemaining = Mathf.Max(0f, snapshot.ChallengeRemaining);
            rewardGold = Mathf.Max(0, snapshot.RewardGold);
            IsActive = snapshot.IsActive && challengeRemaining > 0f && waves.HasActiveChallengeBoss;
            if (snapshot.IsActive && !IsActive) cooldownRemaining = config.bossQuestCooldown;
            int savedReward = rewardGold;
            PreparePreview();
            if (IsActive) rewardGold = savedReward;
            RefreshLabel();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(StartQuest);
            if (waves == null) return;
            waves.ChallengeBossDefeated -= HandleBossDefeated;
            waves.GameLost -= HandleGameLost;
        }
    }
}
