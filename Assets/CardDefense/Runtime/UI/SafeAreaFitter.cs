using UnityEngine;

namespace CardDefense.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        public RectTransform Content => content != null ? content : (content = GetComponent<RectTransform>());

        private RectTransform content;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            content = GetComponent<RectTransform>();
            ApplyCurrentSafeArea();
        }

        private void Update()
        {
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea == lastSafeArea && size == lastScreenSize) return;
            ApplyCurrentSafeArea();
        }

        public void ApplyCurrentSafeArea()
        {
            Vector2Int size = new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            ApplySafeArea(Screen.safeArea, size);
        }

        public void ApplySafeArea(Rect safeArea, Vector2Int screenSize)
        {
            if (content == null) content = GetComponent<RectTransform>();
            Vector2[] anchors = CalculateAnchors(safeArea, screenSize);
            content.anchorMin = anchors[0];
            content.anchorMax = anchors[1];
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
        }

        public static Vector2[] CalculateAnchors(Rect safeArea, Vector2Int screenSize)
        {
            float width = Mathf.Max(1, screenSize.x);
            float height = Mathf.Max(1, screenSize.y);
            Vector2 min = new Vector2(
                Mathf.Clamp01(safeArea.xMin / width),
                Mathf.Clamp01(safeArea.yMin / height));
            Vector2 max = new Vector2(
                Mathf.Clamp01(safeArea.xMax / width),
                Mathf.Clamp01(safeArea.yMax / height));
            if (max.x < min.x) max.x = min.x;
            if (max.y < min.y) max.y = min.y;
            return new[] { min, max };
        }
    }
}
