using UnityEngine;

namespace CardDefense.Core
{
    public static class MobilePerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
        }
    }
}
