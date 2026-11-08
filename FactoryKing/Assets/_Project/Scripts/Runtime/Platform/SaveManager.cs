using System;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Local save (autosave every 20 s, on pause, focus loss and quit) plus iCloud key-value sync on iOS.
    /// The save is base64 UTF-8 text so the native layer never deals with encodings.
    /// </summary>
    public sealed class SaveManager : MonoBehaviour
    {
        public const float AutosaveSeconds = 20f;
        private const string CloudKey = "fk_save_v1";

        private SaveService _service;
        private GameSession _session;
        private float _timer;

        public LoadOutcome LastLoadOutcome { get; private set; }
        public bool CloudUsed { get; private set; }

        public GameState Load(GameContent content, EconomyTuning tuning)
        {
            _service = new SaveService(new FileSaveStorage());
            GameState local = null;
            try
            {
                local = _service.Load(content, tuning, out var outcome);
                LastLoadOutcome = outcome;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                LastLoadOutcome = LoadOutcome.CorruptReset;
                local = GameState.CreateNew(content, tuning);
            }

            var cloud = ReadCloud(content);
            if (cloud != null)
            {
                // A fresh/reset local game has no progress, so the cloud copy always wins in that case.
                CloudUsed = SaveConflictResolver.Choose(local, cloud) == CloudChoice.UseCloud;
                local = SaveConflictResolver.Resolve(local, cloud, content);
            }
            return local;
        }

        public void Attach(GameSession session) => _session = session;

        private static GameState ReadCloud(GameContent content)
        {
            if (!NativeBridge.CloudAvailable) return null;
            try
            {
                string b64 = NativeBridge.CloudGet(CloudKey);
                if (string.IsNullOrEmpty(b64)) return null;
                string text = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                return SaveService.TryDecode(text, content);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FactoryKing] iCloud read failed: " + e.Message);
                return null;
            }
        }

        /// <summary>Set while resetting progress so the old session is not written back on teardown.</summary>
        public bool Suppressed { get; set; }

        public void SaveNow()
        {
            if (Suppressed || _session == null || _service == null) return;
            try
            {
                _service.Save(_session.State, _session.Time.TrustedNow);
                if (NativeBridge.CloudAvailable)
                {
                    string b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(SaveService.Encode(_session.State)));
                    if (b64.Length < 900_000) NativeBridge.CloudSet(CloudKey, b64); // iCloud KVS limit is 1 MB
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>Settings → "Reset progress" (with confirmation in the UI).</summary>
        public void DeleteAll()
        {
            _service?.DeleteAll();
            if (NativeBridge.CloudAvailable) NativeBridge.CloudSet(CloudKey, string.Empty);
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= AutosaveSeconds)
            {
                _timer = 0f;
                SaveNow();
            }
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        private void OnApplicationFocus(bool focus) { if (!focus) SaveNow(); }
        private void OnApplicationQuit() => SaveNow();
    }
}
