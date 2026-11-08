using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public class OnlineTests
    {
        [Test]
        public void Server_Time_Overrides_A_Manipulated_Device_Clock()
        {
            var clock = new ManualClock { UtcNow = new DateTime(2027, 5, 1, 12, 0, 0, DateTimeKind.Utc) };
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            s.State.Time.LastSavedUtcTicks = clock.UtcNow.Ticks;
            s.State.Time.MaxSeenUtcTicks = clock.UtcNow.Ticks;
            clock.Advance(TimeSpan.FromHours(30));                        // player moved the clock forward
            s.Time.ApplyServerTime(new DateTime(2027, 5, 1, 13, 0, 0, DateTimeKind.Utc)); // real time: +1 h
            Assert.That(s.Time.ServerVerified, Is.True);
            var r = s.Resume();
            Assert.That(r.SecondsAway, Is.EqualTo(3600).Within(1), "only real time away counts");
        }

        [Test]
        public void Small_Server_Offset_Is_Ignored()
        {
            var clock = new ManualClock();
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            s.Time.ApplyServerTime(clock.UtcNow.AddSeconds(20));
            Assert.That(s.Time.TrustedNow, Is.EqualTo(clock.UtcNow));
        }

        [Test]
        public void Remote_Event_Schedule_Is_Parsed_And_Bad_Entries_Skipped()
        {
            string json = "{\"events\":[{\"id\":\"spring\",\"nameEn\":\"Spring\",\"nameTr\":\"Bahar\",\"start\":\"2027-04-01T00:00:00Z\",\"end\":\"2027-04-04T00:00:00Z\",\"goal\":\"sell\",\"rewardDiamonds\":99999},"
                        + "{\"id\":\"broken\",\"start\":\"x\"},{\"id\":\"backwards\",\"start\":\"2027-04-05T00:00:00Z\",\"end\":\"2027-04-01T00:00:00Z\"}]}";
            var list = RemoteEvents.Parse(json);
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].NameTr, Is.EqualTo("Bahar"));
            Assert.That(list[0].RewardDiamonds, Is.EqualTo(1000), "server values are clamped");
            Assert.That(list[0].StartUtc, Is.EqualTo(new DateTime(2027, 4, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(RemoteEvents.Parse("garbage"), Is.Empty);

            var clock = new ManualClock { UtcNow = new DateTime(2027, 4, 2, 0, 0, 0, DateTimeKind.Utc) };
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            s.Events.Schedule.AddRange(list);
            Assert.That(s.Events.Active().Exists(e => e.Id == "spring"), Is.True);
        }

        [Test]
        public void Coop_Contribution_Counts_Sales_Since_Joining_And_Reward_Is_Once()
        {
            var s = new GameSession(DefaultContent.Create(), clock: new ManualClock());
            s.State.Stats.UnitsSold = 500;
            s.Coop.Joined("ABC123", "coop_rush_100");
            s.State.Stats.UnitsSold = 1700;
            Assert.That(s.Coop.Contribution, Is.EqualTo(1200));
            long d = s.Wallet.Diamonds;
            Assert.That(s.Coop.TryClaimCompleted("coop_rush_100"), Is.True);
            Assert.That(s.Coop.TryClaimCompleted("coop_rush_100"), Is.False);
            Assert.That(s.Wallet.Diamonds, Is.EqualTo(d + CoopTracker.RewardDiamonds));
            var loaded = SaveService.TryDecode(SaveService.Encode(s.State), s.Content);
            Assert.That(loaded.Live.CoopCode, Is.EqualTo("ABC123"));
            Assert.That(loaded.Live.CoopRewardsClaimed, Does.Contain("coop_rush_100"));
        }

        [Test]
        public void Weekly_Xp_For_Clubs_Resets_Each_Week()
        {
            var clock = new ManualClock();
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            s.Road.AddXp(300);
            Assert.That(s.Road.WeekXp, Is.EqualTo(300));
            clock.Advance(TimeSpan.FromDays(8));
            Assert.That(s.Road.WeekXp, Is.EqualTo(0));
            s.Road.AddXp(50);
            Assert.That(s.Road.WeekXp, Is.EqualTo(50));
        }
    }
}
