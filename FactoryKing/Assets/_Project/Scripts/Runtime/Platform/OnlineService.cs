using System;
using System.Collections;
using System.Text;
using FactoryKing.Core.Meta;
using FactoryKing.Core.Persistence;
using FactoryKing.Core.Simulation;
using UnityEngine;
using UnityEngine.Networking;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Optional online features (Server/ reference backend): server time, remote event schedule, co-op contracts,
    /// clubs, reports and deletion of online data. Disabled when Resources/Online/online.txt has no baseUrl — the game
    /// is fully playable offline. Players are anonymous: a random id + token, plus the company name they chose.
    /// </summary>
    public sealed class OnlineService : MonoBehaviour
    {
        private const string TokenKey = "fk.online.token";
        private GameSession _s;
        private string _base;
        private string _token;
        private float _syncTimer;

        public bool Enabled => !string.IsNullOrEmpty(_base);
        public bool Registered => !string.IsNullOrEmpty(_token);
        public JsonValue Coop { get; private set; }
        public JsonValue Club { get; private set; }
        public JsonValue TopClubs { get; private set; }
        public string LastError { get; private set; }
        public event Action Updated;

        public void Init(GameSession session)
        {
            _s = session;
            var cfg = Resources.Load<TextAsset>("Online/online");
            if (cfg != null && JsonValue.TryParse(cfg.text, out var json)) _base = json.Str("baseUrl", "")?.TrimEnd('/');
            _token = PlayerPrefs.GetString(TokenKey, null);
            if (Enabled) StartCoroutine(Bootstrap());
        }

        private IEnumerator Bootstrap()
        {
            yield return Get("/v1/time", r => { if (r != null && DateTime.TryParse(r.Str("utc"), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var t)) _s.Time.ApplyServerTime(DateTime.SpecifyKind(t, DateTimeKind.Utc)); });
            yield return Get("/v1/events", r =>
            {
                if (r == null) return;
                _s.Events.Schedule.Clear();
                _s.Events.Schedule.AddRange(RemoteEvents.Parse(r.ToString()));
            });
            if (!Registered)
                yield return Post("/v1/register", JsonValue.NewObject().Set("name", _s.State.CompanyName ?? "Factory"), r =>
                {
                    if (r == null) return;
                    _token = r.Str("token");
                    PlayerPrefs.SetString(TokenKey, _token);
                    PlayerPrefs.Save();
                });
            yield return Refresh();
        }

        public IEnumerator Refresh()
        {
            if (!Registered) yield break;
            yield return Get("/v1/coop/mine", r =>
            {
                Coop = r != null && r.Get("code") != null ? r : null;
                if (Coop == null) _s.Coop.Left();
                else
                {
                    var contract = Coop.Obj("contract");
                    _s.Coop.Joined(Coop.Str("code"), contract.Str("id"));
                    if (Coop.Bool("completed")) _s.Coop.TryClaimCompleted(contract.Str("id"));
                }
            });
            yield return Get("/v1/clubs/mine", r => Club = r != null && r.Get("code") != null ? r : null);
            Updated?.Invoke();
        }

        private void Update()
        {
            if (!Enabled || !Registered) return;
            _syncTimer -= Time.unscaledDeltaTime;
            if (_syncTimer > 0) return;
            _syncTimer = 60f; // contributions and club XP once a minute
            if (_s.Coop.InCoop) StartCoroutine(Post("/v1/coop/contribute", JsonValue.NewObject().Set("totalUnits", _s.Coop.Contribution), r =>
            {
                if (r == null) return;
                Coop = r;
                if (r.Bool("completed")) _s.Coop.TryClaimCompleted(r.Obj("contract").Str("id"));
                Updated?.Invoke();
            }));
            if (Club != null) StartCoroutine(Post("/v1/clubs/score", JsonValue.NewObject().Set("weekXp", _s.Road.WeekXp), r => { if (r != null) { Club = r; Updated?.Invoke(); } }));
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused && Enabled) StartCoroutine(Get("/v1/time", r =>
            {
                if (r != null && DateTime.TryParse(r.Str("utc"), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var t))
                    _s.Time.ApplyServerTime(DateTime.SpecifyKind(t, DateTimeKind.Utc));
            }));
        }

        // ---------------- actions used by the UI ----------------
        public void CreateCoop(Action<bool> done) => StartCoroutine(Post("/v1/coop/create", JsonValue.NewObject(), r => { Coop = r; if (r != null) _s.Coop.Joined(r.Str("code"), r.Obj("contract").Str("id")); done?.Invoke(r != null); Updated?.Invoke(); }));
        public void JoinCoop(string code, Action<bool> done) => StartCoroutine(Post("/v1/coop/join", JsonValue.NewObject().Set("code", code), r => { Coop = r; if (r != null) _s.Coop.Joined(r.Str("code"), r.Obj("contract").Str("id")); done?.Invoke(r != null); Updated?.Invoke(); }));
        public void CreateClub(string name, Action<bool> done) => StartCoroutine(Post("/v1/clubs/create", JsonValue.NewObject().Set("name", name), r => { Club = r; done?.Invoke(r != null); Updated?.Invoke(); }));
        public void JoinClub(string code, Action<bool> done) => StartCoroutine(Post("/v1/clubs/join", JsonValue.NewObject().Set("code", code), r => { Club = r; done?.Invoke(r != null); Updated?.Invoke(); }));
        public void LeaveClub() => StartCoroutine(Post("/v1/clubs/leave", JsonValue.NewObject(), r => { Club = null; Updated?.Invoke(); }));
        public void LoadTopClubs() => StartCoroutine(Get("/v1/clubs/top", r => { TopClubs = r; Updated?.Invoke(); }));
        public void Report(string type, string target) => StartCoroutine(Post("/v1/report", JsonValue.NewObject().Set("type", type).Set("target", target).Set("reason", "inappropriate name"), r => { }));
        public void SyncName() { if (Registered) StartCoroutine(Post("/v1/player/name", JsonValue.NewObject().Set("name", _s.State.CompanyName ?? "Factory"), r => { })); }

        /// <summary>Deletes every online record of this player (Settings → Delete online data).</summary>
        public void DeleteOnlineData(Action<bool> done)
        {
            if (!Registered) { done?.Invoke(true); return; }
            StartCoroutine(Send("DELETE", "/v1/player", null, r =>
            {
                PlayerPrefs.DeleteKey(TokenKey);
                _token = null;
                Coop = null;
                Club = null;
                _s.Coop.Left();
                done?.Invoke(r != null);
                Updated?.Invoke();
            }));
        }

        // ---------------- HTTP ----------------
        private IEnumerator Get(string path, Action<JsonValue> done) => Send("GET", path, null, done);
        private IEnumerator Post(string path, JsonValue body, Action<JsonValue> done) => Send("POST", path, body, done);

        private IEnumerator Send(string method, string path, JsonValue body, Action<JsonValue> done)
        {
            if (!Enabled) { done?.Invoke(null); yield break; }
            using (var req = new UnityWebRequest(_base + path, method))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                if (body != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body.ToString()));
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                if (Registered) req.SetRequestHeader("Authorization", "Bearer " + _token);
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    LastError = req.responseCode > 0 ? "http_" + req.responseCode : "network";
                    if (req.responseCode == 401 && path != "/v1/register") { PlayerPrefs.DeleteKey(TokenKey); _token = null; }
                    done?.Invoke(null);
                    yield break;
                }
                LastError = null;
                done?.Invoke(JsonValue.TryParse(req.downloadHandler.text, out var json) ? json : null);
            }
        }
    }
}
