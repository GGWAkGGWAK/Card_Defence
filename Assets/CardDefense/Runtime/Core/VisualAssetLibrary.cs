using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Core
{
    public static class VisualAssetLibrary
    {
        private static Texture2D monsterAtlas;
        private static readonly Sprite[] monsterSprites = new Sprite[5];
        private static Sprite normalCardSprite;
        private static Sprite fusionCardSprite;
        private static Texture2D premiumCardTexture;
        private static Material monsterMaterial;
        private static Sprite uiFrameSprite;

        public static void CreateArenaBackground()
        {
            if (GameObject.Find("CasinoArenaBackground") != null) return;
            Texture2D texture = Resources.Load<Texture2D>("Art/CasinoArenaBackground-v1");
            if (texture == null) return;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            GameObject background = new GameObject("CasinoArenaBackground");
            SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
            float pixelsPerUnit = texture.height / 14.6f;
            renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            renderer.sortingOrder = -100;
            renderer.color = new Color(0.72f, 0.82f, 0.82f, 1f);
            background.transform.position = new Vector3(0f, 0f, 2f);

            LoopPath loop = Object.FindObjectOfType<LoopPath>();
            LineRenderer route = loop != null ? loop.GetComponent<LineRenderer>() : null;
            if (route != null)
            {
                route.startWidth = 0.18f;
                route.endWidth = 0.18f;
                route.startColor = new Color(0.2f, 0.9f, 0.92f, 0.38f);
                route.endColor = route.startColor;
            }
        }

        public static Sprite GetMonsterSprite(MonsterArchetype archetype)
        {
            int index = (int)archetype;
            if (index < 0 || index >= monsterSprites.Length) index = 0;
            if (monsterSprites[index] != null) return monsterSprites[index];
            if (monsterAtlas == null)
            {
                monsterAtlas = Resources.Load<Texture2D>("Art/CasinoMonstersAtlas-v3");
                if (monsterAtlas == null) return null;
                monsterAtlas.filterMode = FilterMode.Bilinear;
                monsterAtlas.wrapMode = TextureWrapMode.Clamp;
            }

            float cellWidth = monsterAtlas.width / 3f;
            float cellHeight = monsterAtlas.height / 2f;
            int column;
            int row;
            switch (archetype)
            {
                case MonsterArchetype.Fast: column = 1; row = 1; break;
                case MonsterArchetype.Tank: column = 2; row = 1; break;
                case MonsterArchetype.Gold: column = 0; row = 0; break;
                case MonsterArchetype.Boss: column = 1; row = 0; break;
                default: column = 0; row = 1; break;
            }
            Rect rect = new Rect(column * cellWidth, row * cellHeight, cellWidth, cellHeight);
            monsterSprites[index] = Sprite.Create(monsterAtlas, rect, new Vector2(0.5f, 0.5f),
                cellWidth, 0, SpriteMeshType.FullRect);
            monsterSprites[index].name = "Monster_" + archetype;
            return monsterSprites[index];
        }

        public static Material GetMonsterMaterial()
        {
            if (monsterMaterial != null) return monsterMaterial;
            Shader shader = Resources.Load<Shader>("Shaders/MonsterChromaKey");
            if (shader == null) shader = Shader.Find("CardDefense/MonsterChromaKey");
            if (shader == null) return null;
            monsterMaterial = new Material(shader) { name = "MonsterChromaKeyRuntime" };
            // Generated atlas background is a pink gradient rather than pure magenta.
            monsterMaterial.SetColor("_KeyColor", new Color(0.94f, 0.045f, 0.89f, 1f));
            monsterMaterial.SetFloat("_Tolerance", 0.22f);
            monsterMaterial.SetFloat("_Softness", 0.10f);
            return monsterMaterial;
        }

        public static Sprite GetCardSprite(bool fusion)
        {
            if (fusion && fusionCardSprite != null) return fusionCardSprite;
            if (!fusion && normalCardSprite != null) return normalCardSprite;
            if (premiumCardTexture == null)
            {
                premiumCardTexture = Resources.Load<Texture2D>("Art/PremiumCardFace-v1");
                if (premiumCardTexture != null)
                {
                    premiumCardTexture.filterMode = FilterMode.Bilinear;
                    premiumCardTexture.wrapMode = TextureWrapMode.Clamp;
                }
            }
            if (premiumCardTexture != null)
            {
                float pixelsPerUnit = premiumCardTexture.height / 1.1f;
                Sprite premium = Sprite.Create(premiumCardTexture,
                    new Rect(0f, 0f, premiumCardTexture.width, premiumCardTexture.height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                premium.name = fusion ? "PremiumFusionCard" : "PremiumPlayingCard";
                if (fusion) fusionCardSprite = premium;
                else normalCardSprite = premium;
                return premium;
            }
            const int width = 64;
            const int height = 88;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = fusion ? "FusionCardTexture" : "CardTexture";
            Color border = fusion
                ? new Color(1f, 0.67f, 0.08f, 1f)
                : new Color(0.18f, 0.25f, 0.28f, 1f);
            Color face = fusion
                ? new Color(1f, 0.94f, 0.72f, 1f)
                : new Color(0.97f, 0.96f, 0.89f, 1f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int edgeX = Mathf.Min(x, width - 1 - x);
                    int edgeY = Mathf.Min(y, height - 1 - y);
                    bool cornerCut = edgeX < 7 && edgeY < 7 &&
                                     (edgeX - 7) * (edgeX - 7) + (edgeY - 7) * (edgeY - 7) > 49;
                    Color pixel = cornerCut ? Color.clear : edgeX < 3 || edgeY < 3 ? border : face;
                    if (!cornerCut && edgeY > height - 14)
                        pixel = Color.Lerp(pixel, Color.white, 0.12f);
                    texture.SetPixel(x, y, pixel);
                }
            }
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f), 80f, 0, SpriteMeshType.FullRect);
            sprite.name = fusion ? "FusionCard" : "PlayingCard";
            if (fusion) fusionCardSprite = sprite;
            else normalCardSprite = sprite;
            return sprite;
        }

        public static Sprite GetUiFrameSprite()
        {
            if (uiFrameSprite != null) return uiFrameSprite;
            Texture2D texture = Resources.Load<Texture2D>("Art/CasinoUiFrame-v1");
            if (texture == null) return null;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float borderX = texture.width * 0.105f;
            float borderY = texture.height * 0.15f;
            uiFrameSprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect,
                new Vector4(borderX, borderY, borderX, borderY));
            uiFrameSprite.name = "CasinoUiFrame";
            return uiFrameSprite;
        }
    }
}
