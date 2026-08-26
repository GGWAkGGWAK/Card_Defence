using CardDefense.Cards;
using UnityEngine;

namespace CardDefense.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrototypeVisual : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector2 size = Vector2.one;

        private static Sprite sharedSprite;
        private SpriteRenderer spriteRenderer;
        private TextMesh label;
        private TextMesh handLabel;
        private Color cardColor;
        private Color cardBaseTint = Color.white;
        private bool monsterMode;
        private Vector3 monsterScale;
        private float animationPhase;
        private float hitFlashTimer;
        private float attackPulseTimer;
        private Vector3 cardRestScale = new Vector3(0.8f, 1.05f, 1f);

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSharedSprite();
            spriteRenderer.color = color;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public void SetMonsterStyle(CardDefense.Enemies.MonsterArchetype archetype)
        {
            EnsureReady();
            Sprite art = VisualAssetLibrary.GetMonsterSprite(archetype);
            if (art != null) spriteRenderer.sprite = art;
            Material artMaterial = VisualAssetLibrary.GetMonsterMaterial();
            if (artMaterial != null) spriteRenderer.sharedMaterial = artMaterial;
            spriteRenderer.color = Color.white;
            monsterMode = true;
            animationPhase = Random.value * Mathf.PI * 2f;
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
            transform.localScale = monsterScale;
            spriteRenderer.sortingOrder = 2;
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
            float pulse = 1f + Mathf.Sin(Time.time * 4.2f + animationPhase) * 0.035f;
            transform.localScale = new Vector3(monsterScale.x * pulse, monsterScale.y * pulse, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(Time.time * 2.8f + animationPhase) * 2.2f);
            if (hitFlashTimer <= 0f) return;
            hitFlashTimer -= Time.deltaTime;
            spriteRenderer.color = hitFlashTimer > 0f
                ? new Color(1f, 0.38f, 0.28f, 1f)
                : Color.white;
        }

        private void EnsureReady()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null) spriteRenderer.sprite = GetSharedSprite();
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
