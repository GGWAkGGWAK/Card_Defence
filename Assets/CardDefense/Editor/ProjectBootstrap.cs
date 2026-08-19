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
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.carddefense.game");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.androidIsGame = true;
            QualitySettings.vSyncCount = 0;

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        private static GameBalanceConfig CreateOrLoadConfig()
        {
            string path = Root + "/GameBalanceConfig.asset";
            GameBalanceConfig config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(path);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(config, path);
            return config;
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
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.3f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.04f, 0.14f, 0.15f, 1f);

            GameObject root = new GameObject("GameRoot");
            EconomyService economy = root.AddComponent<EconomyService>();
            MonsterSystem monsterSystem = root.AddComponent<MonsterSystem>();
            MonsterPool monsterPool = root.AddComponent<MonsterPool>();
            WaveDirector waves = root.AddComponent<WaveDirector>();
            CardTowerSystem towerSystem = root.AddComponent<CardTowerSystem>();
            CardSummonController summon = root.AddComponent<CardSummonController>();
            GameComposition composition = root.AddComponent<GameComposition>();

            LoopPath loopPath = CreateLoopPath();
            Transform[] slots = CreatePlacementSlots();
            PrototypeHud hud = CreateHud(out Text gold, out Text round, out Text alive,
                out Text message, out Button summonButton);

            composition.SetReferences(config, loopPath, monsterPrefab, towerPrefab, slots,
                economy, monsterSystem, monsterPool, waves, towerSystem, summon, hud,
                gold, round, alive, message, summonButton);

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
                    slots.Add(slot.transform);
                }
            }
            return slots.ToArray();
        }

        private static PrototypeHud CreateHud(out Text gold, out Text round, out Text alive,
            out Text message, out Button summonButton)
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

            gold = CreateText(canvas.transform, "GoldText", "GOLD 100", 44, TextAnchor.MiddleLeft,
                new Vector2(0.04f, 0.92f), new Vector2(0.38f, 0.985f), Color.white);
            round = CreateText(canvas.transform, "RoundText", "ROUND 1", 38, TextAnchor.MiddleCenter,
                new Vector2(0.34f, 0.92f), new Vector2(0.70f, 0.985f), Color.white);
            alive = CreateText(canvas.transform, "MonsterText", "MONSTERS 0", 38, TextAnchor.MiddleRight,
                new Vector2(0.66f, 0.92f), new Vector2(0.96f, 0.985f), new Color(1f, 0.45f, 0.42f));
            message = CreateText(canvas.transform, "MessageText", "카드를 소환해 방어를 시작하세요", 34, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.17f), Color.white);
            summonButton = CreateButton(canvas.transform, "SummonButton", "카드 소환", new Vector2(0.18f, 0.015f), new Vector2(0.82f, 0.09f));

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
