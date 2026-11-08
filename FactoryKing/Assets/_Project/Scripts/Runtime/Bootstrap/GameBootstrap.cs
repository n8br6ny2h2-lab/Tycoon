using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Audio;
using FactoryKing.Runtime.CameraRig;
using FactoryKing.Runtime.EnvironmentFx;
using FactoryKing.Runtime.Platform;
using FactoryKing.Runtime.Procedural;
using FactoryKing.Runtime.Sim;
using FactoryKing.Runtime.UI;
using FactoryKing.Runtime.Visuals;
using UnityEngine;

namespace FactoryKing.Runtime.Bootstrap
{
    /// <summary>
    /// Composition root: loads the save, creates the pure game session and wires every Unity-side system to it.
    /// Holds no gameplay rules. If a scene has no bootstrap object one is created automatically.
    /// Startup flow: splash → main menu → offline summary (if any) → analytics consent (once) → play.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>Set to false (e.g. from tests) to stop automatic creation in scenes without a bootstrap.</summary>
        public static bool AutoCreate = true;

        /// <summary>
        /// App Store product id prefix. Defaults to "&lt;bundle id&gt;." so ids are e.g. "com.you.factoryking.vip";
        /// create the products in App Store Connect with exactly these ids (Docs/APPSTORE/IAP.md).
        /// </summary>
        public static string StoreIdPrefix;

        public static GameBootstrap Instance { get; private set; }

        public GameSession Session { get; private set; }
        public LocalizationTable Localization { get; private set; }
        public MaterialLibrary Materials { get; private set; }
        public FactoryView FactoryView { get; private set; }
        public IsometricCameraController CameraController { get; private set; }
        public DayNightCycle DayNight { get; private set; }
        public SimulationDriver Driver { get; private set; }
        public SaveManager Save { get; private set; }
        public AudioManager Audio { get; private set; }
        public UiManager Ui { get; private set; }
        public StoreController Store { get; private set; }
        public GameCenterService GameCenter { get; private set; }
        public NotificationService Notifications { get; private set; }
        public OnlineService Online { get; private set; }
        private DroneView _drone;
        private float _gcTimer;

        private ModelKit _kit;
        private float _activityTimer;

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
            Application.logMessageReceived += OnLog;

            var content = DefaultContent.Create();
            var tuning = EconomyTuning.CreateDefault();

            Save = gameObject.AddComponent<SaveManager>();
            var state = Save.Load(content, tuning);
            Session = new GameSession(content, tuning, state, null, StoreCatalog.CreateDefault(StoreIdPrefix ?? Application.identifier + "."));
            Save.Attach(Session);

            Localization = LoadLocalization(Session.State.Settings);
            QualityProfile.Apply(Session.State.Settings.HighQuality);

            Audio = gameObject.AddComponent<AudioManager>();
            Audio.HapticsEnabled = () => Session.State.Settings.Haptics;
            var perf = gameObject.AddComponent<PhonePerformance>();
            perf.Init(Session);
            if (Save.LastLoadOutcome == Core.Persistence.LoadOutcome.NewGame) Session.State.Settings.HighQuality = QualityProfile.RecommendedHigh;
            Audio.SetVolumes(Session.State.Settings.SfxVolume, Session.State.Settings.MusicVolume);

            Session.Ads.Provider = PlatformRegistry.AdsFactory?.Invoke()
                                   ?? (Application.isEditor || Debug.isDebugBuild ? new TestAdsProvider(this) : (IAdsProvider)new NoAdsProvider());
            if (Debug.isDebugBuild) Session.Analytics.AddSink(new DebugAnalyticsSink());
            Store = new StoreController(Session, Save);
            GameCenter = new GameCenterService(Session);
            Notifications = new NotificationService(Session, Localization);
            Online = gameObject.AddComponent<OnlineService>();
            Online.Init(Session);

            Materials = new MaterialLibrary();
            _kit = new ModelKit(Materials);
            Driver = gameObject.AddComponent<SimulationDriver>();
            Driver.Init(Session);
            Driver.Paused = true; // until the player leaves the main menu (offline time is credited separately)

            var sun = FindOrCreateSun();
            var cam = FindOrCreateCamera();
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
            ApplySkyCosmetic();
            Session.Cosmetics.Changed += ApplySkyCosmetic;

