using System;
using System.Collections.Generic;
using System.IO;
using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Enemies;
using CardDefense.UI;
using UnityEngine;

namespace CardDefense.Core
{
    public sealed class RunStatisticsService : MonoBehaviour
    {
        private static readonly int[] CheckpointRounds = { 10, 20, 30, 50 };

        public int HighestRound { get; private set; }
        public int MonstersDefeated { get; private set; }
        public int GoldEarned { get; private set; }
        public int CardsSummoned { get; private set; }
        public int HandsMerged { get; private set; }
        public int UpgradesPurchased { get; private set; }
        public int CardsSold { get; private set; }
        public int BossQuestWins { get; private set; }
        public int BossQuestFailures { get; private set; }
        public int RegularBossesDefeated { get; private set; }
        public float FastestBossKillSeconds { get; private set; }
        public float LastBossKillSeconds { get; private set; }
        public int PeakGold { get; private set; }
        public float PeakDps { get; private set; }
        public int PeakMonsters { get; private set; }
        public string DefeatReason { get; private set; }
        public string LastExportPath { get; private set; }
        public float ElapsedGameSeconds { get; private set; }
        public IReadOnlyList<BalanceCheckpointSnapshot> Checkpoints => checkpoints;

        private readonly List<BalanceCheckpointSnapshot> checkpoints =
            new List<BalanceCheckpointSnapshot>(CheckpointRounds.Length);
        private WaveDirector waves;
        private CardSummonController summon;
        private PokerProgressionService progression;
        private EconomyService economy;
        private CardTowerSystem towers;
        private MonsterSystem monsters;
        private BossQuestController bossQuest;
        private bool exported;

        public void Configure(WaveDirector waveDirector, CardSummonController summonController,
            PokerProgressionService progressionService, EconomyService economyService,
            CardTowerSystem towerSystem, MonsterSystem monsterSystem)
        {
            waves = waveDirector;
            summon = summonController;
            progression = progressionService;
            economy = economyService;
            towers = towerSystem;
            monsters = monsterSystem;
            waves.RoundChanged += HandleRoundChanged;
            waves.MonsterDefeated += HandleMonsterDefeated;
            waves.RegularBossDefeated += HandleRegularBossDefeated;
            waves.GameLost += HandleGameLost;
            summon.CardSummoned += HandleCardSummoned;
            summon.CardsMerged += HandleCardsMerged;
            summon.CardSold += HandleCardSold;
            progression.HandUpgraded += HandleHandUpgraded;
            UpdatePeaks();
        }

        public void BindBossQuest(BossQuestController controller)
        {
            if (bossQuest != null) bossQuest.QuestCompleted -= HandleBossQuestCompleted;
            bossQuest = controller;
            if (bossQuest != null) bossQuest.QuestCompleted += HandleBossQuestCompleted;
        }

        private void Update()
        {
            if (waves == null || waves.IsGameOver) return;
            ElapsedGameSeconds += Time.deltaTime;
            UpdatePeaks();
        }

        private void OnDestroy()
        {
            if (waves != null)
            {
                waves.RoundChanged -= HandleRoundChanged;
                waves.MonsterDefeated -= HandleMonsterDefeated;
                waves.RegularBossDefeated -= HandleRegularBossDefeated;
                waves.GameLost -= HandleGameLost;
            }
            if (summon != null)
            {
                summon.CardSummoned -= HandleCardSummoned;
                summon.CardsMerged -= HandleCardsMerged;
                summon.CardSold -= HandleCardSold;
            }
            if (progression != null) progression.HandUpgraded -= HandleHandUpgraded;
            if (bossQuest != null) bossQuest.QuestCompleted -= HandleBossQuestCompleted;
        }

        private void HandleRoundChanged(int round)
        {
            HighestRound = Mathf.Max(HighestRound, round);
            for (int i = 0; i < CheckpointRounds.Length; i++)
            {
                if (round != CheckpointRounds[i] || ContainsCheckpoint(round)) continue;
                checkpoints.Add(CaptureCheckpoint(round));
                break;
            }
        }

        private bool ContainsCheckpoint(int round)
        {
            for (int i = 0; i < checkpoints.Count; i++)
                if (checkpoints[i].Round == round) return true;
            return false;
        }

        private void HandleMonsterDefeated(int reward)
        {
            MonstersDefeated++;
            GoldEarned += reward;
        }

        private void HandleCardSummoned() => CardsSummoned++;
        private void HandleCardsMerged(PokerHand hand) => HandsMerged++;
        private void HandleCardSold(int refund) => CardsSold++;
        private void HandleHandUpgraded(PokerHand hand, int level) => UpgradesPurchased++;

        private void HandleBossQuestCompleted(bool success, int reward)
        {
            if (success) BossQuestWins++;
            else BossQuestFailures++;
        }

        private void HandleRegularBossDefeated(int round, float killSeconds)
        {
            RegularBossesDefeated++;
            LastBossKillSeconds = Mathf.Max(0f, killSeconds);
            if (FastestBossKillSeconds <= 0f || LastBossKillSeconds < FastestBossKillSeconds)
                FastestBossKillSeconds = LastBossKillSeconds;
        }

        private void HandleGameLost()
        {
            if (exported) return;
            exported = true;
            UpdatePeaks();
            DefeatReason = string.IsNullOrEmpty(waves.LastGameOverReason)
                ? "몬스터 수량 한계 도달"
                : waves.LastGameOverReason;
            ExportBalanceReport();
        }

