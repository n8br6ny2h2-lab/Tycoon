# Fabrika Patronu · App Store'a yükleme

Altyapı Demir Baronu ile aynıdır: Capacitor 8. Uygulama bulutta, Mac olmadan derlenir. Bu sürümde uygulama içi satın alma ve reklam yoktur.

## Senin yapman gerekenler

Bunları telefondan ya da bilgisayardan yapabilirsin. Toplam 10–15 dakika sürer.

### 1. Bundle ID oluştur (Apple Developer)

1. developer.apple.com adresine gir: **Account → Certificates, IDs & Profiles → Identifiers → +** ile başla.
2. **App IDs**, ardından **App** seç.
3. Açıklamaya `Fabrika Patronu` yaz.
4. Bundle ID'yi **Explicit** seç ve şunu yaz: `com.fabrikapatronu.app`. Bu ad alınmışsa `com.kavcigames.fabrikapatronu` gibi başka bir ad kullan.
5. Ek yetenek seçme ve kaydet.

### 2. Uygulama kaydı aç (App Store Connect)

1. appstoreconnect.apple.com adresinde **Uygulamalar → + → Yeni Uygulama** seç.
2. Formu şöyle doldur:
   - **Platform:** iOS
   - **Ad:** Fabrika Patronu: Sanayi
   - **Birincil dil:** Türkçe
   - **Bundle ID:** 1. adımda açtığın Bundle ID
   - **SKU:** `fabrikapatronu`
   - **Erişim:** Tam erişim
3. Uygulama kaydı API ile açılamaz, bu adımı elle yapman gerekir.

### 3. GitHub ayarları

Depoda **Settings → Secrets and variables → Actions** sayfasını aç.

- **Variables** sekmesine şu değerleri gir:
  - `FP_BUNDLE_ID`: 1. adımdaki Bundle ID
  - `DB_APPLE_TEAM_ID`: Demir Baronu'da girdiysen zaten vardır
  - `ASC_KEY_ID`: zaten varsa dokunma
  - `ASC_ISSUER_ID`: zaten varsa dokunma
- **Secrets** sekmesine `ASC_API_KEY_P8` gir. Değeri .p8 dosyasının tam içeriğidir. Zaten varsa dokunma.
  - Anahtar App Store Connect'te **Kullanıcılar ve Erişim → Entegrasyonlar → App Store Connect API** sayfasında oluşturulur. Rolü **Admin** olmalıdır.
  - Anahtarı asla sohbete yapıştırma.

### 4. Derlemeyi başlat

Hazır olduğunu söylersen Claude `fabrikapatronu-v1.0.0` etiketini gönderir. Derleme bu etiketle başlar ve yaklaşık 20 dakika sürer. Sonra derleme App Store Connect'te **TestFlight** sekmesinde görünür; Apple'ın işlemesi 5–30 dakika sürer.

- Yeni bir derleme için bir sonraki etiket kullanılır (`fabrikapatronu-v1.0.1` gibi).
- Derleme numarası her seferinde kendiliğinden artar.

## Mağaza bilgileri

| Alan | Metin |
|---|---|
| Ad (30) | Fabrika Patronu: Sanayi |
| Alt başlık (30) | Hammadde İmparatorluğu |
| Kategori | Oyunlar → Simülasyon (ikincil: Strateji) |
| Yaş | 4+ (şiddet, kumar, sohbet yok) |
| Fiyat | Ücretsiz |
| Anahtar kelimeler (100) | tycoon,fabrika,sanayi,ekonomi,borsa,ticaret,üretim,simülasyon,patron,şirket,depo,lojistik |
| Gizlilik politikası | `web/privacy.html` bir adreste yayınlanmalı (ör. GitHub Pages). Önce iletişim alanlarını doldur |
| Uygulama gizliliği | "Veri toplanmıyor" |

**Tanıtım metni (170):**
Hammaddeyi ucuza al, fabrikanda işle, pahalıya sat. Gerçek arz-talep piyasasında 10 rakip şirketle yarış ve sanayi imparatorluğunu kur.

**Açıklama:**

