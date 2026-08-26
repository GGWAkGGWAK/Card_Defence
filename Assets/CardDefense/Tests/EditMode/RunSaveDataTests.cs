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
        }
    }
}
