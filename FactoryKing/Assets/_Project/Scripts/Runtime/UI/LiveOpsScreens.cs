using System;
using System.Linq;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Persistence;
using FactoryKing.Runtime.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    // ============================================================== Expeditions
    public sealed class ExpeditionsScreen : UiScreen
    {
        public override string Id => "expeditions";
        public override string TitleKey => "screen.expeditions";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            root.Add(UiKit.Label(T("exp.intro"), 28, false, UiKit.Muted, true));
            for (int slot = 0; slot < ExpeditionService.Slots; slot++)
            {
                int sl = slot;
                var card = Card(root);
                card.Add(UiKit.Label(TF("exp.slot", sl + 1), 34, true));
                var e = s.Expeditions.InSlot(sl);
                if (e == null)
                {
                    var row = UiKit.Row();
                    for (int k = 0; k < ExpeditionService.Hours.Length; k++)
                    {
                        int kind = k;
                        var b = UiKit.Button(T("exp.kind" + kind) + "\n" + NumberFormatter.FormatDuration(s.Expeditions.DurationSeconds(kind)), () =>
                        {
                            if (s.Expeditions.TryStart(sl, kind)) { Click(); Rebuild(); }
                        }, UiKit.CardHi, 26, 110);
                        b.style.flexGrow = 1; b.style.flexBasis = 0;
                        row.Add(b);
                    }
                    card.Add(row);
                    card.Add(UiKit.Label(T("exp.rewards_hint"), 24, false, UiKit.Muted, true));
                }
                else
                {
                    card.Add(UiKit.Label(T("exp.kind" + e.Kind) + " · " + TF("exp.reward", M(e.RewardCash), e.RewardCards, e.RewardDiamonds)
                                         + (e.RewardCrate ? " · " + T("crate.small") : ""), 26, false, UiKit.Warn, true));
                    var bar = UiKit.ProgressBar(out var fill, UiKit.Diamond);
                    B.Progress(fill, () => 1 - s.Expeditions.Remaining(e) / Math.Max(1, (e.EndUtcTicks - e.StartUtcTicks) / (double)TimeSpan.TicksPerSecond));
                    card.Add(bar);
                    var claim = UiKit.Button("", () => { if (s.Expeditions.TryClaim(sl)) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good);
                    B.Button(claim, () => s.Expeditions.Remaining(e) > 0 ? TF("exp.back_in", NumberFormatter.FormatDuration(s.Expeditions.Remaining(e))) : T("ui.claim"),
                        () => s.Expeditions.Remaining(e) <= 0);
                    card.Add(claim);
                }
            }
        }
    }

    // ============================================================== Cards & crates
    public sealed class CardsScreen : UiScreen
    {
        public override string Id => "cards";
        public override string TitleKey => "screen.cards";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            Section(root, "cards.crates");
            foreach (var c in CrateService.Defs)
            {
                var crate = c;
                var card = Card(root);
                var head = UiKit.Row(0, Justify.SpaceBetween);
                head.Add(UiKit.Label(T(crate.NameKey), 34, true));
                head.Add(UiKit.Label("× " + s.Crates.Count(crate.Id), 34, true, UiKit.Warn));
                card.Add(head);
                card.Add(UiKit.Label(TF("crate.contents", crate.Cards), 26, false, UiKit.Muted));
                // Odds are always shown before opening or buying (App Store guideline 3.1.1).
                card.Add(UiKit.Label(TF("crate.odds", Pct(crate.Odds[0]), Pct(crate.Odds[1]), Pct(crate.Odds[2]), Pct(crate.Odds[3])), 24, false, UiKit.Muted, true));
                var row = UiKit.Row();
                var open = UiKit.Button(T("crate.open"), () =>
                {
                    var got = s.Crates.Open(crate.Id);
                    if (got == null) return;
                    C.Audio?.Play(Sfx.Success);
                    ShowCards(got);
                    Rebuild();
                }, UiKit.Good, 28, 90);
                UiKit.SetEnabled(open, s.Crates.Count(crate.Id) > 0);
                open.style.flexGrow = 1;
                row.Add(open);
                var buy = UiKit.Button(TF("crate.buy", crate.DiamondPrice), () => { if (s.Crates.TryBuy(crate.Id)) { Click(); Rebuild(); } }, UiKit.Diamond, 28, 90);
                UiKit.SetEnabled(buy, s.Wallet.Diamonds >= crate.DiamondPrice);
                buy.style.flexGrow = 1;
                row.Add(buy);
                card.Add(row);
            }

            Section(root, "cards.collection");
            foreach (var d in ManagerService.Defs.OrderBy(x => (int)x.Rarity))
            {
                var def = d;
                var card = Card(root);
                card.style.borderLeftWidth = 8;
                card.style.borderLeftColor = UiKit.RarityColor((int)def.Rarity);
                var head = UiKit.Row(0, Justify.SpaceBetween);
                head.Add(UiKit.Label(T(def.NameKey) + " · " + T("rarity." + def.Rarity), 30, true));
                head.Add(UiKit.Label("▣ " + s.Managers.Cards(def.Id), 30, true, UiKit.Warn));
                card.Add(head);
                var owned = s.Managers.GetOwned(def.Id);
                if (owned == null)
                {
                    int need = ManagerService.CardsToUnlock(def.Rarity);
                    var bar = UiKit.ProgressBar(out var fill, UiKit.RarityColor((int)def.Rarity));
                    UiKit.SetProgress(fill, s.Managers.Cards(def.Id) / (double)need);
                    card.Add(bar);
                    var recruit = UiKit.Button(TF("cards.recruit", s.Managers.Cards(def.Id), need), () =>
                    {
                        if (s.Managers.TryRecruitWithCards(def.Id)) { C.Audio?.Play(Sfx.Success); Rebuild(); }
                    }, UiKit.Good, 26, 84);
                    UiKit.SetEnabled(recruit, s.Managers.Cards(def.Id) >= need);
                    card.Add(recruit);
                }
                else
                {
                    card.Add(UiKit.Label(TF("cards.level_need", owned.Level, ManagerService.CardsForLevel(owned.Level)), 26, false, UiKit.Muted));
                }
            }
        }

        private static string Pct(double v) => (v * 100).ToString(v < 0.01 ? "0.0" : "0") + "%";

        private void ShowCards(System.Collections.Generic.Dictionary<string, int> got)
        {
            C.Ui.ShowModal((box, close) =>
            {
                box.Add(UiKit.Label(T("crate.opened"), 40, true, UiKit.Accent));
                foreach (var kv in got.OrderByDescending(k => (int)ManagerService.GetDef(k.Key).Rarity))
                {
                    var d = ManagerService.GetDef(kv.Key);
                    box.Add(UiKit.Label($"{T(d.NameKey)} ×{kv.Value}", 32, true, UiKit.RarityColor((int)d.Rarity)));
                }
                box.Add(UiKit.Button(T("ui.ok"), close, UiKit.Accent));
            });
        }
    }

    // ============================================================== Engineering (permanent)
    public sealed class EngineeringScreen : UiScreen
    {
        public override string Id => "engineering";
        public override string TitleKey => "screen.engineering";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            root.Add(UiKit.Label(T("eng.intro"), 28, false, UiKit.Muted, true));
            foreach (var d in EngineeringService.Defs)
            {
                var def = d;
                var card = Card(root);
                card.Add(UiKit.Label(T(def.NameKey), 34, true));
                card.Add(UiKit.Label(T(def.NameKey + ".desc"), 26, false, UiKit.Muted, true));
                int lv = s.Engineering.Level(def.Id);
                card.Add(UiKit.Label(TF("research.level", lv, def.MaxLevel), 28));
                var buy = UiKit.Button(lv >= def.MaxLevel ? T("ui.max_level") : TF("cosmetics.buy", s.Engineering.Cost(def)), () =>
                {
                    if (s.Engineering.TryBuy(def.Id)) { C.Audio?.Play(Sfx.Upgrade); Rebuild(); }
                }, UiKit.Diamond);
                UiKit.SetEnabled(buy, lv < def.MaxLevel && s.Wallet.Diamonds >= s.Engineering.Cost(def));
                card.Add(buy);
            }
        }
    }

    // ============================================================== Statistics
    public sealed class StatsScreen : UiScreen
    {
        public override string Id => "stats";
        public override string TitleKey => "screen.stats";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            var st = s.State.Stats;
            void Row(string key, Func<string> value)
            {
                var r = UiKit.Row(0, Justify.SpaceBetween);
                r.Add(UiKit.Label(T(key), 28, false, UiKit.Muted));
                r.Add(B.Text(UiKit.Label("", 30, true), value));
                root.Add(r);
            }
            Row("stats.lifetime", () => M(s.State.LifetimeEarnings));
            Row("stats.income", () => C.TF("ui.income_per_sec", M(s.Ledger.NetPerSecond)));
            Row("stats.value", () => M(s.CompanyValue));
            Row("stats.factories", () => $"{s.OwnedRegularFactories}/{s.Content.Factories.Count}");
            Row("stats.machines", () => s.OwnedMachineCount.ToString());
            Row("stats.highest", () => st.HighestMachineLevel.ToString());
            Row("stats.produced", () => NumberFormatter.Format(st.UnitsProduced));
            Row("stats.sold", () => NumberFormatter.Format(st.UnitsSold));
            Row("stats.upgrades", () => st.Upgrades.ToString());
            Row("stats.orders", () => st.OrdersCompleted.ToString());
            Row("stats.drones", () => st.DronesCaught.ToString());
            Row("stats.expeditions", () => st.ExpeditionsDone.ToString());
            Row("stats.crates", () => st.CratesOpened.ToString());
            Row("stats.seasons", () => st.SeasonsCompleted.ToString());
            Row("stats.badges", () => $"{s.Badges.Count} (+{Mathf.RoundToInt((float)(s.Badges.Bonus * 100))}%)");
            Row("stats.playtime", () => NumberFormatter.FormatDuration(s.State.PlayTimeSeconds));
        }
    }

    // ============================================================== Co-op contracts (online)
    public sealed class CoopScreen : UiScreen
    {
        public override string Id => "coop";
        public override string TitleKey => "screen.coop";

        protected override void Build(VisualElement root)
        {
            var on = C.Online;
            if (on == null || !on.Enabled) { root.Add(UiKit.Label(T("online.disabled"), 30, false, UiKit.Muted, true)); return; }
            if (!on.Registered) { root.Add(UiKit.Label(T("online.connecting"), 30, false, UiKit.Muted, true)); return; }
            root.Add(UiKit.Label(T("coop.intro"), 28, false, UiKit.Muted, true));
            var coop = on.Coop;
            if (coop == null)
            {
                root.Add(UiKit.Button(T("coop.create"), () => on.CreateCoop(ok => { Toast(ok); Rebuild(); }), UiKit.Accent));
                JoinRow(root, "coop.join", (code, done) => on.JoinCoop(code, done));
                return;
            }
            var contract = coop.Obj("contract");
            var card = Card(root);
            card.Add(UiKit.Label(C.Loc.Current == Core.Localization.Language.Turkish ? contract.Str("nameTr") : contract.Str("nameEn"), 40, true, UiKit.Accent));
            card.Add(UiKit.Label(TF("coop.code", coop.Str("code")), 30, true, UiKit.Diamond));
            card.Add(UiKit.Label(TF("coop.progress", NumberFormatter.Format(coop.Dbl("total")), NumberFormatter.Format(coop.Dbl("goal"))), 30));
            var bar = UiKit.ProgressBar(out var fill, UiKit.Good, 24);
            UiKit.SetProgress(fill, coop.Dbl("total") / Math.Max(1, coop.Dbl("goal")));
            card.Add(bar);
            card.Add(UiKit.Label(TF("coop.ends", NumberFormatter.FormatDuration((contract.Dbl("end") - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000.0)), 26, false, UiKit.Muted));
            card.Add(UiKit.Label(TF("coop.reward", CoopTracker.RewardDiamonds), 26, false, UiKit.Warn));
            Section(root, "coop.members");
            foreach (var m in coop.Arr("members").Items)
            {
                var r = UiKit.Row(0, Justify.SpaceBetween);
                r.Add(UiKit.Label(m.Str("name"), 28));
                r.Add(UiKit.Label(NumberFormatter.Format(m.Dbl("units")), 28, true));
                root.Add(r);
            }
            if (coop.Bool("completed")) root.Add(UiKit.Label(T("coop.done"), 32, true, UiKit.Good));
        }

        private void Toast(bool ok) => C.Ui.Toast(ok ? T("online.ok") : T("online.error"), ok ? UiKit.Good : UiKit.Bad);

        internal void JoinRow(VisualElement root, string key, Action<string, Action<bool>> join)
        {
            var row = UiKit.Row();
            var field = new TextField { maxLength = 6 };
            field.style.fontSize = 34; field.style.flexGrow = 1; field.style.minHeight = 90;
            row.Add(field);
            row.Add(UiKit.Button(T(key), () => join(field.value.Trim().ToUpperInvariant(), ok => { Toast(ok); Rebuild(); }), UiKit.CardHi, 28, 90));
            root.Add(row);
        }
    }

    // ============================================================== Clubs (online)
    public sealed class ClubScreen : UiScreen
    {
        public override string Id => "club";
        public override string TitleKey => "screen.club";

        protected override void Build(VisualElement root)
        {
            var on = C.Online;
            if (on == null || !on.Enabled) { root.Add(UiKit.Label(T("online.disabled"), 30, false, UiKit.Muted, true)); return; }
            if (!on.Registered) { root.Add(UiKit.Label(T("online.connecting"), 30, false, UiKit.Muted, true)); return; }
            root.Add(UiKit.Label(T("club.intro"), 28, false, UiKit.Muted, true));
            var club = on.Club;
            if (club == null)
            {
                var row = UiKit.Row();
                var name = new TextField { maxLength = 20 };
                name.style.fontSize = 32; name.style.flexGrow = 1; name.style.minHeight = 90;
                row.Add(name);
                row.Add(UiKit.Button(T("club.create"), () =>
                {
                    if (Core.Simulation.CompanyNameRules.Sanitize(name.value) == null) { C.Ui.Toast(T("onboard.name_invalid"), UiKit.Bad); return; }
                    on.CreateClub(name.value, ok => { C.Ui.Toast(ok ? T("online.ok") : T("club.name_taken"), ok ? UiKit.Good : UiKit.Bad); Rebuild(); });
                }, UiKit.Accent, 28, 90));
                root.Add(row);
                var join = UiKit.Row();
                var code = new TextField { maxLength = 6 };
                code.style.fontSize = 34; code.style.flexGrow = 1; code.style.minHeight = 90;
                join.Add(code);
                join.Add(UiKit.Button(T("club.join"), () => on.JoinClub(code.value.Trim().ToUpperInvariant(), ok => { C.Ui.Toast(ok ? T("online.ok") : T("online.error")); Rebuild(); }), UiKit.CardHi, 28, 90));
                root.Add(join);
            }
            else
            {
                var card = Card(root);
                card.Add(UiKit.Label(club.Str("name"), 40, true, UiKit.Accent));
                card.Add(UiKit.Label(TF("coop.code", club.Str("code")), 30, true, UiKit.Diamond));
                card.Add(UiKit.Label(TF("club.score", NumberFormatter.Format(club.Dbl("weekScore"))), 30));
                Section(root, "club.members");
                foreach (var m in club.Arr("members").Items)
                {
                    var r = UiKit.Row(0, Justify.SpaceBetween);
                    r.Add(UiKit.Label(m.Str("name"), 28));
                    r.Add(UiKit.Label(NumberFormatter.Format(m.Dbl("weekXp")) + " XP", 28, true));
                    root.Add(r);
                }
                var actions = UiKit.Row();
                actions.Add(UiKit.Button(T("club.leave"), () => { on.LeaveClub(); Rebuild(); }, UiKit.CardHi, 26, 84));
                actions.Add(UiKit.Button(T("club.report"), () => { on.Report("club", club.Str("code")); C.Ui.Toast(T("club.reported")); }, UiKit.CardHi, 26, 84));
                root.Add(actions);
            }
            Section(root, "club.top");
            if (on.TopClubs == null) on.LoadTopClubs();
            else
            {
                int rank = 1;
                foreach (var c in on.TopClubs.Arr("clubs").Items)
                {
                    var r = UiKit.Row(0, Justify.SpaceBetween);
                    r.Add(UiKit.Label($"{rank++}. {c.Str("name")}", 28));
                    r.Add(UiKit.Label(NumberFormatter.Format(c.Dbl("weekScore")), 28, true));
                    root.Add(r);
                }
            }
        }
    }
}
