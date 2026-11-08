using System;
using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;

namespace FactoryKing.Core.Simulation
{
    public enum MachineStatus
    {
        /// <summary>No cycle running and nothing to do yet (e.g. line just unlocked).</summary>
        Idle,
        Working,
        /// <summary>Waiting for input from the previous stage.</summary>
        Starved,
        /// <summary>Output buffer is full; waiting for the next stage.</summary>
        Blocked,
    }

    [Serializable]
    public sealed class MachineState
    {
        public int Level = 1;
        public bool InCycle;
        /// <summary>0..1 progress of the current cycle.</summary>
        public double Progress;
        /// <summary>Units taken into the current cycle (raw purchased, inputs consumed, or truck load).</summary>
        public int InProcess;
        /// <summary>Finished units waiting for the next stage.</summary>
        public int OutputBuffer;
        public long CyclesCompleted;

        /// <summary>Runtime only; recomputed every tick.</summary>
        [NonSerialized] public MachineStatus Status;
    }

    [Serializable]
    public sealed class LineState
    {
        public bool Unlocked;
        public List<MachineState> Machines = new List<MachineState>();
    }

    [Serializable]
    public sealed class FactoryState
    {
        public string FactoryId;
        public bool Unlocked;
        /// <summary>Power plant level (1..MaxPowerLevel).</summary>
        public int PowerLevel = 1;
        public List<LineState> Lines = new List<LineState>();
    }

    /// <summary>Address of a single machine inside the world.</summary>
    public readonly struct MachineRef : IEquatable<MachineRef>
    {
        public readonly string FactoryId;
        public readonly int Line;
        public readonly int Index;

        public MachineRef(string factoryId, int line, int index)
        {
            FactoryId = factoryId;
            Line = line;
            Index = index;
        }

        public bool Equals(MachineRef other) => FactoryId == other.FactoryId && Line == other.Line && Index == other.Index;
        public override bool Equals(object obj) => obj is MachineRef other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(FactoryId, Line, Index);
        public override string ToString() => $"{FactoryId}/{Line}/{Index}";
    }

    /// <summary>Complete mutable game progress. Contains no logic beyond structure maintenance.</summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public BigDouble Cash;
        /// <summary>Operating costs that could not be paid yet; repaid automatically from income.</summary>
        public BigDouble Debt;
        public long Diamonds;
        public BigDouble LifetimeEarnings;
        public BigDouble TotalInvested;
        public double PlayTimeSeconds;
        public long TotalUnitsSold;
        public string ActiveFactoryId;
        public List<FactoryState> Factories = new List<FactoryState>();
        public Dictionary<string, long> UnitsProduced = new Dictionary<string, long>(StringComparer.Ordinal);

        /// <summary>Earnings since the last prestige (drives prestige crowns).</summary>
        public BigDouble RunEarnings;
        public GameStats Stats = new GameStats();
        public SettingsState Settings = new SettingsState();
        public TimeState Time = new TimeState();
        public List<ManagerState> Managers = new List<ManagerState>();
        public PrestigeState Prestige = new PrestigeState();
        public Dictionary<string, int> Research = new Dictionary<string, int>(StringComparer.Ordinal);
        public MissionBoard Missions = new MissionBoard();
        public List<OrderState> Orders = new List<OrderState>();
        public HashSet<string> AchievementsUnlocked = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> AchievementsClaimed = new HashSet<string>(StringComparer.Ordinal);
        public DailyRewardState Daily = new DailyRewardState();
        public HashSet<string> Entitlements = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> ProcessedTransactions = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Collection = new HashSet<string>(StringComparer.Ordinal);
        public Dictionary<string, EventProgress> Events = new Dictionary<string, EventProgress>(StringComparer.Ordinal);
        public AdState Ads = new AdState();
        public long BoostEndUtcTicks;
        public SeasonState Season = new SeasonState();
        /// <summary>Cosmetic slot name -> equipped cosmetic id.</summary>
        public Dictionary<string, string> EquippedCosmetics = new Dictionary<string, string>(StringComparer.Ordinal);
        public int TutorialStep;

        public static GameState CreateNew(GameContent content, EconomyTuning tuning)
        {
            var state = new GameState
            {
                Cash = tuning.StartingCash,
                Diamonds = tuning.StartingDiamonds,
            };
            state.EnsureConsistent(content);
            var first = state.Factories[0];
            first.Unlocked = true;
            first.Lines[0].Unlocked = true;
            state.ActiveFactoryId = first.FactoryId;
            return state;
        }