            Ui = gameObject.AddComponent<UiManager>();
            var ctx = new UiContext
            {
                Session = Session, Loc = Localization, Store = Store, Save = Save, GameCenter = GameCenter,
                Notifications = Notifications, Audio = Audio, FactoryView = FactoryView, Online = Online,
            };
            Ui.Init(ctx, new UiScreen[]
            {
                new MapScreen(), new ManagersScreen(), new MissionsScreen(), new ShopScreen(), new MoreScreen(),
                new OrdersScreen(), new ResearchScreen(), new WarehouseScreen(), new DailyScreen(), new AchievementsScreen(),
                new EventsScreen(), new PrestigeScreen(), new SettingsScreen(), new SeasonScreen(), new CosmeticsScreen(), new HelpScreen(),
                new ExpeditionsScreen(), new CardsScreen(), new EngineeringScreen(), new StatsScreen(), new CoopScreen(), new ClubScreen(),
            });
            CameraController.IsPointerOverUi = Ui.IsPointerOverUi;
            CameraController.Tapped += OnTapped;
            Store.Message += (p, key) => Ui.Toast(Localization.Get(key), key == "shop.msg.thanks" ? UiKit.Good : (Color?)null);

            WireFeedback();
            FactoryView.Build(Session.State.ActiveFactoryId);
            FactoryView.SetNightAmount(DayNight.NightAmount);
            Session.Analytics.Track(AnalyticsEvents.AppOpen);

