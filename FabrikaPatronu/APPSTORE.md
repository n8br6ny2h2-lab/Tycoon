# Fabrika Patronu · App Store

Altyapı Demir Baronu ile aynı: Capacitor 8 + StoreKit eklentisi. Bulutta Mac'siz derlenir.

## Derleme

- **GitHub Actions:** `.github/workflows/fabrikapatronu-testflight.yml`. Gereken değişkenler ve gizli anahtarlar:
  - `FP_BUNDLE_ID`: yeni uygulamanın Bundle ID'si (ör. `com.kavcigames.fabrikapatronu`)
  - `DB_APPLE_TEAM_ID`
  - `ASC_KEY_ID`, `ASC_ISSUER_ID`
  - `ASC_API_KEY_P8` (gizli anahtar)
- **GitLab + Codemagic:** depo kökünde `FabrikaPatronu/` klasörü kullanılmalı. Adımlar:
  1. `npm ci` (FabrikaPatronu)
  2. `cd app && npm ci && npm run sync`
  3. Xcode arşivi: `app/ios/App/App.xcodeproj`, scheme `App`

## App Store Connect

- Yeni bir uygulama kaydı aç. Ad önerisi: **Fabrika Patronu: Sanayi**. Alt başlık: **Hammadde İmparatorluğu**.
- Cihaz ve yön: yalnızca iPhone, yatay ekran (Info.plist yatay kilitli).
- Ekran görüntüleri 2796×1290 yatay olmalı. Bunları oyunun tarayıcı sürümünden almak gerekir; henüz üretilmedi.
- Gizlilik:
  - Veri toplanmıyor.
  - `app/ios/App/App/PrivacyInfo.xcprivacy` hazır (takip yok, UserDefaults CA92.1).
  - Gizlilik politikası URL'si gerekir. Demir Baronu'nun sayfası ad değiştirilerek kullanılabilir.
- Satın alma yok: bu sürümde uygulama içi ürün tanımlama.
- 4.3 notu: aynı hesapta benzer fabrika oyunları (Factory King) varsa inceleme notunda farkı anlat. Fabrika Patronu'nun ayırt edici yanları:
  - gerçek arz-talep piyasası
  - limit emir ve toplu sipariş
  - sınıflı depolar
  - sözleşme ve ihale
  - bölgeler arası lojistik
  - 10 rakip