> Fabrika Patronu'nda küçük bir tüccar olarak başlarsın, sonunda dev bir sanayi grubunu yönetirsin.
>
> GERÇEK BİR PİYASA
> • 47 hammadde ve ürün. Fiyatları arz ve talep belirler.
> • Piyasa emri, limit emir ve toplu sipariş verebilirsin.
> • Büyük emirler fiyatı oynatır. Kâr etmek için doğru anı yakalaman gerekir.
>
> ÜRETİM ZİNCİRİ
> • 15 fabrika, madenler, kuyular, santraller ve çiftlikler kurabilirsin.
> • Cevherden çeliğe, buğdaydan una, kumdan cama zincirler kurarsın.
> • Makineleri, otomasyonu, kaliteyi ve enerji ihtiyacını yönetirsin.
>
> DEPO VE LOJİSTİK
> • Malı doğru depoda saklarsın: soğuk hava, tank, metal ve dökme yük depoları.
> • 6 bölge arasında kamyon, tren ve gemiyle sevkiyat yaparsın. Otomatik rotalar da kurabilirsin.
>
> SÖZLEŞMELER VE RAKİPLER
> • Müşterilerle sözleşme yapar, ihalelere girersin.
> • 10 rakip şirket piyasayı her gün değiştirir.
> • Araştırma yaparak, kredi alarak ve doğru elemanları işe alarak öne geçersin.
>
> YAŞAYAN BİR ŞEHİR
> • Trafik, işçiler, duman, gece-gündüz ve mevsimler vardır.
> • Uygulama kapalıyken de fabrikaların çalışmaya devam eder.
>
> Reklam yok. İnternet gerekmez. Kayıt cihazında tutulur.

## İnceleme notu (App Review Information → Notes)

```
Fabrika Patronu is a single-player, fully offline economic simulation in Turkish.
No login, no ads, no in-app purchases, no data collection. Landscape only.

How to test: tap "Yeni oyun", enter any company name, then follow the
"İlk adımlar" tutorial (buy wood in Piyasa, build a furniture factory, sell).

Differentiation (Guideline 4.3): this is not a re-skin of any other app on our
account. Its core is a supply-and-demand commodity market (47 goods, limit
orders, bulk orders, price slippage), class-based warehouses, multi-step
production chains with energy and quality, customer contracts and tenders,
inter-region logistics (truck/train/ship, automated routes), 10 AI rival
companies, a research tree and offline progress. All art is drawn in code.
```

## Ekran görüntüleri

`appstore/screenshots/` klasöründeki dosyalar 2796×1290 boyutunda, iPhone 6,9" yatay ekran içindir. App Store Connect bunları küçük ekranlar için kendisi küçültür.

1. `1-menu.png`: Fabrika Patronu
2. `2-map.png`: Sanayi bölgeni kur
3. `3-zoom.png`: Yaşayan bir şehir
4. `4-market.png`: Gerçek bir piyasa
5. `5-factory.png`: Üretim zincirini yönet
6. `6-night.png`: Rakiplerini geç

## Teknik notlar

- **Cihaz ve yön:** yalnızca iPhone ve yalnızca yatay ekran. Info.plist yatay ekrana kilitlidir.
- **Gizlilik dosyası:** `app/ios/App/App/PrivacyInfo.xcprivacy` hazır. Takip yok; UserDefaults gerekçesi CA92.1.
- **Şifreleme:** yalnızca standart şifreleme kullanılır (`ITSAppUsesNonExemptEncryption = false`).
- **GitHub Actions:** iş akışı `.github/workflows/fabrikapatronu-testflight.yml` dosyasındadır. İki yolla başlar:
  - `fabrikapatronu-v*` biçiminde bir etiketle
  - Actions sekmesinden "Run workflow" ile. Bunun için iş akışının varsayılan dalda olması gerekir.
- **GitLab + Codemagic yolu:**
  1. `FabrikaPatronu` klasöründe `npm ci` çalıştır.
  2. `cd app && npm ci && npm run sync` çalıştır.
  3. Xcode arşivi `app/ios/App/App.xcodeproj` projesinden, `App` şemasıyla alınır.
