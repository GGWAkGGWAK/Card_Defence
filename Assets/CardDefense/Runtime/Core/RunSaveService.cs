using CardDefense.Combat;
using CardDefense.Enemies;
using CardDefense.UI;
using UnityEngine;

namespace CardDefense.Core
{
    public sealed class RunSaveService : MonoBehaviour
    {
        private const string SaveKey = "CardDefense.ActiveRun.v1";
        private const float SaveInterval = 5f;

        public bool WasRestored { get; private set; }
        public static bool HasActiveRun => PlayerPrefs.HasKey(ActiveSaveKey);

#if UNITY_EDITOR
        public static string EditorSaveKeyOverride;
#endif

        private static string ActiveSaveKey
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorSaveKeyOverride)) return EditorSaveKeyOverride;
#endif
                return SaveKey;
            }
        }

        private EconomyService economy;
        private PokerProgressionService progression;
        private RunModifierService modifiers;
        private RunStatisticsService statistics;
        private CardSummonController summon;
        private MonsterSystem monsters;
        private WaveDirector waves;
        private GrowthChoiceController growth;
        private BossQuestController bossQuest;
        private float saveTimer;
        private bool configured;

        public void Configure(EconomyService economyService, PokerProgressionService progressionService,
            RunModifierService modifierService, RunStatisticsService statisticsService,
            CardSummonController summonController, MonsterSystem monsterSystem,
            WaveDirector waveDirector, GrowthChoiceController growthController,
            BossQuestController bossQuestController)
        {
            economy = economyService;
            progression = progressionService;
            modifiers = modifierService;
            statistics = statisticsService;
            summon = summonController;
            monsters = monsterSystem;
            waves = waveDirector;
            growth = growthController;
            bossQuest = bossQuestController;
            waves.GameLost += HandleGameLost;
            configured = true;
            saveTimer = SaveInterval;
            TryRestore();
        }

        private void Update()
        {
            if (!configured || waves.IsGameOver) return;
            saveTimer -= Time.unscaledDeltaTime;
            if (saveTimer > 0f) return;
            SaveNow();
            saveTimer = SaveInterval;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && configured && !waves.IsGameOver) SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (configured && !waves.IsGameOver) SaveNow();
        }

        private void OnDestroy()
        {
            if (waves != null) waves.GameLost -= HandleGameLost;
        }

        public void SaveNow()
        {
            RunSaveData data = new RunSaveData
            {
                Gold = economy.Gold,
                HandLevels = progression.CaptureLevels(),
                Modifiers = modifiers.CaptureSnapshot(),
                Statistics = statistics.CaptureSnapshot(),
                Wave = waves.CaptureSnapshot(),
                Growth = growth.CaptureSnapshot(),
                BossQuest = bossQuest.CaptureSnapshot(),
                Towers = summon.CaptureTowers(),
                Monsters = monsters.CaptureMonsters()
            };
            PlayerPrefs.SetString(ActiveSaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public bool TryRestore()
        {
            string json = PlayerPrefs.GetString(ActiveSaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return false;
            RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null || data.Version != 1 || data.Wave == null) return false;

            economy.RestoreGold(data.Gold);
            progression.RestoreLevels(data.HandLevels);
            modifiers.RestoreSnapshot(data.Modifiers);
            statistics.RestoreSnapshot(data.Statistics);
            summon.RestoreTowers(data.Towers);
            waves.RestoreSnapshot(data.Wave, data.Monsters);
            growth.RestoreSnapshot(data.Growth);
            bossQuest.RestoreSnapshot(data.BossQuest);
            WasRestored = true;
            return true;
        }

        public static void DeleteActiveRun()
        {
            PlayerPrefs.DeleteKey(ActiveSaveKey);
            PlayerPrefs.Save();
        }

        private void HandleGameLost()
        {
            DeleteActiveRun();
        }
    }
}
