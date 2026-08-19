using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;
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
        private Button summonButton;
        private Button mergeButton;
        private Button upgradeButton;
        private EconomyService economy;
        private WaveDirector waves;
        private MonsterSystem monsters;
        private CardSummonController summon;
        private float refreshTimer;

        public void Configure(Text gold, Text round, Text alive, Text message, Text selection,
            Button summonButtonReference, Button mergeButtonReference, Button upgradeButtonReference,
            EconomyService economyService, WaveDirector waveDirector, MonsterSystem monsterSystem,
            CardSummonController summonController)
        {
            goldText = gold;
            roundText = round;
            monsterText = alive;
            messageText = message;
            selectionText = selection;
            summonButton = summonButtonReference;
            mergeButton = mergeButtonReference;
            upgradeButton = upgradeButtonReference;
            economy = economyService;
            waves = waveDirector;
            monsters = monsterSystem;
            summon = summonController;

            summonButton.onClick.AddListener(summon.BeginSummonPlacement);
            mergeButton.onClick.AddListener(summon.MergeSelected);
            upgradeButton.onClick.AddListener(summon.UpgradeSelectedHand);
            summon.MessageChanged += SetMessage;
            summon.SelectionChanged += RefreshSelection;
            waves.GameLost += HandleGameLost;
            Refresh();
        }

        private void Update()
        {
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
            if (summon != null) summon.MessageChanged -= SetMessage;
            if (summon != null) summon.SelectionChanged -= RefreshSelection;
            if (waves != null) waves.GameLost -= HandleGameLost;
        }

        private void Refresh()
        {
            if (economy == null || waves == null || monsters == null) return;
            goldText.text = "GOLD  " + economy.Gold;
            roundText.text = "ROUND  " + waves.CurrentRound + "  /  " + Mathf.CeilToInt(waves.SecondsToNextRound) + "s";
            monsterText.text = "MONSTERS  " + monsters.ActiveCount;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (selectionText == null || summon == null) return;
            selectionText.text = summon.GetSelectionSummary();
            if (waves != null && !waves.IsGameOver)
            {
                mergeButton.interactable = summon.CanMergeSelection;
                upgradeButton.interactable = summon.CanUpgradeSelection;
            }
        }

        private void SetMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        private void HandleGameLost()
        {
            SetMessage("패배: 몬스터 수량 한계 도달");
            summonButton.interactable = false;
            mergeButton.interactable = false;
            upgradeButton.interactable = false;
        }
    }
}
