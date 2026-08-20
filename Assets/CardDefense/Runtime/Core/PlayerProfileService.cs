using System;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Core
{
    [Serializable]
    public sealed class PlayerProfileData
    {
        public int BestRound;
        public int TotalRuns;
        public int TotalMonstersDefeated;
        public long TotalGoldEarned;

        public void ApplyRun(RunStatisticsService run)
        {
            if (run == null) return;
            ApplyRunSnapshot(run.HighestRound, run.MonstersDefeated, run.GoldEarned);
        }

        public void ApplyRunSnapshot(int highestRound, int monstersDefeated, int goldEarned)
        {
            BestRound = Mathf.Max(BestRound, highestRound);
            TotalRuns++;
            TotalMonstersDefeated += Mathf.Max(0, monstersDefeated);
            TotalGoldEarned += Mathf.Max(0, goldEarned);
        }
    }

    public sealed class PlayerProfileService : MonoBehaviour
    {
        private const string SaveKey = "CardDefense.PlayerProfile.v1";
        public PlayerProfileData Data { get; private set; }

        private WaveDirector waves;
        private RunStatisticsService run;
        private bool recorded;

        public void Configure(WaveDirector waveDirector, RunStatisticsService statistics)
        {
            waves = waveDirector;
            run = statistics;
            Data = Load();
            waves.GameLost += HandleGameLost;
        }

        private void OnDestroy()
        {
            if (waves != null) waves.GameLost -= HandleGameLost;
        }

        private void HandleGameLost()
        {
            if (recorded) return;
            recorded = true;
            Data.ApplyRun(run);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        private static PlayerProfileData Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new PlayerProfileData();
            PlayerProfileData loaded = JsonUtility.FromJson<PlayerProfileData>(json);
            return loaded ?? new PlayerProfileData();
        }
    }
}
