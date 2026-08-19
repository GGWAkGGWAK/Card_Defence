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
        private Button summonButton;
        private EconomyService economy;
        private WaveDirector waves;
        private MonsterSystem monsters;
        private CardSummonController summon;
        private float refreshTimer;

        public void Configure(Text gold, Text round, Text alive, Text message, Button button,
            EconomyService economyService, WaveDirector waveDirector, MonsterSystem monsterSystem,
            CardSummonController summonController)
        {
            goldText = gold;
            roundText = round;
            monsterText = alive;
            messageText = message;
            summonButton = button;
            economy = economyService;
            waves = waveDirector;
            monsters = monsterSystem;
            summon = summonController;

            summonButton.onClick.AddListener(summon.SummonFirstAvailable);
            summon.MessageChanged += SetMessage;
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
            if (summonButton != null && summon != null) summonButton.onClick.RemoveListener(summon.SummonFirstAvailable);
            if (summon != null) summon.MessageChanged -= SetMessage;
            if (waves != null) waves.GameLost -= HandleGameLost;
        }

        private void Refresh()
        {
            if (economy == null || waves == null || monsters == null) return;
            goldText.text = "GOLD  " + economy.Gold;
            roundText.text = "ROUND  " + waves.CurrentRound + "  /  " + Mathf.CeilToInt(waves.SecondsToNextRound) + "s";
            monsterText.text = "MONSTERS  " + monsters.ActiveCount;
        }

        private void SetMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        private void HandleGameLost()
        {
            SetMessage("패배: 몬스터 수량 한계 도달");
            summonButton.interactable = false;
        }
    }
}
