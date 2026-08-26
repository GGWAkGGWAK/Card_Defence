using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CardDefense.Editor
{
    public static class AndroidBuild
    {
        public static void BuildDevelopmentApk()
        {
            BuildApk("CardDefense-Development.apk", BuildOptions.Development,
                "CARD_DEFENSE_ANDROID_BUILD_SUCCESS");
        }

        public static void BuildReleaseApk()
        {
            BuildApk("CardDefense-Release.apk", BuildOptions.None,
                "CARD_DEFENSE_ANDROID_RELEASE_SUCCESS");
        }

        private static void BuildApk(string fileName, BuildOptions buildOptions, string successMarker)
        {
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Android"));
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, fileName);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/CardDefensePrototype.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = buildOptions
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Android build failed: " + summary.result);
            }

            Debug.Log(successMarker + ": " + outputPath + " / " + summary.totalSize + " bytes");
        }
    }
}
