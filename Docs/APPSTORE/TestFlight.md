# TestFlight and submission checklist

## One-time setup (needs your Apple Developer account)
- [ ] Register the bundle id (e.g. `com.yourcompany.factoryking`) with **Game Center**, **In-App Purchase** and **iCloud** capabilities (iCloud: key-value storage).
- [ ] Create the app in App Store Connect: name, primary language, SKU (e.g. `FACTORYKING001`), bundle id.
- [ ] Enter metadata (Metadata.md), age rating (AgeRating.md), App Privacy (AppPrivacy.md), privacy policy and support URLs (host `Docs/web/privacy.html` and `Docs/web/support.html`).
- [ ] Create the IAP products (IAP.md) and Game Center items (GameCenter.md).
- [ ] Paid Applications agreement plus banking and tax info (required for IAP).

## Each build
1. In Unity: **Factory King → 1. Setup Project** (first time), set the real bundle id (Player Settings, or `FK_BUNDLE_ID`), increase the **build number**.
2. **Factory King → Build iOS (Xcode project)**, or headless:
   `Unity -batchmode -quit -projectPath FactoryKing -executeMethod FactoryKing.EditorTools.BuildScript.BuildIOS -release`
3. Open `FactoryKing/Builds/iOS/Unity-iPhone.xcodeproj` and select your Team (automatic signing). Check *Signing & Capabilities* shows Game Center, In-App Purchase and iCloud (Key-value storage).
4. Product › Archive → Distribute App → App Store Connect → Upload.
5. In App Store Connect › TestFlight: answer export compliance (handled automatically: `ITSAppUsesNonExemptEncryption = NO`), add internal testers.

## What to test on devices (oldest iPhone you support, a recent iPhone, an iPad)
- [ ] First launch: splash → menu → tutorial → consent dialog; Turkish and English; system-language default.
- [ ] Production visuals match the numbers (belts, warehouse, trucks); 60 FPS; battery and heat after 30 min.
- [ ] Kill the app, wait over 1 min / 2 h / more than 8 h → offline summary is correct and capped; ×2 button.
- [ ] Move the device clock back, then forward → no free offline earnings and no extra daily rewards.
- [ ] Sandbox purchases: each product, cancel, interrupted purchase (kill the app during payment), Restore on a second device.
- [ ] iCloud: play on device A, install on device B with the same Apple ID → progress arrives; purchases are never lost.
- [ ] Game Center sign-in, achievements and leaderboards.
- [ ] Notifications are off by default and work only after enabling them in Settings.
- [ ] Safe areas: notch and Dynamic Island devices, iPad portrait. All buttons reachable; text readable.
- [ ] Settings › Reset progress; low graphics mode.

## Screenshots (required sizes)
- iPhone 6.9" (1320 × 2868) or 6.7" (1290 × 2796): **3–10 images**
- iPad 13" (2064 × 2752) or 12.9" (2048 × 2732): **3–10 images** (the app supports iPad)

Capture them from the real game: set the Game view to the size, press Play, then **Factory King → Capture Store Screenshot** (or use the Simulator). Suggested shots: steel hall at day, night lighting, machine upgrade sheet, five-factory map, managers, offline summary, prestige.

## App Review notes (paste into "Notes")
> Factory King is an idle tycoon. All videos are optional rewarded ads; there are no forced ads. Purchases can be tested in the Shop; "Restore purchases" is in the Shop and in Settings. No login is required. Game Center sign-in is optional.
