using CardDefense.Combat;
using CardDefense.Enemies;
using CardDefense.Pooling;
using CardDefense.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.Core
{
    public sealed class GameComposition : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private LoopPath path;
        [SerializeField] private Monster monsterPrefab;
        [SerializeField] private CardTower towerPrefab;
        [SerializeField] private Transform[] placementSlots;
        [SerializeField] private EconomyService economy;
        [SerializeField] private PokerProgressionService progression;
        [SerializeField] private MonsterSystem monsters;
        [SerializeField] private MonsterPool monsterPool;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private CardTowerSystem towers;
        [SerializeField] private CardSummonController summon;
        [SerializeField] private PrototypeHud hud;
        [SerializeField] private Text goldText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text monsterText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Button summonButton;
        [SerializeField] private Button mergeButton;
        [SerializeField] private Button upgradeButton;

        public void SetReferences(GameBalanceConfig balance, LoopPath loopPath, Monster monster,
            CardTower tower, Transform[] slots, EconomyService economyService,
            PokerProgressionService progressionService, MonsterSystem monsterSystem,
            MonsterPool pool, WaveDirector waveDirector, CardTowerSystem towerSystem,
            CardSummonController summonController, PrototypeHud prototypeHud, Text gold, Text round,
            Text alive, Text message, Text selection, Button summonButtonReference,
            Button mergeButtonReference, Button upgradeButtonReference)
        {
            config = balance;
            path = loopPath;
            monsterPrefab = monster;
            towerPrefab = tower;
            placementSlots = slots;
            economy = economyService;
            progression = progressionService;
            monsters = monsterSystem;
            monsterPool = pool;
            waves = waveDirector;
            towers = towerSystem;
            summon = summonController;
            hud = prototypeHud;
            goldText = gold;
            roundText = round;
            monsterText = alive;
            messageText = message;
            selectionText = selection;
            summonButton = summonButtonReference;
            mergeButton = mergeButtonReference;
            upgradeButton = upgradeButtonReference;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            economy.Configure(config);
            progression.Configure(config, economy);
            monsterPool.Configure(monsterPrefab, config.monsterPrewarmCount);
            summon.Configure(towerPrefab, placementSlots, economy, monsters, towers, progression, config);
            waves.Configure(config, path, monsterPool, monsters, economy);
            hud.Configure(goldText, roundText, monsterText, messageText, selectionText,
                summonButton, mergeButton, upgradeButton, economy, waves, monsters, summon);
        }
    }
}
