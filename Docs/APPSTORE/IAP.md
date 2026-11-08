# In-app purchases — App Store Connect configuration

Product ids are built as **`<bundle id>.<product>`** (`GameBootstrap.StoreIdPrefix` defaults to `Application.identifier + "."`).
Example with bundle id `com.yourcompany.factoryking`: `com.yourcompany.factoryking.diamonds_small`.

| Product | Type | Contents | Suggested price (USD tier) | Reference name |
|---|---|---|---|---|
| diamonds_small | Consumable | 80 diamonds | 0.99 | Diamonds 80 |
| diamonds_medium | Consumable | 500 diamonds | 4.99 | Diamonds 500 |
| diamonds_large | Consumable | 1,200 diamonds | 9.99 | Diamonds 1200 |
| no_ads | Non-consumable | Rewarded-video rewards without watching | 3.99 | Ad-free Rewards |
| starter_pack | Non-consumable | 300 diamonds, 2 h of income, manager Ayla | 1.99 | Starter Pack |
| vip | Non-consumable | +10% sale price, +2 h offline cap (permanent) | 7.99 | VIP |
| cosmetic_gold | Non-consumable | Golden floor, walls, sky and statue (cosmetic only, permanent) | 2.99 | Golden Look |
| cosmetic_neon | Non-consumable | Neon floor, walls and sky (cosmetic only, permanent) | 2.99 | Neon Look |
| piggy_bank | **Consumable** | All diamonds saved in the piggy bank (200–1000) | 2.99 | Piggy Bank |
| vip_weekly | **Auto-renewable subscription** (1 week, own subscription group "VIP") | 30 diamonds per day, video rewards without watching | 2.99 | Weekly VIP |
| season_pass | **Consumable** (one per season) | Current 30-day season: 150 diamonds, double season and event diamonds, premium decorations | 4.99 | Season Pass |

The Season Pass is consumable because it is bought again each season. The save (and iCloud) remembers that it is active for the current season, and it cannot be bought twice in one season. Decorations it unlocks are permanent.

Localised display names and descriptions for each product are the `shop.<id>` and `shop.<id>.desc` entries in `FactoryKing/Assets/_Project/Resources/Localization/strings.txt` (EN/TR).

**Subscription requirements (guideline 3.1.2):** The Shop shows the title, the period, the price, a renewal and cancellation notice, and links to the Terms of Use (Apple's standard EULA by default) and the Privacy Policy. Add the same links to the App Store description. The subscription expiry comes from the receipt (Unity IAP `SubscriptionManager`); the test store simulates 7 days.

**Crates:** Crates contain random manager cards. They are earned in play or bought with diamonds, and **the odds are always shown before opening or buying** (guideline 3.1.1, "loot boxes").

## Implementation guarantees (covered by tests)
- Each transaction id is granted **once**; repeated deliveries return `AlreadyProcessed`.
- Owned non-consumables cannot be bought again (the button is disabled and `CanPurchase` is false).
- **Restore Purchases** (Shop and Settings) re-grants only non-consumables; consumables are never restored.
- Invalid or unverifiable receipts grant nothing. Cancelled, declined and pending payments show clear messages ("you were not charged").
- A transaction is confirmed with StoreKit only **after** the grant has been saved, so an interrupted purchase is re-delivered.
- The game is fully playable without paying; diamonds are also earned from missions, achievements, events and the daily calendar.

## Enabling real purchases
1. Install **Unity In-App Purchasing 4.9 – 4.x** from the Package Manager. The `FactoryKing.Purchasing` assembly then compiles automatically (asmdef version define `FK_UNITY_IAP`) and replaces the test store.
2. Optional local receipt validation: run *Services › In-App Purchasing › Receipt Validation Obfuscator*, then add the scripting define `FK_RECEIPT_VALIDATION`.
3. For the strongest protection, add server-side validation with the App Store Server API (`IStoreBackend` is the integration point).
4. Create the products above in App Store Connect, with a review screenshot of the Shop screen for each.
5. Test with Sandbox accounts through TestFlight (see TestFlight.md).

> If you use Unity IAP 5.x, its API differs. `UnityIapBackend` would need porting (the version define keeps it from compiling against 5.x).
