using CardDefense.Combat;
using CardDefense.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public enum TutorialStep
    {
        Summon,
        Placement,
        Selection,
        Merge,
        Upgrade,
        Speed,
        Complete
    }

    public sealed class TutorialController : MonoBehaviour
    {
        private const string CompletionKey = "CardDefense.TutorialCompleted.v1";

        public TutorialStep CurrentStep { get; private set; }
        public bool IsVisible => panel != null && panel.activeSelf;

#if UNITY_EDITOR
        public static bool BypassForTests;
        public static string EditorCompletionKeyOverride;
#endif

        private static string ActiveCompletionKey
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(EditorCompletionKeyOverride)) return EditorCompletionKeyOverride;
#endif
                return CompletionKey;
            }
        }

        private StartupMenuController startup;
        private CardSummonController summon;
        private PokerProgressionService progression;
        private PrototypeHud hud;
        private GameObject panel;
        private Text guideText;
        private Button helpButton;
        private Button skipButton;
        private Button summonButton;
        private Button mergeButton;
        private Button upgradeButton;
        private Button speedButton;
        private bool waitingForStartup;
        private bool started;

        public void Configure(Transform canvas, Font font, StartupMenuController startupMenu,
            CardSummonController summonController, PokerProgressionService progressionService,
            PrototypeHud prototypeHud, Button summonButtonReference, Button mergeButtonReference,
            Button upgradeButtonReference, Button speedButtonReference)
        {
            startup = startupMenu;
            summon = summonController;
            progression = progressionService;
            hud = prototypeHud;
            summonButton = summonButtonReference;
            mergeButton = mergeButtonReference;
            upgradeButton = upgradeButtonReference;
            speedButton = speedButtonReference;
            BuildUi(canvas, font);

            summon.CardSummoned += HandleCardSummoned;
            summon.SelectionChanged += HandleSelectionChanged;
            summon.CardsMerged += HandleCardsMerged;
            progression.HandUpgraded += HandleHandUpgraded;
            hud.SpeedChanged += HandleSpeedChanged;

#if UNITY_EDITOR
            if (BypassForTests) return;
#endif
            if (PlayerPrefs.GetInt(ActiveCompletionKey, 0) != 0) return;
            waitingForStartup = true;
        }

        private void Update()
        {
            if (helpButton != null)
                helpButton.gameObject.SetActive(startup == null || !startup.IsVisible);
            if (waitingForStartup && (startup == null || !startup.IsVisible))
            {
                waitingForStartup = false;
                BeginTutorial();
            }
            if (!started) return;
            if (CurrentStep == TutorialStep.Summon && summon.IsPlacementPending)
                SetStep(TutorialStep.Placement);
            else if (CurrentStep == TutorialStep.Selection && summon.SelectedCount > 0)
                SetStep(TutorialStep.Merge);
        }

        private void OnDestroy()
        {
            if (summon != null)
            {
                summon.CardSummoned -= HandleCardSummoned;
                summon.SelectionChanged -= HandleSelectionChanged;
                summon.CardsMerged -= HandleCardsMerged;
            }
            if (progression != null) progression.HandUpgraded -= HandleHandUpgraded;
            if (hud != null) hud.SpeedChanged -= HandleSpeedChanged;
            if (helpButton != null) helpButton.onClick.RemoveListener(ReplayTutorial);
            if (skipButton != null) skipButton.onClick.RemoveListener(SkipTutorial);
        }

        public void BeginTutorial()
        {
            started = true;
            if (panel != null) panel.SetActive(true);
            SetStep(TutorialStep.Summon);
        }

        public void ReplayTutorial()
        {
            PlayerPrefs.DeleteKey(ActiveCompletionKey);
            PlayerPrefs.Save();
            BeginTutorial();
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        public static void DeleteCompletion()
        {
            PlayerPrefs.DeleteKey(ActiveCompletionKey);
            PlayerPrefs.Save();
        }

        private void HandleCardSummoned()
        {
            if (started && (CurrentStep == TutorialStep.Placement || CurrentStep == TutorialStep.Summon))
                SetStep(TutorialStep.Selection);
        }

        private void HandleSelectionChanged()
        {
            if (started && CurrentStep == TutorialStep.Selection && summon.SelectedCount > 0)
                SetStep(TutorialStep.Merge);
            if (started && CurrentStep == TutorialStep.Merge) RefreshGuide();
        }

        private void HandleCardsMerged(CardDefense.Cards.PokerHand hand)
        {
            if (started && CurrentStep == TutorialStep.Merge) SetStep(TutorialStep.Upgrade);
        }

        private void HandleHandUpgraded(CardDefense.Cards.PokerHand hand, int level)
        {
            if (started && CurrentStep == TutorialStep.Upgrade) SetStep(TutorialStep.Speed);
        }

        private void HandleSpeedChanged(float speed)
        {
            if (started && CurrentStep == TutorialStep.Speed) CompleteTutorial();
        }

        private void SetStep(TutorialStep step)
        {
            CurrentStep = step;
            RefreshGuide();
            RefreshHighlights();
        }

        private void RefreshGuide()
        {
            if (guideText == null) return;
            switch (CurrentStep)
            {
                case TutorialStep.Summon:
                    guideText.text = "1/6  아래의 [소환] 버튼을 눌러 카드를 준비하세요.";
                    break;
                case TutorialStep.Placement:
                    guideText.text = "2/6  필드의 빛나는 빈 슬롯을 눌러 카드를 배치하세요.";
                    break;
                case TutorialStep.Selection:
                    guideText.text = "3/6  배치된 카드를 눌러 선택하세요. 드래그하면 위치도 바꿀 수 있습니다.";
                    break;
                case TutorialStep.Merge:
                    guideText.text = "4/6  카드를 더 소환해 5장을 선택한 뒤 [5장 합성]을 누르세요.  현재 " +
                                     summon.SelectedCount + "/5장";
                    break;
                case TutorialStep.Upgrade:
                    guideText.text = "5/6  합성된 타워가 선택되어 있습니다. [강화]로 해당 족보를 성장시키세요.";
                    break;
                case TutorialStep.Speed:
                    guideText.text = "6/6  우측 상단의 배속 버튼으로 x1 · x2 · x4 속도를 변경하세요.";
                    break;
                default:
                    guideText.text = string.Empty;
                    break;
            }
        }

        private void CompleteTutorial()
        {
            started = false;
            CurrentStep = TutorialStep.Complete;
            if (panel != null) panel.SetActive(false);
            RefreshHighlights();
            PlayerPrefs.SetInt(ActiveCompletionKey, 1);
            PlayerPrefs.Save();
        }

        private void RefreshHighlights()
        {
            SetHighlight(summonButton, CurrentStep == TutorialStep.Summon);
            SetHighlight(mergeButton, CurrentStep == TutorialStep.Merge);
            SetHighlight(upgradeButton, CurrentStep == TutorialStep.Upgrade);
            SetHighlight(speedButton, CurrentStep == TutorialStep.Speed);
        }

        private static void SetHighlight(Button button, bool highlighted)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image == null) return;
            image.color = highlighted
                ? new Color(1f, 0.7f, 0.08f, 1f)
                : UiThemeController.ButtonBaseColor(button.name);
        }

        private void BuildUi(Transform canvas, Font font)
        {
            helpButton = CreateButton(canvas, "TutorialHelpButton", "도움말",
                new Vector2(0.82f, 0.795f), new Vector2(0.98f, 0.845f), font,
                new Color(0.12f, 0.48f, 0.58f, 0.96f));
            helpButton.GetComponentInChildren<Text>().fontSize = 25;
            helpButton.onClick.AddListener(ReplayTutorial);

            panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.72f);
            rect.anchorMax = new Vector2(0.96f, 0.79f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.02f, 0.12f, 0.14f, 0.96f);

            guideText = CreateText(panel.transform, "TutorialGuide", string.Empty, 27,
                new Vector2(0.03f, 0.08f), new Vector2(0.82f, 0.92f), font, Color.white);
            guideText.alignment = TextAnchor.MiddleLeft;
            skipButton = CreateButton(panel.transform, "TutorialSkipButton", "건너뛰기",
                new Vector2(0.83f, 0.18f), new Vector2(0.98f, 0.82f), font,
                new Color(0.35f, 0.4f, 0.42f, 1f));
            skipButton.GetComponentInChildren<Text>().fontSize = 20;
            skipButton.onClick.AddListener(SkipTutorial);
            panel.SetActive(false);
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
            text.resizeTextMinSize = 16;
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
    }
}
