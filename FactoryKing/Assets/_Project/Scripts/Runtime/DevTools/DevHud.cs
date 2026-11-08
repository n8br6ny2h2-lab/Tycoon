using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Localization;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Bootstrap;
using FactoryKing.Runtime.Visuals;
using UnityEngine;

namespace FactoryKing.Runtime.DevTools
{
    /// <summary>
    /// TEMPORARY development HUD (IMGUI) so the economy loop is playable before the production UI (Phase 5).
    /// Shows cash/income, factory tabs, line unlocks, machine details and upgrades; scales to any portrait screen
    /// and respects the safe area. Will be replaced by the UI Toolkit screens.
    /// </summary>
    public sealed class DevHud : MonoBehaviour
    {
        private const float RefWidth = 720f;
        private const float RefHeight = 1280f;

        private GameBootstrap _game;
        private MachineView _selected;
        private readonly List<Rect> _hitRects = new List<Rect>();
        private float _scale = 1f;
        private Rect _safe;
        private GUIStyle _label, _big, _small, _button, _panel, _title;
        private Texture2D _panelTex, _buttonTex, _buttonHoverTex, _buttonDisabledTex;
        private float _statsTimer;
        private string _cashText, _incomeText, _diamondText, _levelText, _hourText;

        public void Init(GameBootstrap game) => _game = game;

        public void Select(MachineView view) => _selected = view;

        /// <summary>Screen position (pixels, origin bottom-left) over any HUD element?</summary>
        public bool IsPointerOverHud(Vector2 screen)
        {
            var gui = new Vector2(screen.x / _scale, (Screen.height - screen.y) / _scale);
            foreach (var r in _hitRects) if (r.Contains(gui)) return true;
            return false;
        }

        private void EnsureStyles()
        {
            if (_label != null) return;
            _panelTex = Solid(new Color(0.09f, 0.11f, 0.15f, 0.88f));
            _buttonTex = Solid(new Color(0.98f, 0.62f, 0.16f, 1f));
            _buttonHoverTex = Solid(new Color(1f, 0.72f, 0.3f, 1f));
            _buttonDisabledTex = Solid(new Color(0.35f, 0.38f, 0.42f, 1f));

            _label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = false, richText = true };
            _label.normal.textColor = Color.white;
            _small = new GUIStyle(_label) { fontSize = 20 };
            _small.normal.textColor = new Color(0.78f, 0.84f, 0.9f);
            _big = new GUIStyle(_label) { fontSize = 40, fontStyle = FontStyle.Bold };
            _title = new GUIStyle(_label) { fontSize = 28, fontStyle = FontStyle.Bold };
            _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(16, 16, 12, 12) };
            _panel.normal.background = _panelTex;
            _button = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            _button.normal.background = _buttonTex;
            _button.hover.background = _buttonHoverTex;
            _button.active.background = _buttonHoverTex;
            _button.normal.textColor = _button.hover.textColor = _button.active.textColor = new Color(0.1f, 0.08f, 0.05f);
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void Update()
        {
            if (_game == null || _game.Session == null) return;
            _statsTimer -= Time.unscaledDeltaTime;
            if (_statsTimer > 0) return;
            _statsTimer = 0.25f; // refresh texts 4x per second, not every frame
            var s = _game.Session;
            var loc = _game.Localization;
            _cashText = loc.Money(s.Wallet.Cash);
            _incomeText = loc.Format("ui.income_per_sec", loc.Money(BigDouble.FromDouble(s.Ledger.NetPerSecond)));
            _hourText = loc.Format("ui.net_per_hour", loc.Money(BigDouble.FromDouble(s.Ledger.NetPerHour)));
            _diamondText = s.Wallet.Diamonds.ToString();
            _levelText = loc.Format("ui.company_level", s.CompanyLevel);
        }

        private void OnGUI()
        {
            if (_game == null || _game.Session == null) return;
            EnsureStyles();
            _scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            if (_scale <= 0) return;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            var sa = Screen.safeArea;
            _safe = new Rect(sa.x / _scale, (Screen.height - sa.yMax) / _scale, sa.width / _scale, sa.height / _scale);
            if (Event.current.type == EventType.Layout) _hitRects.Clear();

            DrawTopBar();
            DrawFactoryTabs();
            if (_selected != null) DrawMachinePanel();
            else DrawLinePanel();
        }

        private Rect Hit(Rect r)
        {
            if (Event.current.type == EventType.Layout) _hitRects.Add(r);
            return r;
        }

