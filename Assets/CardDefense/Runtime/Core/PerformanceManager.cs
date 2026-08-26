using CardDefense.Combat;
using UnityEngine;

namespace CardDefense.Core
{
    public enum DevicePerformanceMode
    {
        BatterySaver,
        Balanced,
        HighRefresh
    }

    public sealed class PerformanceManager : MonoBehaviour
    {
        private const string DefaultKey = "CardDefense.PerformanceMode.v1";

        public DevicePerformanceMode Mode { get; private set; }
        public int TargetFrameRate => Mode == DevicePerformanceMode.BatterySaver ? 30 :
            Mode == DevicePerformanceMode.Balanced ? 60 : 120;
        public float EffectQuality => Mode == DevicePerformanceMode.BatterySaver ? 0.45f :
            Mode == DevicePerformanceMode.Balanced ? 0.75f : 1f;
        public string KoreanName => Mode == DevicePerformanceMode.BatterySaver ? "절전 30 FPS" :
            Mode == DevicePerformanceMode.Balanced ? "균형 60 FPS" : "고주사율 120 FPS";

#if UNITY_EDITOR
        public static string EditorKeyOverride;
#endif

        private static string ActiveKey
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorKeyOverride)) return EditorKeyOverride;
#endif
                return DefaultKey;
            }
        }

        private CombatEffectSystem effects;

        public void Configure(CombatEffectSystem combatEffects)
        {
            effects = combatEffects;
            int saved = PlayerPrefs.GetInt(ActiveKey, (int)DevicePerformanceMode.Balanced);
            Mode = (DevicePerformanceMode)Mathf.Clamp(saved, 0, 2);
            Apply();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public void CycleMode()
        {
            Mode = Mode == DevicePerformanceMode.BatterySaver ? DevicePerformanceMode.Balanced :
                Mode == DevicePerformanceMode.Balanced ? DevicePerformanceMode.HighRefresh :
                DevicePerformanceMode.BatterySaver;
            PlayerPrefs.SetInt(ActiveKey, (int)Mode);
            PlayerPrefs.Save();
            Apply();
        }

        public static void DeleteSetting()
        {
            PlayerPrefs.DeleteKey(ActiveKey);
            PlayerPrefs.Save();
        }

        private void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            if (effects != null) effects.SetQuality(EffectQuality);
        }
    }
}
