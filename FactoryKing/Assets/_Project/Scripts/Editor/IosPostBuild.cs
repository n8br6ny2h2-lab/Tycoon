#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace FactoryKing.EditorTools
{
    /// <summary>
    /// Adds the Xcode capabilities the game needs after Unity generates the iOS project:
    /// iCloud key-value storage (save sync), Game Center, In-App Purchase, plus the UserNotifications framework
    /// and the privacy/export settings App Store review expects.
    /// </summary>
    public static class IosPostBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostProcess(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string mainTarget = proj.GetUnityMainTargetGuid();
            string frameworkTarget = proj.GetUnityFrameworkTargetGuid();
            proj.AddFrameworkToProject(frameworkTarget, "UserNotifications.framework", false);
            proj.AddFrameworkToProject(frameworkTarget, "GameKit.framework", false);
            proj.AddFrameworkToProject(frameworkTarget, "StoreKit.framework", false);
            proj.WriteToFile(projPath);

            var caps = new ProjectCapabilityManager(projPath, "FactoryKing.entitlements", null, mainTarget);
            caps.AddiCloud(true, false, false, false, null);   // key-value storage only
            caps.AddGameCenter();
            caps.AddInAppPurchase();
            caps.WriteToFile();

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false); // only HTTPS/OS crypto -> export compliance "No"
            root.SetString("CFBundleDisplayName", "Factory King");
            var langs = root.CreateArray("CFBundleLocalizations");
            langs.AddString("en");
            langs.AddString("tr");
            plist.WriteToFile(plistPath);

            // Assets/Plugins/iOS/PrivacyInfo.xcprivacy is copied into the Xcode project by Unity automatically.
        }
    }
}
#endif
