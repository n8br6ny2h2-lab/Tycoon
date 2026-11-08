# App Privacy answers (App Store Connect › App Privacy) & KVKK notes

## Current build: "Data Not Collected"

| Question | Answer | Why |
|---|---|---|
| Do you or your third-party partners collect data from this app? | **No** | Saves stay on the device and in the player's own iCloud account (not accessible to the developer). Purchases are processed by Apple; transaction ids are stored only in the save. The analytics service has no network sink in release builds. No ad SDK is integrated yet. |
| Tracking (ATT) | **No tracking** | No IDFA access, no cross-app tracking, so no App Tracking Transparency prompt is shown. |

The privacy manifest `FactoryKing/Assets/Plugins/iOS/PrivacyInfo.xcprivacy` matches this (no collected data types, `NSPrivacyTracking = false`, required-reason APIs: UserDefaults CA92.1, file timestamps C617.1, system boot time 35F9.1).

## When you add an analytics backend
The opt-in analytics already exists in the game (Settings, plus the first-launch consent dialog). Once events are actually sent to a server:
- App Store Connect: Data Types › **Usage Data › Product Interaction**, purpose **Analytics**, **Not linked** to the user, **Not used for tracking**.
- Add the same entry to the privacy manifest.
- Update the privacy policy (section 3).

## When you add a rewarded-ad SDK
Ad networks usually collect device identifiers and advertising data. Follow that SDK's privacy manifest and App Privacy guidance. If you enable personalised ads, show the ATT prompt (`NSUserTrackingUsageDescription`) **only right before the first ad** and serve non-personalised ads if the player declines. Because the game is rated 4+ and may be played by children, prefer non-personalised ads.

## KVKK (Turkey's personal data protection law)
- The data controller details (company title, address, contact email) go in section 1 of the privacy policy.
- Without consent no analytics are processed. Consent can be withdrawn at any time in Settings.
- Data deletion: "Settings › Reset progress" deletes local and iCloud saves. Support handles any other request.
