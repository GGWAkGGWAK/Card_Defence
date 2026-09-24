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
        public int LastFusionBurstCount { get; private set; }

        private CardSummonController summon;
        private WaveDirector waves;
        private Image flash;
        private Text banner;
        private RectTransform fusionBurst;
        private Image[] burstRays;
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
            if (fusionBurst != null && fusionBurst.gameObject.activeSelf)
            {
                fusionBurst.localRotation = Quaternion.Euler(0f, 0f, normalized * 110f);
                fusionBurst.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.45f, normalized);
                for (int i = 0; i < burstRays.Length; i++)
                {
                    Color rayColor = burstRays[i].color;
                    rayColor.a = fade * 0.78f;
                    burstRays[i].color = rayColor;
                }
            }
            if (timer > 0f) return;
            flash.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            if (fusionBurst != null) fusionBurst.gameObject.SetActive(false);
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
            bool legendary = hand >= PokerHand.StraightFlush;
            bool rare = hand >= PokerHand.Flush;
            LastFusionBurstCount = legendary ? 12 : rare ? 8 : 4;
            Show(PokerHandInfo.KoreanName(hand) + " 합성 성공!",
                legendary ? "FUSION_LEGENDARY" : rare ? "FUSION_RARE" : "FUSION_COMMON",
                legendary ? 1.55f : rare ? 1.28f : 1.05f,
                new Color(CombatEffectSystem.HandColor(hand).r,
                    CombatEffectSystem.HandColor(hand).g,
                    CombatEffectSystem.HandColor(hand).b, legendary ? 0.34f : rare ? 0.25f : 0.18f),
                CombatEffectSystem.HandColor(hand));
            ShowFusionBurst(CombatEffectSystem.HandColor(hand), LastFusionBurstCount);
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

        private void ShowFusionBurst(Color color, int count)
        {
            if (fusionBurst == null || burstRays == null) return;
            fusionBurst.localRotation = Quaternion.identity;
            fusionBurst.localScale = Vector3.one * 0.35f;
            fusionBurst.gameObject.SetActive(true);
            fusionBurst.SetAsLastSibling();
            banner.transform.SetAsLastSibling();
            for (int i = 0; i < burstRays.Length; i++)
            {
                bool enabled = i < count;
                burstRays[i].gameObject.SetActive(enabled);
                if (!enabled) continue;
                Color rayColor = color;
                rayColor.a = 0f;
                burstRays[i].color = rayColor;
            }
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

            GameObject burstObject = new GameObject("FusionTierBurst", typeof(RectTransform));
            burstObject.transform.SetParent(canvas, false);
            fusionBurst = burstObject.GetComponent<RectTransform>();
            fusionBurst.anchorMin = new Vector2(0.5f, 0.5f);
            fusionBurst.anchorMax = new Vector2(0.5f, 0.5f);
            fusionBurst.sizeDelta = new Vector2(420f, 420f);
            burstRays = new Image[12];
            for (int i = 0; i < burstRays.Length; i++)
            {
                GameObject rayObject = new GameObject("FusionRay_" + i.ToString("00"),
                    typeof(RectTransform), typeof(Image));
                rayObject.transform.SetParent(fusionBurst, false);
                RectTransform ray = rayObject.GetComponent<RectTransform>();
                ray.anchorMin = new Vector2(0.5f, 0.5f);
                ray.anchorMax = new Vector2(0.5f, 0.5f);
                ray.pivot = new Vector2(0.5f, 0f);
                ray.sizeDelta = new Vector2(i % 2 == 0 ? 8f : 5f, i % 2 == 0 ? 190f : 145f);
                ray.localRotation = Quaternion.Euler(0f, 0f, i * 30f);
                Image image = rayObject.GetComponent<Image>();
                image.raycastTarget = false;
                burstRays[i] = image;
            }
            flashObject.SetActive(false);
            bannerObject.SetActive(false);
            burstObject.SetActive(false);
        }
    }
}
