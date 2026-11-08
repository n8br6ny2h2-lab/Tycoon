using System.IO;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Runtime.Bootstrap;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FactoryKing.EditorTools
{
    /// <summary>
    /// One-click project configuration so the repository does not need hand-written pipeline/scene YAML:
    /// creates the URP asset + renderer, iOS player settings, the main scene and shader keep-alive materials.
    /// Menu: Factory King > 1. Setup Project. Safe to run repeatedly.
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Project";
        private const string SettingsDir = Root + "/Settings";
        private const string ScenesDir = Root + "/Scenes";
        private const string KeepAliveDir = Root + "/Resources/ShaderKeepAlive";
        public const string ScenePath = ScenesDir + "/Factory.unity";

        /// <summary>
        /// Placeholder bundle id. Replace with the id you register in App Store Connect before any device build —
        /// never reuse another app's id. Can also be supplied through the FK_BUNDLE_ID environment variable.
        /// </summary>
        public const string PlaceholderBundleId = "com.CHANGEME.factoryking";

        [MenuItem("Factory King/1. Setup Project (URP + iOS + Scene)", priority = 1)]
        public static void SetupAll()
        {
            EnsureFolder(SettingsDir);
            EnsureFolder(ScenesDir);
            EnsureFolder(KeepAliveDir);
            SetupUrp();
            SetupPlayer();
            CreateShaderKeepAlive();
            SetupIcon();
            CreatePanelSettings();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[FactoryKing] Setup complete. Open Assets/_Project/Scenes/Factory.unity and press Play.");
        }

        [MenuItem("Factory King/Validate Content", priority = 20)]
        public static void ValidateContent()
        {
            var errors = ContentValidator.Validate(DefaultContent.Create(), EconomyTuning.CreateDefault());
            if (errors.Count == 0) Debug.Log("[FactoryKing] Content OK: 5 factories, all lines valid and profitable at levels 1-100.");
            foreach (var e in errors) Debug.LogError("[FactoryKing] " + e);
        }

        // ------------------------------------------------------------------ URP

        public static void SetupUrp()
        {
            var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                string assetPath = SettingsDir + "/FK_URP.asset";
                asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
                if (asset == null)
                {
                    var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, SettingsDir + "/FK_URP_Renderer.asset");
                    asset = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
            }

            // Mobile-oriented defaults: one main light with soft shadows, few additional lights, 4x MSAA.
            asset.supportsHDR = true;
            asset.msaaSampleCount = 4;
            asset.renderScale = 1f;
            asset.shadowDistance = 70f;
            asset.shadowCascadeCount = 2;
            asset.maxAdditionalLightsCount = 4;
            asset.useSRPBatcher = true;
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        // ------------------------------------------------------------------ Player / iOS

        public static void SetupPlayer()
        {
            PlayerSettings.productName = "Factory King";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "Factory King Studio";
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "0.1.0";

            var ios = NamedBuildTarget.iOS;
            string envId = System.Environment.GetEnvironmentVariable("FK_BUNDLE_ID");
            string currentId = PlayerSettings.GetApplicationIdentifier(ios);
            if (!string.IsNullOrEmpty(envId)) PlayerSettings.SetApplicationIdentifier(ios, envId);
            else if (string.IsNullOrEmpty(currentId) || currentId.StartsWith("com.DefaultCompany") || currentId.StartsWith("com.Unity"))
                PlayerSettings.SetApplicationIdentifier(ios, PlaceholderBundleId);
            if (PlayerSettings.GetApplicationIdentifier(ios) == PlaceholderBundleId)
                Debug.LogWarning("[FactoryKing] iOS bundle id is the placeholder '" + PlaceholderBundleId +
                                 "'. Set your own registered id (Player Settings or FK_BUNDLE_ID) before building for devices.");

            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(ios, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            string team = System.Environment.GetEnvironmentVariable("FK_APPLE_TEAM_ID");
            if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
            if (string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) || PlayerSettings.iOS.buildNumber == "0")
                PlayerSettings.iOS.buildNumber = "1";

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.runInBackground = false;
            PlayerSettings.SetManagedStrippingLevel(ios, ManagedStrippingLevel.Low);
        }

        // ------------------------------------------------------------------ Shaders

        /// <summary>
        /// Runtime-created materials use Shader.Find. Materials placed under Resources force those shaders (and the
        /// emission + instancing variants we use) into the player build.
        /// </summary>
        public static void CreateShaderKeepAlive()
        {
            CreateKeepAlive("URP_Lit", "Universal Render Pipeline/Lit", false);
            CreateKeepAlive("URP_Lit_Emission", "Universal Render Pipeline/Lit", true);
            CreateKeepAlive("URP_ParticlesUnlit", "Universal Render Pipeline/Particles/Unlit", false);
        }

        private static void CreateKeepAlive(string name, string shaderName, bool emission)
        {
            string path = $"{KeepAliveDir}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[FactoryKing] Shader '{shaderName}' not found (is URP installed?).");
                return;
            }
            var m = new Material(shader) { enableInstancing = true };
            if (emission) m.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(m, path);
        }

        // ------------------------------------------------------------------ Icon & UI

        public const string IconPath = Root + "/Art/AppIcon.png";

        /// <summary>Uses the generated 1024x1024 icon (no alpha) as the default icon; Unity derives every iOS size.</summary>
        public static void SetupIcon()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null) { Debug.LogWarning("[FactoryKing] App icon not found at " + IconPath); return; }
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { tex });
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.08f, 0.10f, 0.13f);
        }

        /// <summary>Runtime UI panel settings (1080x1920 reference, portrait) loaded by UiManager from Resources.</summary>
        public static void CreatePanelSettings()
        {
            const string dir = Root + "/Resources/UI";
            const string path = dir + "/FKPanelSettings.asset";
            EnsureFolder(dir);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(path) != null) return;
            var ps = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            ps.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1080, 1920);
            ps.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            var themes = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            if (themes.Length > 0)
                ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(themes[0]));
            AssetDatabase.CreateAsset(ps, path);
        }

        /// <summary>Saves a store screenshot of the Game view (set the Game view to 1290x2796 or 2048x2732 first).</summary>
        [MenuItem("Factory King/Capture Store Screenshot", priority = 30)]
        public static void CaptureScreenshot()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[FactoryKing] Enter Play mode first."); return; }
            Directory.CreateDirectory("Screenshots");
            string file = $"Screenshots/FactoryKing_{Screen.width}x{Screen.height}_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            ScreenCapture.CaptureScreenshot(file, 1);
            Debug.Log("[FactoryKing] Screenshot saved: " + file);
        }

        // ------------------------------------------------------------------ Scene

        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(50f, 150f, 0f);

            new GameObject("FactoryKing").AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
