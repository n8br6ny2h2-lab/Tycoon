using System;
using System.Linq;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    // ============================================================== Map (factories)
    public sealed class MapScreen : UiScreen
    {
        public override string Id => "map";
        public override string TitleKey => "screen.map";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            if (s.EventFactory.IsActive)
            {
                var ev = s.EventFactory.Current;
                var evDef = s.Content.GetFactory(ev.FactoryId);
                var card = Card(root);
                card.style.borderLeftWidth = 10;
                card.style.borderLeftColor = UiKit.Diamond;
                card.Add(UiKit.Label("★ " + T(evDef.NameKey), 40, true, UiKit.Diamond));
                card.Add(B.Text(UiKit.Label("", 28, false, UiKit.Muted), () =>
                    TF("event.factory_status", s.EventFactory.Tokens, NumberFormatter.FormatDuration((ev.EndUtcTicks - s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond))));
                if (s.State.ActiveFactoryId != ev.FactoryId)
                    card.Add(UiKit.Button(T("map.visit"), () => { s.Factories.SetActiveFactory(ev.FactoryId); C.Ui.Close(); }, UiKit.Diamond));
            }
            else if (!s.EventFactory.IsUnlockedForPlayer)
            {
                root.Add(UiKit.Label(T("event.factory_locked"), 26, false, UiKit.Muted, true));
            }
            bool shownNext = false;
            foreach (var f in s.Content.Factories.OrderBy(x => x.Order))
            {
                var def = f;
                bool owned = s.Factories.IsUnlocked(def.Id);
                var card = Card(root);
                var head = UiKit.Row(0, Justify.SpaceBetween);
                head.Add(UiKit.Label(T(def.NameKey), 40, true, owned ? UiKit.Text : UiKit.Muted));
                if (s.State.ActiveFactoryId == def.Id) head.Add(UiKit.Label(T("map.here"), 28, true, UiKit.Good));
                card.Add(head);
                var final = s.Content.GetProduct(s.Content.GetMachine(def.MachineChain[def.MachineChain.Count - 1]).OutputProductId);
                card.Add(UiKit.Label(TF("map.product", T(final.NameKey)), 28, false, UiKit.Muted));
                if (def.SupplierId != null)
                    card.Add(UiKit.Label(TF("supply.line", T(s.Content.GetFactory(def.SupplierId).NameKey)) + (s.Supply.IsSupplied(def.Id) ? " ✓" : ""), 26, false,
                        s.Supply.IsSupplied(def.Id) ? UiKit.Good : UiKit.Muted, true));
                card.Add(B.Text(UiKit.Label("", 28, false, UiKit.Diamond), () =>
                {
                    double f = s.Market.PriceFactor(def.Id);
                    string pct = (f >= 1 ? "+" : "") + Mathf.RoundToInt((float)((f - 1) * 100)) + "%";
                    return TF("market.price", pct) + (s.Market.HotFactoryId(s.Time.TrustedNow) == def.Id ? "  ★ " + T("market.hot") : "");
                }));

                if (owned)
                {
                    card.Add(B.Text(UiKit.Label("", 30, false, UiKit.Good), () =>
                    {
                        double net = 0;
                        var fs = s.State.GetFactory(def.Id);
                        int lines = 0;
                        for (int l = 0; l < fs.Lines.Count; l++)
                            if (fs.Lines[l].Unlocked) { lines++; net += s.AnalyzeLine(def.Id, l).NetPerSecond; }
                        return TF("map.lines", lines, def.LineCount) + "  ·  " + TF("ui.income_per_sec", M(net));
                    }));
                    var mgr = s.Managers.AssignedTo(def.Id);
                    if (mgr != null) card.Add(UiKit.Label(TF("map.manager", T(ManagerService.GetDef(mgr.Id).NameKey)), 28, false, UiKit.Muted));
                    if (s.State.ActiveFactoryId != def.Id)
                        card.Add(UiKit.Button(T("map.visit"), () => { s.Factories.SetActiveFactory(def.Id); C.Ui.Close(); }));
                }
                else if (!shownNext)
                {
                    shownNext = true;
                    var buy = UiKit.Button("", () =>
                    {
                        if (s.Factories.TryUnlockFactory(def.Id))
                        {
                            C.Audio?.Play(Sfx.Unlock);
                            s.Factories.SetActiveFactory(def.Id);
                            C.Ui.Close();
                        }
                    });
                    B.Button(buy, () => TF("ui.unlock_factory", M(def.UnlockCost)), () => s.Factories.CanUnlockFactory(def.Id, out _));
                    card.Add(buy);
                }
                else
                {
                    card.Add(UiKit.Label(T("ui.locked"), 30, false, UiKit.Muted));
                }
            }
        }
    }

    // ============================================================== Managers
    public sealed class ManagersScreen : UiScreen
    {
        public override string Id => "managers";
        public override string TitleKey => "screen.managers";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            root.Add(UiKit.Label(T("managers.intro"), 28, false, UiKit.Muted, true));
            foreach (var def in ManagerService.Defs)
            {
                var d = def;
                var card = Card(root);
                card.style.borderLeftWidth = 10;
                card.style.borderLeftColor = UiKit.RarityColor((int)d.Rarity);
                var head = UiKit.Row(0, Justify.SpaceBetween);
                head.Add(UiKit.Label(T(d.NameKey), 40, true));
                head.Add(UiKit.Label(T("rarity." + d.Rarity), 28, true, UiKit.RarityColor((int)d.Rarity)));
                card.Add(head);
                card.Add(UiKit.Label(T(d.TitleKey), 30, false, UiKit.Muted));

                var owned = s.Managers.GetOwned(d.Id);
                if (owned == null && d.HireDiamonds <= 0)
                {
                    card.Add(UiKit.Label(TF("managers.bonus", Pct(ManagerService.Bonus(d, 1)), T("stat." + d.Stat)), 30));
                    card.Add(UiKit.Button(TF("cards.recruit", s.Managers.Cards(d.Id), ManagerService.CardsToUnlock(d.Rarity)), () => C.Ui.Open("cards"), UiKit.CardHi, 26, 84));
                    continue;
                }
                if (owned == null)
                {
                    card.Add(UiKit.Label(TF("managers.bonus", Pct(ManagerService.Bonus(d, 1)), T("stat." + d.Stat)), 30));
                    var hire = UiKit.Button(TF("managers.hire", d.HireDiamonds), () =>
                    {
                        if (s.Managers.TryHire(d.Id))
                        {
                            C.Audio?.Play(Sfx.Success);
                            s.Managers.Assign(d.Id, s.Managers.AssignedTo(s.State.ActiveFactoryId) == null ? s.State.ActiveFactoryId : null);
                            Rebuild();
                        }
                    }, UiKit.Diamond);
                    B.Button(hire, null, () => s.Wallet.Diamonds >= d.HireDiamonds);
                    card.Add(hire);
                    continue;
                }

                card.Add(B.Text(UiKit.Label("", 30), () =>
                    TF("managers.level", owned.Level, d.MaxLevel) + "  ·  " + TF("managers.bonus", Pct(ManagerService.Bonus(d, owned.Level)), T("stat." + d.Stat))));
                card.Add(UiKit.Label(owned.AssignedFactoryId != null
                    ? TF("managers.assigned", T(s.Content.GetFactory(owned.AssignedFactoryId).NameKey))
                    : T("managers.unassigned"), 28, false, owned.AssignedFactoryId != null ? UiKit.Good : UiKit.Warn));

                var row = UiKit.Row();
                var up = UiKit.Button("", () => { if (s.Managers.TryLevelUp(d.Id)) { C.Audio?.Play(Sfx.Upgrade); Rebuild(); } });
                up.style.flexGrow = 1;
                B.Button(up, () => owned.Level >= d.MaxLevel ? T("ui.max_level")
                        : TF("managers.levelup", M(s.Managers.LevelUpCost(d, owned.Level))) + $"  ▣ {s.Managers.Cards(d.Id)}/{ManagerService.CardsForLevel(owned.Level)}",
                    () => owned.Level < d.MaxLevel && s.Wallet.CanAfford(s.Managers.LevelUpCost(d, owned.Level)) && s.Managers.Cards(d.Id) >= ManagerService.CardsForLevel(owned.Level));
                row.Add(up);
                bool here = owned.AssignedFactoryId == s.State.ActiveFactoryId;
                var assign = UiKit.Button(here ? T("managers.unassign") : T("managers.assign_here"), () =>
                {
                    s.Managers.Assign(d.Id, here ? null : s.State.ActiveFactoryId);
                    Click();
                    Rebuild();
                }, UiKit.CardHi);
                assign.style.flexGrow = 1;
                row.Add(assign);
                card.Add(row);

                card.Add(UiKit.Label(T(d.AbilityKey) + " — " + T(d.AbilityKey + ".desc"), 26, false, UiKit.Diamond, true));
                var ability = UiKit.Button("", () =>
                {
                    if (s.Managers.TryUseAbility(d.Id)) C.Audio?.Play(Sfx.Success);
                }, UiKit.Good);
                B.Button(ability, () =>
                {
                    double active = s.Managers.AbilityActiveRemaining(d.Id);
                    if (active > 0) return TF("ability.active", NumberFormatter.FormatDuration(active));
                    double cd = s.Managers.AbilityCooldownRemaining(d.Id);
                    if (cd > 0) return TF("ability.cooldown", NumberFormatter.FormatDuration(cd));
                    return owned.AssignedFactoryId == null ? T("ability.assign_first") : T("ability.use");
                }, () => s.Managers.CanUseAbility(d.Id));
                card.Add(ability);
            }
        }

        private static string Pct(double fraction) => "%" + Math.Round(fraction * 100);
    }

    // ============================================================== Missions
    public sealed class MissionsScreen : UiScreen
    {
        public override string Id => "missions";
        public override string TitleKey => "screen.missions";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            s.Missions.Refresh();
            Section(root, "missions.daily");
            root.Add(B.Text(UiKit.Label("", 28, false, UiKit.Muted), () =>
                TF("missions.resets_in", NumberFormatter.FormatDuration((s.Time.TrustedNow.Date.AddDays(1) - s.Time.TrustedNow).TotalSeconds))));
            foreach (var m in s.State.Missions.Daily) Mission(root, m);
            Section(root, "missions.weekly");
            foreach (var m in s.State.Missions.Weekly) Mission(root, m);
        }

        private void Mission(VisualElement root, MissionState m)
        {
            var s = C.Session;
            var card = Card(root);
            string target = m.Type == MissionTypes.Earn ? M(m.Target) : ((long)m.Target).ToString();
            card.Add(UiKit.Label(TF("mission." + m.Type, target), 32, true, null, true));
            var bar = UiKit.ProgressBar(out var fill, UiKit.Good);
            B.Progress(fill, () => s.Missions.Progress(m) / Math.Max(1, m.Target));
            card.Add(bar);
            card.Add(UiKit.Label(TF("missions.reward", m.RewardDiamonds, NumberFormatter.FormatDuration(m.RewardCashSeconds)), 28, false, UiKit.Muted));
            var claim = UiKit.Button("", () => { if (s.Missions.TryClaim(m)) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good);
            B.Button(claim, () => m.Claimed ? T("ui.claimed") : T("ui.claim"), () => !m.Claimed && s.Missions.IsComplete(m));
            card.Add(claim);
        }
    }

    // ============================================================== Orders
    public sealed class OrdersScreen : UiScreen
    {
        public override string Id => "orders";
        public override string TitleKey => "screen.orders";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            s.Orders.Refresh();
            root.Add(UiKit.Label(T("orders.intro"), 28, false, UiKit.Muted, true));
            foreach (var o in s.State.Orders.ToList())
            {
                var order = o;
                var card = Card(root);
                card.Add(UiKit.Label(TF("orders.line", order.Quantity, T("product." + order.ProductId)), 34, true, null, true));
                card.Add(UiKit.Label(T(s.Content.GetFactory(order.FactoryId).NameKey), 28, false, UiKit.Muted));
                var bar = UiKit.ProgressBar(out var fill, UiKit.Good);
                B.Progress(fill, () => s.Orders.Progress(order) / (double)Math.Max(1, order.Quantity));
                card.Add(bar);
                card.Add(B.Text(UiKit.Label("", 28, false, UiKit.Muted), () =>
                    TF("orders.status", Math.Min(order.Quantity, s.Orders.Progress(order)), order.Quantity,
                       NumberFormatter.FormatDuration(Math.Max(0, (order.ExpiresUtcTicks - s.Time.TrustedNow.Ticks) / (double)TimeSpan.TicksPerSecond)))));
                var claim = UiKit.Button("", () => { if (s.Orders.TryClaim(order)) { C.Audio?.Play(Sfx.Cash); Rebuild(); } }, UiKit.Good);
                B.Button(claim, () => TF("orders.claim", M(s.Orders.Reward(order))), () => s.Orders.IsComplete(order) && !order.Claimed);
                card.Add(claim);
            }
        }
    }

    // ============================================================== Warehouse
    public sealed class WarehouseScreen : UiScreen
    {
        public override string Id => "warehouse";
        public override string TitleKey => "screen.warehouse";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            foreach (var fs in s.State.Factories.Where(f => f.Unlocked))
            {
                var def = s.Content.GetFactory(fs.FactoryId);
                Section(root, def.NameKey);
                int storageIndex = def.MachineChain.FindIndex(id => s.Content.GetMachine(id).Stage == StageType.Storage);
                var storeDef = s.Content.GetMachine(def.MachineChain[storageIndex]);
                for (int l = 0; l < fs.Lines.Count; l++)
                {
                    if (!fs.Lines[l].Unlocked) continue;
                    var line = fs.Lines[l];
                    int li = l;
                    var card = Card(root);
                    card.Add(UiKit.Label($"#{li + 1}  {T(storeDef.NameKey)}", 32, true));
                    var bar = UiKit.ProgressBar(out var fill, UiKit.Diamond, 24);
                    card.Add(bar);
                    var label = UiKit.Label("", 28, false, UiKit.Muted);
                    card.Add(label);
                    B.Custom(() =>
                    {
                        var ms = line.Machines[storageIndex];
                        int cap = UpgradeMath.Capacity(storeDef, ms.Level, s.Tuning, s.Modifiers.Get(ModifierStat.StorageCapacity, def.Id));
                        UiKit.SetProgress(fill, cap > 0 ? ms.OutputBuffer / (double)cap : 0);
                        label.text = TF("ui.buffer", ms.OutputBuffer, cap) + (ms.Status == MachineStatus.Blocked ? "  ⚠ " + T("warehouse.full") : "");
                    });
                }
                var finalId = s.Content.GetMachine(def.MachineChain[def.MachineChain.Count - 1]).OutputProductId;
                root.Add(B.Text(UiKit.Label("", 28, false, UiKit.Muted), () =>
                {
                    s.State.Stats.SoldByProduct.TryGetValue(finalId, out long sold);
                    return TF("warehouse.sold_total", T("product." + finalId), sold);
                }));
            }
            Section(root, "warehouse.tip_title");
            root.Add(UiKit.Label(T("warehouse.tip"), 28, false, UiKit.Muted, true));
        }
    }

    // ============================================================== Research
    public sealed class ResearchScreen : UiScreen
    {
        public override string Id => "research";
        public override string TitleKey => "screen.research";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            root.Add(UiKit.Label(T("research.intro"), 28, false, UiKit.Muted, true));
            foreach (var d in ResearchService.Defs)
            {
                var def = d;
                var card = Card(root);
                card.Add(UiKit.Label(T(def.NameKey), 36, true));
                card.Add(UiKit.Label(T(def.NameKey + ".desc"), 28, false, UiKit.Muted, true));
                card.Add(B.Text(UiKit.Label("", 30), () => TF("research.level", s.Research.Level(def.Id), def.MaxLevel)));
                var buy = UiKit.Button("", () => { if (s.Research.TryBuy(def.Id)) C.Audio?.Play(Sfx.Upgrade); });
                B.Button(buy, () =>
                {
                    int lv = s.Research.Level(def.Id);
                    if (lv >= def.MaxLevel) return T("ui.max_level");
                    if (!s.Research.IsUnlocked(def)) return TF("research.requires", def.RequiredCompanyLevel);
                    return TF("research.buy", M(s.Research.Cost(def, lv)));
                }, () =>
                {
                    int lv = s.Research.Level(def.Id);
                    return lv < def.MaxLevel && s.Research.IsUnlocked(def) && s.Wallet.CanAfford(s.Research.Cost(def, lv));
                });
                card.Add(buy);
            }
        }
    }

    // ============================================================== Daily rewards
    public sealed class DailyScreen : UiScreen
    {
        public override string Id => "daily";
        public override string TitleKey => "screen.daily";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            root.Add(UiKit.Label(T("daily.intro"), 28, false, UiKit.Muted, true));
            var grid = UiKit.Row();
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.Center;
            for (int i = 0; i < DailyRewardService.Cycle.Length; i++)
            {
                var r = DailyRewardService.Cycle[i];
                bool isNext = i == s.DailyRewards.NextIndex;
                var tile = UiKit.Box(isNext ? UiKit.CardHi : UiKit.Card, 20, 14, 14);
                tile.style.width = 300;
                tile.style.minHeight = 190;
                UiKit.Margin(tile, 8, 8);
                if (isNext) { tile.style.borderTopWidth = tile.style.borderBottomWidth = tile.style.borderLeftWidth = tile.style.borderRightWidth = 4; tile.style.borderTopColor = tile.style.borderBottomColor = tile.style.borderLeftColor = tile.style.borderRightColor = UiKit.Accent; }
                tile.Add(UiKit.Label(TF("daily.day", i + 1), 30, true));
                if (r.Diamonds > 0) tile.Add(UiKit.Label("◆ " + r.Diamonds, 34, true, UiKit.Diamond));
                if (r.CashSeconds > 0) tile.Add(UiKit.Label(TF("daily.cash", NumberFormatter.FormatDuration(r.CashSeconds)), 26, false, UiKit.Good, true));
                if (r.BoostMinutes > 0) tile.Add(UiKit.Label(TF("daily.boost", r.BoostMinutes), 26, false, UiKit.Good, true));
                if (r.DecorId != null) tile.Add(UiKit.Label(T("daily.decor"), 26, false, UiKit.Warn, true));
                grid.Add(tile);
            }
            root.Add(grid);
            var claim = UiKit.Button("", () => { if (s.DailyRewards.TryClaim()) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good, 38, 120);
            B.Button(claim, () => s.DailyRewards.CanClaim ? T("ui.claim") : T("daily.come_back"), () => s.DailyRewards.CanClaim);
            root.Add(claim);
        }
    }

    // ============================================================== Achievements & collection
    public sealed class AchievementsScreen : UiScreen
    {
        public override string Id => "achievements";
        public override string TitleKey => "screen.achievements";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            if (C.GameCenter != null && C.GameCenter.Authenticated)
            {
                var gc = UiKit.Row();
                gc.Add(UiKit.Button(T("gc.achievements"), C.GameCenter.ShowAchievements, UiKit.CardHi, 28, 90));
                gc.Add(UiKit.Button(T("gc.leaderboards"), C.GameCenter.ShowLeaderboards, UiKit.CardHi, 28, 90));
                root.Add(gc);
            }
            foreach (var a in AchievementService.Defs)
            {
                var def = a;
                var card = Card(root);
                card.Add(UiKit.Label(T(def.NameKey), 34, true));
                card.Add(UiKit.Label(T(def.NameKey + ".desc"), 28, false, UiKit.Muted, true));
                var bar = UiKit.ProgressBar(out var fill, UiKit.Warn);
                B.Progress(fill, () => s.Achievements.Progress01(def));
                card.Add(bar);
                var claim = UiKit.Button("", () => { if (s.Achievements.TryClaim(def.Id)) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good);
                B.Button(claim, () => s.State.AchievementsClaimed.Contains(def.Id) ? T("ui.claimed") : TF("achievements.claim", def.RewardDiamonds),
                    () => s.State.AchievementsUnlocked.Contains(def.Id) && !s.State.AchievementsClaimed.Contains(def.Id));
                card.Add(claim);
            }

            Section(root, "collection.title");
            var badges = s.State.Prestige.Badges.Concat(s.State.Live.EventBadges).Concat(s.State.Collection.Where(c => CosmeticService.Get(c) == null)).ToList();
            root.Add(UiKit.Label(TF("badges.bonus", s.Badges.Count, Mathf.RoundToInt((float)(s.Badges.Bonus * 100))), 28, true, UiKit.Warn, true));
            if (badges.Count == 0) root.Add(UiKit.Label(T("collection.empty"), 28, false, UiKit.Muted, true));
            foreach (var id in badges)
            {
                var card = Card(root);
                card.Add(UiKit.Label(T("decor." + DecorKey(id)), 30, true, UiKit.Warn));
            }
        }

        /// <summary>Numbered ids share one translation (badge_prestige_3 -> badge_prestige).</summary>
        public static string DecorKey(string id)
        {
            int last = id.LastIndexOf('_');
            return last > 0 && int.TryParse(id.Substring(last + 1), out _) ? id.Substring(0, last) : id;
        }
    }

    // ============================================================== Events
    public sealed class EventsScreen : UiScreen
    {
        public override string Id => "events";
        public override string TitleKey => "screen.events";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            if (s.EventFactory.IsActive)
            {
                var ev = s.EventFactory.Current;
                var evDef = s.Content.GetFactory(ev.FactoryId);
                var head = Card(root);
                head.Add(UiKit.Label("★ " + T(evDef.NameKey), 42, true, UiKit.Diamond));
                head.Add(UiKit.Label(T("event.factory_intro"), 26, false, UiKit.Muted, true));
                head.Add(B.Text(UiKit.Label("", 30, true), () => TF("event.tokens", s.EventFactory.Tokens)));
                if (s.State.ActiveFactoryId != ev.FactoryId)
                    head.Add(UiKit.Button(T("map.visit"), () => { s.Factories.SetActiveFactory(ev.FactoryId); C.Ui.Close(); }, UiKit.Diamond));
                if (C.GameCenter != null && C.GameCenter.Authenticated)
                    head.Add(UiKit.Button(T("gc.leaderboards"), C.GameCenter.ShowLeaderboards, UiKit.CardHi, 26, 84));
                for (int i = 0; i < EventFactoryService.Tiers.Length; i++)
                {
                    int tier = i;
                    var t = EventFactoryService.Tiers[i];
                    var card = Card(root);
                    card.Add(UiKit.Label(TF("event.tier", t.tokens) + $"  ◆ {t.diamonds}" + (t.crate != null ? " + " + T("crate." + t.crate.Substring(6)) : "")
                                         + (tier == EventFactoryService.Tiers.Length - 1 ? " + " + T("event.badge") : ""), 28, true, null, true));
                    var bar = UiKit.ProgressBar(out var fill, UiKit.Diamond);
                    B.Progress(fill, () => s.EventFactory.Tokens / (double)t.tokens);
                    card.Add(bar);
                    var claim = UiKit.Button("", () => { if (s.EventFactory.TryClaim(tier)) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good, 28, 84);
                    B.Button(claim, () => ev.ClaimedTiers.Contains(tier) ? T("ui.claimed") : T("ui.claim"),
                        () => s.EventFactory.IsTierReached(tier) && !ev.ClaimedTiers.Contains(tier));
                    card.Add(claim);
                }
            }
            var active = s.Events.Active();
            if (active.Count == 0)
            {
                root.Add(UiKit.Label(T("events.none"), 30, false, UiKit.Muted, true));
                var next = s.Events.WeekendEvent(s.Time.TrustedNow);
                double wait = (next.StartUtc - s.Time.TrustedNow).TotalSeconds;
                if (wait < 0) wait += 7 * 86400;
                root.Add(UiKit.Label(TF("events.next_in", NumberFormatter.FormatDuration(wait)), 30, true));
                return;
            }
            foreach (var e in active)
            {
                var ev = e;
                var card = Card(root);
                string evName = ev.NameKey != null ? T(ev.NameKey) : (C.Loc.Current == Core.Localization.Language.Turkish ? ev.NameTr : ev.NameEn);
                card.Add(UiKit.Label(evName, 40, true, UiKit.Diamond));
                if (ev.NameKey != null) card.Add(UiKit.Label(T(ev.NameKey + ".desc"), 28, false, UiKit.Muted, true));
                var p = s.Events.Progress(ev);
                var bar = UiKit.ProgressBar(out var fill, UiKit.Diamond, 24);
                B.Progress(fill, () => s.Events.Value(ev) / Math.Max(1, p.Target));
                card.Add(bar);
                card.Add(B.Text(UiKit.Label("", 28), () =>
                    TF("events.progress", (long)Math.Min(p.Target, s.Events.Value(ev)), (long)p.Target,
                       NumberFormatter.FormatDuration((ev.EndUtc - s.Time.TrustedNow).TotalSeconds))));
                long dia = ev.RewardDiamonds * (s.Seasons.HasPremium ? 2 : 1);
                card.Add(UiKit.Label(TF("events.reward", dia, T("decor." + ev.RewardDecorId)), 28, false, UiKit.Warn, true));
                var claim = UiKit.Button("", () => { if (s.Events.TryClaim(ev)) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good);
                B.Button(claim, () => p.Claimed ? T("ui.claimed") : T("ui.claim"), () => !p.Claimed && s.Events.Value(ev) >= p.Target);
                card.Add(claim);
            }
        }
    }

    // ============================================================== Prestige
    public sealed class PrestigeScreen : UiScreen
    {
        public override string Id => "prestige";
        public override string TitleKey => "screen.prestige";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            var pv = s.Prestige.Preview();
            root.Add(UiKit.Label(T("prestige.intro"), 30, false, UiKit.Muted, true));
            var card = Card(root);
            card.Add(UiKit.Label(TF("prestige.crowns_now", s.State.Prestige.Crowns, Math.Round((pv.MultiplierNow - 1) * 100)), 32, true));
            card.Add(UiKit.Label(TF("prestige.run", M(pv.RunEarnings), M(pv.RequiredRunEarnings)), 28, false, UiKit.Muted, true));
            card.Add(UiKit.Label(TF("prestige.gain", pv.CrownsGained, Math.Round((pv.MultiplierAfter - 1) * 100)), 34, true, UiKit.Warn, true));

            Section(root, "prestige.you_get");
            foreach (var k in pv.Bonuses) root.Add(UiKit.Label("+ " + TF(k, M(pv.StartingCashAfter)), 28, false, UiKit.Good, true));
            root.Add(UiKit.Label("+ " + T("decor." + AchievementsScreen.DecorKey(pv.BadgeId)) + ", " + T("decor." + AchievementsScreen.DecorKey(pv.DecorId)), 28, false, UiKit.Good, true));
            Section(root, "prestige.you_keep");
            foreach (var k in pv.Keeps) root.Add(UiKit.Label("✓ " + T(k), 28, false, UiKit.Text, true));
            Section(root, "prestige.you_reset");
            foreach (var k in pv.Resets) root.Add(UiKit.Label("↺ " + T(k), 28, false, UiKit.Warn, true));

            var go = UiKit.Button(T("prestige.confirm"), () =>
            {
                C.Ui.ShowModal((box, close) =>
                {
                    box.Add(UiKit.Label(T("prestige.are_you_sure"), 34, true, null, true));
                    box.Add(UiKit.Button(T("prestige.do_it"), () =>
                    {
                        close();
                        if (s.Prestige.TryPrestige())
                        {
                            C.Audio?.Play(Sfx.Success);
                            C.Save.SaveNow();
                            C.Ui.Close();
                            C.FactoryView.Build(s.State.ActiveFactoryId);
                            C.Ui.Toast(T("prestige.done"), UiKit.Good);
                        }
                    }, UiKit.Accent));
                    box.Add(UiKit.Button(T("ui.cancel"), close, UiKit.CardHi));
                });
            }, UiKit.Accent, 38, 120);
            UiKit.SetEnabled(go, pv.Available);
            root.Add(go);
        }
    }

    // ============================================================== More (secondary navigation)
    public sealed class MoreScreen : UiScreen
    {
        public override string Id => "more";
        public override string TitleKey => "screen.more";

        protected override void Build(VisualElement root)
        {
            (string key, string id)[] items =
            {
                ("screen.season", "season"), ("screen.cards", "cards"), ("screen.expeditions", "expeditions"),
                ("screen.coop", "coop"), ("screen.club", "club"), ("screen.engineering", "engineering"),
                ("screen.cosmetics", "cosmetics"), ("screen.stats", "stats"), ("menu.help", "help"),
                ("screen.orders", "orders"), ("screen.research", "research"), ("screen.warehouse", "warehouse"),
                ("screen.daily", "daily"), ("screen.achievements", "achievements"), ("screen.events", "events"),
                ("screen.prestige", "prestige"), ("screen.settings", "settings"),
            };
            foreach (var (key, id) in items)
            {
                var screenId = id;
                root.Add(UiKit.Button(T(key), () => C.Ui.Open(screenId), UiKit.CardHi, 36, 120));
            }
        }
    }
}
