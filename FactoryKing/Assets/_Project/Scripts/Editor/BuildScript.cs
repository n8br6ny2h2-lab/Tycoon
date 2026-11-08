using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FactoryKing.EditorTools
{
    /// <summary>
    /// Generates the iOS Xcode project. Run from the menu, headless, or in the cloud (.github/workflows/ios-testflight.yml):
    ///   Unity -batchmode -quit -projectPath FactoryKing -executeMethod FactoryKing.EditorTools.BuildScript.BuildIOS
    /// Environment: FK_BUNDLE_ID, FK_APPLE_TEAM_ID, FK_BUILD_NUMBER, FK_DEV_BUILD.
    /// </summary>
    public static class BuildScript
    {
        public const string OutputPath = "Builds/iOS";

        [MenuItem("Factory King/Build iOS (Xcode project)", priority = 40)]
        public static void BuildIOS()
        {
            // Cloud builds start from a clean checkout: create URP asset, scene, icon and iOS settings first.
            if (EditorBuildSettings.scenes.Length == 0 || Application.isBatchMode) ProjectSetup.SetupAll();
            else ProjectSetup.SetupPlayer();

            // CI: build number from the pipeline run (must increase for every TestFlight upload).
            string buildNumber = Environment.GetEnvironmentVariable("FK_BUILD_NUMBER");
            if (!string.IsNullOrEmpty(buildNumber)) PlayerSettings.iOS.buildNumber = buildNumber;

            // FK_DEV_BUILD=true keeps the test store, test ads and developer tools for the first TestFlight tests.
            string dev = Environment.GetEnvironmentVariable("FK_DEV_BUILD");
            bool development = string.IsNullOrEmpty(dev)
                ? Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-release") < 0
                : dev.Equals("true", StringComparison.OrdinalIgnoreCase);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            Debug.Log($"[FactoryKing] Building iOS {PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS)} " +
                      $"{PlayerSettings.bundleVersion} ({PlayerSettings.iOS.buildNumber}) development={development}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[FactoryKing] iOS build {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings, " +
                      $"{summary.totalSize / (1024 * 1024)} MB, output '{OutputPath}'.");
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
