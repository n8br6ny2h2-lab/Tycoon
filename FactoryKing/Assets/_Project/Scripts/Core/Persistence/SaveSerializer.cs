using System;
using System.Collections.Generic;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Persistence
{
    /// <summary>
    /// GameState &lt;-&gt; JSON. Every read is tolerant: a missing or malformed field falls back to its default, so old
    /// saves (and partially damaged ones) still load. Version migrations live in <see cref="Migrate"/>.
    /// </summary>
    public static class SaveSerializer
    {
        public static JsonValue ToJson(GameState s)
        {
            var o = JsonValue.NewObject();
            o.Set("version", GameState.CurrentVersion);
            o.Set("cash", s.Cash.ToInvariantString());
            o.Set("debt", s.Debt.ToInvariantString());
            o.Set("diamonds", s.Diamonds);
            o.Set("lifetime", s.LifetimeEarnings.ToInvariantString());
            o.Set("run", s.RunEarnings.ToInvariantString());
            o.Set("invested", s.TotalInvested.ToInvariantString());
            o.Set("playTime", s.PlayTimeSeconds);
            o.Set("sold", s.TotalUnitsSold);
            o.Set("active", s.ActiveFactoryId);
            o.Set("tutorial", s.TutorialStep);
            o.Set("boostEnd", s.BoostEndUtcTicks.ToString());

            var factories = JsonValue.NewArray();
            foreach (var f in s.Factories)
            {
                var fo = JsonValue.NewObject().Set("id", f.FactoryId).Set("unlocked", f.Unlocked);
                var lines = JsonValue.NewArray();
                foreach (var l in f.Lines)
                {
                    var machines = JsonValue.NewArray();
                    foreach (var m in l.Machines)
                    {
                        machines.Add(JsonValue.NewObject()
                            .Set("lv", m.Level).Set("in", m.InCycle).Set("p", m.Progress)
                            .Set("ip", m.InProcess).Set("ob", m.OutputBuffer).Set("cc", m.CyclesCompleted));
                    }
                    lines.Add(JsonValue.NewObject().Set("unlocked", l.Unlocked).Set("machines", machines));
                }
                fo.Set("lines", lines);
                factories.Add(fo);
            }
            o.Set("factories", factories);
            o.Set("produced", LongMap(s.UnitsProduced));

            var st = s.Stats;
            o.Set("stats", JsonValue.NewObject()
                .Set("produced", st.UnitsProduced).Set("sold", st.UnitsSold).Set("upgrades", st.Upgrades)
                .Set("orders", st.OrdersCompleted).Set("mgrUps", st.ManagerLevelUps).Set("ads", st.AdsWatched)
                .Set("factories", st.FactoriesBought).Set("lines", st.LinesBought).Set("research", st.ResearchBought)
                .Set("daily", st.DailyRewardsClaimed).Set("highest", st.HighestMachineLevel).Set("sessions", st.SessionCount)
                .Set("soldBy", LongMap(st.SoldByProduct)));

            var se = s.Settings;
            o.Set("settings", JsonValue.NewObject()
                .Set("music", se.MusicVolume).Set("sfx", se.SfxVolume).Set("notif", se.Notifications)
                .Set("analytics", se.AnalyticsConsent).Set("asked", se.ConsentAsked).Set("lang", se.Language).Set("hq", se.HighQuality));

            // Ticks exceed double precision: store as strings.
            o.Set("time", JsonValue.NewObject()
                .Set("saved", s.Time.LastSavedUtcTicks.ToString()).Set("max", s.Time.MaxSeenUtcTicks.ToString())
                .Set("first", s.Time.FirstLaunchUtcTicks.ToString()).Set("rollbacks", s.Time.ClockRollbacksDetected));

            var managers = JsonValue.NewArray();
            foreach (var m in s.Managers)
                managers.Add(JsonValue.NewObject().Set("id", m.Id).Set("lv", m.Level).Set("at", m.AssignedFactoryId));
            o.Set("managers", managers);

            o.Set("prestige", JsonValue.NewObject().Set("count", s.Prestige.Count).Set("crowns", s.Prestige.Crowns)
                .Set("badges", StrArray(s.Prestige.Badges)));

            var research = JsonValue.NewObject();
            foreach (var kv in s.Research) research.Set(kv.Key, kv.Value);
            o.Set("research", research);

            o.Set("missions", JsonValue.NewObject()
                .Set("day", s.Missions.DailyDay).Set("week", s.Missions.WeeklyWeek)
                .Set("daily", Missions(s.Missions.Daily)).Set("weekly", Missions(s.Missions.Weekly)));

            var orders = JsonValue.NewArray();
            foreach (var r in s.Orders)
                orders.Add(JsonValue.NewObject().Set("id", r.Id).Set("f", r.FactoryId).Set("p", r.ProductId).Set("q", r.Quantity)
                    .Set("b", r.Baseline).Set("m", r.RewardMultiplier).Set("exp", r.ExpiresUtcTicks.ToString()).Set("c", r.Claimed));
            o.Set("orders", orders);

            o.Set("achUnlocked", StrArray(s.AchievementsUnlocked));
            o.Set("achClaimed", StrArray(s.AchievementsClaimed));
            o.Set("dailyReward", JsonValue.NewObject().Set("last", s.Daily.LastClaimDay).Set("next", s.Daily.NextIndex));
            o.Set("entitlements", StrArray(s.Entitlements));
            o.Set("transactions", StrArray(s.ProcessedTransactions));
            o.Set("collection", StrArray(s.Collection));

            var events = JsonValue.NewObject();
            foreach (var kv in s.Events)
                events.Set(kv.Key, JsonValue.NewObject().Set("b", kv.Value.Baseline).Set("t", kv.Value.Target).Set("c", kv.Value.Claimed));
            o.Set("events", events);

            var shown = JsonValue.NewObject();
            foreach (var kv in s.Ads.ShownToday) shown.Set(kv.Key, kv.Value);
            var last = JsonValue.NewObject();
            foreach (var kv in s.Ads.LastShownUtcTicks) last.Set(kv.Key, kv.Value.ToString());
            o.Set("ads", JsonValue.NewObject().Set("day", s.Ads.Day).Set("shown", shown).Set("last", last));
            return o;
        }

        public static GameState FromJson(JsonValue o)
        {
            if (o == null || o.Type != JsonType.Object) throw new FormatException("Save root must be an object.");
            Migrate(o);
            var s = new GameState
            {
                Cash = Big(o.Str("cash")),
                Debt = Big(o.Str("debt")),
                Diamonds = o.Lng("diamonds"),
                LifetimeEarnings = Big(o.Str("lifetime")),
                RunEarnings = Big(o.Str("run", o.Str("lifetime"))),
                TotalInvested = Big(o.Str("invested")),
                PlayTimeSeconds = o.Dbl("playTime"),
                TotalUnitsSold = o.Lng("sold"),
                ActiveFactoryId = o.Str("active"),
                TutorialStep = o.Int("tutorial"),
                BoostEndUtcTicks = Ticks(o.Str("boostEnd")),
            };

            foreach (var fo in o.Arr("factories").Items)
            {
                if (fo.Type != JsonType.Object) continue;
                var f = new FactoryState { FactoryId = fo.Str("id"), Unlocked = fo.Bool("unlocked") };
                foreach (var lo in fo.Arr("lines").Items)
                {
                    var l = new LineState { Unlocked = lo.Bool("unlocked") };
                    foreach (var mo in lo.Arr("machines").Items)
                    {
                        l.Machines.Add(new MachineState
                        {
                            Level = mo.Int("lv", 1), InCycle = mo.Bool("in"), Progress = mo.Dbl("p"),
                            InProcess = mo.Int("ip"), OutputBuffer = mo.Int("ob"), CyclesCompleted = mo.Lng("cc"),
                        });
                    }
                    f.Lines.Add(l);
                }
                s.Factories.Add(f);
            }
            ReadLongMap(o.Obj("produced"), s.UnitsProduced);

            var st = o.Obj("stats");
            s.Stats.UnitsProduced = st.Lng("produced");
            s.Stats.UnitsSold = st.Lng("sold", s.TotalUnitsSold);
            s.Stats.Upgrades = st.Lng("upgrades");
            s.Stats.OrdersCompleted = st.Lng("orders");
            s.Stats.ManagerLevelUps = st.Lng("mgrUps");
            s.Stats.AdsWatched = st.Lng("ads");
            s.Stats.FactoriesBought = st.Lng("factories");
            s.Stats.LinesBought = st.Lng("lines");
            s.Stats.ResearchBought = st.Lng("research");
            s.Stats.DailyRewardsClaimed = st.Lng("daily");
            s.Stats.HighestMachineLevel = st.Int("highest", 1);
            s.Stats.SessionCount = st.Dbl("sessions");
            ReadLongMap(st.Obj("soldBy"), s.Stats.SoldByProduct);

            var se = o.Obj("settings");
            s.Settings.MusicVolume = Clamp01((float)se.Dbl("music", 0.6));
            s.Settings.SfxVolume = Clamp01((float)se.Dbl("sfx", 0.8));
            s.Settings.Notifications = se.Bool("notif");
            s.Settings.AnalyticsConsent = se.Bool("analytics");
            s.Settings.ConsentAsked = se.Bool("asked");
            s.Settings.Language = se.Int("lang", -1);
            s.Settings.HighQuality = se.Bool("hq", true);

            var t = o.Obj("time");
            s.Time.LastSavedUtcTicks = Ticks(t.Str("saved"));
            s.Time.MaxSeenUtcTicks = Ticks(t.Str("max"));
            s.Time.FirstLaunchUtcTicks = Ticks(t.Str("first"));
            s.Time.ClockRollbacksDetected = t.Int("rollbacks");

            foreach (var mo in o.Arr("managers").Items)
                s.Managers.Add(new ManagerState { Id = mo.Str("id"), Level = mo.Int("lv", 1), AssignedFactoryId = mo.Str("at") });

            var p = o.Obj("prestige");
            s.Prestige.Count = p.Int("count");
            s.Prestige.Crowns = p.Dbl("crowns");
            foreach (var b in p.Arr("badges").Items) if (b.AsString() != null) s.Prestige.Badges.Add(b.AsString());

            var r = o.Obj("research");
            foreach (var k in r.Keys) s.Research[k] = Math.Max(0, r.Int(k));

            var mi = o.Obj("missions");
            s.Missions.DailyDay = mi.Lng("day", -1);
            s.Missions.WeeklyWeek = mi.Lng("week", -1);
            ReadMissions(mi.Arr("daily"), s.Missions.Daily);
            ReadMissions(mi.Arr("weekly"), s.Missions.Weekly);

            foreach (var ro in o.Arr("orders").Items)
                s.Orders.Add(new OrderState
                {
                    Id = ro.Str("id"), FactoryId = ro.Str("f"), ProductId = ro.Str("p"), Quantity = ro.Lng("q"),
                    Baseline = ro.Lng("b"), RewardMultiplier = ro.Dbl("m", 1), ExpiresUtcTicks = Ticks(ro.Str("exp")), Claimed = ro.Bool("c"),
                });

            ReadSet(o.Arr("achUnlocked"), s.AchievementsUnlocked);
            ReadSet(o.Arr("achClaimed"), s.AchievementsClaimed);
            var d = o.Obj("dailyReward");
            s.Daily.LastClaimDay = d.Lng("last", -1);
            s.Daily.NextIndex = Math.Max(0, d.Int("next"));
            ReadSet(o.Arr("entitlements"), s.Entitlements);
            ReadSet(o.Arr("transactions"), s.ProcessedTransactions);
            ReadSet(o.Arr("collection"), s.Collection);

            var ev = o.Obj("events");
            foreach (var k in ev.Keys)
            {
                var e = ev.Get(k);
                s.Events[k] = new EventProgress { EventId = k, Baseline = e.Dbl("b"), Target = e.Dbl("t"), Claimed = e.Bool("c") };
            }

            var ads = o.Obj("ads");
            s.Ads.Day = ads.Lng("day", -1);
            var shown = ads.Obj("shown");
            foreach (var k in shown.Keys) s.Ads.ShownToday[k] = shown.Int(k);
            var last = ads.Obj("last");
            foreach (var k in last.Keys) s.Ads.LastShownUtcTicks[k] = Ticks(last.Str(k));
            return s;
        }

        /// <summary>Upgrades older save layouts in place before reading.</summary>
        private static void Migrate(JsonValue o)
        {
            int version = o.Int("version", 1);
            if (version < 2)
            {
                // v1 had no run earnings: treat lifetime as the current run.
                if (!o.Has("run")) o.Set("run", o.Str("lifetime", "0"));
            }
            if (version > GameState.CurrentVersion)
                throw new FormatException($"Save version {version} is newer than this build ({GameState.CurrentVersion}).");
        }

        // ---------- helpers ----------
        private static BigDouble Big(string s) => BigDouble.TryParse(s, out var v) ? v : BigDouble.Zero;
        private static long Ticks(string s) => long.TryParse(s, out var v) && v >= 0 ? v : 0;
        private static float Clamp01(float f) => float.IsNaN(f) ? 0 : Math.Max(0, Math.Min(1, f));

        private static JsonValue LongMap(Dictionary<string, long> map)
        {
            var o = JsonValue.NewObject();
            foreach (var kv in map) o.Set(kv.Key, kv.Value);
            return o;
        }

        private static void ReadLongMap(JsonValue o, Dictionary<string, long> into)
        {
            foreach (var k in o.Keys) into[k] = Math.Max(0, o.Lng(k));
        }

        private static JsonValue StrArray(IEnumerable<string> items)
        {
            var a = JsonValue.NewArray();
            foreach (var i in items) a.Add(JsonValue.From(i));
            return a;
        }

        private static void ReadSet(JsonValue a, ICollection<string> into)
        {
            foreach (var v in a.Items) if (v.AsString() != null) into.Add(v.AsString());
        }

        private static JsonValue Missions(List<MissionState> list)
        {
            var a = JsonValue.NewArray();
            foreach (var m in list)
                a.Add(JsonValue.NewObject().Set("id", m.Id).Set("type", m.Type).Set("target", m.Target).Set("base", m.Baseline)
                    .Set("dia", m.RewardDiamonds).Set("cashSec", m.RewardCashSeconds).Set("cash", m.RewardCash).Set("claimed", m.Claimed));
            return a;
        }

        private static void ReadMissions(JsonValue a, List<MissionState> into)
        {
            foreach (var m in a.Items)
                into.Add(new MissionState
                {
                    Id = m.Str("id"), Type = m.Str("type"), Target = m.Dbl("target"), Baseline = m.Dbl("base"),
                    RewardDiamonds = m.Lng("dia"), RewardCashSeconds = m.Dbl("cashSec"), RewardCash = m.Dbl("cash"), Claimed = m.Bool("claimed"),
                });
        }
    }
}
