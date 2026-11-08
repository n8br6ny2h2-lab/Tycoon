using System;
using System.Collections.Generic;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    public enum ProductKind { Consumable, NonConsumable }

    public sealed class StoreProductDef
    {
        /// <summary>Internal id (stable, used in saves and analytics).</summary>
        public string Id;
        /// <summary>App Store Connect product id. Configurable: must match what you create in App Store Connect.</summary>
        public string StoreId;
        public ProductKind Kind;
        public string NameKey;
        public string DescriptionKey;
        public long Diamonds;
        public string Entitlement;
        public double IncomeSeconds;
        public string ManagerId;
        public string[] DecorIds = Array.Empty<string>();
        /// <summary>Cosmetics added to the permanent collection.</summary>
        public string[] CosmeticIds = Array.Empty<string>();
        /// <summary>Activates the premium track of the current season (re-purchasable each season).</summary>
        public bool SeasonPass;
        /// <summary>Suggested App Store price tier in USD (display fallback only; real prices come from StoreKit).</summary>
        public string FallbackPrice;
    }

    /// <summary>Catalogue of real-money products. Store ids use a configurable prefix (your bundle id).</summary>
    public sealed class StoreCatalog
    {
        public const string EntitlementNoAds = "no_ads";
        public const string EntitlementVip = "vip";
        public const string EntitlementStarter = "starter_pack";
        public const string EntitlementCosmeticGold = "cosmetic_gold";
        public const string EntitlementCosmeticNeon = "cosmetic_neon";
        public const double VipOfflineHours = 2;
        public const double VipPriceBonus = 1.10;

        public readonly List<StoreProductDef> Products = new List<StoreProductDef>();

        public static StoreCatalog CreateDefault(string storeIdPrefix = "com.CHANGEME.factoryking.")
        {
            var c = new StoreCatalog();
            void Add(string id, ProductKind kind, string price, Action<StoreProductDef> setup)
            {
                var p = new StoreProductDef
                {
                    Id = id, StoreId = storeIdPrefix + id, Kind = kind, FallbackPrice = price,
                    NameKey = "shop." + id, DescriptionKey = "shop." + id + ".desc",
                };
                setup(p);
                c.Products.Add(p);
            }
            Add("diamonds_small", ProductKind.Consumable, "$0.99", p => p.Diamonds = 80);
            Add("diamonds_medium", ProductKind.Consumable, "$4.99", p => p.Diamonds = 500);
            Add("diamonds_large", ProductKind.Consumable, "$9.99", p => p.Diamonds = 1200);
            Add("no_ads", ProductKind.NonConsumable, "$3.99", p => p.Entitlement = EntitlementNoAds);
            Add("starter_pack", ProductKind.NonConsumable, "$1.99", p =>
            {
                p.Entitlement = EntitlementStarter; p.Diamonds = 300; p.IncomeSeconds = 7200; p.ManagerId = "mgr_ayla";
            });
            Add("vip", ProductKind.NonConsumable, "$7.99", p => p.Entitlement = EntitlementVip);
            Add("cosmetic_gold", ProductKind.NonConsumable, "$2.99", p =>
            {
                p.Entitlement = EntitlementCosmeticGold; p.DecorIds = new[] { "decor_gold_statue" };
                p.CosmeticIds = new[] { "floor_gold", "wall_gold", "sky_gold" };
            });
            Add("cosmetic_neon", ProductKind.NonConsumable, "$2.99", p =>
            {
                p.Entitlement = EntitlementCosmeticNeon; p.CosmeticIds = new[] { "floor_neon", "wall_neon", "sky_neon" };
            });
            // Bought once per season (consumable in App Store Connect; the save remembers it for the season).
            Add("season_pass", ProductKind.Consumable, "$4.99", p => { p.SeasonPass = true; p.Diamonds = 150; });
            return c;
        }

        public StoreProductDef ByStoreId(string storeId) => Products.Find(p => p.StoreId == storeId);
        public StoreProductDef ById(string id) => Products.Find(p => p.Id == id);
    }

    public enum PurchaseResult { Granted, AlreadyProcessed, AlreadyOwned, UnknownProduct, InvalidReceipt }

    /// <summary>
    /// Grants purchases exactly once. Every transaction id is recorded (in the save), so repeats, restores and
    /// re-deliveries never double-grant; owned non-consumables cannot be sold again; restore only re-grants
    /// non-consumable entitlements (never consumables).
    /// </summary>
    public sealed class PurchaseService
    {
        private readonly GameSession _s;

        public PurchaseService(GameSession session, StoreCatalog catalog)
        {
            _s = session;
            Catalog = catalog;
        }

        public StoreCatalog Catalog { get; }

        public event Action<StoreProductDef> Granted;

        public bool Owns(string entitlement) => _s.State.Entitlements.Contains(entitlement);

        /// <summary>False for non-consumables already owned (the shop hides/disables them).</summary>
        public bool CanPurchase(StoreProductDef p) =>
            p != null && !(p.Kind == ProductKind.NonConsumable && p.Entitlement != null && Owns(p.Entitlement))
                      && !(p.SeasonPass && _s.Seasons.HasPremium);

        public PurchaseResult Process(string storeId, string transactionId, bool receiptValid, bool isRestore)
        {
            var p = Catalog.ByStoreId(storeId);
            if (p == null) return PurchaseResult.UnknownProduct;
            if (!receiptValid) return PurchaseResult.InvalidReceipt;
            string tx = string.IsNullOrEmpty(transactionId) ? null : transactionId;
            if (tx != null && _s.State.ProcessedTransactions.Contains(tx)) return PurchaseResult.AlreadyProcessed;

            if (p.Kind == ProductKind.Consumable && isRestore)
            {
                if (tx != null) _s.State.ProcessedTransactions.Add(tx);
                return PurchaseResult.AlreadyProcessed; // consumables are never restored
            }
            if (p.Kind == ProductKind.NonConsumable && p.Entitlement != null && Owns(p.Entitlement))
            {
                if (tx != null) _s.State.ProcessedTransactions.Add(tx);
                return PurchaseResult.AlreadyOwned;
            }
            if (tx == null && p.Kind == ProductKind.Consumable) return PurchaseResult.InvalidReceipt; // must be traceable

            if (tx != null) _s.State.ProcessedTransactions.Add(tx);
            if (p.Entitlement != null) _s.State.Entitlements.Add(p.Entitlement);
            if (!isRestore || p.Kind == ProductKind.NonConsumable)
            {
                // Restoring a bundle re-grants its one-time contents only if it was never granted on this save.
                if (p.Diamonds > 0) _s.Wallet.AddDiamonds(p.Diamonds);
                if (p.IncomeSeconds > 0) _s.Wallet.Grant(_s.IncomeReward(p.IncomeSeconds));
                if (p.ManagerId != null) _s.Managers.Grant(p.ManagerId);
                foreach (var d in p.DecorIds) _s.State.Collection.Add(d);
            }
            foreach (var c in p.CosmeticIds) _s.Cosmetics.Grant(c); // restored too: cosmetics are permanent
            if (p.SeasonPass) _s.Seasons.ActivatePremium();
            ApplyEntitlements();
            _s.Analytics.Track(AnalyticsEvents.Purchase, "product", p.Id);
            Granted?.Invoke(p);
            return PurchaseResult.Granted;
        }

        public void ApplyEntitlements()
        {
            if (Owns(StoreCatalog.EntitlementVip)) _s.Modifiers.Set("vip", ModifierStat.SellPrice, StoreCatalog.VipPriceBonus);
            else _s.Modifiers.Remove("vip");
        }
    }

    public static class AdPlacements
    {
        public const string DoubleOffline = "double_offline";
        public const string Boost = "boost";
        public const string FreeDiamonds = "free_diamonds";
        public const string FreeCash = "free_cash";

        public static int DailyCap(string p) => p == DoubleOffline ? 20 : p == Boost ? 8 : p == FreeDiamonds ? 5 : 10;
        public static TimeSpan Cooldown(string p) =>
            p == FreeDiamonds ? TimeSpan.FromMinutes(10) : p == FreeCash ? TimeSpan.FromMinutes(5) : TimeSpan.Zero;
    }

    public enum AdResult { Completed, Skipped, Failed, NotReady, Capped }

    /// <summary>Platform ad SDK adapter (implemented in the Unity layer once an ad network and its ids are provided).</summary>
    public interface IAdsProvider
    {
        bool IsReady(string placement);
        void Show(string placement, Action<AdResult> done);
    }

    /// <summary>Always "not ready"; used until a real SDK is configured. Never shows fake ads.</summary>
    public sealed class NoAdsProvider : IAdsProvider
    {
        public bool IsReady(string placement) => false;
        public void Show(string placement, Action<AdResult> done) => done?.Invoke(AdResult.NotReady);
    }

    /// <summary>
    /// Opt-in rewarded ads only — there are no interstitial or forced ads anywhere in the game.
    /// Players who bought "no_ads" receive the reward instantly without watching.
    /// </summary>
    public sealed class RewardedAdService
    {
        private readonly GameSession _s;

        public RewardedAdService(GameSession session) => _s = session;

        public IAdsProvider Provider { get; set; } = new NoAdsProvider();

        private bool AdFree => _s.State.Entitlements.Contains(StoreCatalog.EntitlementNoAds);

        private void RollDay()
        {
            long day = _s.Time.TrustedDayIndex;
            if (_s.State.Ads.Day != day)
            {
                _s.State.Ads.Day = day;
                _s.State.Ads.ShownToday.Clear();
            }
        }

        public int RemainingToday(string placement)
        {
            RollDay();
            _s.State.Ads.ShownToday.TryGetValue(placement, out int n);
            return Math.Max(0, AdPlacements.DailyCap(placement) - n);
        }

        public double CooldownRemainingSeconds(string placement)
        {
            if (!_s.State.Ads.LastShownUtcTicks.TryGetValue(placement, out long last)) return 0;
            double elapsed = (_s.Time.TrustedNow.Ticks - last) / (double)TimeSpan.TicksPerSecond;
            return Math.Max(0, AdPlacements.Cooldown(placement).TotalSeconds - elapsed);
        }

        public bool IsAvailable(string placement) =>
            RemainingToday(placement) > 0 && CooldownRemainingSeconds(placement) <= 0 && (AdFree || Provider.IsReady(placement));

        /// <summary>Shows a rewarded ad and runs <paramref name="reward"/> only after it was fully watched.</summary>
        public void Request(string placement, Action reward, Action<AdResult> done = null)
        {
            if (RemainingToday(placement) <= 0 || CooldownRemainingSeconds(placement) > 0)
            {
                done?.Invoke(AdResult.Capped);
                return;
            }
            if (AdFree)
            {
                Record(placement, false);
                reward?.Invoke();
                done?.Invoke(AdResult.Completed);
                return;
            }
            if (!Provider.IsReady(placement))
            {
                done?.Invoke(AdResult.NotReady);
                return;
            }
            Provider.Show(placement, result =>
            {
                if (result == AdResult.Completed)
                {
                    Record(placement, true);
                    reward?.Invoke();
                }
                done?.Invoke(result);
            });
        }

        private void Record(string placement, bool watched)
        {
            RollDay();
            _s.State.Ads.ShownToday.TryGetValue(placement, out int n);
            _s.State.Ads.ShownToday[placement] = n + 1;
            _s.State.Ads.LastShownUtcTicks[placement] = _s.Time.TrustedNow.Ticks;
            if (watched)
            {
                _s.State.Stats.AdsWatched++;
                _s.Analytics.Track(AnalyticsEvents.AdWatched, "placement", placement);
            }
        }
    }

    public static class AnalyticsEvents
    {
        public const string AppOpen = "app_open";
        public const string TutorialComplete = "tutorial_complete";
        public const string FirstMachineUpgrade = "first_machine_upgrade";
        public const string FirstUpgrade = "first_upgrade";
        public const string FactoryUnlocked = "factory_unlocked";
        public const string Prestige = "prestige";
        public const string AdWatched = "ad_watched";
        public const string Purchase = "purchase";
        public const string SessionEnd = "session_end";
        public const string Error = "error";
        public const string ManagerHired = "manager_hired";
        public const string MissionClaimed = "mission_claimed";
    }

    public interface IAnalyticsSink
    {
        void Send(string name, IReadOnlyDictionary<string, string> parameters);
    }

    /// <summary>
    /// Privacy-first analytics: nothing is sent unless the player opted in (Settings), no personal data, no device
    /// identifiers and no cross-app tracking (so no ATT prompt is required). Sinks are pluggable.
    /// </summary>
    public sealed class AnalyticsService
    {
        private readonly GameSession _s;
        private readonly List<IAnalyticsSink> _sinks = new List<IAnalyticsSink>();

        public AnalyticsService(GameSession session) => _s = session;

        public int SentCount { get; private set; }
        public int DroppedCount { get; private set; }

        public void AddSink(IAnalyticsSink sink) { if (sink != null) _sinks.Add(sink); }

        public void Track(string name, string key = null, string value = null)
        {
            var p = new Dictionary<string, string>(StringComparer.Ordinal);
            if (key != null) p[key] = value ?? string.Empty;
            Track(name, p);
        }

        public void Track(string name, Dictionary<string, string> parameters)
        {
            if (_s.State?.Settings == null || !_s.State.Settings.AnalyticsConsent)
            {
                DroppedCount++;
                return;
            }
            parameters ??= new Dictionary<string, string>(StringComparer.Ordinal);
            parameters["session_play_minutes"] = ((int)(_s.State.PlayTimeSeconds / 60)).ToString();
            parameters["company_level"] = _s.CompanyLevel.ToString();
            foreach (var sink in _sinks)
            {
                try { sink.Send(name, parameters); }
                catch (Exception) { /* analytics must never break the game */ }
            }
            SentCount++;
        }
    }
}
