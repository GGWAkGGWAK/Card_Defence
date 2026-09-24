using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CardDefense.Editor
{
    public static class AndroidReleaseBuild
    {
        public const string ProductVersion = "0.3.0";
        public const int VersionCode = 3;
        public const string PackageName = "com.carddefense.game";

        private const string IconPath =
            "Assets/CardDefense/Art/Brand/Store/CardDefense-StoreIcon-512.png";
        private const string SplashPath =
            "Assets/CardDefense/Art/Brand/Store/CardDefense-Splash-1080x1920.png";
        private const string FeatureGraphicPath =
            "Assets/CardDefense/Art/Brand/Store/CardDefense-FeatureGraphic-1024x500.png";

        [MenuItem("Card Defense/Release/Prepare Android Store Settings")]
        public static void PrepareAndroidStoreSettings()
        {
            ValidateBrandAssets();
            ConfigureTexture(SplashPath, true);
            ConfigureTexture(IconPath, true);

            PlayerSettings.companyName = "Card Defense Studio";
            PlayerSettings.productName = "Card Defense";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
            PlayerSettings.bundleVersion = ProductVersion;
            PlayerSettings.Android.bundleVersionCode = VersionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.stripEngineCode = true;

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
#pragma warning disable 618
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon });
#pragma warning restore 618

            Sprite splash = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPath);
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.008f, 0.055f, 0.067f, 1f);
            PlayerSettings.SplashScreen.background = splash;
            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.Create(2.4f,
                    AssetDatabase.LoadAssetAtPath<Sprite>(IconPath))
            };
            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();
            Debug.Log("CARD_DEFENSE_ANDROID_RELEASE_SETTINGS_READY: " + PackageName + " v" +
                      ProductVersion + " (" + VersionCode + ")");
        }

        [MenuItem("Card Defense/Release/Validate Android Store Package")]
        public static void ValidateReleasePackage()
        {
            ValidateBrandAssets();
            if (PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) != PackageName)
                throw new InvalidOperationException("Android package name is not prepared.");
            if (PlayerSettings.bundleVersion != ProductVersion ||
                PlayerSettings.Android.bundleVersionCode != VersionCode)
                throw new InvalidOperationException("Android version is not prepared.");
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) !=
                ScriptingImplementation.IL2CPP)
                throw new InvalidOperationException("Android release must use IL2CPP.");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                throw new InvalidOperationException("Android release must include ARM64.");
            Debug.Log("CARD_DEFENSE_ANDROID_RELEASE_VALIDATION_SUCCESS");
        }

        [MenuItem("Card Defense/Release/Build QA App Bundle")]
        public static void BuildQaAppBundle()
        {
            PrepareAndroidStoreSettings();
            PlayerSettings.Android.useCustomKeystore = false;
            BuildBundle("CardDefense-" + ProductVersion + "-qa.aab",
                "CARD_DEFENSE_ANDROID_QA_AAB_SUCCESS");
        }

        [MenuItem("Card Defense/Release/Build Signed Store App Bundle")]
        public static void BuildSignedStoreAppBundle()
        {
            PrepareAndroidStoreSettings();
            string keystorePath = RequireEnvironment("CARD_DEFENSE_KEYSTORE_PATH");
            string keystorePassword = RequireEnvironment("CARD_DEFENSE_KEYSTORE_PASSWORD");
            string alias = RequireEnvironment("CARD_DEFENSE_KEY_ALIAS");
            string aliasPassword = RequireEnvironment("CARD_DEFENSE_KEY_PASSWORD");
            if (!File.Exists(keystorePath))
                throw new FileNotFoundException("Android keystore not found.", keystorePath);

            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystorePath);
                PlayerSettings.Android.keystorePass = keystorePassword;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = aliasPassword;
                BuildBundle("CardDefense-" + ProductVersion + "-store.aab",
                    "CARD_DEFENSE_ANDROID_SIGNED_AAB_SUCCESS");
            }
            finally
            {
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
                PlayerSettings.Android.keyaliasName = string.Empty;
                PlayerSettings.Android.keystoreName = string.Empty;
                PlayerSettings.Android.useCustomKeystore = false;
            }
        }

        private static void BuildBundle(string fileName, string marker)
        {
            ValidateReleasePackage();
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../Builds/Android"));
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, fileName);
            EditorUserBuildSettings.buildAppBundle = true;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/CardDefensePrototype.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.CompressWithLz4HC
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android App Bundle build failed: " +
                                                    report.summary.result);
            Debug.Log(marker + ": " + outputPath + " / " + report.summary.totalSize + " bytes");
        }

        private static void ValidateBrandAssets()
        {
            ValidateTexture(IconPath, 512, 512);
            ValidateTexture(SplashPath, 1080, 1920);
            ValidateTexture(FeatureGraphicPath, 1024, 500);
        }

        private static void ValidateTexture(string path, int width, int height)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("Brand image is missing.", path);
            if (texture.width != width || texture.height != height)
                throw new InvalidOperationException(path + " must be " + width + "x" + height + ".");
        }

        private static void ConfigureTexture(string path, bool sprite)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static string RequireEnvironment(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(name + " environment variable is required. " +
                                                    "Do not store signing secrets in Git.");
            return value;
        }
    }
}
