# Rewarded ads — status and integration

**Status:** The game logic is complete (opt-in rewarded placements, daily caps, cooldowns, the "no_ads" purchase, rewards only after a fully watched video). **No ad network SDK is integrated, and no fake ad ids are used.** Release builds show "no video available" until an SDK is added. Editor and development builds use `TestAdsProvider`, which simulates a 2-second ad (complete, skip or fail can be chosen in Settings › Developer tools).

| Placement id | Reward | Daily cap | Cooldown |
|---|---|---|---|
| double_offline | 2× offline earnings | 20 | – |
| boost | 30 min of 2× production speed (stacks up to 4 h) | 8 | – |
| free_diamonds | 5 diamonds | 5 | 10 min |
| free_cash | 10 min of income | 10 | 5 min |

There are no interstitial, banner or forced ads.

## To integrate (needs your account and ids)
1. Choose a network (e.g. Google AdMob, Unity LevelPlay or AppLovin MAX) and create the iOS app and a **rewarded** ad unit for each placement.
2. Add the SDK and its SKAdNetwork ids (Info.plist) and privacy manifest.
3. Implement `IAdsProvider` (two methods: `IsReady(placement)` and `Show(placement, callback)`) in a separate assembly and register it in `PlatformRegistry.AdsFactory` (see `UnityIapBackend.Register` for the pattern).
4. Update the App Privacy answers and the privacy policy (see AppPrivacy.md).
