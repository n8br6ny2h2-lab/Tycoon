using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FactoryKing.EditorTools
{
    /// <summary>
    /// Generates the iOS Xcode project. Run from the menu, or headless on a Mac:
    ///   Unity -batchmode -quit -projectPath FactoryKing -executeMethod FactoryKing.EditorTools.BuildScript.BuildIOS
    /// Archiving, signing and TestFlight upload then happen in Xcode (requires an Apple Developer account).
    /// </summary>
    public static class BuildScript
    {
        public const string OutputPath = "Builds/iOS";

        [MenuItem("Factory King/Build iOS (Xcode project)", priority = 40)]
        public static void BuildIOS()
        {
            if (EditorBuildSettings.scenes.Length == 0) ProjectSetup.SetupAll();

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-release") < 0
                    ? BuildOptions.Development
                    : BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[FactoryKing] iOS build {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings, " +
                      $"{summary.totalSize / (1024 * 1024)} MB, output '{OutputPath}'.");
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