        private bool Button(Rect r, string text, bool enabled = true)
        {
            var prev = _button.normal.background;
            if (!enabled) _button.normal.background = _buttonDisabledTex;
            bool old = GUI.enabled;
            GUI.enabled = enabled;
            bool clicked = GUI.Button(r, text, _button);
            GUI.enabled = old;
            _button.normal.background = prev;
            return clicked && enabled;
        }

        private void DrawTopBar()
        {
            var s = _game.Session;
            var loc = _game.Localization;
            var r = Hit(new Rect(_safe.x + 12, _safe.y + 8, _safe.width - 24, 132));
            GUI.Box(r, GUIContent.none, _panel);
            GUI.Label(new Rect(r.x + 18, r.y + 8, 420, 52), "$ " + _cashText, _big);
            GUI.Label(new Rect(r.x + 20, r.y + 62, 420, 30), _incomeText, _label);
            GUI.Label(new Rect(r.x + 20, r.y + 94, 440, 30), _hourText, _small);
            GUI.Label(new Rect(r.xMax - 250, r.y + 14, 230, 34), "◆ " + _diamondText, _title);
            GUI.Label(new Rect(r.xMax - 250, r.y + 52, 230, 30), _levelText, _label);
            if (s.Wallet.Debt.IsPositive)
                GUI.Label(new Rect(r.xMax - 250, r.y + 96, 240, 30), loc.Format("ui.debt", loc.Money(s.Wallet.Debt)), _small);

            var langRect = Hit(new Rect(r.xMax - 250, r.yMax + 8, 238, 52));
            if (Button(langRect, loc.Get("ui.language")))
                loc.SetLanguage(loc.Current == Language.English ? Language.Turkish : Language.English);

            if (Debug.isDebugBuild)
            {
                var speedRect = Hit(new Rect(r.x, r.yMax + 8, 140, 52));
                float speed = _game.Driver.DebugSpeed;
                if (Button(speedRect, speed > 1f ? "DEV x10" : "DEV x1"))
                    _game.Driver.DebugSpeed = speed > 1f ? 1f : 10f;
            }
        }

        private void DrawFactoryTabs()
        {
            var s = _game.Session;
            var loc = _game.Localization;
            var factories = s.Content.Factories;
            float w = (_safe.width - 24 - (factories.Count - 1) * 8) / factories.Count;
            float y = _safe.y + 210;
            bool shownNextPurchase = false;
            for (int i = 0; i < factories.Count; i++)
            {
                var f = factories[i];
                var r = Hit(new Rect(_safe.x + 12 + i * (w + 8), y, w, 76));
                bool owned = s.Factories.IsUnlocked(f.Id);
                string name = loc.Get(f.NameKey);
                if (owned)
                {
                    bool active = s.State.ActiveFactoryId == f.Id;
                    if (Button(r, (active ? "▶ " : "") + name) && !active)
                    {
                        _selected = null;
                        s.Factories.SetActiveFactory(f.Id);
                    }
                }
                else if (!shownNextPurchase)
                {
                    shownNextPurchase = true;
                    bool can = s.Factories.CanUnlockFactory(f.Id, out var cost);
                    if (Button(r, name + "\n" + loc.Money(cost), can) && s.Factories.TryUnlockFactory(f.Id))
                    {
                        _selected = null;
                        s.Factories.SetActiveFactory(f.Id);
                    }
                }
                else
                {
                    Button(r, loc.Get("ui.locked"), false);
                }
            }
        }

        private void DrawLinePanel()
        {
            var s = _game.Session;
            var loc = _game.Localization;
            string fid = s.State.ActiveFactoryId;
            var def = s.Content.GetFactory(fid);
            var fs = s.State.GetFactory(fid);

            float h = 120 + def.LineCount * 44;
            var r = Hit(new Rect(_safe.x + 12, _safe.yMax - h - 12, _safe.width - 24, h));
            GUI.Box(r, GUIContent.none, _panel);
            GUI.Label(new Rect(r.x + 18, r.y + 10, r.width - 36, 36), loc.Get(def.NameKey), _title);

            float y = r.y + 54;
            int nextLocked = -1;
            for (int l = 0; l < def.LineCount; l++)
            {
                if (!fs.Lines[l].Unlocked)
                {
                    if (nextLocked < 0) nextLocked = l;
                    continue;
                }
                var rep = s.AnalyzeLine(fid, l);
                var bottleneck = s.Content.GetMachine(def.MachineChain[rep.BottleneckIndex]);
                GUI.Label(new Rect(r.x + 18, y, r.width - 36, 32),
                    $"#{l + 1}  {loc.Format("ui.income_per_sec", loc.Money(BigDouble.FromDouble(rep.NetPerSecond)))}   " +
                    $"<color=#FFC531>{loc.Get("ui.bottleneck")}: {loc.Get(bottleneck.NameKey)}</color>", _label);
                y += 40;
            }
            if (nextLocked >= 0)
            {
                bool can = s.Factories.CanUnlockLine(fid, nextLocked, out var cost);
                var br = new Rect(r.x + 18, r.yMax - 66, r.width - 36, 54);
                if (Button(br, loc.Format("ui.unlock_line", nextLocked + 1, loc.Money(cost)), can))
                    s.Factories.TryUnlockLine(fid, nextLocked);
            }
        }