        /// <summary>
        /// Adds structure for content that did not exist when the state was created (new factories, lines or machines
        /// added in a game update), and trims anything that no longer exists. Safe to call repeatedly.
        /// </summary>
        public void EnsureConsistent(GameContent content)
        {
            var byId = new Dictionary<string, FactoryState>(StringComparer.Ordinal);
            foreach (var fs in Factories)
                if (fs?.FactoryId != null && !byId.ContainsKey(fs.FactoryId)) byId[fs.FactoryId] = fs;

            var ordered = new List<FactoryDef>(content.Factories);
            ordered.Sort((a, b) => a.Order.CompareTo(b.Order));

            var rebuilt = new List<FactoryState>();
            foreach (var def in ordered)
            {
                if (!byId.TryGetValue(def.Id, out var fs)) fs = new FactoryState { FactoryId = def.Id };
                fs.Lines ??= new List<LineState>();
                if (fs.PowerLevel < 1) fs.PowerLevel = 1;
                while (fs.Lines.Count < def.LineCount) fs.Lines.Add(new LineState());
                if (fs.Lines.Count > def.LineCount) fs.Lines.RemoveRange(def.LineCount, fs.Lines.Count - def.LineCount);
                foreach (var line in fs.Lines)
                {
                    line.Machines ??= new List<MachineState>();
                    while (line.Machines.Count < def.MachineChain.Count) line.Machines.Add(new MachineState());
                    if (line.Machines.Count > def.MachineChain.Count)
                        line.Machines.RemoveRange(def.MachineChain.Count, line.Machines.Count - def.MachineChain.Count);
                    foreach (var m in line.Machines)
                    {
                        if (m.Level < 1) m.Level = 1;
                        if (m.OutputBuffer < 0) m.OutputBuffer = 0;
                        if (m.InProcess < 0) m.InProcess = 0;
                        if (double.IsNaN(m.Progress) || m.Progress < 0 || m.Progress > 1) m.Progress = 0;
                    }
                }
                rebuilt.Add(fs);
            }
            Factories = rebuilt;
            UnitsProduced ??= new Dictionary<string, long>(StringComparer.Ordinal);
            Stats ??= new GameStats();
            Stats.SoldByProduct ??= new Dictionary<string, long>(StringComparer.Ordinal);
            Settings ??= new SettingsState();
            Time ??= new TimeState();
            Managers ??= new List<ManagerState>();
            Managers.RemoveAll(m => m == null || m.Id == null);
            foreach (var m in Managers) if (m.Level < 1) m.Level = 1;
            Prestige ??= new PrestigeState();
            Prestige.Badges ??= new List<string>();
            if (double.IsNaN(Prestige.Crowns) || Prestige.Crowns < 0) Prestige.Crowns = 0;
            Research ??= new Dictionary<string, int>(StringComparer.Ordinal);
            Missions ??= new MissionBoard();
            Missions.Daily ??= new List<MissionState>();
            Missions.Weekly ??= new List<MissionState>();
            Orders ??= new List<OrderState>();
            Orders.RemoveAll(o => o == null || o.FactoryId == null || content.Factories.TrueForAll(f => f.Id != o.FactoryId));
            AchievementsUnlocked ??= new HashSet<string>(StringComparer.Ordinal);
            AchievementsClaimed ??= new HashSet<string>(StringComparer.Ordinal);
            Daily ??= new DailyRewardState();
            Entitlements ??= new HashSet<string>(StringComparer.Ordinal);
            ProcessedTransactions ??= new HashSet<string>(StringComparer.Ordinal);
            Collection ??= new HashSet<string>(StringComparer.Ordinal);
            Events ??= new Dictionary<string, EventProgress>(StringComparer.Ordinal);
            Season ??= new SeasonState();
            Season.ReachedTiers ??= new HashSet<int>();
            Season.ClaimedTiers ??= new HashSet<int>();
            Season.History ??= new List<string>();
            if (Season.Number < 1) Season.Number = 1;
            if (Season.LegacyStars < 0) Season.LegacyStars = 0;
            EquippedCosmetics ??= new Dictionary<string, string>(StringComparer.Ordinal);
            Ads ??= new AdState();
            Ads.ShownToday ??= new Dictionary<string, int>(StringComparer.Ordinal);
            Ads.LastShownUtcTicks ??= new Dictionary<string, long>(StringComparer.Ordinal);
            if (Cash.IsNegative) Cash = BigDouble.Zero;
            if (Debt.IsNegative) Debt = BigDouble.Zero;
            if (Diamonds < 0) Diamonds = 0;
            if (Factories.Count > 0 && !Factories[0].Unlocked)
            {
                Factories[0].Unlocked = true;
                Factories[0].Lines[0].Unlocked = true;
            }
            foreach (var f in Factories) if (f.Unlocked && !f.Lines[0].Unlocked) f.Lines[0].Unlocked = true;
            if (ActiveFactoryId == null || !rebuilt.Exists(f => f.FactoryId == ActiveFactoryId))
                ActiveFactoryId = rebuilt.Count > 0 ? rebuilt[0].FactoryId : null;
        }

        public FactoryState GetFactory(string id)
        {
            foreach (var f in Factories) if (f.FactoryId == id) return f;
            throw new KeyNotFoundException($"Factory state '{id}' not found.");
        }

        public MachineState GetMachine(MachineRef r) => GetFactory(r.FactoryId).Lines[r.Line].Machines[r.Index];
    }
}