            ctx.ShowPause = () => Ui.ShowPauseMenu(ShowTitle, () => CameraController.ResetView());
            ctx.ResetView = () => CameraController.ResetView();
            ctx.CatchDrone = CatchDrone;
            Ui.ApplyAccessibility();
            _drone = DroneView.Create(transform, _kit, Session);
            FactoryView.Rebuilt += _ => _drone.SetArea(FactoryView.WorldBounds);
            _drone.SetArea(FactoryView.WorldBounds);
            Ui.ShowSplash(() =>
            {
                if (Session.State.Settings.OnboardingDone) ShowTitle();
                else
                {
                    SetAttract(true);
                    Ui.ShowOnboarding(() => { SetAttract(false); Save.SaveNow(); StartPlaying(); });
                }
            });
            if (Save.LastLoadOutcome == Core.Persistence.LoadOutcome.RecoveredFromBackup || Save.LastLoadOutcome == Core.Persistence.LoadOutcome.CorruptReset)
                Ui.Toast(Localization.Get("save.recovered"), UiKit.Warn);
        }

        private void ApplySkyCosmetic()
        {
            var sky = Session.Cosmetics.EquippedDef(CosmeticSlot.Sky);
            DayNight.SkyTint = sky != null && !string.IsNullOrEmpty(sky.ColorHex) ? Theme.Hex(sky.ColorHex) : (Color?)null;
        }

        private void ShowTitle() => Ui.ShowTitle(StartPlaying, SetAttract);

        private void SetAttract(bool on)
        {
            CameraController.AttractMode = on && !Session.State.Settings.ReduceMotion;
            Ui.Hud.SetNavVisible(!on);
        }

        private void StartPlaying()
        {
            if (!Driver.Paused) return; // already playing (returning from the title screen)
            Driver.Paused = false;
            Session.Resume();
            if (Session.Offline.Pending != null) Ui.ShowOfflineSummary();
            Ui.ShowConsent();
            GameCenter.SignIn();
        }

        private void WireFeedback()
        {
            Session.Simulator.GoodsSold += (factory, product, units, revenue) =>
            {
                if (factory == Session.State.ActiveFactoryId) Audio.Play(Sfx.Cash, 0.6f, 0.5f);
            };
            Session.Achievements.Unlocked += a =>
            {
                Audio.Play(Sfx.Success);
                Ui.Toast(Localization.Format("achievement.unlocked", Localization.Get(a.NameKey)), UiKit.Good);
            };
            Session.Factories.FactoryUnlocked += _ =>
            {
                Save.SaveNow();
                // Ask for a rating once, at a happy moment (third factory). iOS decides whether to show it.
                var st = Session.State.Settings;
                if (!st.ReviewRequested && Session.State.Factories.FindAll(f => f.Unlocked).Count >= 3)
                {
                    st.ReviewRequested = true;
                    NativeBridge.RequestReview();
                }
            };
            Session.Prestige.Prestiged += () => GameCenter.ReportScores();
            Session.Purchases.Granted += _ => Audio.Play(Sfx.Success);
            Session.Seasons.SeasonEnded += (season, reached) =>
            {
                Audio.Play(Sfx.Success);
                Save.SaveNow();
                Ui.ShowSeasonSummary(season, reached);
            };
        }

        private void Update()
        {
            // Ambience follows how many machines are really working in the visible factory.
            _gcTimer -= Time.unscaledDeltaTime;
            if (_gcTimer <= 0f) { _gcTimer = 300f; GameCenter?.ReportScores(); }
            _activityTimer -= Time.unscaledDeltaTime;
            if (_activityTimer > 0 || Session == null) return;
            _activityTimer = 0.5f;
            int total = 0, working = 0;
            foreach (var line in Session.State.GetFactory(Session.State.ActiveFactoryId).Lines)
            {
                if (!line.Unlocked) continue;
                foreach (var m in line.Machines) { total++; if (m.Status == MachineStatus.Working) working++; }
            }
            Audio.SetFactoryActivity(total > 0 ? working / (float)total : 0f);
        }

        /// <summary>Tap on the drone (or its HUD chip): show the reward, optionally doubled by a rewarded video.</summary>
        private void CatchDrone()
        {
            if (!Session.Drone.IsVisible) return;
            var preview = Session.Drone.Peek();
            Audio.Play(Sfx.Click);
            Ui.ShowModal((box, close) =>
            {
                string text = preview.Kind == DroneRewardKind.Cash ? "$ " + Localization.Money(preview.Cash)
                    : preview.Kind == DroneRewardKind.Diamonds ? "◆ " + preview.Diamonds
                    : Localization.Format("daily.boost", preview.BoostMinutes);
                box.Add(UiKit.Label(Localization.Get("drone.title"), 42, true, UiKit.Accent));
                box.Add(UiKit.Label(text, 56, true, UiKit.Good));
                box.Add(UiKit.Button(Localization.Get("ui.claim"), () =>
                {
                    close();
                    if (Session.Drone.TryCatch(out _)) Audio.Play(Sfx.Cash);
                }, UiKit.Good, 36, 110));
                var twice = UiKit.Button(Localization.Get("drone.double"), () =>
                {
                    Session.Ads.Request(AdPlacements.Drone, () => { close(); if (Session.Drone.TryCatch(out _, 2)) Audio.Play(Sfx.Cash); },
                        r => { if (r != AdResult.Completed) Ui.Toast(Localization.Get("ads.not_ready")); });
                }, UiKit.Diamond, 30, 96);
                UiKit.SetEnabled(twice, Session.Ads.IsAvailable(AdPlacements.Drone));
                box.Add(twice);
            });
        }

        private void OnTapped(Collider hit)
        {
            if (hit != null && hit.GetComponentInParent<DroneView>() != null) { CatchDrone(); return; }
            var view = hit != null ? hit.GetComponentInParent<MachineView>() : null;
            Ui.Hud.Select(view);
            if (view != null)
            {
                Audio.Play(Sfx.Click);
                CameraController.Focus(view.transform.position, 26f);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (Session == null) return;
            if (paused)
            {
                Session.Analytics.Track(AnalyticsEvents.SessionEnd, "minutes", ((int)(Session.State.PlayTimeSeconds / 60)).ToString());
                Notifications.OnBackground();
                Save.SaveNow();
            }
            else
            {
                Notifications.OnForeground();
                if (!Driver.Paused)
                {
                    Session.Resume();
                    if (Session.Offline.Pending != null) Ui.ShowOfflineSummary();
                }
            }
        }

        /// <summary>Settings → Reset progress: rebuilds the whole game from a fresh state.</summary>
        public void RestartFresh()
        {
            Save.Suppressed = true;
            Instance = null;
            CameraController.Tapped -= OnTapped;
            Destroy(gameObject);
            new GameObject("FactoryKing").AddComponent<GameBootstrap>();
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || Session == null) return;
            // Only an anonymous hash of the exception type/message is sent (and only with consent).
            Session.Analytics.Track(AnalyticsEvents.Error, "hash", ((uint)condition.GetHashCode()).ToString("x8"));
        }

        private static LocalizationTable LoadLocalization(SettingsState settings)
        {
            var asset = Resources.Load<TextAsset>("Localization/strings");
            var table = LocalizationTable.ParseTsv(asset != null ? asset.text : string.Empty);
            if (asset == null) Debug.LogError("[FactoryKing] Localization/strings.txt missing from Resources.");
            Language lang = settings.Language >= 0
                ? (Language)settings.Language
                : Application.systemLanguage == SystemLanguage.Turkish ? Language.Turkish : Language.English;
            table.SetLanguage(lang);
            return table;
        }

        private static Light FindOrCreateSun()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return Configure(l);
            return Configure(new GameObject("Sun").AddComponent<Light>());

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
            Application.logMessageReceived -= OnLog;
            if (CameraController != null) CameraController.Tapped -= OnTapped;
            if (Instance == this) Instance = null;
            _kit?.DestroyMeshes();
        }
    }
}
