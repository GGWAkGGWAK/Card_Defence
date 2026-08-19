using System.Globalization;
using System.IO;
using System.Text;
using CardDefense.Core;
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
            string path = Path.Combine(directory, "round_balance_1_100.csv");
            StringBuilder csv = new StringBuilder(16384);
            csv.AppendLine("Round,MonsterCount,HPPerMonster,TotalHP,RewardPerKill,PotentialGold,RequiredDPS,CumulativeGold,EquivalentSummons");
            long cumulativeGold = config.startingGold;
            for (int round = 1; round <= 100; round++)
            {
                RoundBalanceSnapshot row = RoundBalanceCalculator.Calculate(config, round);
                cumulativeGold += row.PotentialGold;
                csv.Append(row.Round).Append(',')
                    .Append(row.MonsterCount).Append(',')
                    .Append(row.HealthPerMonster.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.TotalHealth.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.RewardPerMonster).Append(',')
                    .Append(row.PotentialGold).Append(',')
                    .Append(row.RequiredDps.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(cumulativeGold).Append(',')
                    .Append((cumulativeGold / (double)config.summonCost).ToString("0.00", CultureInfo.InvariantCulture))
                    .AppendLine();
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            Debug.Log("CARD_DEFENSE_BALANCE_REPORT_SUCCESS: " + path);
        }
    }
}
