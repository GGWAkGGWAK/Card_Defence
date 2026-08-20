using UnityEngine;

namespace CardDefense.Enemies
{
    public sealed class MonsterHealthBar : MonoBehaviour
    {
        private static Sprite sharedSprite;
        private GameObject root;
        private Transform fill;

        private void Awake()
        {
            root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0.72f, -0.2f);

            SpriteRenderer background = CreatePart("Background", root.transform,
                new Color(0.08f, 0.08f, 0.08f, 0.9f), 4);
            background.transform.localScale = new Vector3(1.05f, 0.14f, 1f);
            SpriteRenderer fillRenderer = CreatePart("Fill", root.transform,
                new Color(0.2f, 0.9f, 0.32f, 1f), 5);
            fill = fillRenderer.transform;
            fill.localScale = new Vector3(1f, 0.09f, 1f);
            root.SetActive(false);
        }

        public void Show(MonsterArchetype archetype)
        {
            if (root == null) return;
            root.transform.localScale = archetype == MonsterArchetype.Boss
                ? new Vector3(1.25f, 1.25f, 1f)
                : Vector3.one;
            root.SetActive(true);
            SetHealth(1f);
        }

        public void SetHealth(float normalized)
        {
            if (fill == null) return;
            normalized = Mathf.Clamp01(normalized);
            fill.localScale = new Vector3(normalized, 0.09f, 1f);
            fill.localPosition = new Vector3((normalized - 1f) * 0.5f, 0f, -0.01f);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private static SpriteRenderer CreatePart(string name, Transform parent, Color color, int order)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSharedSprite();
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
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
