using UnityEditor;
using UnityEngine;

namespace FactoryKing.EditorTools
{
    /// <summary>
    /// Quick phone testing without a Mac or an Apple Developer account: Unity Remote 5 (free on the App Store /
    /// Google Play) mirrors the Editor's Game view to the phone over USB and sends touches back.
    /// Native iOS features (haptics, iCloud, Game Center, purchases) need a real build (TestFlight) instead.
    /// </summary>
    public static class PhoneTestSetup
    {
        [MenuItem("Factory King/Phone Test (Unity Remote) Setup", priority = 25)]
        public static void Setup()
        {
            EditorSettings.unityRemoteDevice = "Any iOS Device";
            EditorSettings.unityRemoteCompression = "JPEG";
            EditorSettings.unityRemoteResolution = "Normal";
            EditorUtility.DisplayDialog("Factory King – Telefon testi",
                "Hazır! Şimdi:\n\n" +
                "1. iPhone'a App Store'dan 'Unity Remote 5' uygulamasını kurup açın.\n" +
                "2. iPhone'u USB kablosuyla bilgisayara takın; telefonda 'Bu bilgisayara güven' deyin.\n" +
                "   (Windows'ta Microsoft Store'dan 'Apple Devices' veya iTunes kurulu olmalı.)\n" +
                "3. Game penceresinin üstündeki çözünürlük menüsünden dikey bir iPhone boyutu seçin.\n" +
                "4. Unity'de Play'e basın. Oyun birkaç saniye içinde telefonda görünür; dokunarak oynayabilirsiniz.\n\n" +
                "Görünmezse: Play'i durdurun, kabloyu çıkarıp takın, Unity Remote'u kapatıp açın ve Play'e tekrar basın.",
                "Tamam");
        }
    }
}
