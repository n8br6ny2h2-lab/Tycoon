using System;
using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Numerics;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using FactoryKing.Core.TimeKeeping;
using NUnit.Framework;

namespace FactoryKing.Tests
{
    public sealed class MemoryStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Files = new Dictionary<string, string>();
        public bool TryRead(string name, out string contents) => Files.TryGetValue(name, out contents);
        public void WriteAtomic(string name, string contents) => Files[name] = contents;
        public bool Exists(string name) => Files.ContainsKey(name);
        public void Delete(string name) => Files.Remove(name);
    }

    public class SaveTests
    {
        private static readonly DateTime Now = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        private static GameSession Played()
        {
            var clock = new ManualClock { UtcNow = Now };
            var s = new GameSession(DefaultContent.Create(), clock: clock);
            s.State.Cash = 1e6;
            s.Upgrades.TryUpgrade(new MachineRef("steel", 0, 1), 7);
            s.Factories.TryUnlockLine("steel", 1);
            for (int i = 0; i < 600; i++) s.Tick(0.1);
            s.Wallet.AddDiamonds(30);
            s.Managers.TryHire("mgr_kemal");
            s.Managers.Assign("mgr_kemal", "steel");
            s.State.Entitlements.Add("no_ads");
            s.State.ProcessedTransactions.Add("tx-1");
            s.State.Research["res_cost"] = 2;
            s.ApplyAllModifiers();
            s.State.Settings.Language = 1;
            s.State.Settings.MusicVolume = 0.25f;
            return s;
        }

        [Test]
        public void RoundTrip_Preserves_Everything()
        {
            var s = Played();
            string text = SaveService.Encode(s.State);
            var loaded = SaveService.TryDecode(text, s.Content);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(SaveSerializer.ToJson(loaded).ToString(), Is.EqualTo(SaveSerializer.ToJson(s.State).ToString()));
            Assert.That(loaded.Cash, Is.EqualTo(s.State.Cash));
            Assert.That(loaded.GetMachine(new MachineRef("steel", 0, 1)).Level, Is.EqualTo(8));
            Assert.That(loaded.Managers[0].AssignedFactoryId, Is.EqualTo("steel"));
            Assert.That(loaded.Entitlements, Does.Contain("no_ads"));
            Assert.That(loaded.ProcessedTransactions, Does.Contain("tx-1"));
            Assert.That(loaded.Settings.Language, Is.EqualTo(1));
        }

        [Test]
        public void Loaded_Game_Continues_Identically()
        {
            var a = Played();
            var b = new GameSession(a.Content, state: SaveService.TryDecode(SaveService.Encode(a.State), a.Content),
                clock: new ManualClock { UtcNow = Now });
            for (int i = 0; i < 300; i++) { a.Tick(0.1); b.Tick(0.1); }
            Assert.That(b.State.TotalUnitsSold, Is.EqualTo(a.State.TotalUnitsSold));
            Assert.That(b.Wallet.Cash.ApproximatelyEquals(a.Wallet.Cash, 1e-9), Is.True);
        }

        [Test]
        public void Huge_Numbers_Survive()
        {
            var s = new GameSession(DefaultContent.Create());
            s.State.Cash = BigDouble.FromLog10(512.345);
            var loaded = SaveService.TryDecode(SaveService.Encode(s.State), s.Content);
            Assert.That(loaded.Cash.ApproximatelyEquals(s.State.Cash, 1e-12), Is.True);
        }

        [Test]
        public void Corrupted_Main_Recovers_From_Backup()
        {
            var storage = new MemoryStorage();
            var svc = new SaveService(storage);
            var s = Played();
            svc.Save(s.State, Now);                     // becomes backup on next save
            s.State.Cash = 777;
            svc.Save(s.State, Now.AddMinutes(1));
            storage.Files[SaveService.MainFile] = storage.Files[SaveService.MainFile].Substring(0, 200); // truncated write
            var loaded = svc.Load(s.Content, s.Tuning, out var outcome);
            Assert.That(outcome, Is.EqualTo(LoadOutcome.RecoveredFromBackup));
            Assert.That(loaded.GetMachine(new MachineRef("steel", 0, 1)).Level, Is.EqualTo(8));
            Assert.That(storage.Files.ContainsKey(SaveService.CorruptFile), Is.True, "damaged file kept for support");
        }

        [Test]
        public void Tampered_Checksum_Is_Rejected()
        {
            var s = Played();
            string text = SaveService.Encode(s.State).Replace("\"diamonds\":", "\"diamonds\":9");
            Assert.That(SaveService.TryDecode(text, s.Content), Is.Null);
        }

        [Test]
        public void Both_Files_Broken_Starts_New_Game_Without_Crashing()
        {
            var storage = new MemoryStorage();
            storage.Files[SaveService.MainFile] = "garbage";
            storage.Files[SaveService.BackupFile] = "FK1 0000\n{";
            var loaded = new SaveService(storage).Load(DefaultContent.Create(), EconomyTuning.CreateDefault(), out var outcome);
            Assert.That(outcome, Is.EqualTo(LoadOutcome.CorruptReset));
            Assert.That(loaded.Factories[0].Unlocked, Is.True);
        }

        [Test]
        public void No_Save_Is_New_Game()
        {
            var loaded = new SaveService(new MemoryStorage()).Load(DefaultContent.Create(), EconomyTuning.CreateDefault(), out var outcome);
            Assert.That(outcome, Is.EqualTo(LoadOutcome.NewGame));
            Assert.That(loaded.Cash.ToDouble(), Is.EqualTo(EconomyTuning.CreateDefault().StartingCash));
        }

        [Test]
        public void Corrupt_Main_Never_Overwrites_Good_Backup()
        {
            var storage = new MemoryStorage();
            var svc = new SaveService(storage);
            var s = Played();
            svc.Save(s.State, Now);
            svc.Save(s.State, Now);
            string goodBackup = storage.Files[SaveService.BackupFile];
            storage.Files[SaveService.MainFile] = "broken";
            svc.Save(s.State, Now);
            Assert.That(storage.Files[SaveService.BackupFile], Is.EqualTo(goodBackup));
        }

        [Test]
        public void Version1_Save_Migrates()
        {
            var content = DefaultContent.Create();
            string json = "{\"version\":1,\"cash\":\"5e3\",\"lifetime\":\"2e4\",\"diamonds\":12,\"factories\":[]}";
            string text = "FK1 " + SaveService.Checksum(json) + "\n" + json;
            var s = SaveService.TryDecode(text, content);
            Assert.That(s, Is.Not.Null);
            Assert.That(s.Cash.ToDouble(), Is.EqualTo(5000));
            Assert.That(s.RunEarnings.ToDouble(), Is.EqualTo(20000), "v1 lifetime becomes the current run");
            Assert.That(s.Factories.Count, Is.EqualTo(5));
            Assert.That(s.Factories[0].Unlocked, Is.True);
        }

        [Test]
        public void Future_Version_Is_Not_Loaded()
        {
            string json = "{\"version\":99}";
            Assert.That(SaveService.TryDecode("FK1 " + SaveService.Checksum(json) + "\n" + json, DefaultContent.Create()), Is.Null);
        }

        [Test]
        public void Json_Handles_Unicode_And_Escapes()
        {
            var o = JsonValue.NewObject().Set("t", "Çelik \"Fabrikası\" ığüşö\n\\").Set("n", -1.5e-7);
            var back = JsonValue.Parse(o.ToString());
            Assert.That(back.Str("t"), Is.EqualTo("Çelik \"Fabrikası\" ığüşö\n\\"));
            Assert.That(back.Dbl("n"), Is.EqualTo(-1.5e-7));
            Assert.That(JsonValue.TryParse("{\"a\":[1,2,", out _), Is.False);
        }
    }
}

