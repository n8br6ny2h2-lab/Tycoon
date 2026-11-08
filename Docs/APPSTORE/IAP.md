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
| cosmetic_gold | Non-consumable | 3 golden decorations (cosmetic only) | 2.99 | Golden Look |
| season_pass_s1 | Non-consumable | 150 diamonds and double event diamonds for season 1 | 4.99 | Season Pass S1 |

Localised display names and descriptions for each product are the `shop.<id>` and `shop.<id>.desc` entries in `FactoryKing/Assets/_Project/Resources/Localization/strings.txt` (EN/TR).

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
