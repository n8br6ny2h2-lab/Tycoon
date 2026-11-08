# Sizden Gerekenler

Aşağıdaki adımlar hesap, cihaz veya kimlik bilgisi gerektirdiği için koddan tamamlanamaz. Gerçek kimlikler verilene kadar projede **sahte SDK kimliği yok** ve yayın yapıldığı iddia edilmiyor.

## 1. Oyunu çalıştırmak (Mac)
1. Unity Hub'dan **Unity 6 LTS (6000.0.x)** sürümünü **iOS Build Support** modülüyle, ayrıca **Xcode 16+** kurun.
2. `FactoryKing/` klasörünü Unity Hub'da açın. Menüden **Factory King → 1. Setup Project** çalıştırın, `Factory` sahnesini açıp **Play**'e basın.
3. **Window → General → Test Runner**'da EditMode ve PlayMode testlerini çalıştırın.
4. Console'daki hataları ve ekran görüntülerini bana iletin; düzeltmeleri hemen yaparım.
5. Unity'nin ürettiği `.meta` dosyalarını ve `Assets/_Project/Settings`, `Scenes`, `Resources/UI`, `Resources/ShaderKeepAlive` klasörlerini commit edin.

## 2. Onaylamanız gereken kimlikler
| Değer | Şu an | Nereye |
|---|---|---|
| **Bundle ID** (ör. `com.sirketiniz.factoryking`) | yer tutucu `com.CHANGEME.factoryking` | Player Settings veya `FK_BUNDLE_ID` |
| **SKU** | yok | App Store Connect (ör. `FACTORYKING001`) |
| **Şirket adı, adres, e-posta** (KVKK veri sorumlusu) | yer tutucu | Player Settings, `Docs/web/privacy.html`, `support.html` |
| **Apple Team ID** | yok | Xcode veya `FK_APPLE_TEAM_ID` |
| **Gizlilik ve destek URL'leri** | `example.com` | `UiContext.PrivacyPolicyUrl` ve `SupportUrl` (`UiManager.cs`) |

IAP ürün kimlikleri bundle ID'den otomatik türetilir (`<bundle id>.vip` gibi). Liste: `Docs/APPSTORE/IAP.md`.

## 3. Hesaplar ve paketler
- **Apple Developer Program** üyeliği; App Store Connect'te uygulama kaydı; Ücretli Uygulamalar sözleşmesi ile banka ve vergi bilgileri (IAP için şart).
- **Unity In-App Purchasing 4.9+ (<5)** paketi: kurulunca gerçek StoreKit adaptörü otomatik devreye girer.
- **Reklam ağı seçimi** (AdMob / LevelPlay / AppLovin MAX) ve ödüllü reklam birim kimlikleri. Entegrasyon adımları: `Docs/APPSTORE/Ads.md`.
- App Store Connect'te 8 IAP ürünü, 12 başarım ve 2 liderlik tablosu (`IAP.md`, `GameCenter.md`).
- iCloud capability (key-value storage) App ID'de açık olmalı.

## 4. Yayın öncesi
- `Docs/APPSTORE/TestFlight.md` listesindeki cihaz testleri: en eski desteklediğiniz iPhone, güncel bir iPhone ve bir iPad.
- Ekran görüntüleri: iPhone 6.9"/6.7" ve iPad 13"/12.9" boyutlarında 3–10 adet.
- Başarım görselleri (Game Center).
- **Sanat yönü kararı:** prosedürel stille mi devam, yoksa 3D sanatçı modelleriyle mi? Mimari model değişimine hazır.
