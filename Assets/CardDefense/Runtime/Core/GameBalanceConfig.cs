using UnityEngine;

namespace CardDefense.Core
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Card Defense/Game Balance")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Economy")]
        [Min(0)] public int startingGold = 150;
        [Min(1)] public int summonCost = 25;
        [Min(0)] public int baseCardSellGold = 8;
        [Min(0)] public int fusionSellBaseGold = 30;
        [Min(0)] public int fusionSellTierBonus = 8;
        [Min(1)] public int baseKillGold = 5;
        [Min(1f)] public float rewardGrowthPerRound = 1.025f;

        [Header("Risk Boss Quest")]
        [Min(5f)] public float bossQuestInitialCooldown = 25f;
        [Min(10f)] public float bossQuestCooldown = 75f;
        [Min(5f)] public float bossQuestTimeLimit = 25f;
        [Min(1f)] public float bossQuestHealthMultiplier = 16f;
        [Min(0)] public int bossQuestBaseGold = 80;
        [Min(0)] public int bossQuestGoldPerRound = 5;
        [Range(0f, 0.25f)] public float bossQuestAttackBonus = 0.03f;

        [Header("Rounds")]
        [Min(5f)] public float roundDuration = 20f;
        [Min(1)] public int baseMonstersPerRound = 6;
        [Min(0)] public int extraMonstersPerRound = 1;
        [Min(0.05f)] public float spawnInterval = 0.65f;
        [Min(1)] public int defeatMonsterLimit = 80;

        [Header("Monster")]
        [Min(1f)] public float baseMonsterHealth = 20f;
        [Min(1f)] public float healthGrowthPerRound = 1.09f;
        [Min(0.1f)] public float monsterMoveSpeed = 1.6f;
        [Range(0f, 1f)] public float milestoneHealthBonus = 0.15f;

        [Header("Monster Archetypes")]
        [Min(0.1f)] public float fastHealthMultiplier = 0.65f;
        [Min(0.1f)] public float fastSpeedMultiplier = 1.8f;
        [Min(0.1f)] public float fastRewardMultiplier = 1.2f;
        [Min(0.1f)] public float tankHealthMultiplier = 2.3f;
        [Min(0.1f)] public float tankSpeedMultiplier = 0.7f;
        [Min(0.1f)] public float tankRewardMultiplier = 2f;
        [Min(0.1f)] public float goldHealthMultiplier = 1.2f;
        [Min(0.1f)] public float goldRewardMultiplier = 2.25f;
        [Min(0.1f)] public float bossHealthMultiplier = 10f;
        [Min(0.1f)] public float bossSpeedMultiplier = 0.55f;
        [Min(0.1f)] public float bossRewardMultiplier = 5f;

        [Header("Tower")]
        [Min(0.1f)] public float baseTowerDamage = 6f;
        [Min(0.05f)] public float towerAttackInterval = 0.55f;
        [Min(0.5f)] public float towerRange = 3.5f;
        [Min(0.05f)] public float targetRefreshInterval = 0.25f;
        [Range(0f, 0.5f)] public float discardedMaterialPowerRatio = 0.1f;

        [Header("Poker Progression")]
        [Min(1)] public int baseHandUpgradeCost = 40;
        [Min(1f)] public float handUpgradeCostGrowth = 1.26f;
        [Min(0f)] public float handUpgradeDamageStep = 0.18f;
        [Min(0.1f)] public float towerSelectionRadius = 0.7f;
        [Min(1f)] public float cardDragThresholdPixels = 24f;

        [Header("Pooling")]
        [Min(0)] public int monsterPrewarmCount = 96;
        [Min(0)] public int towerPrewarmCount = 24;
    }
}
