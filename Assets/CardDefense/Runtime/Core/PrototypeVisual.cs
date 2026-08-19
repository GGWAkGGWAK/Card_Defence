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

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSharedSprite();
            spriteRenderer.color = color;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public void SetMonsterStyle()
        {
            EnsureReady();
            spriteRenderer.color = new Color(0.85f, 0.25f, 0.22f, 1f);
            transform.localScale = new Vector3(0.45f, 0.45f, 1f);
        }

        public void SetCard(PlayingCard card)
        {
            EnsureReady();
            spriteRenderer.color = card.Suit == CardSuit.Heart || card.Suit == CardSuit.Diamond
                ? new Color(0.95f, 0.78f, 0.78f, 1f)
                : new Color(0.78f, 0.87f, 0.95f, 1f);
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

            label.text = RankText(card.Rank) + SuitText(card.Suit);
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
