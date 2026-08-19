using UnityEngine;

namespace CardDefense.Core
{
    [CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Card Defense/Game Balance")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Economy")]
        [Min(0)] public int startingGold = 150;
        [Min(1)] public int summonCost = 25;
        [Min(1)] public int baseKillGold = 5;
        [Min(1f)] public float rewardGrowthPerRound = 1.04f;

        [Header("Rounds")]
        [Min(5f)] public float roundDuration = 20f;
        [Min(1)] public int baseMonstersPerRound = 6;
        [Min(0)] public int extraMonstersPerRound = 1;
        [Min(0.05f)] public float spawnInterval = 0.65f;
        [Min(1)] public int defeatMonsterLimit = 80;

        [Header("Monster")]
        [Min(1f)] public float baseMonsterHealth = 20f;
        [Min(1f)] public float healthGrowthPerRound = 1.16f;
        [Min(0.1f)] public float monsterMoveSpeed = 1.6f;
        [Range(0f, 1f)] public float milestoneHealthBonus = 0.25f;

        [Header("Tower")]
        [Min(0.1f)] public float baseTowerDamage = 6f;
        [Min(0.05f)] public float towerAttackInterval = 0.55f;
        [Min(0.5f)] public float towerRange = 3.5f;
        [Min(0.05f)] public float targetRefreshInterval = 0.25f;

        [Header("Poker Progression")]
        [Min(1)] public int baseHandUpgradeCost = 40;
        [Min(1f)] public float handUpgradeCostGrowth = 1.32f;
        [Min(0f)] public float handUpgradeDamageStep = 0.22f;
        [Min(0.1f)] public float towerSelectionRadius = 0.7f;

        [Header("Pooling")]
        [Min(0)] public int monsterPrewarmCount = 96;
        [Min(0)] public int towerPrewarmCount = 24;
    }
}
