using CardDefense.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardDefense.UI
{
    public sealed class StartupMenuController : MonoBehaviour
    {
        public bool IsVisible => panel != null && panel.activeSelf;
        public bool CanContinue => continueButton != null && continueButton.gameObject.activeSelf;

#if UNITY_EDITOR
        public static bool BypassForTests;
#endif

        private static bool freshStartRequested;
        private GameObject panel;
        private Button continueButton;
        private Button newGameButton;

        public void Configure(Transform canvas, Font font, RunSaveService runSave,
            PlayerProfileService profile)
        {
            bool skipMenu = freshStartRequested;
            freshStartRequested = false;
#if UNITY_EDITOR
            skipMenu |= BypassForTests;
#endif
            if (skipMenu)
            {
                Time.timeScale = 1f;
                return;
            }

            BuildMenu(canvas, font, runSave != null && runSave.WasRestored, profile);
            Time.timeScale = 0f;
        }

        public void ContinueGame()
        {
            if (!CanContinue) return;
            HideAndPlay();
        }

        public void StartNewGame()
        {
            RunSaveService.DeleteActiveRun();
            if (continueButton != null && continueButton.gameObject.activeSelf)
            {
                freshStartRequested = true;
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
                return;
            }
            HideAndPlay();
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueGame);
            if (newGameButton != null) newGameButton.onClick.RemoveListener(StartNewGame);
        }

        private void HideAndPlay()
        {
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void BuildMenu(Transform canvas, Font font, bool hasRestoredRun,
            PlayerProfileService profile)
        {
            panel = new GameObject("StartupMenu", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.015f, 0.045f, 0.055f, 0.985f);

            Text title = CreateText(panel.transform, "Title", "CARD DEFENSE", 72,
                new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.84f), font, Color.white);
            title.fontStyle = FontStyle.Bold;
            CreateText(panel.transform, "Subtitle", "포커 덱으로 끝없는 웨이브를 방어하세요", 32,
                new Vector2(0.08f, 0.63f), new Vector2(0.92f, 0.70f), font,
                new Color(1f, 0.82f, 0.3f, 1f));

            PlayerProfileData data = profile != null ? profile.Data : null;
            string record = data == null
                ? "최고 기록  R0"
                : "최고 기록  R" + data.BestRound + "   ·   총 플레이 " + data.TotalRuns +
                  "회\n누적 처치 " + data.TotalMonstersDefeated + "   ·   누적 골드 " +
                  data.TotalGoldEarned;
            CreateText(panel.transform, "ProfileSummary", record, 29,
                new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.61f), font,
                new Color(0.75f, 0.88f, 0.88f, 1f));

            continueButton = CreateButton(panel.transform, "ContinueButton", "이어하기",
                new Vector2(0.16f, 0.34f), new Vector2(0.84f, 0.43f), font,
                new Color(0.12f, 0.65f, 0.42f, 1f));
            continueButton.gameObject.SetActive(hasRestoredRun);
            continueButton.onClick.AddListener(ContinueGame);

            Vector2 newMin = hasRestoredRun ? new Vector2(0.16f, 0.22f) : new Vector2(0.16f, 0.30f);
            Vector2 newMax = hasRestoredRun ? new Vector2(0.84f, 0.31f) : new Vector2(0.84f, 0.39f);
            newGameButton = CreateButton(panel.transform, "NewGameButton", "새 게임",
                newMin, newMax, font, new Color(0.82f, 0.16f, 0.2f, 1f));
            newGameButton.onClick.AddListener(StartNewGame);

            string guide = hasRestoredRun
                ? "저장된 라운드를 이어가거나 새 게임을 시작할 수 있습니다."
                : "새 게임을 눌러 방어를 시작하세요.";
            CreateText(panel.transform, "Guide", guide, 25,
                new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.19f), font,
                new Color(0.62f, 0.72f, 0.73f, 1f));
            panel.transform.SetAsLastSibling();
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
            text.resizeTextMinSize = 18;
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
            Text text = CreateText(child.transform, "Label", label, 42, Vector2.zero, Vector2.one,
                font, Color.white);
            text.fontStyle = FontStyle.Bold;
            return child.GetComponent<Button>();
        }
    }
}
