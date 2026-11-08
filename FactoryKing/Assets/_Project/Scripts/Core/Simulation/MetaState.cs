using System;
using System.Collections.Generic;

namespace FactoryKing.Core.Simulation
{
    /// <summary>Cumulative counters. Never reset (not even by prestige); missions/events measure deltas of these.</summary>
    [Serializable]
    public sealed class GameStats
    {
        public long UnitsProduced;
        public long UnitsSold;
        public long Upgrades;
        public long OrdersCompleted;
        public long ManagerLevelUps;
        public long AdsWatched;
        public long FactoriesBought;
        public long LinesBought;
        public long ResearchBought;
        public long DailyRewardsClaimed;
        public int HighestMachineLevel = 1;
        public long SeasonsCompleted;
        public long DronesCaught;
        public long ExpeditionsDone;
        public long CratesOpened;
        public long EventTokensBest;
        public double SessionCount;
        public Dictionary<string, long> SoldByProduct = new Dictionary<string, long>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class SettingsState
    {
        public float MusicVolume = 0.6f;
        public float SfxVolume = 0.8f;
        public bool Notifications;      // opt-in only
        public bool AnalyticsConsent;   // opt-in only (KVKK / App Privacy)
        public bool ConsentAsked;
        public int Language = -1;       // -1 = follow device
        public bool HighQuality = true;
        public bool Haptics = true;
        /// <summary>Battery saver: 30 FPS and no extra effects.</summary>
        public bool BatterySaver;
        public bool OnboardingDone;
        public bool ReviewRequested;
        public bool AutoUpgrade;
        /// <summary>0 = normal, 1 = large, 2 = extra large text.</summary>
        public int TextScale;
        public bool Colorblind;
        public bool ReduceMotion;
    }

    /// <summary>Wall-clock bookkeeping used by offline earnings and the anti clock-tampering guard.</summary>
    [Serializable]
    public sealed class TimeState
    {
        public long LastSavedUtcTicks;
        public long MaxSeenUtcTicks;
        public long FirstLaunchUtcTicks;
        public int ClockRollbacksDetected;
    }

    [Serializable]
    public sealed class ManagerState
    {
        public string Id;
        public int Level = 1;
        public string AssignedFactoryId;
        /// <summary>Active skill: running until / available again at (UTC ticks).</summary>
        public long AbilityEndUtcTicks;
        public long AbilityReadyUtcTicks;
    }

    [Serializable]
    public sealed class PrestigeState
    {
        public int Count;
        /// <summary>Crowns: permanent prestige currency; each gives a permanent income multiplier.</summary>
        public double Crowns;
        public List<string> Badges = new List<string>();
    }

    [Serializable]
    public sealed class MissionState
    {
        public string Id;
        public string Type;
        public double Target;
        public double Baseline;
        public long RewardDiamonds;
        public double RewardCashSeconds;
        /// <summary>Cash reward fixed when the mission is created (so it cannot be inflated before claiming).</summary>
        public double RewardCash;
        public bool Claimed;
    }

    [Serializable]
    public sealed class MissionBoard
    {
        public long DailyDay = -1;
        public long WeeklyWeek = -1;
        public List<MissionState> Daily = new List<MissionState>();
        public List<MissionState> Weekly = new List<MissionState>();
    }

    [Serializable]
    public sealed class OrderState
    {
        public string Id;
        public string FactoryId;
        public string ProductId;
        public long Quantity;
        public long Baseline;
        public double RewardMultiplier;
        public long ExpiresUtcTicks;
        public bool Claimed;
    }

    [Serializable]
    public sealed class DailyRewardState
    {
        public long LastClaimDay = -1;
        /// <summary>0-based day within the 7-day cycle of the next claim.</summary>
        public int NextIndex;
    }

    [Serializable]
    public sealed class AdState
    {
        public long Day = -1;
        public Dictionary<string, int> ShownToday = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, long> LastShownUtcTicks = new Dictionary<string, long>(StringComparer.Ordinal);
    }

    [Serializable]
    public sealed class SeasonState
    {
        public int Number = 1;
        public long StartUtcTicks;
        public int BestFactories = 1;
        public bool PremiumPass;
        /// <summary>Permanent: each star = +5% sale price in every future season.</summary>
        public int LegacyStars;
        public HashSet<int> ReachedTiers = new HashSet<int>();
        public HashSet<int> ClaimedTiers = new HashSet<int>();
        /// <summary>"seasonNumber:factoriesReached" entries.</summary>
        public List<string> History = new List<string>();
    }

    [Serializable]
    public sealed class ExpeditionState
    {
        public int Slot;
        public int Kind;            // 0 short, 1 medium, 2 long
        public long StartUtcTicks;
        public long EndUtcTicks;
        public double RewardCash;   // fixed at departure
        public int RewardCards;
        public long RewardDiamonds;
        public bool RewardCrate;
    }

    [Serializable]
    public sealed class EventFactoryState
    {
        public string FactoryId;
        public long StartUtcTicks;
        public long EndUtcTicks;
        public long BaselineSold;
        public double Scale = 1;
        public HashSet<int> ClaimedTiers = new HashSet<int>();
    }

    [Serializable]
    public sealed class LiveOpsState
    {
        public long DroneNextUtcTicks;
        public long DroneVisibleUntilUtcTicks;
        public int DroneSeed;
        public EventFactoryState EventFactory;
        public List<ExpeditionState> Expeditions = new List<ExpeditionState>();
        public Dictionary<string, int> ManagerCards = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> Crates = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> Engineering = new Dictionary<string, int>(StringComparer.Ordinal);
        public int SeasonXp;
        public HashSet<int> RoadClaimedFree = new HashSet<int>();
        public HashSet<int> RoadClaimedPremium = new HashSet<int>();
        public List<string> EventBadges = new List<string>();
        public long PiggyDiamonds;
        public long StarterOfferStartUtcTicks;
        public long SubscriptionExpiresUtcTicks;
        public long SubscriptionLastDailyDay = -1;
        public double AutoUpgradeTimer;
        // Online (optional server)
        public string CoopCode;
        public string CoopContractId;
        public long CoopBaselineSold;
        public HashSet<string> CoopRewardsClaimed = new HashSet<string>(StringComparer.Ordinal);
        public long WeekXpWeek = -1;
        public int WeekXp;
    }

    [Serializable]
    public sealed class EventProgress
    {
        public string EventId;
        public double Baseline;
        public double Target;
        public bool Claimed;
    }
}
