namespace CardDefense.Combat
{
    public enum CombatThreatLevel
    {
        Stable,
        Caution,
        Danger,
        Critical
    }

    public static class CombatThreatEvaluator
    {
        public static CombatThreatLevel Evaluate(float towerDps, float requiredDps,
            int activeMonsters, int defeatLimit)
        {
            float load = defeatLimit > 0 ? activeMonsters / (float)defeatLimit : 1f;
            if (load >= 0.75f) return CombatThreatLevel.Critical;
            float coverage = requiredDps > 0f ? towerDps / requiredDps : 1f;
            if (coverage < 0.65f || load >= 0.5f) return CombatThreatLevel.Danger;
            if (coverage < 1f || load >= 0.3f) return CombatThreatLevel.Caution;
            return CombatThreatLevel.Stable;
        }

        public static string KoreanName(CombatThreatLevel level)
        {
            switch (level)
            {
                case CombatThreatLevel.Stable: return "안정";
                case CombatThreatLevel.Caution: return "주의";
                case CombatThreatLevel.Danger: return "위험";
                default: return "패배 임박";
            }
        }
    }
}
