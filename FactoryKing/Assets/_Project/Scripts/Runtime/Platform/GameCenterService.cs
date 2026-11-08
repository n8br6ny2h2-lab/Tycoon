using FactoryKing.Core.Meta;
using FactoryKing.Core.Simulation;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Game Center via Unity's Social API: sign-in (silent, Apple shows its own banner), achievements and two
    /// leaderboards. Ids must exist in App Store Connect (see Docs/APPSTORE/GameCenter.md).
    /// </summary>
    public sealed class GameCenterService
    {
        public const string LeaderboardCompanyLevel = "factoryking.company_level";
        public const string LeaderboardPrestige = "factoryking.prestige_count";

        private readonly GameSession _session;
        public bool Authenticated { get; private set; }

        public GameCenterService(GameSession session)
        {
            _session = session;
            _session.Achievements.Unlocked += Report;
        }

        public void SignIn()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Social.localUser.Authenticate(ok =>
            {
                Authenticated = ok;
                if (!ok) return;
                foreach (var id in _session.State.AchievementsUnlocked)
                {
                    var def = AchievementService.Defs.Find(d => d.Id == id);
                    if (def != null) Report(def);
                }
                ReportScores();
            });
#endif
        }

        private void Report(AchievementDef def)
        {
            if (!Authenticated) return;
            Social.ReportProgress(def.GameCenterId, 100.0, _ => { });
        }

        public void ReportScores()
        {
            if (!Authenticated) return;
            Social.ReportScore(_session.CompanyLevel, LeaderboardCompanyLevel, _ => { });
            Social.ReportScore(_session.State.Prestige.Count, LeaderboardPrestige, _ => { });
        }

        public void ShowLeaderboards()
        {
            if (Authenticated) Social.ShowLeaderboardUI();
        }

        public void ShowAchievements()
        {
            if (Authenticated) Social.ShowAchievementsUI();
        }
    }
}
