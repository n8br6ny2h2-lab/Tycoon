using System;
using System.Text;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Core.Persistence
{
    /// <summary>Platform file access. The Unity layer implements it with persistentDataPath; tests use memory.</summary>
    public interface ISaveStorage
    {
        bool TryRead(string name, out string contents);
        /// <summary>Must replace the file atomically (write temp + move) so a crash never leaves a half file.</summary>
        void WriteAtomic(string name, string contents);
        bool Exists(string name);
        void Delete(string name);
    }

    public enum LoadOutcome
    {
        /// <summary>No save existed; a new game was created.</summary>
        NewGame,
        Loaded,
        /// <summary>The main save was damaged; the backup was used.</summary>
        RecoveredFromBackup,
        /// <summary>Main and backup were unreadable; a new game was created and the damaged file kept aside.</summary>
        CorruptReset,
    }

    /// <summary>
    /// Versioned, checksummed local save with automatic backup and recovery.
    /// File format: first line "FK1 &lt;fnv1a64-hex&gt;", then the JSON body.
    /// The checksum detects corruption (truncated/partial writes); it is not an anti-cheat measure.
    /// </summary>
    public sealed class SaveService
    {
        public const string MainFile = "factoryking_save.json";
        public const string BackupFile = "factoryking_save.bak";
        public const string CorruptFile = "factoryking_save.corrupt";
        private const string Magic = "FK1";

        private readonly ISaveStorage _storage;

        public SaveService(ISaveStorage storage) => _storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public static string Encode(GameState state)
        {
            string json = SaveSerializer.ToJson(state).ToString();
            return Magic + " " + Checksum(json) + "\n" + json;
        }

        /// <summary>Returns null if the text is not a valid, intact save.</summary>
        public static GameState TryDecode(string text, GameContent content)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int nl = text.IndexOf('\n');
            if (nl < 0) return null;
            string header = text.Substring(0, nl).Trim();
            string json = text.Substring(nl + 1);
            var parts = header.Split(' ');
            if (parts.Length != 2 || parts[0] != Magic || !string.Equals(parts[1], Checksum(json), StringComparison.OrdinalIgnoreCase))
                return null;
            if (!JsonValue.TryParse(json, out var root)) return null;
            try
            {
                var state = SaveSerializer.FromJson(root);
                state.EnsureConsistent(content);
                return state;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Writes the save; the previous good save becomes the backup.</summary>
        public void Save(GameState state, DateTime utcNow)
        {
            state.Time.LastSavedUtcTicks = utcNow.Ticks;
            if (utcNow.Ticks > state.Time.MaxSeenUtcTicks) state.Time.MaxSeenUtcTicks = utcNow.Ticks;
            string encoded = Encode(state);
            // Only an intact previous save may replace the backup, so corruption can never overwrite the good copy.
            if (_storage.TryRead(MainFile, out var previous) && IsIntact(previous))
                _storage.WriteAtomic(BackupFile, previous);
            _storage.WriteAtomic(MainFile, encoded);
        }

        public GameState Load(GameContent content, EconomyTuning tuning, out LoadOutcome outcome)
        {
            bool mainExists = _storage.Exists(MainFile);
            bool backupExists = _storage.Exists(BackupFile);

            if (mainExists && _storage.TryRead(MainFile, out var main))
            {
                var s = TryDecode(main, content);
                if (s != null) { outcome = LoadOutcome.Loaded; return s; }
            }
            if (backupExists && _storage.TryRead(BackupFile, out var bak))
            {
                var s = TryDecode(bak, content);
                if (s != null)
                {
                    if (mainExists && _storage.TryRead(MainFile, out var bad)) _storage.WriteAtomic(CorruptFile, bad);
                    _storage.WriteAtomic(MainFile, bak);
                    outcome = LoadOutcome.RecoveredFromBackup;
                    return s;
                }
            }
            if (mainExists || backupExists)
            {
                if (_storage.TryRead(MainFile, out var bad)) _storage.WriteAtomic(CorruptFile, bad);
                outcome = LoadOutcome.CorruptReset;
            }
            else
            {
                outcome = LoadOutcome.NewGame;
            }
            return GameState.CreateNew(content, tuning);
        }

        public void DeleteAll()
        {
            _storage.Delete(MainFile);
            _storage.Delete(BackupFile);
        }

        /// <summary>Header and checksum are valid (cheap check, no JSON parsing).</summary>
        public static bool IsIntact(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int nl = text.IndexOf('\n');
            if (nl < 0) return false;
            var parts = text.Substring(0, nl).Trim().Split(' ');
            return parts.Length == 2 && parts[0] == Magic &&
                   string.Equals(parts[1], Checksum(text.Substring(nl + 1)), StringComparison.OrdinalIgnoreCase);
        }

        public static string Checksum(string text)
        {
            const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
            ulong hash = offset;
            foreach (byte b in Encoding.UTF8.GetBytes(text))
            {
                hash ^= b;
                hash *= prime;
            }
            return hash.ToString("x16");
        }
    }
}
