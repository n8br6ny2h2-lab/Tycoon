using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Audio;
using FactoryKing.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    /// <summary>
    /// Always-visible factory screen UI: resources bar, notification chips, tutorial hint, line strip, machine
    /// details sheet and the bottom navigation.
    /// </summary>
    public sealed class Hud
    {
        private readonly UiContext C;
        private readonly VisualElement _layer;
        private readonly Bindings B = new Bindings();
        private readonly Bindings SB = new Bindings(); // machine sheet bindings, reset per selection
        private VisualElement _nav, _sheetHost, _lineStrip;
        private MachineView _selected;

        public Hud(UiContext ctx, VisualElement layer)
        {
            C = ctx;
            _layer = layer;
        }

        private GameSession S => C.Session;
        private string T(string k) => C.T(k);

        public MachineView Selected => _selected;

        public void Build()
        {
            B.Clear();
            _layer.Clear();

            var column = new VisualElement();
            column.style.flexGrow = 1;
            column.pickingMode = PickingMode.Ignore;
            UiKit.Pad(column, 16, 20);
            _layer.Add(column);

            column.Add(TopBar());
            column.Add(Chips());
            column.Add(TutorialBanner());
            column.Add(UiKit.Grow());

            _sheetHost = new VisualElement();
            column.Add(_sheetHost);
            _lineStrip = LineStrip();
            column.Add(_lineStrip);
            _nav = Nav();
            column.Add(_nav);
            if (_selected != null) Select(_selected);
            B.Refresh();
        }

        public void Rebuild() => Build();
        public void Refresh() { B.Refresh(); SB.Refresh(); }
        public void SetNavVisible(bool v) => _layer.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

        // ---------------- top bar ----------------

        private VisualElement TopBar()
        {
            var bar = UiKit.Box(UiKit.Bg, 26, 16, 24);
            var row = UiKit.Row(0, Justify.SpaceBetween);
            var left = new VisualElement();
            left.Add(B.Text(UiKit.Label("", 56, true), () => "$ " + C.M(S.Wallet.Cash)));
            left.Add(B.Text(UiKit.Label("", 32, false, UiKit.Good), () =>
                C.TF("ui.income_per_sec", C.M(S.Ledger.NetPerSecond)) + "   " + C.TF("ui.net_per_hour", C.M(S.Ledger.NetPerHour))));
            row.Add(left);
            var right = new VisualElement();
            right.style.alignItems = Align.FlexEnd;
            right.Add(B.Text(UiKit.Label("", 42, true, UiKit.Diamond), () => "◆ " + S.Wallet.Diamonds));
            right.Add(B.Text(UiKit.Label("", 30, false, UiKit.Muted), () => C.TF("ui.company_level", S.CompanyLevel)));
            right.Add(B.Text(UiKit.Label("", 26, false, UiKit.Warn), () =>
                C.TF("hud.season_left", S.Seasons.Number, NumberFormatter.FormatDuration(S.Seasons.SecondsLeft))));
            row.Add(right);
            bar.Add(row);
            var debt = B.Text(UiKit.Label("", 28, false, UiKit.Warn), () => C.TF("ui.debt", C.M(S.Wallet.Debt)));
            B.Visible(debt, () => S.Wallet.Debt.IsPositive);
            bar.Add(debt);
            return bar;
        }

        private VisualElement Chips()
        {
            var row = UiKit.Row();
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginTop = 8;
            row.pickingMode = PickingMode.Ignore;

            Button Chip(string id, System.Func<string> text, System.Func<bool> visible, Color color, System.Action onClick)
            {
                var b = UiKit.Button("", onClick, color, 28, 72);
                b.name = id;
                B.Button(b, text, null);
                B.Visible(b, visible);
                row.Add(b);
                return b;
            }

            Chip("boost", () => C.TF("hud.boost", NumberFormatter.FormatDuration(S.Boosts.RemainingSeconds)),
                () => S.Boosts.IsActive, UiKit.Good, () => C.Ui.Open("shop"));
            Chip("daily", () => T("hud.daily_ready"), () => S.DailyRewards.CanClaim, UiKit.Warn, () => C.Ui.Open("daily"));
            Chip("missions", () => C.TF("hud.missions_ready", S.Missions.ClaimableCount), () => S.Missions.ClaimableCount > 0,
                UiKit.Warn, () => C.Ui.Open("missions"));
            Chip("orders", () => T("hud.order_ready"), () => S.State.Orders.Exists(o => !o.Claimed && S.Orders.IsComplete(o)),
                UiKit.Warn, () => C.Ui.Open("orders"));
            Chip("event", () => T("hud.event_live"), () => S.Events.Active().Count > 0, UiKit.Diamond, () => C.Ui.Open("events"));
            Chip("prestige", () => T("hud.prestige_ready"), () => S.Prestige.Preview().Available, UiKit.Accent, () => C.Ui.Open("prestige"));
            Chip("season", () => C.TF("hud.season_reward", S.Seasons.ClaimableCount), () => S.Seasons.ClaimableCount > 0,
                UiKit.Diamond, () => C.Ui.Open("season"));
            Chip("power", () => T("hud.power_low"), () => S.Power.Throttle(S.State.ActiveFactoryId) < 0.999, UiKit.Bad, () => Select(null));
            Chip("ability", () => T("hud.ability_ready"), () =>
            {
                var m = S.Managers.AssignedTo(S.State.ActiveFactoryId);
                return m != null && S.Managers.CanUseAbility(m.Id);
            }, UiKit.Good, () =>
            {
                var m = S.Managers.AssignedTo(S.State.ActiveFactoryId);
                if (m != null && S.Managers.TryUseAbility(m.Id))
                {
                    C.Audio?.Play(Sfx.Success);
                    C.Ui.Toast(C.TF("ability.used", T(ManagerService.GetDef(m.Id).NameKey), T(ManagerService.GetDef(m.Id).AbilityKey)), UiKit.Good);
                }
            });
            return row;
        }

        // ---------------- tutorial ----------------

        private VisualElement TutorialBanner()
        {
            var box = UiKit.Box(UiKit.Hex("#2E5BFFDD"), 20, 14, 22);
            box.style.marginTop = 8;
            box.pickingMode = PickingMode.Ignore;
            box.Add(B.Text(UiKit.Label("", 32, true, null, true), () => T("tutorial.step" + Mathf.Min(S.State.TutorialStep, 2))));
            B.Custom(AdvanceTutorial);
            B.Visible(box, () => S.State.TutorialStep < 3);
            return box;
        }

        private void AdvanceTutorial()
        {
            int step = S.State.TutorialStep;
            if (step == 0 && _selected != null) S.State.TutorialStep = 1;
            else if (step == 1 && S.State.Stats.Upgrades > 0) S.State.TutorialStep = 2;
            else if (step == 2 && (S.State.Stats.LinesBought > 0 || S.State.Stats.FactoriesBought > 0))
            {
                S.State.TutorialStep = 3;
                S.Analytics.Track(AnalyticsEvents.TutorialComplete);
                C.Audio?.Play(Sfx.Success);
                C.Ui.Toast(T("tutorial.done"), UiKit.Good);
            }
        }

        // ---------------- line strip ----------------

        private VisualElement LineStrip()
        {
            var box = UiKit.Box(UiKit.Bg, 22, 12, 20);
            box.style.marginBottom = 10;
            string fid = S.State.ActiveFactoryId;
            var def = S.Content.GetFactory(fid);
            box.Add(UiKit.Label(T(def.NameKey), 36, true));
            for (int l = 0; l < def.LineCount; l++)
            {
                int line = l;
                var lbl = UiKit.Label("", 28, false, UiKit.Muted);
                B.Text(lbl, () =>
                {
                    if (!S.State.GetFactory(fid).Lines[line].Unlocked) return $"#{line + 1}  {T("ui.locked")}";
                    var r = S.AnalyzeLine(fid, line);
                    var bn = S.Content.GetMachine(def.MachineChain[r.BottleneckIndex]);
                    return $"#{line + 1}  {C.TF("ui.income_per_sec", C.M(r.NetPerSecond))}  ·  {T("ui.bottleneck")}: {T(bn.NameKey)}";
                });
                box.Add(lbl);
            }
            // Market price of this factory's product (living market).
            box.Add(B.Text(UiKit.Label("", 28, false, UiKit.Diamond), () =>
            {
                double f = S.Market.PriceFactor(fid);
                string pct = (f >= 1 ? "+" : "") + Mathf.RoundToInt((float)((f - 1) * 100)) + "%";
                string hot = S.Market.HotFactoryId(S.Time.TrustedNow) == fid ? "  ★ " + T("market.hot") : "";
                return C.TF("market.price", pct) + hot;
            }));

            // Power plant: demand vs capacity and its upgrade.
            var powerRow = UiKit.Row(0, Justify.SpaceBetween);
            var powerLabel = UiKit.Label("", 28, false, UiKit.Muted);
            powerLabel.style.flexShrink = 1;
            B.Text(powerLabel, () =>
            {
                double demand = S.Power.DemandKw(fid), cap = S.Power.CapacityKw(fid);
                string text = C.TF("power.status", Mathf.RoundToInt((float)demand), Mathf.RoundToInt((float)cap), S.Power.Level(fid));
                double t = S.Power.Throttle(fid);
                return t < 0.999 ? text + "  ⚠ " + C.TF("power.throttled", Mathf.RoundToInt((float)(t * 100))) : text;
            });
            powerRow.Add(powerLabel);
            var powerUp = UiKit.Button("", () => { if (S.Power.TryUpgrade(fid)) C.Audio?.Play(Sfx.Upgrade); }, UiKit.Warn, 26, 80);
            B.Button(powerUp, () => S.Power.IsMax(fid) ? T("ui.max_level") : C.TF("power.upgrade", C.M(S.Power.UpgradeCost(fid))),
                () => !S.Power.IsMax(fid) && S.Wallet.CanAfford(S.Power.UpgradeCost(fid)));
            powerRow.Add(powerUp);
            box.Add(powerRow);

            var unlock = UiKit.Button("", () =>
            {
                int next = NextLockedLine(fid);
                if (next >= 0 && S.Factories.TryUnlockLine(fid, next)) C.Audio?.Play(Sfx.Unlock);
            }, UiKit.Accent, 32, 92);
            B.Button(unlock, () =>
            {
                int next = NextLockedLine(fid);
                if (next < 0) return "";
                S.Factories.CanUnlockLine(fid, next, out var cost);
                return C.TF("ui.unlock_line", next + 1, C.M(cost));
            }, () => { int n = NextLockedLine(fid); return n >= 0 && S.Factories.CanUnlockLine(fid, n, out _); });
            B.Visible(unlock, () => NextLockedLine(fid) >= 0);
            box.Add(unlock);
            return box;
        }

        private int NextLockedLine(string fid)
        {
            var fs = S.State.GetFactory(fid);
            for (int l = 0; l < fs.Lines.Count; l++) if (!fs.Lines[l].Unlocked) return l;
            return -1;
        }

        // ---------------- machine sheet ----------------

        public void Select(MachineView view)
        {
            _selected = view;
            _sheetHost.Clear();
            SB.Clear();
            if (view == null || view.Ref.FactoryId != S.State.ActiveFactoryId)
            {
                _selected = null;
                _lineStrip.style.display = DisplayStyle.Flex;
                return;
            }
            _lineStrip.style.display = DisplayStyle.None;
            var mr = view.Ref;
            var def = view.Def;
            var t = S.Tuning;

            var sheet = UiKit.Box(UiKit.Bg, 26, 18, 24);
            sheet.style.marginBottom = 10;
            var head = UiKit.Row(0, Justify.SpaceBetween);
            head.Add(UiKit.Label(T(def.NameKey), 42, true));
            var close = UiKit.Button("X", () => Select(null), UiKit.CardHi, 34, 84);
            close.style.width = 100;
            head.Add(close);
            sheet.Add(head);

            sheet.Add(SB.Text(UiKit.Label("", 30, false, UiKit.Muted), () =>
            {
                var ms = S.State.GetMachine(mr);
                return $"{T("stage." + def.Stage)} · {C.TF("ui.level", ms.Level)} · {T("status." + ms.Status)}";
            }));
            sheet.Add(SB.Text(UiKit.Label("", 28, false, UiKit.Muted), () =>
            {
                var ms = S.State.GetMachine(mr);
                double cycle = UpgradeMath.CycleSeconds(def, ms.Level, t, S.Modifiers.Get(ModifierStat.ProductionSpeed, mr.FactoryId));
                string text = C.TF("ui.cycle_time", cycle.ToString(cycle < 1 ? "0.00" : "0.0"));
                if (def.Stage != StageType.Shipping)
                {
                    int cap = UpgradeMath.Capacity(def, ms.Level, t, S.Modifiers.Get(ModifierStat.StorageCapacity, mr.FactoryId));
                    text += "   " + C.TF("ui.buffer", ms.OutputBuffer, cap);
                }
                var rep = S.AnalyzeLine(mr.FactoryId, mr.Line);
                if (rep.BottleneckIndex == mr.Index) text += "   ⚠ " + T("ui.bottleneck");
                return text;
            }));
            var bar = UiKit.ProgressBar(out var fill);
            SB.Progress(fill, () => S.State.GetMachine(mr).Progress);
            sheet.Add(bar);

            var row = UiKit.Row();
            int[] counts = { 1, 10, UpgradeService.Max };
            string[] labels = { "ui.upgrade_x1", "ui.upgrade_x10", "ui.upgrade_max" };
            for (int i = 0; i < 3; i++)
            {
                int count = counts[i];
                string label = labels[i];
                var b = UiKit.Button("", () =>
                {
                    if (S.Upgrades.TryUpgrade(mr, count)) C.Audio?.Play(Sfx.Upgrade);
                    else C.Audio?.Play(Sfx.Error);
                }, UiKit.Accent, 30, 140);
                b.style.flexGrow = 1;
                b.style.flexBasis = 0;
                SB.Button(b, () =>
                {
                    var q = S.Upgrades.GetQuote(mr, count);
                    return q.IsMaxLevel ? T("ui.max_level") : $"{T(label)}  →{q.TargetLevel}\n{C.M(q.Cost)}";
                }, () => { var q = S.Upgrades.GetQuote(mr, count); return q.CanAfford && !q.IsMaxLevel; });
                row.Add(b);
            }
            sheet.Add(row);
            _sheetHost.Add(sheet);
            SB.Refresh();
        }

        // ---------------- navigation ----------------

        private VisualElement Nav()
        {
            var nav = UiKit.Box(UiKit.Bg, 28, 8, 8);
            nav.style.flexDirection = FlexDirection.Row;
            void Item(string key, string screen)
            {
                var b = UiKit.Button(T(key), () => C.Ui.Open(screen), UiKit.CardHi, 28, 110);
                b.style.flexGrow = 1;
                b.style.flexBasis = 0;
                nav.Add(b);
            }
            Item("nav.map", "map");
            Item("nav.managers", "managers");
            Item("nav.missions", "missions");
            Item("nav.shop", "shop");
            Item("nav.more", "more");
            return nav;
        }
    }
}
