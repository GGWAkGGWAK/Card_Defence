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
        private Color cardColor;

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
            switch (archetype)
            {
                case CardDefense.Enemies.MonsterArchetype.Fast:
                    spriteRenderer.color = new Color(0.25f, 0.75f, 1f, 1f);
                    transform.localScale = new Vector3(0.38f, 0.38f, 1f);
                    break;
                case CardDefense.Enemies.MonsterArchetype.Tank:
                    spriteRenderer.color = new Color(0.55f, 0.32f, 0.82f, 1f);
                    transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                    break;
                case CardDefense.Enemies.MonsterArchetype.Gold:
                    spriteRenderer.color = new Color(1f, 0.76f, 0.12f, 1f);
                    transform.localScale = new Vector3(0.48f, 0.48f, 1f);
                    break;
                case CardDefense.Enemies.MonsterArchetype.Boss:
                    spriteRenderer.color = new Color(0.95f, 0.08f, 0.18f, 1f);
                    transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                    break;
                default:
                    spriteRenderer.color = new Color(0.85f, 0.25f, 0.22f, 1f);
                    transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                    break;
            }
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
            cardColor = card.Suit == CardSuit.Heart || card.Suit == CardSuit.Diamond
                ? new Color(0.95f, 0.78f, 0.78f, 1f)
                : new Color(0.78f, 0.87f, 0.95f, 1f);
            spriteRenderer.color = cardColor;
            transform.localScale = new Vector3(0.8f, 1.05f, 1f);

            if (label == null)
            {
                GameObject labelObject = new GameObject("CardLabel");
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                label = labelObject.AddComponent<TextMesh>();
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = 0.15f;
                label.fontSize = 32;
                label.color = Color.black;
            }

            bool suitDefinesFusion = hand == PokerHand.Flush || hand == PokerHand.StraightFlush ||
                                     hand == PokerHand.RoyalStraightFlush;
            string suit = !isFusionResult || suitDefinesFusion ? SuitText(card.Suit) : string.Empty;
            label.text = RankText(card.Rank) + suit + "\n" +
                         PokerHandInfo.ShortName(hand) + (isFusionResult ? " ★" : string.Empty);
        }

        public void SetSelected(bool selected)
        {
            EnsureReady();
            spriteRenderer.color = selected ? Color.Lerp(cardColor, new Color(1f, 0.82f, 0.12f), 0.55f) : cardColor;
            transform.localScale = selected ? new Vector3(0.9f, 1.17f, 1f) : new Vector3(0.8f, 1.05f, 1f);
        }

        private void EnsureReady()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer.sprite == null) spriteRenderer.sprite = GetSharedSprite();
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
