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
            BeginNextRound();
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
            if (defeated)
            {
                int adjustedReward = modifiers != null ? modifiers.ApplyKillGold(reward) : reward;
                economy.AddGold(adjustedReward);
                MonsterDefeated?.Invoke(adjustedReward);
            }
            pool.Release(monster);
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
        }
    }
}
