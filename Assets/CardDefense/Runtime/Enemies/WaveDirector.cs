using System;
using System.Collections.Generic;
using CardDefense.Core;
using CardDefense.Pooling;
using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class WaveDirector : MonoBehaviour
    {
        public event Action<int> RoundChanged;
        public event Action GameLost;
        public event Action<int> MonsterDefeated;
        public event Action<Vector3, int> MonsterRewarded;
        public event Action ChallengeBossDefeated;
        public event Action ChallengeBossSpawned;

        public int CurrentRound { get; private set; }
        public float SecondsToNextRound { get; private set; }
        public bool IsGameOver { get; private set; }
        public float CurrentRequiredDps { get; private set; }

        private readonly Queue<WaveSpawnBatch> pendingBatches = new Queue<WaveSpawnBatch>(8);
        private GameBalanceConfig config;
        private LoopPath path;
        private MonsterPool pool;
        private MonsterSystem monsters;
        private EconomyService economy;
        private float spawnTimer;
        private Action<Monster, bool, int> releaseHandler;
        private RunModifierService modifiers;
        private bool restoredState;
        private Monster challengeBoss;

        public bool HasActiveChallengeBoss => challengeBoss != null && challengeBoss.IsAlive;
        public float ChallengeBossHealthNormalized => HasActiveChallengeBoss
            ? Mathf.Clamp01(challengeBoss.Health / Mathf.Max(1f, challengeBoss.MaxHealth))
            : 0f;
        public int LastFailureReinforcements { get; private set; }

        public void Configure(GameBalanceConfig balance, LoopPath loopPath, MonsterPool monsterPool,
            MonsterSystem monsterSystem, EconomyService economyService)
        {
            config = balance;
            path = loopPath;
            pool = monsterPool;
            monsters = monsterSystem;
            economy = economyService;
            releaseHandler = HandleMonsterRelease;
            pendingBatches.Clear();
            SecondsToNextRound = 0f;
        }

        public void SetRunModifiers(RunModifierService modifierService)
        {
            modifiers = modifierService;
        }

        private void Start()
        {
            if (!restoredState) BeginNextRound();
        }

        private void Update()
        {
            if (IsGameOver || config == null) return;

            SecondsToNextRound -= Time.deltaTime;
            if (SecondsToNextRound <= 0f) BeginNextRound();
            if (pendingBatches.Count == 0) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            WaveSpawnBatch batch = pendingBatches.Peek();
            SpawnOne(batch.Round, batch.SpawnedCount);
            batch.SpawnedCount++;
            batch.Remaining--;
            if (batch.Remaining <= 0) pendingBatches.Dequeue();
            spawnTimer = CalculateSpawnInterval(config, batch.TotalCount);
        }

        private void BeginNextRound()
        {
            CurrentRound++;
            SecondsToNextRound = config.roundDuration;
            RoundBalanceSnapshot balance = RoundBalanceCalculator.Calculate(config, CurrentRound);
            pendingBatches.Enqueue(new WaveSpawnBatch(CurrentRound, balance.MonsterCount));
            CurrentRequiredDps = AdjustedRoundBalanceCalculator.Calculate(config, CurrentRound).RequiredDps;
            RoundChanged?.Invoke(CurrentRound);
        }

        private void SpawnOne(int round, int spawnIndex)
        {
            Monster monster = pool.Get();
            monster.transform.SetParent(null, true);
            RoundBalanceSnapshot balance = RoundBalanceCalculator.Calculate(config, round);
            MonsterArchetype archetype = MonsterArchetypeRules.Select(round, spawnIndex);
            MonsterArchetypeStats stats = MonsterArchetypeRules.GetStats(config, archetype);
            float health = balance.HealthPerMonster * stats.HealthMultiplier;
            float speed = config.monsterMoveSpeed * stats.SpeedMultiplier;
            int reward = Mathf.Max(1, Mathf.CeilToInt(balance.RewardPerMonster * stats.RewardMultiplier));

            monster.Spawn(path, archetype, health, speed, reward, releaseHandler);
            monsters.Register(monster);
            if (monsters.ActiveCount >= config.defeatMonsterLimit) LoseGame();
        }

        private void HandleMonsterRelease(Monster monster, bool defeated, int reward)
        {
            monsters.Unregister(monster);
            bool wasChallengeBoss = monster == challengeBoss;
            if (wasChallengeBoss)
            {
                challengeBoss = null;
                if (defeated) ChallengeBossDefeated?.Invoke();
            }
            if (defeated)
            {
                int adjustedReward = modifiers != null ? modifiers.ApplyKillGold(reward) : reward;
                economy.AddGold(adjustedReward);
                MonsterRewarded?.Invoke(monster.transform.position, adjustedReward);
                MonsterDefeated?.Invoke(adjustedReward);
            }
            pool.Release(monster);
        }

        public bool TrySpawnChallengeBoss()
        {
            return TrySpawnChallengeBoss(1f);
        }

        public bool TrySpawnChallengeBoss(float difficultyMultiplier)
        {
            if (IsGameOver || HasActiveChallengeBoss || config == null || pool == null) return false;
            RoundBalanceSnapshot balance = RoundBalanceCalculator.Calculate(config, Mathf.Max(1, CurrentRound));
            Monster monster = pool.Get();
            monster.transform.SetParent(null, true);
            float health = balance.HealthPerMonster * config.bossQuestHealthMultiplier *
                           Mathf.Max(1f, difficultyMultiplier);
            float speed = config.monsterMoveSpeed * config.bossSpeedMultiplier;
            monster.Spawn(path, MonsterArchetype.Boss, health, speed, 0, releaseHandler);
            challengeBoss = monster;
            monsters.Register(monster);
            ChallengeBossSpawned?.Invoke();
            if (monsters.ActiveCount >= config.defeatMonsterLimit) LoseGame();
            return true;
        }

        public bool ApplyChallengeFailure(int reinforcementCount, float healthBonus, float speedBonus)
        {
            if (!HasActiveChallengeBoss || config == null) return false;
            challengeBoss.Enrage(healthBonus, speedBonus);
            LastFailureReinforcements = Mathf.Max(0, reinforcementCount);
            if (LastFailureReinforcements > 0)
                pendingBatches.Enqueue(new WaveSpawnBatch(Mathf.Max(1, CurrentRound),
                    LastFailureReinforcements));
            return true;
        }

        private void LoseGame()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            GameLost?.Invoke();
            Time.timeScale = 0f;
        }

        public static float CalculateSpawnInterval(GameBalanceConfig balance, int monsterCount)
        {
            if (balance == null) return 0.65f;
            float waveFitInterval = balance.roundDuration * 0.85f / Mathf.Max(1, monsterCount);
            return Mathf.Max(0.05f, Mathf.Min(balance.spawnInterval, waveFitInterval));
        }

        public WaveDirectorSnapshot CaptureSnapshot()
        {
            WaveDirectorSnapshot snapshot = new WaveDirectorSnapshot
            {
                CurrentRound = CurrentRound,
                SecondsToNextRound = SecondsToNextRound,
                SpawnTimer = spawnTimer
            };
            foreach (WaveSpawnBatch batch in pendingBatches)
            {
                snapshot.PendingBatches.Add(new WaveSpawnBatchSnapshot
                {
                    Round = batch.Round,
                    Remaining = batch.Remaining,
                    SpawnedCount = batch.SpawnedCount,
                    TotalCount = batch.TotalCount
                });
            }
            return snapshot;
        }

        public void RestoreSnapshot(WaveDirectorSnapshot snapshot, List<MonsterSnapshot> monsterSnapshots)
        {
            if (snapshot == null) return;
            CurrentRound = Mathf.Max(1, snapshot.CurrentRound);
            SecondsToNextRound = Mathf.Clamp(snapshot.SecondsToNextRound, 0.01f, config.roundDuration);
            spawnTimer = Mathf.Max(0f, snapshot.SpawnTimer);
            CurrentRequiredDps = AdjustedRoundBalanceCalculator.Calculate(config, CurrentRound).RequiredDps;
            pendingBatches.Clear();
            if (snapshot.PendingBatches != null)
            {
                for (int i = 0; i < snapshot.PendingBatches.Count; i++)
                {
                    WaveSpawnBatchSnapshot saved = snapshot.PendingBatches[i];
                    if (saved.Remaining <= 0) continue;
                    pendingBatches.Enqueue(new WaveSpawnBatch(saved.Round, saved.Remaining,
                        saved.SpawnedCount, saved.TotalCount));
                }
            }
            if (monsterSnapshots != null)
            {
                for (int i = 0; i < monsterSnapshots.Count; i++)
                {
                    Monster monster = pool.Get();
                    monster.transform.SetParent(null, true);
                    monster.Restore(path, monsterSnapshots[i], releaseHandler);
                    monsters.Register(monster);
                    if (monsterSnapshots[i].Archetype == MonsterArchetype.Boss &&
                        monsterSnapshots[i].Reward <= 0) challengeBoss = monster;
                }
            }
            restoredState = true;
        }

        private sealed class WaveSpawnBatch
        {
            public readonly int Round;
            public int Remaining;
            public int SpawnedCount;
            public readonly int TotalCount;

            public WaveSpawnBatch(int round, int count)
            {
                Round = round;
                Remaining = count;
                SpawnedCount = 0;
                TotalCount = count;
            }

            public WaveSpawnBatch(int round, int remaining, int spawnedCount, int totalCount)
            {
                Round = round;
                Remaining = remaining;
                SpawnedCount = spawnedCount;
                TotalCount = Mathf.Max(remaining + spawnedCount, totalCount);
            }
        }
    }
}
