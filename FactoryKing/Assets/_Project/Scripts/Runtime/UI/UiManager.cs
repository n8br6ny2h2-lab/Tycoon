using System;
using System.Collections.Generic;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Audio;
using FactoryKing.Runtime.Platform;
using FactoryKing.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    /// <summary>Everything screens need; passed instead of global singletons.</summary>
    public sealed class UiContext
    {
        public GameSession Session;
        public LocalizationTable Loc;
        public StoreController Store;
        public SaveManager Save;
        public GameCenterService GameCenter;
        public NotificationService Notifications;
        public AudioManager Audio;
        public FactoryView FactoryView;
        public UiManager Ui;
        public Action<MachineView> FocusMachine;
        public string PrivacyPolicyUrl = "https://example.com/factoryking/privacy";
        public string SupportUrl = "https://example.com/factoryking/support";

        public string T(string key) => Loc.Get(key);
        public string TF(string key, params object[] args) => Loc.Format(key, args);
        public string M(BigDouble v) => Loc.Money(v);
        public string M(double v) => Loc.Money(BigDouble.FromDouble(v));
    }

    /// <summary>A full-screen page opened from the navigation bar.</summary>
    public abstract class UiScreen
    {
        protected UiContext C;
        protected readonly Bindings B = new Bindings();
        public VisualElement Root { get; private set; }
        public abstract string Id { get; }
        public abstract string TitleKey { get; }

        public void Init(UiContext ctx) => C = ctx;

        protected string T(string key) => C.T(key);
        protected string TF(string key, params object[] args) => C.TF(key, args);
        protected string M(BigDouble v) => C.M(v);
        protected string M(double v) => C.M(v);

        public VisualElement Create()
        {
            B.Clear();
            Root = new VisualElement();
            Root.style.flexGrow = 1;
            Build(Root);
            B.Refresh();
            return Root;
        }

        /// <summary>Re-creates the content (after a structural change such as a purchase).</summary>
        public void Rebuild()
        {
            if (Root == null) return;
            Root.Clear();
            B.Clear();
            Build(Root);
            B.Refresh();
        }

        public void Refresh() => B.Refresh();

        protected abstract void Build(VisualElement root);

        protected void Section(VisualElement parent, string titleKey)
        {
            var l = UiKit.Label(T(titleKey), 38, true, UiKit.Accent);
            l.style.marginTop = 22;
            l.style.marginBottom = 8;
            parent.Add(l);
        }

        protected VisualElement Card(VisualElement parent)
        {
            var card = UiKit.Box(UiKit.Card);
            card.style.marginBottom = 14;
            parent.Add(card);
            return card;
        }

        protected void Click() => C.Audio?.Play(Sfx.Click);
    }

    /// <summary>
    /// Root of the interface: HUD, navigation, full-screen pages, modals and toasts. Built entirely from code with
    /// UI Toolkit (no UXML assets) at a 1080x1920 reference resolution, honouring the device safe area.
    /// </summary>
    public sealed class UiManager : MonoBehaviour
    {
        private UiContext _ctx;
        private UIDocument _doc;
        private VisualElement _root, _safe, _hudLayer, _pageLayer, _modalLayer, _toastLayer;
        private readonly Dictionary<string, UiScreen> _screens = new Dictionary<string, UiScreen>();
        private UiScreen _current;
        private Hud _hud;
        private float _refreshTimer;
        private readonly Queue<VisualElement> _toasts = new Queue<VisualElement>();
        private Rect _lastSafe;

        public bool IsPageOpen => _current != null;
        public bool IsModalOpen => _modalLayer != null && _modalLayer.childCount > 0;

        public void Init(UiContext ctx, IEnumerable<UiScreen> screens)
        {
            _ctx = ctx;
            ctx.Ui = this;
            foreach (var s in screens)
            {
                s.Init(ctx);
                _screens[s.Id] = s;
            }

            var settings = Resources.Load<PanelSettings>("UI/FKPanelSettings");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1080, 1920);
                settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                settings.match = 0.5f;
            }
            _doc = gameObject.AddComponent<UIDocument>();
            _doc.panelSettings = settings;

            _root = _doc.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            var font = LoadFont();
            if (font != null) _root.style.unityFontDefinition = FontDefinition.FromFont(font);
            _root.style.color = UiKit.Text;

            _safe = Layer(_root);
            _hudLayer = Layer(_safe);
            _pageLayer = Layer(_safe);
            _modalLayer = Layer(_root);
            _toastLayer = Layer(_safe);
            _toastLayer.style.alignItems = Align.Center;
            _toastLayer.style.justifyContent = Justify.FlexStart;
            _toastLayer.style.paddingTop = 260;

            _hud = new Hud(ctx, _hudLayer);
            _hud.Build();
            ctx.Loc.LanguageChanged += RebuildAll;
        }

        private static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
            return f;
        }

        private static VisualElement Layer(VisualElement parent)
        {
            var e = new VisualElement();
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            e.pickingMode = PickingMode.Ignore;
            parent.Add(e);
            return e;
        }

        public Hud Hud => _hud;
        public UiContext Context => _ctx;

        public void Open(string id)
        {
            if (!_screens.TryGetValue(id, out var screen)) return;
            Close();
            _ctx.Audio?.Play(Sfx.Click);
            _current = screen;

            var page = new VisualElement();
            page.style.position = Position.Absolute;
            page.style.left = 0; page.style.right = 0; page.style.top = 0; page.style.bottom = 0;
            page.style.backgroundColor = UiKit.Bg;
            UiKit.Pad(page, 24, 28);

            var header = UiKit.Row(0, Justify.SpaceBetween);
            header.Add(UiKit.Label(_ctx.T(screen.TitleKey), 50, true));
            var close = UiKit.Button("X", Close, UiKit.CardHi, 40, 96);
            close.style.width = 110;
            header.Add(close);
            page.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.contentContainer.Add(screen.Create());
            page.Add(scroll);
            _pageLayer.Add(page);
            _hud.SetNavVisible(false);
        }

        public void Close()
        {
            _pageLayer.Clear();
            _current = null;
            _hud?.SetNavVisible(true);
        }

        public void RefreshCurrent() => _current?.Rebuild();

        /// <summary>Shows a modal dialog; returns the container to fill. Closing removes it.</summary>
        public VisualElement ShowModal(Action<VisualElement, Action> build, bool dismissible = true)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0; overlay.style.right = 0; overlay.style.top = 0; overlay.style.bottom = 0;
            overlay.style.backgroundColor = UiKit.Overlay;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            var box = UiKit.Box(UiKit.Card, 30, 34, 36);
            box.style.width = Length.Percent(88);
            box.style.maxWidth = 980;
            overlay.Add(box);
            void CloseModal() { overlay.RemoveFromHierarchy(); }
            if (dismissible) overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == overlay) CloseModal(); });
            build(box, CloseModal);
            _modalLayer.Add(overlay);
            return box;
        }

        public void Toast(string text, Color? color = null)
        {
            var t = UiKit.Box(color ?? UiKit.CardHi, 24, 16, 28);
            t.Add(UiKit.Label(text, 32, true, null, true));
            t.style.maxWidth = 900;
            t.pickingMode = PickingMode.Ignore;
            _toastLayer.Add(t);
            _toasts.Enqueue(t);
            t.schedule.Execute(() =>
            {
                t.RemoveFromHierarchy();
                if (_toasts.Count > 0) _toasts.Dequeue();
            }).StartingIn(2600);
            while (_toasts.Count > 3) _toasts.Dequeue().RemoveFromHierarchy();
        }

        /// <summary>For the camera: ignore touches that land on interactive UI.</summary>
        public bool IsPointerOverUi(Vector2 screenPos)
        {
            if (IsModalOpen || IsPageOpen) return true;
            var panel = _root?.panel;
            if (panel == null) return false;
            var p = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPos.x, Screen.height - screenPos.y));
            var picked = panel.Pick(p);
            return picked != null && picked.pickingMode == PickingMode.Position;
        }

        private void RebuildAll()
        {
            _hud.Rebuild();
            if (_current != null)
            {
                string id = _current.Id;
                Open(id);
            }
        }

        private void ApplySafeArea()
        {
            var sa = Screen.safeArea;
            if (sa == _lastSafe || _root.panel == null) return;
            _lastSafe = sa;
            var topLeft = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(sa.xMin, Screen.height - sa.yMax));
            var bottomRight = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(sa.xMax, Screen.height - sa.yMin));
            var full = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(Screen.width, Screen.height));
            _safe.style.left = topLeft.x;
            _safe.style.top = topLeft.y;
            _safe.style.right = Mathf.Max(0, full.x - bottomRight.x);
            _safe.style.bottom = Mathf.Max(0, full.y - bottomRight.y);
        }

        private void Update()
        {
            if (_ctx == null) return;
            ApplySafeArea();
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0) return;
            _refreshTimer = 0.2f;
            _hud.Refresh();
            _current?.Refresh();
        }
    }
}
