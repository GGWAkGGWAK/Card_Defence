using CardDefense.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public enum GuidePage
    {
        Settings,
        PokerHands,
        Rules
    }

    public sealed class SettingsGuideController : MonoBehaviour
    {
        public bool IsVisible => panel != null && panel.activeSelf;
        public GuidePage CurrentPage { get; private set; }

        private StartupMenuController startup;
        private GameSettingsService settings;
        private PerformanceManager performance;
        private GameObject panel;
        private Text titleText;
        private Text contentText;
        private Button openButton;
        private Button bgmButton;
        private Button sfxButton;
        private Button vibrationButton;
        private Button performanceButton;
        private Button handsButton;
        private Button rulesButton;
        private Button closeButton;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private float previousTimeScale = 1f;

        public void Configure(Transform canvas, Font font, StartupMenuController startupMenu,
            GameSettingsService settingsService, PerformanceManager performanceManager)
        {
            startup = startupMenu;
            settings = settingsService;
            performance = performanceManager;
            BuildUi(canvas, font);
            ShowPage(GuidePage.Settings);
            panel.SetActive(false);
        }

        private void Update()
        {
            if (openButton != null)
                openButton.gameObject.SetActive(startup == null || !startup.IsVisible);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (bgmButton != null) bgmButton.onClick.RemoveListener(ToggleBgm);
            if (sfxButton != null) sfxButton.onClick.RemoveListener(ToggleSfx);
            if (vibrationButton != null) vibrationButton.onClick.RemoveListener(ToggleVibration);
            if (performanceButton != null) performanceButton.onClick.RemoveListener(TogglePerformance);
            if (handsButton != null) handsButton.onClick.RemoveListener(ShowPokerHands);
            if (rulesButton != null) rulesButton.onClick.RemoveListener(ShowRules);
            if (closeButton != null) closeButton.onClick.RemoveListener(HandleCloseOrBack);
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(ChangeBgmVolume);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(ChangeSfxVolume);
        }

        public void Open()
        {
            if (startup != null && startup.IsVisible) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            ShowPage(GuidePage.Settings);
        }

        public void Close()
        {
            panel.SetActive(false);
            Time.timeScale = previousTimeScale;
        }

        public void ToggleBgm()
        {
            settings.SetBgmEnabled(!settings.BgmEnabled);
            RefreshSettingsLabels();
        }

        public void ToggleSfx()
        {
            settings.SetSfxEnabled(!settings.SfxEnabled);
            RefreshSettingsLabels();
        }

        public void ToggleVibration()
        {
            settings.SetVibrationEnabled(!settings.VibrationEnabled);
            RefreshSettingsLabels();
        }

        public void TogglePerformance()
        {
            performance.CycleMode();
            RefreshSettingsLabels();
        }

        private void ChangeBgmVolume(float value)
        {
            settings.SetBgmVolume(value);
            RefreshSettingsLabels();
        }

        private void ChangeSfxVolume(float value)
        {
            settings.SetSfxVolume(value);
            RefreshSettingsLabels();
        }

        public void ShowPokerHands() => ShowPage(GuidePage.PokerHands);
        public void ShowRules() => ShowPage(GuidePage.Rules);

        public void ShowPage(GuidePage page)
        {
            CurrentPage = page;
            bool showSettings = page == GuidePage.Settings;
            bgmButton.gameObject.SetActive(showSettings);
            sfxButton.gameObject.SetActive(showSettings);
            vibrationButton.gameObject.SetActive(showSettings);
            performanceButton.gameObject.SetActive(showSettings);
            handsButton.gameObject.SetActive(showSettings);
            rulesButton.gameObject.SetActive(showSettings);
            bgmSlider.gameObject.SetActive(showSettings);
            sfxSlider.gameObject.SetActive(showSettings);
            SetLabel(closeButton, showSettings ? "닫기" : "설정으로");

            if (showSettings)
            {
                titleText.text = "게임 설정";
                contentText.text = "v" + Application.version + "  ·  " + SystemInfo.systemMemorySize +
                                   "MB  ·  설정은 기기에 자동 저장됩니다.";
                RefreshSettingsLabels();
            }
            else if (page == GuidePage.PokerHands)
            {
                titleText.text = "포커 족보 도감";
                contentText.text =
                    "하이 · 완성 족보 없음\n" +
                    "원페어 · 같은 숫자 2장\n" +
                    "투페어 · 서로 다른 페어 2개\n" +
                    "트리플 · 같은 숫자 3장\n" +
                    "스트레이트 · 연속 숫자 5장\n" +
                    "플러시 · 같은 무늬 5장\n" +
                    "풀하우스 · 트리플 + 원페어\n" +
                    "포카드 · 같은 숫자 4장\n" +
                    "스트레이트 플러시 · 같은 무늬의 연속 5장\n" +
                    "로열 스트레이트 플러시 · 같은 무늬 10·J·Q·K·A\n\n" +
                    "A는 1 또는 K 다음의 높은 카드로 사용할 수 있습니다.";
            }
            else
            {
                titleText.text = "게임 및 합성 규칙";
                contentText.text =
                    "· 골드를 사용해 카드를 소환하고 원하는 빈 슬롯에 배치합니다.\n" +
                    "· 카드를 드래그하면 빈 슬롯 이동 또는 카드끼리 위치 교환이 가능합니다.\n" +
                    "· 정확히 5장을 선택하면 포커 족보로 무료 합성할 수 있습니다.\n" +
                    "· 무늬와 숫자가 모두 같은 카드가 조합에 중복되면 합성할 수 없습니다.\n" +
                    "· 합성이 완료된 타워는 다시 합성할 수 없고 강화만 가능합니다.\n" +
                    "· 합성 공격력은 핵심 족보 카드 100%, 나머지 재료 10%를 반영합니다.\n" +
                    "· 라운드가 바뀌어도 살아 있는 몬스터는 사라지지 않습니다.\n" +
                    "· 몬스터 수가 한계에 도달하면 패배합니다.\n" +
                    "· 10라운드마다 보스와 영구 런 성장 선택이 등장합니다.";
            }
        }

        private void RefreshSettingsLabels()
        {
            SetLabel(bgmButton, "배경음  " + OnOff(settings.BgmEnabled) + "  " +
                                Mathf.RoundToInt(settings.BgmVolume * 100f) + "%");
            SetLabel(sfxButton, "효과음  " + OnOff(settings.SfxEnabled) + "  " +
                                Mathf.RoundToInt(settings.SfxVolume * 100f) + "%");
            SetLabel(vibrationButton, "진동  " + OnOff(settings.VibrationEnabled));
            SetLabel(performanceButton, "성능  " + performance.KoreanName);
        }

        private static string OnOff(bool value) => value ? "켜짐" : "꺼짐";

        private static void SetLabel(Button button, string value)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = value;
        }

        private void BuildUi(Transform canvas, Font font)
        {
            openButton = CreateButton(canvas, "SettingsButton", "설정",
                new Vector2(0.64f, 0.795f), new Vector2(0.80f, 0.845f), font,
                new Color(0.22f, 0.34f, 0.38f, 0.96f));
            openButton.GetComponentInChildren<Text>().fontSize = 25;
            openButton.onClick.AddListener(Open);

            panel = new GameObject("SettingsGuidePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.06f, 0.12f);
            panelRect.anchorMax = new Vector2(0.94f, 0.88f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.015f, 0.055f, 0.065f, 0.99f);

            titleText = CreateText(panel.transform, "SettingsTitle", "게임 설정", 48,
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f), font, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            contentText = CreateText(panel.transform, "SettingsContent", string.Empty, 27,
                new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.84f), font,
                new Color(0.88f, 0.94f, 0.94f, 1f));
            contentText.alignment = TextAnchor.UpperLeft;
            contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            contentText.verticalOverflow = VerticalWrapMode.Overflow;

            bgmButton = CreateButton(panel.transform, "BgmToggle", "배경음", new Vector2(0.10f, 0.72f),
                new Vector2(0.90f, 0.79f), font, new Color(0.1f, 0.52f, 0.55f, 1f));
            bgmSlider = CreateSlider(panel.transform, "BgmVolume", new Vector2(0.14f, 0.665f),
                new Vector2(0.86f, 0.705f), settings.BgmVolume);
            sfxButton = CreateButton(panel.transform, "SfxToggle", "효과음", new Vector2(0.10f, 0.57f),
                new Vector2(0.90f, 0.64f), font, new Color(0.1f, 0.52f, 0.55f, 1f));
            sfxSlider = CreateSlider(panel.transform, "SfxVolume", new Vector2(0.14f, 0.515f),
                new Vector2(0.86f, 0.555f), settings.SfxVolume);
            vibrationButton = CreateButton(panel.transform, "VibrationToggle", "진동", new Vector2(0.10f, 0.42f),
                new Vector2(0.90f, 0.49f), font, new Color(0.1f, 0.52f, 0.55f, 1f));
            performanceButton = CreateButton(panel.transform, "PerformanceToggle", "성능",
                new Vector2(0.10f, 0.33f), new Vector2(0.90f, 0.40f), font,
                new Color(0.1f, 0.52f, 0.55f, 1f));
            handsButton = CreateButton(panel.transform, "PokerHandsButton", "포커 족보 도감",
                new Vector2(0.10f, 0.24f), new Vector2(0.90f, 0.31f), font,
                new Color(0.72f, 0.42f, 0.1f, 1f));
            rulesButton = CreateButton(panel.transform, "RulesButton", "게임·합성 규칙",
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.22f), font,
                new Color(0.72f, 0.42f, 0.1f, 1f));
            closeButton = CreateButton(panel.transform, "SettingsCloseButton", "닫기 / 설정으로",
                new Vector2(0.24f, 0.045f), new Vector2(0.76f, 0.125f), font,
                new Color(0.78f, 0.16f, 0.2f, 1f));

            bgmButton.onClick.AddListener(ToggleBgm);
            sfxButton.onClick.AddListener(ToggleSfx);
            vibrationButton.onClick.AddListener(ToggleVibration);
            performanceButton.onClick.AddListener(TogglePerformance);
            handsButton.onClick.AddListener(ShowPokerHands);
            rulesButton.onClick.AddListener(ShowRules);
            closeButton.onClick.AddListener(HandleCloseOrBack);
            bgmSlider.onValueChanged.AddListener(ChangeBgmVolume);
            sfxSlider.onValueChanged.AddListener(ChangeSfxVolume);
        }

        private void HandleCloseOrBack()
        {
            if (CurrentPage == GuidePage.Settings) Close();
            else ShowPage(GuidePage.Settings);
        }

        private static Text CreateText(Transform parent, string name, string value, int size,
            Vector2 min, Vector2 max, Font font, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = child.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 15;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 min, Vector2 max, Font font, Color color)
        {
            GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            child.GetComponent<Image>().color = color;
            Text text = CreateText(child.transform, "Label", label, 32, Vector2.zero, Vector2.one, font, Color.white);
            text.fontStyle = FontStyle.Bold;
            return child.GetComponent<Button>();
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 min,
            Vector2 max, float value)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.28f);
            backgroundRect.anchorMax = new Vector2(1f, 0.72f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.02f, 0.12f, 0.14f, 1f);

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.28f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.72f);
            fillAreaRect.offsetMin = new Vector2(4f, 0f);
            fillAreaRect.offsetMax = new Vector2(-4f, 0f);
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.12f, 0.88f, 0.86f, 1f);

            GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);
            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28f, 28f);
            handle.GetComponent<Image>().color = new Color(1f, 0.78f, 0.22f, 1f);

            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            return slider;
        }
    }
}
