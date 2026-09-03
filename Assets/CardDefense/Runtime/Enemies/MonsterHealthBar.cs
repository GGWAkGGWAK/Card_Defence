using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class MonsterHealthBar : MonoBehaviour
    {
        private static Sprite sharedSprite;
        private GameObject root;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer backgroundRenderer;
        private SpriteRenderer fillRenderer;
        private TextMesh nameLabel;
        private TextMesh typeIcon;
        private MonsterArchetype archetype;
        private Color healthyColor;
        private float barWidth;
        private float barHeight;

        public string DisplayName { get; private set; }
        public float DisplayedNormalizedHealth { get; private set; }
        public Color CurrentBarColor => fillRenderer != null ? fillRenderer.color : Color.clear;
        public bool IsBossStyle => archetype == MonsterArchetype.Boss;

        private void Awake()
        {
            root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            frameRenderer = CreatePart("Frame", root.transform,
                new Color(0.88f, 0.78f, 0.46f, 0.98f));
            backgroundRenderer = CreatePart("Background", root.transform,
                new Color(0.025f, 0.035f, 0.045f, 0.96f));
            fillRenderer = CreatePart("Fill", root.transform, Color.green);
            nameLabel = CreateText("TypeName", root.transform, 28, 0.065f);
            typeIcon = CreateText("TypeIcon", root.transform, 31, 0.075f);
            root.SetActive(false);
        }

        private void LateUpdate()
        {
            if (root == null || !root.activeSelf) return;
            root.transform.rotation = Quaternion.identity;
            int baseOrder = 200 - Mathf.RoundToInt(transform.position.y * 10f);
            frameRenderer.sortingOrder = baseOrder + 3;
            backgroundRenderer.sortingOrder = baseOrder + 4;
            fillRenderer.sortingOrder = baseOrder + 5;
            nameLabel.GetComponent<MeshRenderer>().sortingOrder = baseOrder + 6;
            typeIcon.GetComponent<MeshRenderer>().sortingOrder = baseOrder + 7;
        }

        public void Show(MonsterArchetype style)
        {
            if (root == null) return;
            archetype = style;
            DisplayName = GetDisplayName(style);
            healthyColor = GetHealthyColor(style);
            barWidth = style == MonsterArchetype.Boss ? 1.42f :
                style == MonsterArchetype.Tank ? 1.12f : 0.96f;
            barHeight = style == MonsterArchetype.Boss ? 0.13f : 0.09f;

            root.transform.localPosition = style == MonsterArchetype.Boss
                ? new Vector3(0f, 0.86f, -0.2f)
                : style == MonsterArchetype.Tank
                    ? new Vector3(0f, 0.75f, -0.2f)
                    : new Vector3(0f, 0.69f, -0.2f);
            root.transform.localScale = Vector3.one;
            frameRenderer.transform.localScale = new Vector3(barWidth + 0.1f, barHeight + 0.075f, 1f);
            backgroundRenderer.transform.localScale = new Vector3(barWidth + 0.04f, barHeight + 0.025f, 1f);

            typeIcon.text = GetTypeIcon(style);
            typeIcon.color = healthyColor;
            typeIcon.transform.localPosition = new Vector3(-barWidth * 0.62f, 0.02f, -0.03f);
            nameLabel.color = style == MonsterArchetype.Boss
                ? new Color(1f, 0.8f, 0.28f, 1f)
                : new Color(0.94f, 0.97f, 1f, 1f);
            nameLabel.transform.localPosition = new Vector3(0f, barHeight + 0.14f, -0.03f);
            nameLabel.gameObject.SetActive(style != MonsterArchetype.Normal);
            root.SetActive(true);
            SetHealth(1f);
            LateUpdate();
        }

        public void SetHealth(float normalized)
        {
            if (fillRenderer == null) return;
            DisplayedNormalizedHealth = Mathf.Clamp01(normalized);
            float visibleWidth = barWidth * DisplayedNormalizedHealth;
            fillRenderer.transform.localScale = new Vector3(visibleWidth, barHeight, 1f);
            fillRenderer.transform.localPosition = new Vector3((visibleWidth - barWidth) * 0.5f, 0f, -0.02f);
            Color dangerColor = DisplayedNormalizedHealth <= 0.3f
                ? new Color(1f, 0.12f, 0.08f, 1f)
                : new Color(1f, 0.68f, 0.12f, 1f);
            float healthyBlend = Mathf.InverseLerp(0.2f, 0.72f, DisplayedNormalizedHealth);
            fillRenderer.color = Color.Lerp(dangerColor, healthyColor, healthyBlend);

            if (nameLabel != null && archetype != MonsterArchetype.Normal)
            {
                string percent = Mathf.CeilToInt(DisplayedNormalizedHealth * 100f) + "%";
                nameLabel.text = archetype == MonsterArchetype.Boss
                    ? "★ " + DisplayName + "  " + percent
                    : DisplayName + "  " + percent;
            }
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private static SpriteRenderer CreatePart(string name, Transform parent, Color color)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSharedSprite();
            renderer.color = color;
            return renderer;
        }

        private static TextMesh CreateText(string name, Transform parent, int fontSize, float size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.characterSize = size;
            return text;
        }

        private static string GetDisplayName(MonsterArchetype style)
        {
            switch (style)
            {
                case MonsterArchetype.Fast: return "신속";
                case MonsterArchetype.Tank: return "중장";
                case MonsterArchetype.Gold: return "황금";
                case MonsterArchetype.Boss: return "운명의 수호자";
                default: return "일반";
            }
        }

        private static string GetTypeIcon(MonsterArchetype style)
        {
            switch (style)
            {
                case MonsterArchetype.Fast: return ">>";
                case MonsterArchetype.Tank: return "■";
                case MonsterArchetype.Gold: return "G";
                case MonsterArchetype.Boss: return "★";
                default: return string.Empty;
            }
        }

        private static Color GetHealthyColor(MonsterArchetype style)
        {
            switch (style)
            {
                case MonsterArchetype.Fast: return new Color(0.1f, 0.88f, 1f, 1f);
                case MonsterArchetype.Tank: return new Color(1f, 0.46f, 0.16f, 1f);
                case MonsterArchetype.Gold: return new Color(1f, 0.82f, 0.08f, 1f);
                case MonsterArchetype.Boss: return new Color(0.96f, 0.16f, 0.7f, 1f);
                default: return new Color(0.18f, 0.92f, 0.36f, 1f);
            }
        }

        private static Sprite GetSharedSprite()
        {
            if (sharedSprite != null) return sharedSprite;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "HealthBarPixel";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            sharedSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sharedSprite.name = "HealthBarSprite";
            return sharedSprite;
        }
    }
}
