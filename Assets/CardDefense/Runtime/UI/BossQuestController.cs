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
        public bool IsReady => !IsActive && cooldownRemaining <= 0f;
        public float CooldownRemaining => cooldownRemaining;
        public float ChallengeRemaining => challengeRemaining;

        private GameBalanceConfig config;
        private WaveDirector waves;
        private EconomyService economy;
        private RunModifierService modifiers;
        private Button button;
        private Text label;
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
            if (!IsReady || !waves.TrySpawnChallengeBoss()) return;
            IsActive = true;
            challengeRemaining = config.bossQuestTimeLimit;
            rewardGold = CalculateReward(config, waves.CurrentRound);
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
            RefreshLabel("성공! " + rewardGold + "G + 공격력 " +
                         Mathf.RoundToInt(config.bossQuestAttackBonus * 100f) + "%");
        }

        private void FailQuest()
        {
            IsActive = false;
            challengeRemaining = 0f;
            cooldownRemaining = config.bossQuestCooldown;
            QuestCompleted?.Invoke(false, 0);
            RefreshLabel("실패 - 보스는 필드에 잔류");
        }

        private void HandleGameLost()
        {
            if (button != null) button.interactable = false;
        }

        private void RefreshLabel(string temporary = null)
        {
            if (label == null) return;
            if (!string.IsNullOrEmpty(temporary))
            {
                statusMessage = temporary;
                statusRemaining = 2.4f;
            }
            if (!string.IsNullOrEmpty(statusMessage)) label.text = statusMessage;
            else if (IsActive)
                label.text = "위험 보스  " + Mathf.CeilToInt(challengeRemaining) + "초\n보상 " + rewardGold + "G + 공격력";
            else if (IsReady)
                label.text = "위험 보스 소환\n" + Mathf.CeilToInt(config.bossQuestTimeLimit) + "초 제한";
            else
                label.text = "보스 퀘스트\n재충전 " + Mathf.CeilToInt(cooldownRemaining) + "초";
            button.interactable = IsReady;
        }

        private void BuildUi(Transform canvas, Font font)
        {
            GameObject objectRoot = new GameObject("BossQuestButton", typeof(RectTransform), typeof(Image), typeof(Button));
            objectRoot.transform.SetParent(canvas, false);
            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.035f, 0.795f);
            rect.anchorMax = new Vector2(0.36f, 0.865f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = objectRoot.GetComponent<Image>();
            image.color = new Color(0.32f, 0.035f, 0.055f, 0.96f);
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
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);
            label = textObject.GetComponent<Text>();
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.9f, 0.72f, 1f);
            label.raycastTarget = false;
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
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

        public static int CalculateReward(GameBalanceConfig balance, int round)
        {
            if (balance == null) return 0;
            return Mathf.Max(0, balance.bossQuestBaseGold +
                                Mathf.Max(1, round) * balance.bossQuestGoldPerRound);
        }

        public void RestoreSnapshot(BossQuestSnapshot snapshot)
        {
            cooldownRemaining = Mathf.Max(0f, snapshot.CooldownRemaining);
            challengeRemaining = Mathf.Max(0f, snapshot.ChallengeRemaining);
            rewardGold = Mathf.Max(0, snapshot.RewardGold);
            IsActive = snapshot.IsActive && challengeRemaining > 0f && waves.HasActiveChallengeBoss;
            if (snapshot.IsActive && !IsActive) cooldownRemaining = config.bossQuestCooldown;
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
