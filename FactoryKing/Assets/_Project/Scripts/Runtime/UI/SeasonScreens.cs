using System;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Numerics;
using FactoryKing.Runtime.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace FactoryKing.Runtime.UI
{
    // ============================================================== Season
    public sealed class SeasonScreen : UiScreen
    {
        public override string Id => "season";
        public override string TitleKey => "screen.season";

        private string RewardText(RoadReward r)
        {
            if (r.Cosmetic != null) return T(CosmeticService.Get(r.Cosmetic).NameKey) + (r.Crate != null ? " + " + T("crate." + r.Crate.Substring(6)) : "");
            if (r.Crate != null) return T("crate." + r.Crate.Substring(6));
            if (r.Cards > 0) return TF("road.cards", r.Cards);
            if (r.BoostMinutes > 0) return TF("daily.boost", r.BoostMinutes);
            return "◆ " + r.Diamonds;
        }

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            var head = Card(root);
            head.Add(UiKit.Label(TF("season.title", s.Seasons.Number), 44, true, UiKit.Accent));
            head.Add(B.Text(UiKit.Label("", 30), () => TF("season.time_left", NumberFormatter.FormatDuration(s.Seasons.SecondsLeft))));
            head.Add(B.Text(UiKit.Label("", 30), () => TF("season.factories", s.Seasons.OwnedFactories, s.Content.Factories.Count)));
            var bar = UiKit.ProgressBar(out var fill, UiKit.Accent, 24);
            B.Progress(fill, () => s.Seasons.OwnedFactories / (double)s.Content.Factories.Count);
            head.Add(bar);
            head.Add(B.Text(UiKit.Label("", 28, false, UiKit.Warn, true), () =>
                TF("season.legacy", s.Seasons.LegacyStars, Mathf.RoundToInt((float)(s.Seasons.LegacyStars * SeasonService.LegacyBonusPerStar * 100)))));
            head.Add(B.Text(UiKit.Label("", 28, false, UiKit.Good, true), () =>
                TF("season.momentum", Mathf.RoundToInt((float)((s.Seasons.Momentum - 1) * 100)))));
            head.Add(UiKit.Label(T("season.rules"), 26, false, UiKit.Muted, true));

            if (!s.Seasons.HasPremium)
            {
                var pass = s.Purchases.Catalog.ById("season_pass");
                var premium = Card(root);
                premium.Add(UiKit.Label(T("shop.season_pass"), 36, true, UiKit.Diamond));
                premium.Add(UiKit.Label(T("shop.season_pass.desc"), 28, false, UiKit.Muted, true));
                premium.Add(UiKit.Button(C.Store.PriceText(pass), () => C.Store.Buy(pass), UiKit.Diamond));
            }
            else
            {
                root.Add(UiKit.Label(T("season.premium_active"), 30, true, UiKit.Diamond));
            }

            Section(root, "road.title");
            root.Add(B.Text(UiKit.Label("", 30, true), () => TF("road.progress", s.Road.TiersReached, SeasonRoadService.TierCount, s.Road.Xp % SeasonRoadService.XpPerTier, SeasonRoadService.XpPerTier)));
            var roadBar = UiKit.ProgressBar(out var roadFill, UiKit.Diamond, 22);
            B.Progress(roadFill, () => s.Road.TiersReached / (double)SeasonRoadService.TierCount);
            root.Add(roadBar);
            root.Add(UiKit.Label(T("road.how"), 24, false, UiKit.Muted, true));
            for (int i = 0; i < SeasonRoadService.TierCount; i++)
            {
                int tier = i;
                var row = UiKit.Box(tier < s.Road.TiersReached ? UiKit.CardHi : UiKit.Card, 16, 8, 14);
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 6;
                var num = UiKit.Label((tier + 1).ToString(), 30, true, UiKit.Accent);
                num.style.width = 70;
                row.Add(num);
                Button Reward(RoadReward r, bool premium)
                {
                    var b = UiKit.Button(RewardText(r), () => { if (s.Road.TryClaim(tier, premium)) { C.Audio?.Play(Sfx.Success); Rebuild(); } },
                        premium ? UiKit.Diamond : UiKit.Good, 22, 80);
                    b.style.flexGrow = 1; b.style.flexBasis = 0;
                    bool claimed = premium ? s.State.Live.RoadClaimedPremium.Contains(tier) : s.State.Live.RoadClaimedFree.Contains(tier);
                    if (claimed) b.text = "✓ " + b.text;
                    UiKit.SetEnabled(b, premium ? s.Road.CanClaimPremium(tier) : s.Road.CanClaimFree(tier));
                    return b;
                }
                row.Add(Reward(SeasonRoadService.Free(tier), false));
                row.Add(Reward(SeasonRoadService.Premium(tier), true));
                root.Add(row);
            }

            Section(root, "season.rewards");
            for (int i = 0; i < SeasonService.Tiers.Length; i++)
            {
                int tier = i;
                var t = SeasonService.Tiers[i];
                var card = Card(root);
                var row = UiKit.Row(0, Justify.SpaceBetween);
                row.Add(UiKit.Label(TF("season.tier", t.Factories), 32, true));
                row.Add(UiKit.Label("◆ " + t.Diamonds * (s.Seasons.HasPremium ? 2 : 1), 32, true, UiKit.Diamond));
                card.Add(row);
                string extras = "";
                if (t.LegacyStars > 0) extras += "★ " + TF("season.stars", t.LegacyStars) + "   ";
                if (t.CosmeticId != null) extras += T(CosmeticService.Get(t.CosmeticId).NameKey) + "   ";
                if (t.PremiumCosmeticId != null) extras += "[" + T("season.premium") + "] " + T(CosmeticService.Get(t.PremiumCosmeticId).NameKey);
                if (extras.Length > 0) card.Add(UiKit.Label(extras, 26, false, UiKit.Warn, true));
                var claim = UiKit.Button("", () => { if (s.Seasons.TryClaim(tier)) { C.Audio?.Play(Sfx.Success); Rebuild(); } }, UiKit.Good, 30, 90);
                B.Button(claim, () => s.Seasons.IsClaimed(tier) ? T("ui.claimed") : s.Seasons.IsReached(tier) ? T("ui.claim") : T("ui.locked"),
                    () => s.Seasons.IsReached(tier) && !s.Seasons.IsClaimed(tier));
                card.Add(claim);
            }

            if (s.Seasons.CanEndEarly)
            {
                Section(root, "season.complete");
                root.Add(UiKit.Button(T("season.start_new"), () => C.Ui.ShowModal((box, close) =>
                {
                    box.Add(UiKit.Label(T("season.start_new_confirm"), 32, true, null, true));
                    box.Add(UiKit.Button(T("season.start_new"), () => { close(); C.Ui.Close(); s.Seasons.EndSeason(); }, UiKit.Accent));
                    box.Add(UiKit.Button(T("ui.cancel"), close, UiKit.CardHi));
                }), UiKit.Accent, 36, 120));
            }

            if (s.State.Season.History.Count > 0)
            {
                Section(root, "season.history");
                foreach (var h in s.State.Season.History)
                {
                    var parts = h.Split(':');
                    if (parts.Length == 2) root.Add(UiKit.Label(TF("season.history_row", parts[0], parts[1]), 28, false, UiKit.Muted));
                }
            }
        }
    }

    // ============================================================== Cosmetics
    public sealed class CosmeticsScreen : UiScreen
    {
        public override string Id => "cosmetics";
        public override string TitleKey => "screen.cosmetics";

        protected override void Build(VisualElement root)
        {
            var s = C.Session;
            root.Add(UiKit.Label(T("cosmetics.intro"), 28, false, UiKit.Muted, true));
            foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
            {
                Section(root, "cosmetics.slot." + slot);
                foreach (var def in CosmeticService.ForSlot(slot))
                {
                    var d = def;
                    var card = Card(root);
                    var row = UiKit.Row(0, Justify.SpaceBetween);
                    var swatch = new VisualElement();
                    swatch.style.width = 90; swatch.style.height = 90;
                    UiKit.Radius(swatch, 16);
                    swatch.style.backgroundColor = string.IsNullOrEmpty(d.ColorHex) ? UiKit.Muted : UiKit.Hex(d.ColorHex);
                    if (!string.IsNullOrEmpty(d.SecondaryHex))
                    {
                        swatch.style.borderBottomWidth = 22;
                        swatch.style.borderBottomColor = UiKit.Hex(d.SecondaryHex);
                    }
                    row.Add(swatch);
                    var info = new VisualElement();
                    info.style.flexGrow = 1;
                    info.style.marginLeft = 20;
                    info.Add(UiKit.Label(T(d.NameKey), 32, true, null, true));
                    info.Add(UiKit.Label(SourceText(d), 24, false, UiKit.Muted, true));
                    row.Add(info);
                    card.Add(row);

                    bool owned = s.Cosmetics.IsOwned(d.Id);
                    bool equipped = s.Cosmetics.Equipped(slot) == d.Id;
                    if (equipped)
                    {
                        var b = UiKit.Button(T("cosmetics.equipped"), null, UiKit.Good, 28, 84);
                        UiKit.SetEnabled(b, false);
                        card.Add(b);
                    }
                    else if (owned)
                    {
                        card.Add(UiKit.Button(T("cosmetics.equip"), () => { s.Cosmetics.Equip(d.Id); Click(); Rebuild(); }, UiKit.Accent, 28, 84));
                    }
                    else if (d.Source == CosmeticSource.Diamonds)
                    {
                        var buy = UiKit.Button(TF("cosmetics.buy", d.DiamondPrice), () =>
                        {
                            if (s.Cosmetics.TryBuy(d.Id)) { s.Cosmetics.Equip(d.Id); C.Audio?.Play(Sfx.Success); Rebuild(); }
                        }, UiKit.Diamond, 28, 84);
                        UiKit.SetEnabled(buy, s.Wallet.Diamonds >= d.DiamondPrice);
                        card.Add(buy);
                    }
                    else if (d.Source == CosmeticSource.Purchase)
                    {
                        var product = s.Purchases.Catalog.ById(d.SourceRef);
                        if (product != null) card.Add(UiKit.Button(T(product.NameKey) + " · " + C.Store.PriceText(product), () => C.Store.Buy(product), UiKit.Good, 26, 84));
                    }
                }
            }
        }

        private string SourceText(CosmeticDef d)
        {
            switch (d.Source)
            {
                case CosmeticSource.Default: return T("cosmetics.src.default");
                case CosmeticSource.Diamonds: return TF("cosmetics.src.diamonds", d.DiamondPrice);
                case CosmeticSource.Season: return T("cosmetics.src.season");
                case CosmeticSource.SeasonPremium: return T("cosmetics.src.season_premium");
                case CosmeticSource.Achievement: return TF("cosmetics.src.achievement", T("achievement." + d.SourceRef));
                default: return T("cosmetics.src.purchase");
            }
        }
    }

    public static class SeasonModals
    {
        public static void ShowSeasonSummary(this UiManager ui, int season, int reached)
        {
            var C = ui.Context;
            ui.Close();
            ui.ShowModal((box, close) =>
            {
                box.Add(UiKit.Label(C.TF("season.ended", season), 44, true, UiKit.Accent));
                box.Add(UiKit.Label(C.TF("season.ended_reached", reached), 32, false, null, true));
                box.Add(UiKit.Label(C.TF("season.legacy", C.Session.Seasons.LegacyStars,
                    Mathf.RoundToInt((float)(C.Session.Seasons.LegacyStars * SeasonService.LegacyBonusPerStar * 100))), 30, true, UiKit.Warn, true));
                box.Add(UiKit.Label(C.T("season.kept"), 26, false, UiKit.Muted, true));
                box.Add(UiKit.Button(C.TF("season.begin", C.Session.Seasons.Number), close, UiKit.Accent, 36, 120));
            }, dismissible: false);
        }
    }
}
