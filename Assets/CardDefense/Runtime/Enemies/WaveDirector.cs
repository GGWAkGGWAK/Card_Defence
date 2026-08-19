using System;
using CardDefense.Core;
using CardDefense.Pooling;
using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class WaveDirector : MonoBehaviour
    {
        public event Action<int> RoundChanged;
        public event Action GameLost;

        public int CurrentRound { get; private set; }
        public float SecondsToNextRound { get; private set; }
        public bool IsGameOver { get; private set; }

        private GameBalanceConfig config;
        private LoopPath path;
        private MonsterPool pool;
        private MonsterSystem monsters;
        private EconomyService economy;
        private int remainingToSpawn;
        private float spawnTimer;
        private Action<Monster, bool, int> releaseHandler;

        public void Configure(GameBalanceConfig balance, LoopPath loopPath, MonsterPool monsterPool,
            MonsterSystem monsterSystem, EconomyService economyService)
        {
            config = balance;
            path = loopPath;
            pool = monsterPool;
            monsters = monsterSystem;
            economy = economyService;
            releaseHandler = HandleMonsterRelease;
            SecondsToNextRound = 0f;
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

            if (remainingToSpawn <= 0) return;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;

            SpawnOne();
            remainingToSpawn--;
            spawnTimer = config.spawnInterval;
        }

        private void BeginNextRound()
        {
            CurrentRound++;
            SecondsToNextRound = config.roundDuration;
            remainingToSpawn += config.baseMonstersPerRound +
                                ((CurrentRound - 1) * config.extraMonstersPerRound);
            RoundChanged?.Invoke(CurrentRound);
        }

        private void SpawnOne()
        {
            Monster monster = pool.Get();
            monster.transform.SetParent(null, true);

            float milestone = 1f + (Mathf.Floor((CurrentRound - 1) / 10f) * config.milestoneHealthBonus);
            float health = config.baseMonsterHealth *
                           Mathf.Pow(config.healthGrowthPerRound, CurrentRound - 1) * milestone;
            int reward = Mathf.CeilToInt(config.baseKillGold *
                                         Mathf.Pow(config.rewardGrowthPerRound, CurrentRound - 1));

            monster.Spawn(path, health, config.monsterMoveSpeed, reward, releaseHandler);
            monsters.Register(monster);

            if (monsters.ActiveCount >= config.defeatMonsterLimit) LoseGame();
        }

        private void HandleMonsterRelease(Monster monster, bool defeated, int reward)
        {
            monsters.Unregister(monster);
            if (defeated) economy.AddGold(reward);
            pool.Release(monster);
        }

        private void LoseGame()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            GameLost?.Invoke();
            Time.timeScale = 0f;
        }
    }
}
