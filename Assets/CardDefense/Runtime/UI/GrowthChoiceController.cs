using System;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class GrowthChoiceController : MonoBehaviour
    {
        public event Action<RunGrowthChoice> ChoiceSelected;
        public bool IsChoiceVisible => panel != null && panel.activeSelf;

        private GameObject panel;
        private Text title;
        private Button attackButton;
        private Button goldButton;
        private Button summonButton;
        private WaveDirector waves;
        private RunModifierService modifiers;
        private int offeredRound;

        public void Configure(GameObject panelObject, Text titleText, Button attack, Button gold,
            Button summon, WaveDirector waveDirector, RunModifierService modifierService)
        {
            panel = panelObject;
            title = titleText;
            attackButton = attack;
            goldButton = gold;
            summonButton = summon;
            waves = waveDirector;
            modifiers = modifierService;

            attackButton.onClick.AddListener(SelectAttack);
            goldButton.onClick.AddListener(SelectGold);
            summonButton.onClick.AddListener(SelectSummonDiscount);
            waves.RoundChanged += HandleRoundChanged;
            waves.GameLost += Hide;
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (attackButton != null) attackButton.onClick.RemoveListener(SelectAttack);
            if (goldButton != null) goldButton.onClick.RemoveListener(SelectGold);
            if (summonButton != null) summonButton.onClick.RemoveListener(SelectSummonDiscount);
            if (waves != null)
            {
                waves.RoundChanged -= HandleRoundChanged;
                waves.GameLost -= Hide;
            }
        }

        private void HandleRoundChanged(int round)
        {
            if (round <= 0 || round % 10 != 0 || round == offeredRound) return;
            offeredRound = round;
            title.text = "ROUND " + round + " 성장 선택";
            panel.SetActive(true);
        }

        public void SelectAttack() => Apply(RunGrowthChoice.AttackPower);
        public void SelectGold() => Apply(RunGrowthChoice.KillGold);
        public void SelectSummonDiscount() => Apply(RunGrowthChoice.SummonDiscount);

#if UNITY_EDITOR
        public void OfferForTesting(int round) => HandleRoundChanged(round);
#endif

        private void Apply(RunGrowthChoice choice)
        {
            if (!IsChoiceVisible) return;
            modifiers.Apply(choice);
            panel.SetActive(false);
            ChoiceSelected?.Invoke(choice);
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        public GrowthChoiceSnapshot CaptureSnapshot()
        {
            return new GrowthChoiceSnapshot { OfferedRound = offeredRound, IsVisible = IsChoiceVisible };
        }

        public void RestoreSnapshot(GrowthChoiceSnapshot snapshot)
        {
            offeredRound = Mathf.Max(0, snapshot.OfferedRound);
            if (snapshot.IsVisible && offeredRound > 0)
            {
                title.text = "ROUND " + offeredRound + " 성장 선택";
                panel.SetActive(true);
            }
            else
            {
                panel.SetActive(false);
            }
        }
    }
}
