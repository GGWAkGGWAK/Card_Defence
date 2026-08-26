using System;
using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Core
{
    public sealed class RunStatisticsService : MonoBehaviour
    {
        public int HighestRound { get; private set; }
        public int MonstersDefeated { get; private set; }
        public int GoldEarned { get; private set; }
        public int CardsSummoned { get; private set; }
        public int HandsMerged { get; private set; }
        public int UpgradesPurchased { get; private set; }
        public float ElapsedGameSeconds { get; private set; }

        private WaveDirector waves;
        private CardSummonController summon;
        private PokerProgressionService progression;

        public void Configure(WaveDirector waveDirector, CardSummonController summonController,
            PokerProgressionService progressionService)
        {
            waves = waveDirector;
            summon = summonController;
            progression = progressionService;
            waves.RoundChanged += HandleRoundChanged;
            waves.MonsterDefeated += HandleMonsterDefeated;
            summon.CardSummoned += HandleCardSummoned;
            summon.CardsMerged += HandleCardsMerged;
            progression.HandUpgraded += HandleHandUpgraded;
        }

        private void Update()
        {
            if (waves != null && !waves.IsGameOver) ElapsedGameSeconds += Time.deltaTime;
        }

        private void OnDestroy()
        {
            if (waves != null)
            {
                waves.RoundChanged -= HandleRoundChanged;
                waves.MonsterDefeated -= HandleMonsterDefeated;
            }
            if (summon != null)
            {
                summon.CardSummoned -= HandleCardSummoned;
                summon.CardsMerged -= HandleCardsMerged;
            }
            if (progression != null) progression.HandUpgraded -= HandleHandUpgraded;
        }

        private void HandleRoundChanged(int round) => HighestRound = Mathf.Max(HighestRound, round);

        private void HandleMonsterDefeated(int reward)
        {
            MonstersDefeated++;
            GoldEarned += reward;
        }

        private void HandleCardSummoned() => CardsSummoned++;
        private void HandleCardsMerged(PokerHand hand) => HandsMerged++;
        private void HandleHandUpgraded(PokerHand hand, int level) => UpgradesPurchased++;

        public string GetRunSummary()
        {
            int minutes = Mathf.FloorToInt(ElapsedGameSeconds / 60f);
            int seconds = Mathf.FloorToInt(ElapsedGameSeconds % 60f);
            return "R" + HighestRound + " · 처치 " + MonstersDefeated + " · 획득 " + GoldEarned +
                   "G · 소환 " + CardsSummoned + " · 합성 " + HandsMerged + " · " +
                   minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        public RunStatisticsSnapshot CaptureSnapshot()
        {
            return new RunStatisticsSnapshot
            {
                HighestRound = HighestRound,
                MonstersDefeated = MonstersDefeated,
                GoldEarned = GoldEarned,
                CardsSummoned = CardsSummoned,
                HandsMerged = HandsMerged,
                UpgradesPurchased = UpgradesPurchased,
                ElapsedGameSeconds = ElapsedGameSeconds
            };
        }

        public void RestoreSnapshot(RunStatisticsSnapshot snapshot)
        {
            HighestRound = Mathf.Max(0, snapshot.HighestRound);
            MonstersDefeated = Mathf.Max(0, snapshot.MonstersDefeated);
            GoldEarned = Mathf.Max(0, snapshot.GoldEarned);
            CardsSummoned = Mathf.Max(0, snapshot.CardsSummoned);
            HandsMerged = Mathf.Max(0, snapshot.HandsMerged);
            UpgradesPurchased = Mathf.Max(0, snapshot.UpgradesPurchased);
            ElapsedGameSeconds = Mathf.Max(0f, snapshot.ElapsedGameSeconds);
        }
    }
}
