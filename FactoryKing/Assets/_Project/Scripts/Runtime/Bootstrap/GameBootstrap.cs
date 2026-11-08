using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.CameraRig;
using FactoryKing.Runtime.DevTools;
using FactoryKing.Runtime.EnvironmentFx;
using FactoryKing.Runtime.Procedural;
using FactoryKing.Runtime.Sim;
using FactoryKing.Runtime.Visuals;
using UnityEngine;

namespace FactoryKing.Runtime.Bootstrap
{
    /// <summary>
    /// Composition root. Creates the pure game session and wires the Unity-side systems to it. Holds no gameplay
    /// rules. If a scene has no bootstrap object (e.g. pressing Play in an empty scene) one is created automatically.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public const string LanguagePrefKey = "fk.language";

        /// <summary>Set to false (e.g. from tests) to stop automatic creation in scenes without a bootstrap.</summary>
        public static bool AutoCreate = true;

        public static GameBootstrap Instance { get; private set; }

        public GameSession Session { get; private set; }
        public LocalizationTable Localization { get; private set; }
        public MaterialLibrary Materials { get; private set; }
        public FactoryView FactoryView { get; private set; }
        public IsometricCameraController CameraController { get; private set; }
        public DayNightCycle DayNight { get; private set; }
        public SimulationDriver Driver { get; private set; }

        private ModelKit _kit;
        private DevHud _hud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (!AutoCreate || Instance != null) return;
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("FactoryKing").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            Localization = LoadLocalization();

            // Phase 6 replaces this with SaveManager.Load(); until then every launch is a new game.
            var content = DefaultContent.Create();
            Session = new GameSession(content, EconomyTuning.CreateDefault());

            Materials = new MaterialLibrary();
            _kit = new ModelKit(Materials);

            Driver = gameObject.AddComponent<SimulationDriver>();
            Driver.Init(Session);

            var sun = FindOrCreateSun();
            var cam = FindOrCreateCamera();
            // Explicit check: Unity's fake-null objects break the ?? operator in the Editor.
            CameraController = cam.GetComponent<IsometricCameraController>();
            if (CameraController == null) CameraController = cam.gameObject.AddComponent<IsometricCameraController>();

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 190f;

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            FactoryView = worldGo.AddComponent<FactoryView>();
            FactoryView.Init(Session, Materials, _kit);
            FactoryView.Rebuilt += _ => CameraController.SetBounds(FactoryView.WorldBounds, true);

            DayNight = gameObject.AddComponent<DayNightCycle>();
            DayNight.NightAmountChanged += FactoryView.SetNightAmount;
            DayNight.Init(sun, cam);

            _hud = gameObject.AddComponent<DevHud>();
            _hud.Init(this);
            CameraController.IsPointerOverUi = _hud.IsPointerOverHud;
            CameraController.Tapped += OnTapped;

            FactoryView.Build(Session.State.ActiveFactoryId);
            FactoryView.SetNightAmount(DayNight.NightAmount);
        }

        private void OnTapped(Collider hit)
        {
            var view = hit != null ? hit.GetComponentInParent<MachineView>() : null;
            _hud.Select(view);
            if (view != null) CameraController.Focus(view.transform.position, 26f);
        }

        private static LocalizationTable LoadLocalization()
        {
            var asset = Resources.Load<TextAsset>("Localization/strings");
            var table = LocalizationTable.ParseTsv(asset != null ? asset.text : string.Empty);
            if (asset == null) Debug.LogError("[FactoryKing] Localization/strings.txt missing from Resources.");

            int saved = PlayerPrefs.GetInt(LanguagePrefKey, -1);
            Language lang = saved >= 0
                ? (Language)saved
                : Application.systemLanguage == SystemLanguage.Turkish ? Language.Turkish : Language.English;
            table.SetLanguage(lang);
            table.LanguageChanged += () => PlayerPrefs.SetInt(LanguagePrefKey, (int)table.Current);
            return table;
        }

        private static Light FindOrCreateSun()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return Configure(l);
            var go = new GameObject("Sun");
            return Configure(go.AddComponent<Light>());

            Light Configure(Light light)
            {
                light.type = LightType.Directional;
                light.shadows = LightShadows.Soft;
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                RenderSettings.sun = light;
                return light;
            }
        }

        private static Camera FindOrCreateCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            return cam;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            _kit?.DestroyMeshes();
        }
    }
}
