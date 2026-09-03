using System.Globalization;
using System.IO;
using System.Text;
using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
using UnityEditor;
using UnityEngine;

namespace CardDefense.Editor
{
    public static class BalanceReportGenerator
    {
        private const string ConfigPath = "Assets/CardDefense/Generated/GameBalanceConfig.asset";

        [MenuItem("Card Defense/Generate Round Balance CSV")]
        public static void GenerateDefault()
        {
            GameBalanceConfig config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(ConfigPath);
            if (config == null) throw new FileNotFoundException("Balance config not found", ConfigPath);

            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "Balance");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "round_balance_1_150.csv");
            StringBuilder csv = new StringBuilder(16384);
            csv.AppendLine("라운드,몬스터수,소환간격(초),웨이브소환시간(초),기본몬스터개별체력,특수포함총체력,획득가능골드,요구초당피해량(DPS),보스체력,보스보상골드,누적골드,누적골드기준소환횟수,A기준하이합성DPS,A원페어합성DPS,A로열합성DPS,요구하이타워수,요구원페어타워수,요구로열타워수,12카드상태소환비용,예산기반원페어강화레벨,예상원페어필드DPS,DPS충족률,경제압력");
            long cumulativeGold = config.startingGold;
            float highDps = ReferenceFusionDps(config, PokerHand.High, 0);
            float pairDps = ReferenceFusionDps(config, PokerHand.OnePair, 0);
            float royalDps = ReferenceFusionDps(config, PokerHand.RoyalStraightFlush, 0);
            for (int round = 1; round <= 150; round++)
            {
                RoundBalanceSnapshot row = RoundBalanceCalculator.Calculate(config, round);
                AdjustedRoundBalanceSnapshot adjusted = AdjustedRoundBalanceCalculator.Calculate(config, round);
                EndlessBalanceSnapshot simulation = EndlessBalanceSimulator.Calculate(config, round);
                cumulativeGold += adjusted.PotentialGold;
                csv.Append(row.Round).Append(',')
                    .Append(row.MonsterCount).Append(',')
                    .Append(WaveDirector.CalculateSpawnInterval(config, row.MonsterCount)
                        .ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append((WaveDirector.CalculateSpawnInterval(config, row.MonsterCount) * row.MonsterCount)
                        .ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.HealthPerMonster.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(adjusted.TotalHealth.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(adjusted.PotentialGold).Append(',')
                    .Append(adjusted.RequiredDps.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(adjusted.BossHealth.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(adjusted.BossReward).Append(',')
                    .Append(cumulativeGold).Append(',')
                    .Append((cumulativeGold / (double)config.summonCost).ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(highDps.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(pairDps.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(royalDps.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Mathf.CeilToInt(adjusted.RequiredDps / highDps)).Append(',')
                    .Append(Mathf.CeilToInt(adjusted.RequiredDps / pairDps)).Append(',')
                    .Append(Mathf.CeilToInt(adjusted.RequiredDps / royalDps)).Append(',')
                    .Append(CardSummonController.CalculateSummonCost(config, 12)).Append(',')
                    .Append(simulation.ProjectedUpgradeLevel).Append(',')
                    .Append(simulation.ProjectedReferenceDps.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(simulation.DpsCoverage.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(simulation.EconomyPressure.ToString("0.0000", CultureInfo.InvariantCulture))
                    .AppendLine();
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            GeneratePokerPowerReport(config, directory);
            Debug.Log("CARD_DEFENSE_BALANCE_REPORT_SUCCESS: " + path);
        }

        private static void GeneratePokerPowerReport(GameBalanceConfig config, string directory)
        {
            string path = Path.Combine(directory, "poker_combat_power_ace_level_0_30.csv");
            StringBuilder csv = new StringBuilder(16384);
            csv.AppendLine("족보,강화레벨,다음강화비용,공격력배율,예상합성초당피해량(DPS),공격특성");
            for (int handIndex = (int)PokerHand.High;
                 handIndex <= (int)PokerHand.RoyalStraightFlush; handIndex++)
            {
                PokerHand hand = (PokerHand)handIndex;
                PokerHandCombatProfile profile = PokerHandCombatProfile.Get(hand);
                for (int level = 0; level <= 30; level++)
                {
                    int cost = EndlessBalanceSimulator.CalculateUpgradeCost(config, hand, level);
                    csv.Append(PokerHandInfo.KoreanName(hand)).Append(',')
                        .Append(level).Append(',')
                        .Append(cost).Append(',')
                        .Append(PokerCombatMath.DamageMultiplier(config, hand, level)
                            .ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                        .Append(ReferenceFusionDps(config, hand, level)
                            .ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                        .Append(profile.KoreanTrait)
                        .AppendLine();
                }
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
        }

        private static float ReferenceFusionDps(GameBalanceConfig config, PokerHand hand, int level)
        {
            PlayingCard[] cards = CreateReferenceHand(hand);
            PokerFusionCombatResult fusion = PokerFusionCombatCalculator.Calculate(config, cards, hand);
            return PokerCombatMath.EstimatedDps(config, fusion.BaseDamage, hand, level);
        }

        private static PlayingCard[] CreateReferenceHand(PokerHand hand)
        {
            switch (hand)
            {
                case PokerHand.OnePair:
                    return Cards(CardRank.Ace, CardRank.Ace, CardRank.Two, CardRank.Five, CardRank.Nine);
                case PokerHand.TwoPair:
                    return Cards(CardRank.Ace, CardRank.Ace, CardRank.King, CardRank.King, CardRank.Two);
                case PokerHand.ThreeOfAKind:
                    return Cards(CardRank.Ace, CardRank.Ace, CardRank.Ace, CardRank.Two, CardRank.Five);
                case PokerHand.Straight:
                    return Cards(CardRank.Ten, CardRank.Jack, CardRank.Queen, CardRank.King, CardRank.Ace);
                case PokerHand.Flush:
                    return SameSuitCards(CardRank.Two, CardRank.Five, CardRank.Eight, CardRank.Jack, CardRank.Ace);
                case PokerHand.FullHouse:
                    return Cards(CardRank.Ace, CardRank.Ace, CardRank.Ace, CardRank.King, CardRank.King);
                case PokerHand.FourOfAKind:
                    return Cards(CardRank.Ace, CardRank.Ace, CardRank.Ace, CardRank.Ace, CardRank.Two);
                case PokerHand.StraightFlush:
                    return SameSuitCards(CardRank.Five, CardRank.Six, CardRank.Seven, CardRank.Eight, CardRank.Nine);
                case PokerHand.RoyalStraightFlush:
                    return SameSuitCards(CardRank.Ten, CardRank.Jack, CardRank.Queen, CardRank.King, CardRank.Ace);
                default:
                    return Cards(CardRank.Two, CardRank.Four, CardRank.Seven, CardRank.Nine, CardRank.Ace);
            }
        }

        private static PlayingCard[] Cards(CardRank first, CardRank second, CardRank third,
            CardRank fourth, CardRank fifth)
        {
            return new[]
            {
                new PlayingCard(CardSuit.Spade, first),
                new PlayingCard(CardSuit.Heart, second),
                new PlayingCard(CardSuit.Diamond, third),
                new PlayingCard(CardSuit.Club, fourth),
                new PlayingCard(CardSuit.Spade, fifth)
            };
        }

        private static PlayingCard[] SameSuitCards(CardRank first, CardRank second, CardRank third,
            CardRank fourth, CardRank fifth)
        {
            return new[]
            {
                new PlayingCard(CardSuit.Spade, first), new PlayingCard(CardSuit.Spade, second),
                new PlayingCard(CardSuit.Spade, third), new PlayingCard(CardSuit.Spade, fourth),
                new PlayingCard(CardSuit.Spade, fifth)
            };
        }
    }
}
