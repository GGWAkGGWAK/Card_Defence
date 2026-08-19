using System.Globalization;
using System.IO;
using System.Text;
using CardDefense.Cards;
using UnityEditor;
using UnityEngine;

namespace CardDefense.Editor
{
    public static class PokerProbabilityReportGenerator
    {
        private const int Iterations = 2000000;
        private const int Seed = 20260819;

        [MenuItem("Card Defense/Generate Poker Probability CSV")]
        public static void GenerateDefault()
        {
            PokerProbabilityResult result = PokerProbabilitySimulator.SimulateWithReplacement(Iterations, Seed);
            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "Balance");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "poker_merge_probability.csv");
            StringBuilder csv = new StringBuilder(1024);
            csv.AppendLine("Result,KoreanName,Count,OverallProbabilityPercent,ValidMergeProbabilityPercent,BaseDamageMultiplier");
            csv.Append("InvalidDuplicateExactCard,동일 카드 중복으로 합성 불가,")
                .Append(result.InvalidDuplicateCount).Append(',')
                .Append((result.InvalidDuplicateCount * 100d / result.Iterations).ToString("0.000000", CultureInfo.InvariantCulture))
                .Append(",0.000000,0.00").AppendLine();
            for (int index = 0; index < result.Counts.Length; index++)
            {
                PokerHand hand = (PokerHand)index;
                csv.Append(hand).Append(',')
                    .Append(PokerHandInfo.KoreanName(hand)).Append(',')
                    .Append(result.Counts[index]).Append(',')
                    .Append((result.GetOverallProbability(hand) * 100d).ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append((result.GetValidMergeProbability(hand) * 100d).ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(PokerHandInfo.DamageMultiplier(hand).ToString("0.00", CultureInfo.InvariantCulture))
                    .AppendLine();
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            Debug.Log("CARD_DEFENSE_POKER_REPORT_SUCCESS: " + path);
        }
    }
}
