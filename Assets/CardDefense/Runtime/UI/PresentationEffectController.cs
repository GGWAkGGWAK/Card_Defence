using CardDefense.Cards;
using CardDefense.Combat;
using CardDefense.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class PresentationEffectController : MonoBehaviour
    {
        public bool IsPlaying => timer > 0f;
        public string LastPresentation { get; private set; }
        public PokerHand LastFusionHand { get; private set; }

        private CardSummonController summon;
        private WaveDirector waves;
        private Image flash;
        private Text banner;
        private float timer;
        private float duration;
        private Color flashColor;
        private Color bannerColor;

        public void Configure(Transform canvas, Font font, CardSummonController summonController,
            WaveDirector waveDirector)
        {
            summon = summonController;
            waves = waveDirector;
            BuildUi(canvas, font);
            summon.CardsMerged += HandleMerged;
            waves.RoundChanged += HandleRoundChanged;
            waves.ChallengeBossSpawned += HandleChallengeBossSpawned;
            waves.GameLost += HandleGameLost;
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;
            float normalized = 1f - Mathf.Clamp01(timer / duration);
            float fade = normalized < 0.22f ? normalized / 0.22f : 1f - ((normalized - 0.22f) / 0.78f);
            fade = Mathf.Clamp01(fade);
            Color screen = flashColor;
            screen.a *= fade;
            flash.color = screen;
            Color text = bannerColor;
            text.a = fade;
            banner.color = text;
            banner.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.22f, 1f, Mathf.Clamp01(normalized * 4f));
            if (timer > 0f) return;
            flash.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (summon != null) summon.CardsMerged -= HandleMerged;
            if (waves != null)
            {
                waves.RoundChanged -= HandleRoundChanged;
                waves.ChallengeBossSpawned -= HandleChallengeBossSpawned;
                waves.GameLost -= HandleGameLost;
            }
        }

        private void HandleMerged(PokerHand hand)
        {
            LastFusionHand = hand;
            Show(PokerHandInfo.KoreanName(hand) + " 합성 성공!", "FUSION", 1.05f,
                new Color(1f, 0.72f, 0.08f, 0.18f), CombatEffectSystem.HandColor(hand));
        }

        private void HandleRoundChanged(int round)
        {
            if (round % 10 != 0) return;
            Show("BOSS ROUND  " + round, "BOSS", 1.35f,
                new Color(0.8f, 0.02f, 0.02f, 0.24f), new Color(1f, 0.24f, 0.12f, 1f));
        }

        private void HandleGameLost()
        {
            Show("DEFENSE BREAK", "DEFEAT", 1.5f,
                new Color(0.35f, 0f, 0f, 0.38f), new Color(1f, 0.28f, 0.22f, 1f));
        }

        private void HandleChallengeBossSpawned()
        {
            Show("위험 보스 출현!  제한 시간 내 처치", "CHALLENGE_BOSS", 1.25f,
                new Color(0.85f, 0.03f, 0.02f, 0.27f), new Color(1f, 0.72f, 0.12f, 1f));
        }

        private void Show(string text, string id, float seconds, Color screen, Color textColor)
        {
            LastPresentation = id;
            duration = seconds;
            timer = seconds;
            flashColor = screen;
            bannerColor = textColor;
            banner.text = text;
            flash.gameObject.SetActive(true);
            banner.gameObject.SetActive(true);
            flash.transform.SetAsLastSibling();
            banner.transform.SetAsLastSibling();
        }

        private void BuildUi(Transform canvas, Font font)
        {
            GameObject flashObject = new GameObject("PresentationFlash", typeof(RectTransform), typeof(Image));
            flashObject.transform.SetParent(canvas, false);
            RectTransform flashRect = flashObject.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            flash = flashObject.GetComponent<Image>();
            flash.raycastTarget = false;
            flash.color = Color.clear;

            GameObject bannerObject = new GameObject("PresentationBanner", typeof(RectTransform), typeof(Text));
            bannerObject.transform.SetParent(canvas, false);
            RectTransform bannerRect = bannerObject.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.06f, 0.43f);
            bannerRect.anchorMax = new Vector2(0.94f, 0.57f);
            bannerRect.offsetMin = Vector2.zero;
            bannerRect.offsetMax = Vector2.zero;
            banner = bannerObject.GetComponent<Text>();
            banner.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            banner.fontSize = 58;
            banner.fontStyle = FontStyle.Bold;
            banner.alignment = TextAnchor.MiddleCenter;
            banner.raycastTarget = false;
            Outline outline = bannerObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            flashObject.SetActive(false);
            bannerObject.SetActive(false);
        }
    }
}