        private void DrawMachinePanel()
        {
            var s = _game.Session;
            var loc = _game.Localization;
            var mr = _selected.Ref;
            if (mr.FactoryId != s.State.ActiveFactoryId) { _selected = null; return; }
            var def = _selected.Def;
            var ms = s.State.GetMachine(mr);
            var t = s.Tuning;
            double speedMod = s.Modifiers.Get(ModifierStat.ProductionSpeed, mr.FactoryId);

            var r = Hit(new Rect(_safe.x + 12, _safe.yMax - 360, _safe.width - 24, 348));
            GUI.Box(r, GUIContent.none, _panel);
            GUI.Label(new Rect(r.x + 18, r.y + 10, r.width - 160, 36), loc.Get(def.NameKey), _title);
            if (Button(new Rect(r.xMax - 130, r.y + 10, 112, 46), loc.Get("ui.close"))) { _selected = null; return; }

            GUI.Label(new Rect(r.x + 18, r.y + 50, r.width, 30),
                $"{loc.Get("stage." + def.Stage)} · {loc.Format("ui.level", ms.Level)} · <color=#9EE6A8>{loc.Get("status." + ms.Status)}</color>", _label);
            double cycle = UpgradeMath.CycleSeconds(def, ms.Level, t, speedMod);
            int cap = UpgradeMath.Capacity(def, ms.Level, t, s.Modifiers.Get(ModifierStat.StorageCapacity, mr.FactoryId));
            string stock = def.Stage == StageType.Shipping ? "" : "   " + loc.Format("ui.buffer", ms.OutputBuffer, cap);
            GUI.Label(new Rect(r.x + 18, r.y + 86, r.width, 30),
                loc.Format("ui.cycle_time", cycle.ToString(cycle < 1 ? "0.00" : "0.0")) + stock, _small);

            // Progress bar of the current cycle (real state).
            var bar = new Rect(r.x + 18, r.y + 124, r.width - 36, 14);
            GUI.DrawTexture(bar, _buttonDisabledTex);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * (float)ms.Progress, bar.height), _buttonTex);

            float bw = (r.width - 36 - 16) / 3f;
            int[] counts = { 1, 10, UpgradeService.Max };
            string[] labels = { loc.Get("ui.upgrade_x1"), loc.Get("ui.upgrade_x10"), loc.Get("ui.upgrade_max") };
            for (int i = 0; i < 3; i++)
            {
                var q = s.Upgrades.GetQuote(mr, counts[i]);
                var br = new Rect(r.x + 18 + i * (bw + 8), r.y + 160, bw, 110);
                string text = q.IsMaxLevel
                    ? loc.Get("ui.max_level")
                    : $"{loc.Get("ui.upgrade")} {labels[i]}\n+{q.Levels} → {q.TargetLevel}\n{loc.Money(q.Cost)}";
                if (Button(br, text, q.CanAfford && !q.IsMaxLevel)) s.Upgrades.TryUpgrade(mr, counts[i]);
            }

            var rep = s.AnalyzeLine(mr.FactoryId, mr.Line);
            bool isBottleneck = rep.BottleneckIndex == mr.Index;
            GUI.Label(new Rect(r.x + 18, r.y + 284, r.width - 36, 30),
                loc.Format("ui.sold_per_min", (rep.FinalUnitsPerSecond * 60).ToString("0.#")) +
                (isBottleneck ? $"   <color=#FFC531>{loc.Get("ui.bottleneck")}</color>" : ""), _small);
        }

        private void OnDestroy()
        {
            foreach (var t in new[] { _panelTex, _buttonTex, _buttonHoverTex, _buttonDisabledTex })
                if (t != null) Destroy(t);
        }
    }
}