        private void UpdatePeaks()
        {
            if (economy != null) PeakGold = Mathf.Max(PeakGold, economy.Gold);
            if (towers != null) PeakDps = Mathf.Max(PeakDps, towers.EstimatedTotalDps);
            if (monsters != null) PeakMonsters = Mathf.Max(PeakMonsters, monsters.ActiveCount);
        }

        public string GetRunSummary()
        {
            int minutes = Mathf.FloorToInt(ElapsedGameSeconds / 60f);
            int seconds = Mathf.FloorToInt(ElapsedGameSeconds % 60f);
            return "R" + HighestRound + " · 처치 " + MonstersDefeated + " · 획득 " + GoldEarned +
                   "G · 소환 " + CardsSummoned + " · 합성 " + HandsMerged + " · " +
                   minutes.ToString("00") + ":" + seconds.ToString("00");
        }

        public string GetDetailedRunSummary()
        {
            string reason = string.IsNullOrEmpty(DefeatReason) ? "진행 중" : DefeatReason;
            string bossTime = LastBossKillSeconds > 0f ? LastBossKillSeconds.ToString("0.0") + "초" : "기록 없음";
            return GetRunSummary() + "\n" +
                   "패배 원인  " + reason + "\n" +
                   "강화 " + UpgradesPurchased + " · 판매 " + CardsSold +
                   " · 도전 성공/실패 " + BossQuestWins + "/" + BossQuestFailures + "\n" +
                   "최대 DPS " + PeakDps.ToString("0") + " · 필요 DPS " +
                   (waves != null ? waves.CurrentRequiredDps.ToString("0") : "0") +
                   " · 최대 몬스터 " + PeakMonsters + "\n" +
                   "정규 보스 처치 " + RegularBossesDefeated + " · 최근 처치시간 " + bossTime;
        }

        public string GetCheckpointSummary()
        {
            if (checkpoints.Count == 0) return "체크포인트 기록 없음 (10·20·30·50 라운드)";
            string value = string.Empty;
            for (int i = 0; i < checkpoints.Count; i++)
            {
                BalanceCheckpointSnapshot checkpoint = checkpoints[i];
                if (i > 0) value += "\n";
                value += "R" + checkpoint.Round + "  DPS " + checkpoint.TotalDps.ToString("0") + "/" +
                         checkpoint.RequiredDps.ToString("0") + "  골드 " + checkpoint.Gold +
                         "  몬스터 " + checkpoint.ActiveMonsters;
            }
            return value;
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
                CardsSold = CardsSold,
                BossQuestWins = BossQuestWins,
                BossQuestFailures = BossQuestFailures,
                RegularBossesDefeated = RegularBossesDefeated,
                FastestBossKillSeconds = FastestBossKillSeconds,
                LastBossKillSeconds = LastBossKillSeconds,
                PeakGold = PeakGold,
                PeakDps = PeakDps,
                PeakMonsters = PeakMonsters,
                DefeatReason = DefeatReason,
                Checkpoints = new List<BalanceCheckpointSnapshot>(checkpoints),
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
            CardsSold = Mathf.Max(0, snapshot.CardsSold);
            BossQuestWins = Mathf.Max(0, snapshot.BossQuestWins);
            BossQuestFailures = Mathf.Max(0, snapshot.BossQuestFailures);
            RegularBossesDefeated = Mathf.Max(0, snapshot.RegularBossesDefeated);
            FastestBossKillSeconds = Mathf.Max(0f, snapshot.FastestBossKillSeconds);
            LastBossKillSeconds = Mathf.Max(0f, snapshot.LastBossKillSeconds);
            PeakGold = Mathf.Max(0, snapshot.PeakGold);
            PeakDps = Mathf.Max(0f, snapshot.PeakDps);
            PeakMonsters = Mathf.Max(0, snapshot.PeakMonsters);
            DefeatReason = snapshot.DefeatReason;
            checkpoints.Clear();
            if (snapshot.Checkpoints != null) checkpoints.AddRange(snapshot.Checkpoints);
            ElapsedGameSeconds = Mathf.Max(0f, snapshot.ElapsedGameSeconds);
            UpdatePeaks();
        }

        private BalanceCheckpointSnapshot CaptureCheckpoint(int round)
        {
            return new BalanceCheckpointSnapshot
            {
                Round = round,
                ElapsedGameSeconds = ElapsedGameSeconds,
                Gold = economy != null ? economy.Gold : 0,
                TotalDps = towers != null ? towers.EstimatedTotalDps : 0f,
                RequiredDps = waves != null ? waves.CurrentRequiredDps : 0f,
                ActiveMonsters = monsters != null ? monsters.ActiveCount : 0,
                ActiveTowers = towers != null ? towers.ActiveCount : 0,
                CardsSummoned = CardsSummoned,
                HandsMerged = HandsMerged,
                UpgradesPurchased = UpgradesPurchased,
                CardsSold = CardsSold,
                BossQuestWins = BossQuestWins,
                BossQuestFailures = BossQuestFailures,
                RegularBossesDefeated = RegularBossesDefeated
            };
        }

        private void ExportBalanceReport()
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "BalanceLogs");
                Directory.CreateDirectory(directory);
                BalanceRunReport report = new BalanceRunReport
                {
                    RecordedAtUtc = DateTime.UtcNow.ToString("O"),
                    Statistics = CaptureSnapshot(),
                    FinalState = CaptureCheckpoint(HighestRound)
                };
                LastExportPath = Path.Combine(directory,
                    "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
                File.WriteAllText(LastExportPath, JsonUtility.ToJson(report, true));
            }
            catch (Exception exception)
            {
                LastExportPath = string.Empty;
                Debug.LogWarning("밸런스 로그 저장 실패: " + exception.Message);
            }
        }
    }
}
