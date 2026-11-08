using System;
using System.Collections.Generic;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Meta
{
    /// <summary>Parses the remote event schedule served by the backend (GET /v1/events).</summary>
    public static class RemoteEvents
    {
        public static List<LiveEventDef> Parse(string json)
        {
            var list = new List<LiveEventDef>();
            if (!JsonValue.TryParse(json, out var root) || root.Type != JsonType.Object) return list;
            foreach (var e in root.Arr("events").Items)
            {
                if (!DateTime.TryParse(e.Str("start"), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var start)) continue;
                if (!DateTime.TryParse(e.Str("end"), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var end)) continue;
                string id = e.Str("id");
                if (string.IsNullOrEmpty(id) || end <= start) continue;
                list.Add(new LiveEventDef
                {
                    Id = id, NameKey = e.Str("nameKey"), NameEn = e.Str("nameEn"), NameTr = e.Str("nameTr"),
                    StartUtc = DateTime.SpecifyKind(start, DateTimeKind.Utc), EndUtc = DateTime.SpecifyKind(end, DateTimeKind.Utc),
                    GoalType = e.Str("goal", MissionTypes.Sell) == "earn" ? MissionTypes.Earn : MissionTypes.Sell,
                    TargetMinutesOfOutput = Math.Max(10, e.Dbl("targetMinutesOfOutput", 180)),
                    RewardDiamonds = Math.Max(0, Math.Min(1000, e.Lng("rewardDiamonds", 50))),
                    RewardDecorId = e.Str("rewardDecorId"),
                });
            }
            return list;
        }
    }

    /// <summary>
    /// Client-side part of co-op contracts: what this player has contributed since joining (goods sold), and the
    /// one-time reward when the server reports the team goal as completed.
    /// </summary>
    public sealed class CoopTracker
    {
        public const long RewardDiamonds = 60;
        private readonly GameSession _s;

        public CoopTracker(GameSession session) => _s = session;

        public bool InCoop => _s.State.Live.CoopCode != null;

        public void Joined(string code, string contractId)
        {
            var l = _s.State.Live;
            if (l.CoopCode == code && l.CoopContractId == contractId) return;
            l.CoopCode = code;
            l.CoopContractId = contractId;
            l.CoopBaselineSold = _s.State.Stats.UnitsSold;
        }

        public void Left()
        {
            _s.State.Live.CoopCode = null;
            _s.State.Live.CoopContractId = null;
        }

        /// <summary>Goods sold since joining (reported to the server as a cumulative total).</summary>
        public long Contribution => InCoop ? Math.Max(0, _s.State.Stats.UnitsSold - _s.State.Live.CoopBaselineSold) : 0;

        public bool TryClaimCompleted(string contractId)
        {
            if (string.IsNullOrEmpty(contractId) || !_s.State.Live.CoopRewardsClaimed.Add(contractId)) return false;
            _s.Wallet.AddDiamonds(RewardDiamonds);
            _s.Crates.Add("crate_big");
            _s.Road.AddXp(SeasonXp.EventTier);
            return true;
        }
    }
}
