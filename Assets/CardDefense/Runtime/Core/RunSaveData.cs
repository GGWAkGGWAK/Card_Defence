using System;
using System.Collections.Generic;
using CardDefense.Cards;
using CardDefense.Enemies;

namespace CardDefense.Core
{
    [Serializable]
    public sealed class RunSaveData
    {
        public int Version = 1;
        public int Gold;
        public int[] HandLevels;
        public RunModifierSnapshot Modifiers;
        public RunStatisticsSnapshot Statistics;
        public WaveDirectorSnapshot Wave;
        public GrowthChoiceSnapshot Growth;
        public BossQuestSnapshot BossQuest;
        public List<CardTowerSnapshot> Towers = new List<CardTowerSnapshot>();
        public List<MonsterSnapshot> Monsters = new List<MonsterSnapshot>();
    }

    [Serializable]
    public struct RunModifierSnapshot
    {
        public float DamageMultiplier;
        public float KillGoldMultiplier;
        public float SummonCostMultiplier;
        public int ChoiceCount;
        public int BossQuestWins;
    }

    [Serializable]
    public struct RunStatisticsSnapshot
    {
        public int HighestRound;
        public int MonstersDefeated;
        public int GoldEarned;
        public int CardsSummoned;
        public int HandsMerged;
        public int UpgradesPurchased;
        public float ElapsedGameSeconds;
    }

    [Serializable]
    public struct CardTowerSnapshot
    {
        public int SlotIndex;
        public PlayingCard Card;
        public PokerHand Hand;
        public bool IsFusionResult;
        public float BaseDamage;
        public int FusionCoreCardCount;
    }

    [Serializable]
    public struct MonsterSnapshot
    {
        public MonsterArchetype Archetype;
        public float Health;
        public float MaxHealth;
        public float MoveSpeed;
        public float Progress;
        public int Reward;
    }

    [Serializable]
    public struct WaveSpawnBatchSnapshot
    {
        public int Round;
        public int Remaining;
        public int SpawnedCount;
        public int TotalCount;
    }

    [Serializable]
    public sealed class WaveDirectorSnapshot
    {
        public int CurrentRound;
        public float SecondsToNextRound;
        public float SpawnTimer;
        public List<WaveSpawnBatchSnapshot> PendingBatches = new List<WaveSpawnBatchSnapshot>();
    }

    [Serializable]
    public struct GrowthChoiceSnapshot
    {
        public int OfferedRound;
        public bool IsVisible;
    }

    [Serializable]
    public struct BossQuestSnapshot
    {
        public float CooldownRemaining;
        public float ChallengeRemaining;
        public bool IsActive;
        public int RewardGold;
    }
}
