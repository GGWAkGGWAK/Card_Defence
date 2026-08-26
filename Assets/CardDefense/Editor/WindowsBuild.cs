using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CardDefense.Editor
{
    public static class WindowsBuild
    {
        [MenuItem("Card Defense/Build Windows Development")]
        public static void BuildDevelopment()
        {
            Build("CardDefense-Windows-Development.exe", BuildOptions.Development,
                "CARD_DEFENSE_WINDOWS_BUILD_SUCCESS");
        }

        [MenuItem("Card Defense/Build Windows Release")]
        public static void BuildRelease()
        {
            Build("CardDefense-Windows.exe", BuildOptions.None,
                "CARD_DEFENSE_WINDOWS_RELEASE_SUCCESS");
        }

        private static void Build(string fileName, BuildOptions options, string marker)
        {
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../Builds/Windows"));
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, fileName);

            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/CardDefensePrototype.unity" },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);

            Debug.Log(marker + ": " + outputPath + " / " + report.summary.totalSize + " bytes");
        }
    }
}
