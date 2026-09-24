using System.Collections.Generic;
using CardDefense.Cards;
using CardDefense.Core;
using CardDefense.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace CardDefense.Tests
{
    public sealed class RunSaveDataTests
    {
        [Test]
        public void FullRunSnapshotRoundTripsThroughJson()
        {
            RunSaveData data = new RunSaveData
            {
                Gold = 777,
                HandLevels = new[] { 1, 2, 3 },
                Modifiers = new RunModifierSnapshot
                {
                    DamageMultiplier = 1.15f,
                    KillGoldMultiplier = 1.12f,
                    SummonCostMultiplier = 0.9f,
                    ChoiceCount = 3
                },
                Statistics = new RunStatisticsSnapshot
                {
                    HighestRound = 30,
                    CardsSold = 4,
                    BossQuestWins = 2,
                    BossQuestFailures = 1,
                    PeakDps = 987.6f,
                    DefeatReason = "몬스터 수량 한계 도달",
                    Checkpoints = new List<BalanceCheckpointSnapshot>
                    {
                        new BalanceCheckpointSnapshot
                        {
                            Round = 30, Gold = 222, TotalDps = 987.6f,
                            RequiredDps = 1100f, ActiveMonsters = 26
                        }
                    }
                },
                Wave = new WaveDirectorSnapshot { CurrentRound = 23, SecondsToNextRound = 8.5f },
                Towers = new List<CardTowerSnapshot>
                {
                    new CardTowerSnapshot
                    {
                        SlotIndex = 4,
                        Card = new PlayingCard(CardSuit.Heart, CardRank.Seven),
                        Hand = PokerHand.OnePair,
                        IsFusionResult = true,
                        BaseDamage = 21.24f,
                        FusionCoreCardCount = 2
                    }
                },
                Monsters = new List<MonsterSnapshot>
                {
                    new MonsterSnapshot
                    {
                        Archetype = MonsterArchetype.Boss, Health = 50f, MaxHealth = 100f,
                        MoveSpeed = 1f, Progress = 0.4f, Reward = 20
                    }
                }
            };

            string json = JsonUtility.ToJson(data);
            RunSaveData restored = JsonUtility.FromJson<RunSaveData>(json);

            Assert.AreEqual(777, restored.Gold);
            Assert.AreEqual(23, restored.Wave.CurrentRound);
            Assert.AreEqual(CardRank.Seven, restored.Towers[0].Card.Rank);
            Assert.AreEqual(PokerHand.OnePair, restored.Towers[0].Hand);
            Assert.AreEqual(MonsterArchetype.Boss, restored.Monsters[0].Archetype);
            Assert.AreEqual(0.4f, restored.Monsters[0].Progress, 0.001f);
            Assert.AreEqual(4, restored.Statistics.CardsSold);
            Assert.AreEqual(2, restored.Statistics.BossQuestWins);
            Assert.AreEqual(1, restored.Statistics.BossQuestFailures);
            Assert.AreEqual(30, restored.Statistics.Checkpoints[0].Round);
            Assert.AreEqual(1100f, restored.Statistics.Checkpoints[0].RequiredDps, 0.001f);
            Assert.AreEqual("몬스터 수량 한계 도달", restored.Statistics.DefeatReason);
        }
    }
}
