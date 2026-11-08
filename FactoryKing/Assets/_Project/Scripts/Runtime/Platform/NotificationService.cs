using FactoryKing.Core.Localization;
using FactoryKing.Core.Simulation;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Optional local notifications (off by default; enabled only from Settings). At most two reminders are
    /// scheduled when the app goes to the background: "offline storage full" and the next daily reward.
    /// </summary>
    public sealed class NotificationService
    {
        private readonly GameSession _s;
        private readonly LocalizationTable _loc;

        public NotificationService(GameSession session, LocalizationTable loc)
        {
            _s = session;
            _loc = loc;
        }

        public void SetEnabled(bool enabled)
        {
            _s.State.Settings.Notifications = enabled;
            if (enabled) NativeBridge.RequestNotificationPermission();
            else NativeBridge.CancelAllNotifications();
        }

        public void OnBackground()
        {
            NativeBridge.CancelAllNotifications();
            if (!_s.State.Settings.Notifications) return;
            NativeBridge.ScheduleNotification("offline_full", _loc.Get("notif.offline.title"), _loc.Get("notif.offline.body"), _s.Offline.CapSeconds);
            var now = _s.Time.TrustedNow;
            double toTomorrow = (now.Date.AddDays(1).AddHours(9) - now).TotalSeconds;
            if (toTomorrow > 3600) NativeBridge.ScheduleNotification("daily", _loc.Get("notif.daily.title"), _loc.Get("notif.daily.body"), toTomorrow);
        }

        public void OnForeground() => NativeBridge.CancelAllNotifications();
    }
}
