using CardDefense.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class UiThemeController : MonoBehaviour
    {
        public void Configure(Transform hudRoot)
        {
            Button[] buttons = hudRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++) ApplyButton(buttons[i]);
            Text[] texts = hudRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text.GetComponentInParent<Button>() != null || text.GetComponent<Outline>() != null) continue;
                Outline outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0.04f, 0.05f, 0.72f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            ApplyPanelFrames(hudRoot);
        }

        private static void ApplyPanelFrames(Transform root)
        {
            Sprite frame = VisualAssetLibrary.GetUiFrameSprite();
            if (frame == null) return;
            string[] names = { "StartupMenu", "SettingsGuidePanel", "GrowthChoicePanel" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform panel = FindDeep(root, names[i]);
                if (panel == null || panel.Find("PremiumFrame") != null) continue;
                GameObject overlay = new GameObject("PremiumFrame", typeof(RectTransform), typeof(Image));
                overlay.transform.SetParent(panel, false);
                RectTransform rect = overlay.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-5f, -5f);
                rect.offsetMax = new Vector2(5f, 5f);
                Image image = overlay.GetComponent<Image>();
                image.sprite = frame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = false;
                overlay.transform.SetAsLastSibling();
            }
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == objectName) return all[i];
            return null;
        }

        public static Color ButtonBaseColor(string buttonName)
        {
            if (buttonName.Contains("Summon")) return new Color(0.05f, 0.58f, 0.62f, 0.98f);
            if (buttonName.Contains("Merge") || buttonName.Contains("PokerHands"))
                return new Color(0.76f, 0.43f, 0.08f, 0.98f);
            if (buttonName.Contains("Upgrade") || buttonName.Contains("Growth"))
                return new Color(0.42f, 0.24f, 0.68f, 0.98f);
            if (buttonName.Contains("Sell") || buttonName.Contains("Close") || buttonName.Contains("NewGame"))
                return new Color(0.72f, 0.12f, 0.17f, 0.98f);
            if (buttonName.Contains("Continue")) return new Color(0.08f, 0.58f, 0.36f, 0.98f);
            return new Color(0.16f, 0.32f, 0.37f, 0.98f);
        }

        private static void ApplyButton(Button button)
        {
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = ButtonBaseColor(button.name);
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.72f, 0.76f, 0.78f, 1f);
            colors.disabledColor = new Color(0.35f, 0.38f, 0.4f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (button.GetComponent<Outline>() == null)
            {
                Outline outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.02f, 0.05f, 0.06f, 0.95f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
            if (button.GetComponent<Shadow>() == null)
            {
                Shadow shadow = button.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
                shadow.effectDistance = new Vector2(0f, -4f);
            }
        }
    }
}
