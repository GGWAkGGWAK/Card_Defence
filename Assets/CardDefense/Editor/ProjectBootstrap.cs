using System.Collections.Generic;
using CardDefense.Combat;
using CardDefense.Core;
using CardDefense.Enemies;
using CardDefense.Pooling;
using CardDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardDefense.Editor
{
    public static class ProjectBootstrap
    {
        private const string Root = "Assets/CardDefense/Generated";
        private const string ScenePath = "Assets/Scenes/CardDefensePrototype.unity";

        [MenuItem("Card Defense/Build Prototype Project")]
        public static void Build()
        {
            EnsureFolders();
            ConfigureProject();

            GameBalanceConfig config = CreateOrLoadConfig();
            Monster monsterPrefab = CreateMonsterPrefab();
            CardTower towerPrefab = CreateTowerPrefab();
            BuildScene(config, monsterPrefab, towerPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("CARD_DEFENSE_BOOTSTRAP_SUCCESS");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "CardDefense");
            EnsureFolder("Assets/CardDefense", "Generated");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Card Defense Studio";
            PlayerSettings.productName = "Card Defense";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.carddefense.game");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
            QualitySettings.vSyncCount = 0;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/CardDefense/Art/Brand/CardDefenseIcon-v1.png");
            if (icon != null)
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android,
                    new[] { icon }, IconKind.Application);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        private static GameBalanceConfig CreateOrLoadConfig()
        {
            string path = Root + "/GameBalanceConfig.asset";
            GameBalanceConfig config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(path);
            if (config != null)
            {
                ApplyCurrentBalance(config);
                EditorUtility.SetDirty(config);
                return config;
            }

            config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            ApplyCurrentBalance(config);
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static void ApplyCurrentBalance(GameBalanceConfig config)
        {
            config.rewardGrowthPerRound = 1.022f;
            config.rewardSoftCapRound = 20;
            config.lateRewardGrowthPerRound = 1.012f;
            config.summonCostGrowthPerOccupiedCard = 1.045f;
            config.healthAccelerationRound = 50;
            config.lateHealthAccelerationPerRound = 1.006f;
            config.handUpgradeCostGrowth = 1.29f;
            config.goldRewardMultiplier = 2.25f;
            config.bossRewardMultiplier = 5f;
            config.bossQuestInitialCooldown = 25f;
            config.bossQuestCooldown = 75f;
            config.bossQuestTimeLimit = 25f;
            config.bossQuestHealthMultiplier = 16f;
            config.bossQuestBaseGold = 80;
            config.bossQuestGoldPerRound = 5;
            config.bossQuestAttackBonus = 0.03f;
            config.bossQuestHealthPerRiskTier = 0.18f;
            config.bossQuestRewardPerRiskTier = 0.12f;
            config.bossQuestFailureReinforcements = 5;
            config.bossQuestFailureHealthBonus = 0.3f;
            config.bossQuestFailureSpeedBonus = 0.2f;
        }

        private static Monster CreateMonsterPrefab()
        {
            string path = Root + "/Monster.prefab";
            Monster existing = AssetDatabase.LoadAssetAtPath<Monster>(path);
            if (existing != null) return existing;

            GameObject gameObject = new GameObject("Monster");
            gameObject.AddComponent<SpriteRenderer>();
            gameObject.AddComponent<PrototypeVisual>();
            Monster monster = gameObject.AddComponent<Monster>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
            Object.DestroyImmediate(gameObject);
            return prefab.GetComponent<Monster>();
        }

        private static CardTower CreateTowerPrefab()
        {
            string path = Root + "/CardTower.prefab";
            CardTower existing = AssetDatabase.LoadAssetAtPath<CardTower>(path);
            if (existing != null) return existing;

            GameObject gameObject = new GameObject("CardTower");
            gameObject.AddComponent<SpriteRenderer>();
            gameObject.AddComponent<PrototypeVisual>();
            CardTower tower = gameObject.AddComponent<CardTower>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
            Object.DestroyImmediate(gameObject);
            return prefab.GetComponent<CardTower>();
        }

        private static void BuildScene(GameBalanceConfig config, Monster monsterPrefab, CardTower towerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.gameObject.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.3f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.04f, 0.14f, 0.15f, 1f);

            GameObject root = new GameObject("GameRoot");
            EconomyService economy = root.AddComponent<EconomyService>();
            PokerProgressionService progression = root.AddComponent<PokerProgressionService>();
            MonsterSystem monsterSystem = root.AddComponent<MonsterSystem>();
            MonsterPool monsterPool = root.AddComponent<MonsterPool>();
            WaveDirector waves = root.AddComponent<WaveDirector>();
            CardTowerSystem towerSystem = root.AddComponent<CardTowerSystem>();
            CardSummonController summon = root.AddComponent<CardSummonController>();
            GameComposition composition = root.AddComponent<GameComposition>();

            LoopPath loopPath = CreateLoopPath();
            Transform[] slots = CreatePlacementSlots();
            PrototypeHud hud = CreateHud(out Text gold, out Text round, out Text alive,
                out Text message, out Text selection, out Text threat, out Button summonButton,
                out Button mergeButton, out Button upgradeButton, out Button sellButton,
                out Button restartButton, out Button speedButton, out GameObject growthPanel,
                out Text growthTitle, out Button growthAttack, out Button growthGold,
                out Button growthSummon);

            composition.SetReferences(config, loopPath, monsterPrefab, towerPrefab, slots,
                economy, progression, monsterSystem, monsterPool, waves, towerSystem, summon, hud,
                gold, round, alive, message, selection, threat, summonButton, mergeButton, upgradeButton,
                sellButton, restartButton, speedButton, growthPanel, growthTitle, growthAttack,
                growthGold, growthSummon);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeObject = root;
        }

        private static LoopPath CreateLoopPath()
        {
            GameObject pathObject = new GameObject("MonsterLoopPath");
            LoopPath path = pathObject.AddComponent<LoopPath>();
            const int count = 16;
            Transform[] points = new Transform[count];
            Vector3[] linePoints = new Vector3[count + 1];

            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 3.7f, Mathf.Sin(angle) * 5.4f, 0f);
                GameObject point = new GameObject("Point_" + i.ToString("00"));
                point.transform.SetParent(pathObject.transform, false);
                point.transform.position = position;
                points[i] = point.transform;
                linePoints[i] = position;
            }
            linePoints[count] = linePoints[0];
            path.Configure(points);

            LineRenderer line = pathObject.AddComponent<LineRenderer>();
            line.loop = false;
            line.positionCount = linePoints.Length;
            line.SetPositions(linePoints);
            line.startWidth = 0.85f;
            line.endWidth = 0.85f;
            line.sortingOrder = -10;
            line.startColor = new Color(0.28f, 0.36f, 0.34f, 1f);
            line.endColor = line.startColor;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            string materialPath = Root + "/PathMaterial.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            line.sharedMaterial = material;
            return path;
        }

        private static Transform[] CreatePlacementSlots()
        {
            GameObject root = new GameObject("PlacementSlots");
            List<Transform> slots = new List<Transform>(12);
            float[] xPositions = { -2.1f, -0.7f, 0.7f, 2.1f };
            float[] yPositions = { -2.4f, 0f, 2.4f };

            for (int y = 0; y < yPositions.Length; y++)
            {
                for (int x = 0; x < xPositions.Length; x++)
                {
                    GameObject slot = new GameObject("Slot_" + slots.Count.ToString("00"));
                    slot.transform.SetParent(root.transform, false);
                    slot.transform.position = new Vector3(xPositions[x], yPositions[y], 0f);
                    slot.AddComponent<SpriteRenderer>();
                    PrototypeVisual visual = slot.AddComponent<PrototypeVisual>();
                    visual.SetPlacementSlotStyle();
                    slots.Add(slot.transform);
                }
            }
            return slots.ToArray();
        }

        private static PrototypeHud CreateHud(out Text gold, out Text round, out Text alive,
            out Text message, out Text selection, out Text threat, out Button summonButton,
            out Button mergeButton, out Button upgradeButton, out Button sellButton,
            out Button restartButton, out Button speedButton, out GameObject growthPanel,
            out Text growthTitle, out Button growthAttack, out Button growthGold,
            out Button growthSummon)
        {
            GameObject canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(canvasObject.transform, false);

            GameObject safeAreaObject = new GameObject("SafeAreaRoot", typeof(RectTransform),
                typeof(SafeAreaFitter));
            safeAreaObject.transform.SetParent(canvas.transform, false);
            RectTransform safeRect = safeAreaObject.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            Transform uiRoot = safeAreaObject.transform;

            gold = CreateText(uiRoot, "GoldText", "GOLD 100", 44, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.92f), new Vector2(0.38f, 0.985f), Color.white);
            round = CreateText(uiRoot, "RoundText", "ROUND 1", 38, TextAnchor.MiddleCenter,
                new Vector2(0.34f, 0.92f), new Vector2(0.70f, 0.985f), Color.white);
            alive = CreateText(uiRoot, "MonsterText", "MONSTERS 0", 38, TextAnchor.MiddleRight,
                new Vector2(0.66f, 0.92f), new Vector2(0.96f, 0.985f), new Color(1f, 0.45f, 0.42f));
            message = CreateText(uiRoot, "MessageText", "카드를 소환해 방어를 시작하세요", 32, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.165f), new Vector2(0.95f, 0.215f), Color.white);
            selection = CreateText(uiRoot, "SelectionText", "카드를 터치해 선택", 27, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.095f), new Vector2(0.96f, 0.165f), new Color(1f, 0.86f, 0.35f));
            threat = CreateText(uiRoot, "ThreatText", "전투력 계산 중", 30, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.855f), new Vector2(0.80f, 0.915f), new Color(1f, 0.85f, 0.25f));
            summonButton = CreateButton(uiRoot, "SummonButton", "소환", new Vector2(0.01f, 0.015f), new Vector2(0.245f, 0.09f));
            mergeButton = CreateButton(uiRoot, "MergeButton", "5장 합성", new Vector2(0.255f, 0.015f), new Vector2(0.49f, 0.09f));
            upgradeButton = CreateButton(uiRoot, "UpgradeButton", "강화", new Vector2(0.51f, 0.015f), new Vector2(0.745f, 0.09f));
            sellButton = CreateButton(uiRoot, "SellButton", "판매", new Vector2(0.755f, 0.015f), new Vector2(0.99f, 0.09f));
            restartButton = CreateButton(uiRoot, "RestartButton", "새 게임", new Vector2(0.24f, 0.43f), new Vector2(0.76f, 0.53f));
            restartButton.GetComponent<Image>().color = new Color(0.12f, 0.65f, 0.42f, 0.98f);
            speedButton = CreateButton(uiRoot, "SpeedButton", "x1", new Vector2(0.82f, 0.855f), new Vector2(0.98f, 0.915f));

            growthPanel = new GameObject("GrowthChoicePanel", typeof(RectTransform), typeof(Image));
            growthPanel.transform.SetParent(uiRoot, false);
            RectTransform growthRect = growthPanel.GetComponent<RectTransform>();
            growthRect.anchorMin = new Vector2(0.08f, 0.28f);
            growthRect.anchorMax = new Vector2(0.92f, 0.72f);
            growthRect.offsetMin = Vector2.zero;
            growthRect.offsetMax = Vector2.zero;
            growthPanel.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.1f, 0.97f);
            growthTitle = CreateText(growthPanel.transform, "GrowthTitle", "성장 선택", 44,
                TextAnchor.MiddleCenter, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.96f), Color.white);
            growthTitle.fontStyle = FontStyle.Bold;
            growthAttack = CreateButton(growthPanel.transform, "GrowthAttackButton",
                "공격 훈련  ·  모든 타워 +15%", new Vector2(0.08f, 0.53f), new Vector2(0.92f, 0.71f));
            growthGold = CreateButton(growthPanel.transform, "GrowthGoldButton",
                "현상금  ·  처치 골드 +12%", new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.48f));
            growthSummon = CreateButton(growthPanel.transform, "GrowthSummonButton",
                "조달  ·  소환 비용 -10%", new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.25f));
            growthAttack.GetComponentInChildren<Text>().fontSize = 30;
            growthGold.GetComponentInChildren<Text>().fontSize = 30;
            growthSummon.GetComponentInChildren<Text>().fontSize = 30;

            return canvasObject.AddComponent<PrototypeHud>();
        }

        private static Text CreateText(Transform parent, string name, string value, int size,
            TextAnchor anchor, Vector2 min, Vector2 max, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 min, Vector2 max)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(0.9f, 0.18f, 0.22f, 0.96f);
            Button button = gameObject.GetComponent<Button>();
            Text text = CreateText(gameObject.transform, "Label", label, 42, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Color.white);
            text.fontStyle = FontStyle.Bold;
            return button;
        }
    }
}
