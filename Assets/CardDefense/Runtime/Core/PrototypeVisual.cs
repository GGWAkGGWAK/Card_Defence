using CardDefense.Cards;
using CardDefense.Enemies;
using UnityEngine;

namespace CardDefense.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypeVisual : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector2 size = Vector2.one;

        private static Sprite sharedSprite;
        private static Sprite monsterAuraSprite;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer monsterAura;
        private TextMesh label;
        private TextMesh handLabel;
        private Color cardColor;
        private Color cardBaseTint = Color.white;
        private bool monsterMode;
        private Vector3 monsterScale;
        private float animationPhase;
        private float hitFlashTimer;
        private float attackPulseTimer;
        private float spawnAnimationTimer;
        private float spawnAnimationDuration;
        private float deathAnimationTimer;
        private float deathAnimationDuration;
        private Vector2 moveDirection = Vector2.right;
        private MonsterArchetype monsterArchetype;
        private Color monsterAccentColor = Color.white;
        private Vector3 cardRestScale = new Vector3(0.8f, 1.05f, 1f);

        public bool IsSpawnAnimating => monsterMode && spawnAnimationTimer > 0f;
        public bool IsDeathAnimating => monsterMode && deathAnimationTimer > 0f;
        public Color MonsterAccentColor => monsterAccentColor;
        public bool HasMonsterAura => monsterAura != null && monsterAura.enabled;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSharedSprite();
            spriteRenderer.color = color;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public void SetMonsterStyle(MonsterArchetype archetype)
        {
            EnsureReady();
            Sprite art = VisualAssetLibrary.GetMonsterSprite(archetype);
            if (art != null) spriteRenderer.sprite = art;
            Material artMaterial = VisualAssetLibrary.GetMonsterMaterial();
            if (artMaterial != null) spriteRenderer.sharedMaterial = artMaterial;
            spriteRenderer.color = Color.white;
            monsterMode = true;
            monsterArchetype = archetype;
            ConfigureMonsterAura(archetype);
            animationPhase = Random.value * Mathf.PI * 2f;
            hitFlashTimer = 0f;
            deathAnimationTimer = 0f;
            deathAnimationDuration = 0f;
            moveDirection = Vector2.right;
            switch (archetype)
            {
                case CardDefense.Enemies.MonsterArchetype.Fast:
                    monsterScale = new Vector3(0.82f, 0.82f, 1f);
                    break;
                case CardDefense.Enemies.MonsterArchetype.Tank:
                    monsterScale = new Vector3(1.08f, 1.08f, 1f);
                    break;
                case CardDefense.Enemies.MonsterArchetype.Gold:
                    monsterScale = new Vector3(0.95f, 0.95f, 1f);
                    break;
                case CardDefense.Enemies.MonsterArchetype.Boss:
                    monsterScale = new Vector3(1.5f, 1.5f, 1f);
                    break;
                default:
                    monsterScale = new Vector3(0.9f, 0.9f, 1f);
                    break;
            }
            spawnAnimationDuration = archetype == MonsterArchetype.Boss ? 0.58f : 0.28f;
            spawnAnimationTimer = spawnAnimationDuration;
            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            UpdateMonsterSorting();
        }

        public void SetMonsterMoveDirection(Vector2 direction)
        {
            if (!monsterMode || direction.sqrMagnitude < 0.000001f) return;
            moveDirection = direction.normalized;
            if (Mathf.Abs(moveDirection.x) > 0.08f)
                spriteRenderer.flipX = moveDirection.x < 0f;
        }

        public float PlayMonsterDeath()
        {
            if (!monsterMode) return 0f;
            deathAnimationDuration = monsterArchetype == MonsterArchetype.Boss ? 0.52f : 0.34f;
            deathAnimationTimer = deathAnimationDuration;
            spawnAnimationTimer = 0f;
            hitFlashTimer = 0f;
            return deathAnimationDuration;
        }

        public void SetPlacementSlotStyle()
        {
            EnsureReady();
            color = new Color(0.3f, 0.75f, 0.68f, 0.18f);
            size = new Vector2(0.95f, 1.2f);
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = -2;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public void SetCard(PlayingCard card, PokerHand hand, bool isFusionResult)
        {
            EnsureReady();
            monsterMode = false;
            if (monsterAura != null) monsterAura.enabled = false;
            deathAnimationTimer = 0f;
            spawnAnimationTimer = 0f;
            transform.localRotation = Quaternion.identity;
            spriteRenderer.sprite = VisualAssetLibrary.GetCardSprite(isFusionResult);
            cardColor = card.Suit == CardSuit.Heart || card.Suit == CardSuit.Diamond
                ? new Color(1f, 0.93f, 0.93f, 1f)
                : new Color(0.92f, 0.96f, 1f, 1f);
            cardBaseTint = isFusionResult
                ? new Color(1f, 0.86f, 0.52f, 1f)
                : Color.white;
            spriteRenderer.color = cardBaseTint;
            spriteRenderer.sortingOrder = 3;
            transform.localScale = new Vector3(0.8f, 1.05f, 1f);
            cardRestScale = transform.localScale;

            Color ink = card.Suit == CardSuit.Heart || card.Suit == CardSuit.Diamond
                ? new Color(0.68f, 0.04f, 0.07f, 1f)
                : new Color(0.03f, 0.09f, 0.14f, 1f);
            label = EnsureCardText(label, "CardLabel", new Vector3(0f, 0.12f, -0.1f),
                50, 0.115f, spriteRenderer.sortingOrder + 2);
            handLabel = EnsureCardText(handLabel, "CardHandLabel", new Vector3(0f, -0.34f, -0.1f),
                24, 0.07f, spriteRenderer.sortingOrder + 3);
            label.color = ink;
            handLabel.color = isFusionResult
                ? new Color(0.47f, 0.25f, 0.015f, 1f)
                : new Color(0.18f, 0.22f, 0.22f, 1f);

            bool suitDefinesFusion = hand == PokerHand.Flush || hand == PokerHand.StraightFlush ||
                                     hand == PokerHand.RoyalStraightFlush;
            string suit = !isFusionResult || suitDefinesFusion ? SuitText(card.Suit) : string.Empty;
            label.text = RankText(card.Rank) + suit;
            handLabel.text = PokerHandInfo.ShortName(hand) + (isFusionResult ? " ★" : string.Empty);
        }

        public void SetSelected(bool selected)
        {
            EnsureReady();
            spriteRenderer.color = selected ? new Color(1f, 0.72f, 0.16f, 1f) : cardBaseTint;
            cardRestScale = selected ? new Vector3(0.9f, 1.17f, 1f) : new Vector3(0.8f, 1.05f, 1f);
            transform.localScale = cardRestScale;
        }

        public void PlayAttackPulse()
        {
            if (!monsterMode) attackPulseTimer = 0.12f;
        }

        public void FlashHit()
        {
            hitFlashTimer = 0.09f;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        private void Update()
        {
            if (spriteRenderer == null) return;
            if (!monsterMode)
            {
                if (attackPulseTimer <= 0f) return;
                attackPulseTimer -= Time.deltaTime;
                float normalized = 1f - Mathf.Clamp01(attackPulseTimer / 0.12f);
                float kick = Mathf.Sin(normalized * Mathf.PI) * 0.13f;
                transform.localScale = new Vector3(cardRestScale.x * (1f + kick),
                    cardRestScale.y * (1f - kick * 0.35f), 1f);
                if (attackPulseTimer <= 0f) transform.localScale = cardRestScale;
                return;
            }
            float deltaTime = Time.deltaTime;
            if (deathAnimationTimer > 0f)
            {
                UpdateMonsterSorting();
                deathAnimationTimer -= deltaTime;
                float normalized = 1f - Mathf.Clamp01(deathAnimationTimer / deathAnimationDuration);
                float remaining = 1f - normalized;
                float stretch = 1f + Mathf.Sin(normalized * Mathf.PI) * 0.22f;
                transform.localScale = new Vector3(monsterScale.x * remaining * stretch,
                    monsterScale.y * remaining * (2f - stretch), 1f);
                float spinDirection = moveDirection.x < 0f ? 1f : -1f;
                transform.localRotation = Quaternion.Euler(0f, 0f,
                    spinDirection * Mathf.Lerp(0f, 105f, normalized));
                spriteRenderer.color = new Color(1f, Mathf.Lerp(0.48f, 0.08f, normalized),
                    Mathf.Lerp(0.32f, 0.05f, normalized), remaining);
                if (monsterAura != null)
                {
                    Color auraColor = monsterAccentColor;
                    auraColor.a = remaining * 0.52f;
                    monsterAura.color = auraColor;
                    monsterAura.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.55f, normalized);
                }
                return;
            }

            UpdateMonsterSorting();

            float frequency = 4.2f;
            float bounceAmount = 0.045f;
            float swayAmount = 2.4f;
            switch (monsterArchetype)
            {
                case MonsterArchetype.Fast:
                    frequency = 8.4f;
                    bounceAmount = 0.095f;
                    swayAmount = 5f;
                    break;
                case MonsterArchetype.Tank:
                    frequency = 2.3f;
                    bounceAmount = 0.025f;
                    swayAmount = 1.2f;
                    break;
                case MonsterArchetype.Gold:
                    frequency = 5.4f;
                    bounceAmount = 0.06f;
                    swayAmount = 3.4f;
                    break;
                case MonsterArchetype.Boss:
                    frequency = 1.9f;
                    bounceAmount = 0.07f;
                    swayAmount = 2f;
                    break;
            }

            float wave = Mathf.Sin(Time.time * frequency + animationPhase);
            float verticalPulse = 1f + wave * bounceAmount;
            float horizontalPulse = 1f - wave * bounceAmount * 0.45f;
            float hitKick = 0f;
            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= deltaTime;
                hitKick = Mathf.Sin(Mathf.Clamp01(hitFlashTimer / 0.09f) * Mathf.PI) * 0.18f;
                spriteRenderer.color = new Color(1f, 0.32f, 0.22f, 1f);
            }
            else if (monsterArchetype == MonsterArchetype.Gold)
            {
                float shine = 0.88f + (wave + 1f) * 0.06f;
                spriteRenderer.color = new Color(1f, shine, 0.72f, 1f);
            }
            else spriteRenderer.color = Color.Lerp(Color.white, monsterAccentColor,
                monsterArchetype == MonsterArchetype.Normal ? 0.05f : 0.13f);

            if (monsterAura != null)
            {
                float auraPulse = 1f + (wave + 1f) *
                                  (monsterArchetype == MonsterArchetype.Boss ? 0.09f : 0.035f);
                float auraBase = monsterArchetype == MonsterArchetype.Boss ? 1.34f :
                    monsterArchetype == MonsterArchetype.Tank ? 1.18f : 1.08f;
                monsterAura.transform.localScale = Vector3.one * auraBase * auraPulse;
                Color auraColor = monsterAccentColor;
                auraColor.a = monsterArchetype == MonsterArchetype.Normal ? 0.2f :
                    monsterArchetype == MonsterArchetype.Boss ? 0.62f : 0.43f;
                monsterAura.color = auraColor;
            }

            float spawnScale = 1f;
            if (spawnAnimationTimer > 0f)
            {
                spawnAnimationTimer -= deltaTime;
                float normalized = 1f - Mathf.Clamp01(spawnAnimationTimer / spawnAnimationDuration);
                float overshoot = 1f + Mathf.Sin(normalized * Mathf.PI) *
                                  (monsterArchetype == MonsterArchetype.Boss ? 0.28f : 0.16f);
                spawnScale = Mathf.SmoothStep(0f, 1f, normalized) * overshoot;
            }

            transform.localScale = new Vector3(monsterScale.x * horizontalPulse * (1f + hitKick),
                monsterScale.y * verticalPulse * (1f - hitKick * 0.55f), 1f) * spawnScale;
            float travelLean = Mathf.Clamp(-moveDirection.x * moveDirection.y * 4f, -2.5f, 2.5f);
            transform.localRotation = Quaternion.Euler(0f, 0f,
                wave * swayAmount + travelLean + (moveDirection.x < 0f ? hitKick : -hitKick) * 35f);
        }

        private void EnsureReady()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null) spriteRenderer.sprite = GetSharedSprite();
        }

        private void ConfigureMonsterAura(MonsterArchetype archetype)
        {
            switch (archetype)
            {
                case MonsterArchetype.Fast:
                    monsterAccentColor = new Color(0.14f, 0.9f, 1f, 1f);
                    break;
                case MonsterArchetype.Tank:
                    monsterAccentColor = new Color(0.95f, 0.48f, 0.18f, 1f);
                    break;
                case MonsterArchetype.Gold:
                    monsterAccentColor = new Color(1f, 0.82f, 0.12f, 1f);
                    break;
                case MonsterArchetype.Boss:
                    monsterAccentColor = new Color(0.92f, 0.16f, 0.68f, 1f);
                    break;
                default:
                    monsterAccentColor = new Color(0.36f, 0.92f, 0.5f, 1f);
                    break;
            }
            if (monsterAura == null)
            {
                Transform existing = transform.Find("MonsterAura");
                if (existing != null) monsterAura = existing.GetComponent<SpriteRenderer>();
            }
            if (monsterAura == null)
            {
                GameObject auraObject = new GameObject("MonsterAura");
                auraObject.transform.SetParent(transform, false);
                monsterAura = auraObject.AddComponent<SpriteRenderer>();
            }
            monsterAura.sprite = GetMonsterAuraSprite();
            monsterAura.transform.localPosition = new Vector3(0f, -0.04f, 0.08f);
            monsterAura.transform.localRotation = Quaternion.identity;
            monsterAura.enabled = true;
        }

        private void UpdateMonsterSorting()
        {
            int baseOrder = 200 - Mathf.RoundToInt(transform.position.y * 10f);
            spriteRenderer.sortingOrder = baseOrder;
            if (monsterAura != null) monsterAura.sortingOrder = baseOrder - 1;
        }

        private TextMesh EnsureCardText(TextMesh current, string objectName, Vector3 localPosition,
            int fontSize, float characterSize, int sortingOrder)
        {
            if (current == null)
            {
                Transform existing = transform.Find(objectName);
                if (existing != null) current = existing.GetComponent<TextMesh>();
            }
            if (current == null)
            {
                GameObject labelObject = new GameObject(objectName);
                labelObject.transform.SetParent(transform, false);
                current = labelObject.AddComponent<TextMesh>();
            }
            current.transform.localPosition = localPosition;
            current.anchor = TextAnchor.MiddleCenter;
            current.alignment = TextAlignment.Center;
            current.characterSize = characterSize;
            current.fontSize = fontSize;
            current.fontStyle = FontStyle.Bold;

            // SpriteRenderer.sortingOrder takes precedence over distance. The explicit
            // order keeps identifiers visible over both normal and gold fusion faces.
            MeshRenderer renderer = current.GetComponent<MeshRenderer>();
            renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
            return current;
        }

        private static Sprite GetSharedSprite()
        {
            if (sharedSprite != null) return sharedSprite;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "PrototypeWhitePixel";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, true);
            sharedSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            sharedSprite.name = "PrototypeSquare";
            return sharedSprite;
        }

        private static Sprite GetMonsterAuraSprite()
        {
            if (monsterAuraSprite != null) return monsterAuraSprite;
            const int resolution = 32;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.name = "MonsterAuraRing";
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float dx = (x - 15.5f) / 15.5f;
                float dy = (y - 15.5f) / 15.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) / 0.2f);
                float glow = Mathf.Clamp01(1f - distance) * 0.28f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(ring * 0.82f, glow)));
            }
            texture.Apply(false, true);
            monsterAuraSprite = Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
            monsterAuraSprite.name = "MonsterAuraSprite";
            return monsterAuraSprite;
        }

        private static string SuitText(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Spade: return "♠";
                case CardSuit.Diamond: return "♦";
                case CardSuit.Heart: return "♥";
                default: return "♣";
            }
        }

        private static string RankText(CardRank rank)
        {
            switch (rank)
            {
                case CardRank.Jack: return "J";
                case CardRank.Queen: return "Q";
                case CardRank.King: return "K";
                case CardRank.Ace: return "A";
                default: return ((int)rank).ToString();
            }
        }
    }
}