namespace FactoryKing.Tests
{
    public class CloudConflictTests
    {
        [Test]
        public void More_Progress_Wins_And_Purchases_Merge()
        {
            var content = DefaultContent.Create();
            var local = GameState.CreateNew(content, EconomyTuning.CreateDefault());
            var cloud = GameState.CreateNew(content, EconomyTuning.CreateDefault());
            local.LifetimeEarnings = 1e6;
            local.Time.LastSavedUtcTicks = 200;
            local.Entitlements.Add("no_ads");
            cloud.LifetimeEarnings = 5e6;
            cloud.Time.LastSavedUtcTicks = 100; // older but further ahead
            Assert.That(SaveConflictResolver.Choose(local, cloud), Is.EqualTo(CloudChoice.UseCloud));
            var merged = SaveConflictResolver.Resolve(local, cloud, content);
            Assert.That(merged.LifetimeEarnings.ToDouble(), Is.EqualTo(5e6));
            Assert.That(merged.Entitlements, Does.Contain("no_ads"));
        }

        [Test]
        public void Prestige_Count_Beats_Earnings_And_Missing_Cloud_Keeps_Local()
        {
            var content = DefaultContent.Create();
            var local = GameState.CreateNew(content, EconomyTuning.CreateDefault());
            var cloud = GameState.CreateNew(content, EconomyTuning.CreateDefault());
            local.Prestige.Count = 2;
            cloud.LifetimeEarnings = 1e12;
            Assert.That(SaveConflictResolver.Choose(local, cloud), Is.EqualTo(CloudChoice.KeepLocal));
            Assert.That(SaveConflictResolver.Choose(local, null), Is.EqualTo(CloudChoice.KeepLocal));
        }
    }
}
