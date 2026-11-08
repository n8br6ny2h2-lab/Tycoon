using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Calls into Assets/Plugins/iOS/FactoryKingNative.mm on device; safe no-ops in the Editor and other platforms.
    /// </summary>
    public enum HapticStyle { Light = 0, Medium = 1, Heavy = 2, Success = 3, Warning = 4, Error = 5, Selection = 6 }

    public static class NativeBridge
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool FK_CloudAvailable();
        [DllImport("__Internal")] private static extern void FK_CloudSet(string key, string value);
        [DllImport("__Internal")] private static extern IntPtr FK_CloudGet(string key);
        [DllImport("__Internal")] private static extern void FK_NotificationsRequest();
        [DllImport("__Internal")] private static extern void FK_NotificationSchedule(string id, string title, string body, double seconds);
        [DllImport("__Internal")] private static extern void FK_NotificationsCancelAll();
        [DllImport("__Internal")] private static extern void FK_FreeString(IntPtr p);
        [DllImport("__Internal")] private static extern void FK_Haptic(int style);
        [DllImport("__Internal")] private static extern bool FK_LowPowerMode();
        [DllImport("__Internal")] private static extern void FK_RequestReview();

        public static void Haptic(HapticStyle style) => FK_Haptic((int)style);
        public static bool LowPowerMode => FK_LowPowerMode();
        public static void RequestReview() => FK_RequestReview();

        public static bool CloudAvailable => FK_CloudAvailable();
        public static void CloudSet(string key, string value) => FK_CloudSet(key, value);
        public static string CloudGet(string key)
        {
            IntPtr p = FK_CloudGet(key);
            if (p == IntPtr.Zero) return null;
            string s = Marshal.PtrToStringAnsi(p);
            FK_FreeString(p);
            return s;
        }
        public static void RequestNotificationPermission() => FK_NotificationsRequest();
        public static void ScheduleNotification(string id, string title, string body, double seconds) => FK_NotificationSchedule(id, title, body, seconds);
        public static void CancelAllNotifications() => FK_NotificationsCancelAll();
#else
        public static bool CloudAvailable => false;
        public static void CloudSet(string key, string value) { }
        public static string CloudGet(string key) => null;
        public static void RequestNotificationPermission() { }
        public static void ScheduleNotification(string id, string title, string body, double seconds) =>
            Debug.Log($"[FactoryKing] (editor) notification '{id}' in {seconds:0}s: {title} — {body}");
        public static void CancelAllNotifications() { }
        public static void Haptic(HapticStyle style) { }
        public static bool LowPowerMode => false;
        public static void RequestReview() => Debug.Log("[FactoryKing] (editor) App Store rating prompt requested");
#endif
    }
}
