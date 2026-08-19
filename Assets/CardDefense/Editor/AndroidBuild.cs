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
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Android"));
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "CardDefense-Development.apk");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/CardDefensePrototype.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Android build failed: " + summary.result);
            }

            Debug.Log("CARD_DEFENSE_ANDROID_BUILD_SUCCESS: " + outputPath + " / " + summary.totalSize + " bytes");
        }
    }
}
