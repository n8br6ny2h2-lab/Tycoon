# Demir Baronu · App Store'a çıkış

Oyun (`web/play.html`) Capacitor ile iOS uygulamasına sarılır (`app/`). Bulutta macOS üzerinde derlenir ve TestFlight'a yüklenir. **Mac gerekmez.**

```
web/play.html ──build-web──▶ app/www (yazı tipleri gömülü, internetsiz) ──cap sync──▶ app/ios (Xcode projesi)
                                                                      GitHub Actions (macOS) ──▶ imza ──▶ TestFlight ──▶ App Store
```

Senin yapacakların **A–F** adımları. Hepsi bir kez yapılır; sonraki her sürüm tek tıktır.

---

## A. Apple Developer: uygulama kimliği (5 dk)

1. https://developer.apple.com/account → **Certificates, Identifiers & Profiles → Identifiers → +**
2. **App IDs → App → Continue**
   - Description: `Demir Baronu`
   - Bundle ID: **Explicit**, örneğin `com.adiniz.demirbaronu`. Bundle ID sonradan değiştirilemez.
   - Capabilities: hiçbirini işaretleme. Oyun Game Center, iCloud ya da bildirim kullanmıyor.
3. **Continue → Register**

## B. App Store Connect: uygulama kaydı (5 dk)

1. https://appstoreconnect.apple.com → **Uygulamalar → + → Yeni Uygulama**
2. Doldur:
   - Platform: **iOS**
   - Ad: **Demir Baronu: Çelik Ticareti** (ad alınmışsa: *Demir Baronu: Metal Ticareti*)
   - Birincil dil: **Türkçe**
   - Paket kimliği: A adımındaki Bundle ID
   - SKU: `DEMIRBARONU001`
   - Kullanıcı erişimi: Tam erişim

## C. App Store Connect API anahtarı (5 dk)

Factory King için anahtar oluşturduysan ve GitHub'a girdiysen bu adımı ve D'deki aynı adlı ayarları atla.

1. App Store Connect → **Kullanıcılar ve Erişim → Entegrasyonlar → App Store Connect API → Takım Anahtarları → +**
2. Ad: `GitHub Build`, Erişim: **Admin**. Bulutta otomatik imza sertifikası üretebilmek için Admin gerekir.
3. Kaydet:
   - **Issuer ID** (sayfanın üstünde)
   - **Key ID** (anahtarın satırında)
   - **.p8 dosyası**: İndir. Yalnızca bir kez indirilebilir. Kimseyle ve sohbette paylaşma.
4. **Team ID**: https://developer.apple.com/account → **Membership details**

## D. GitHub ayarları (5 dk)

Depo → **Settings → Secrets and variables → Actions**

| Sekme | Ad | Değer |
|---|---|---|
| Secrets | `ASC_API_KEY_P8` | .p8 dosyasının tüm içeriği (`-----BEGIN PRIVATE KEY-----` dahil) |
| Variables | `DB_BUNDLE_ID` | A adımındaki Bundle ID |
| Variables | `DB_APPLE_TEAM_ID` | Team ID (10 karakter). `FK_APPLE_TEAM_ID` varsa o da kullanılır |
| Variables | `ASC_KEY_ID` | Key ID |
| Variables | `ASC_ISSUER_ID` | Issuer ID |

## E. Derle ve TestFlight'a yükle (20–30 dk bekleme)

1. Depo → **Actions → "Demir Baronu → TestFlight" → Run workflow**
   - Branch: oyunun olduğu dal (şu an `claude/brave-albattani-w53sfc`; birleştirdikten sonra `main`)
   - Version: ilk sürüm için `1.0.0`
2. Yaklaşık 10–15 dakikada biter. Kırmızı biterse en alttaki hata satırlarını bana gönder.
3. App Store Connect → uygulaman → **TestFlight**. Yapı 5–30 dakikada "İşleniyor"dan çıkar.
4. **Dahili Test → +** ile bir grup aç, kendini ekle.
5. iPhone'a **TestFlight** uygulamasını kur. Davet e-postasından oyunu yükle ve dene.

## F. Mağaza sayfası ve incelemeye gönderme

App Store Connect → uygulaman → **1.0 Hazırlanıyor**:

1. **Ekran görüntüleri:** `appstore/screenshots/` klasöründeki 6 görseli **iPhone 6,9"** alanına sürükle (1290×2796, Apple kabul eder). Uygulama yalnızca iPhone olduğu için iPad görseli gerekmez.
2. **Metinler:** aşağıdaki tabloları kopyala.
3. **Yapı:** "Yapı" bölümünde + ile TestFlight'a yüklenen yapıyı seç.
4. **Uygulama Gizliliği:**
   - Gizlilik politikası URL'si gir (bkz. G).
   - "Veri toplama" sorusuna **Hayır, bu uygulamadan veri toplamıyoruz** de.
5. **Yaş derecesi:** Anketteki tüm içerik sorularına **Hiçbiri / Hayır** de. Oyunda kumar, şiddet ya da sohbet yok. Sonuç 4+ çıkar.
6. **Fiyatlandırma:** Ücretsiz, tüm ülkeler (ya da yalnızca Türkiye).
7. **Kategori:** Birincil **Oyunlar › Simülasyon**, ikincil **Oyunlar › Strateji**.
8. **Uygulama İnceleme bilgileri:**
   - Ad, telefon, e-posta.
   - Giriş bilgisi gerekmez. "Oturum açma gerekli" kutusunu boş bırak.
   - Not: `Tamamen çevrimdışı çalışan, gün tabanlı bir ticaret simülasyonu. Hesap, reklam ya da uygulama içi satın alma yok. Ana menüden "Yeni oyun" ile başlanır.`
9. **İncelemeye Gönder.** İnceleme genelde 1–3 gün sürer. Onaylanınca **Yayınla**.

### Türkçe metinler

| Alan | Değer | Sınır |
|---|---|---|
| Ad | Demir Baronu: Çelik Ticareti | 28/30 |
| Alt başlık | Boru, profil, sac al-sat | 24/30 |
| Anahtar kelimeler | `tycoon,ticaret,demir,çelik,sac,boru,profil,inşaat,tüccar,işletme,simülasyon,depo,ihale,toptan` | 93/100 |
| Tanıtım metni | Yeni: 7 rakip şirket, kapalı zarf ihaleler ve gerçek hayat gibi tahsilat. Müşteri ödemezse icraya ver, dava aç. Bakalım baron olabilecek misin? | 143/170 |

**Açıklama**

```
Cebinde 5.000 ₺ ve bir telefon var. Deponu, kamyonun, kredin yok. Boru, profil, sac ve inşaat demiri ticaretinde sıfırdan başla ve sektörün baronu ol.

KOMİSYONCULUKTAN BARONLUĞA
• Sermayesiz başla: siparişi al, mal fabrikadan doğrudan müşteriye gitsin, farkı cebine koy.
• Para biriktir, depo kirala, ucuzken stok al, stoktan satarak marjını büyüt.
• Komisyoncu → Esnaf → Toptancı → Bayi → Distribütör → Demir Baronu.

48 ÜRÜN, GERÇEK ÖLÇÜLER
• Siyah sac, DKP, galvaniz, baklavalı, Hardox; kutu ve dikdörtgen profil, köşebent, NPU, IPE, HEA, HEB; sanayi, doğalgaz, dikişsiz ve spiral boru; nervürlü demir, hasır çelik; paslanmaz ve alüminyum.
• Her üründe norm, ölçü ve boy ağırlığı.

15 ŞEHİR, DEPO VE DÜKKAN
• İkitelli'den Ostim'e, Başpınar'dan Çiğli'ye tesis aç.
• Dükkanına her gün kapıdan müşteri gelsin.
• Forklift, tavan vinci, kamyon, TIR; giyotin makas, abkant, fiber lazer, bobin dilme hattı.
• Şehirler arasında fiyat farkı var: ucuz yerden al, pahalı yerde sat.

YAŞAYAN PİYASA
• Dolar, hurda, inşaat sezonu, ihaleler, nikel fiyatı: haberleri takip et, fiyat hareketinden önce davran.
• Fabrikadan toplu alım, gemiyle ithalat ve kur riski.

KIYASIYA REKABET
• 7 rakip şirket şube açar, müşterini kapar, fiyat savaşı başlatır.
• Kapalı zarf ihalelerde viyadük, hastane, metro projelerini kazan.
• Zayıflayan rakibi satın al, sektör lideri ol.

GERÇEK HAYAT GİBİ
• Vadeli sat ama dikkat: müşteri erteler, kısmen öder, çek karşılıksız çıkar.
• İcra takibi başlat, dava aç, uzlaş. Borçlu konkordato ilan edebilir.
• Kalite tazminatı davaları, iş kazaları, hırsızlık, yangın, vergi denetimi.
• Ay sonunda kira, maaş, faiz ve vergi. Nakit akışını yönetemeyen batar.

VE DAHA FAZLASI
• Fuarlar, bayramlar, meydan okumalar, mevsimler, Yılın Tüccarı ödülü.
• Kur Krizi ve Deprem Bölgesi senaryoları.
• Kolay, Normal ve Zor seviyeler. Şirketini satıp miras puanıyla yeniden başla.

Tamamen çevrimdışı. Hesap yok, reklam yok, veri toplama yok.
```

### İngilizce metinler (isteğe bağlı ikinci dil)

| Alan | Değer | Sınır |
|---|---|---|
| Ad | Iron Baron: Steel Trade | 23/30 |
| Alt başlık | Trade pipes, beams & sheets | 27/30 |
| Anahtar kelimeler | `tycoon,trade,steel,iron,merchant,business,simulator,tender,warehouse,pipes,beams,sheet metal,rebar` | 98/100 |
| Tanıtım metni | New: seven rival companies, sealed-bid tenders and real-life debt collection. If a customer will not pay, take them to court. Can you become the baron? | 151/170 |

Oyunun içi şu an yalnızca Türkçedir. İngilizce mağaza sayfası açılırsa açıklamaya "Game language: Turkish" yazılmalı ya da önce oyun çevrilmeli.

## G. Gizlilik politikası ve destek adresi

Apple bir **gizlilik politikası URL'si** ve bir **destek URL'si** ister. Hazır sayfa: `web/privacy.html`. İçindeki `[İLETİŞİM E-POSTASI]` ve `[GELİŞTİRİCİ ADI]` alanlarını doldur ve sayfayı herkese açık bir adreste yayınla. Seçenekler:

- **GitHub Pages:** Depo herkese açıksa Settings → Pages. Depo gizliyse ücretsiz yol, ayrı ve herkese açık küçük bir depoya yalnızca bu dosyayı koymaktır.
- **GitLab Pages:** GitLab'de herkese açık bir projeye `public/index.html` olarak koy.
- Destek URL'si olarak aynı sayfa ya da bir iletişim sayfası kullanılabilir.

## Güncelleme yayınlamak

1. Oyunda değişiklik yap (`web/play.html`).
2. Actions → **Run workflow**. Version'ı artır: `1.0.1`, `1.1.0`…
3. App Store Connect'te yeni sürüm aç, yeni yapıyı seç, "Bu sürümdeki yenilikler"i yaz, gönder.

Yalnızca TestFlight'ta denemek için version'ı aynı bırakabilirsin. Yapı numarası her çalıştırmada kendiliğinden artar.

## Sık ret sebepleri ve önlemleri

| Kural | Risk | Önlem |
|---|---|---|
| 4.2 En az işlevsellik ("web sitesi paketlemiş") | Düşük | Oyun telefona gömülü, internetsiz çalışır. Titreşim ve kalıcı kayıt yerel eklentilerle yapılır, dış bağlantı yoktur |
| 2.1 Çökme / boş ekran | — | TestFlight'ta mutlaka dene |
| 5.1.1 Gizlilik | — | Gizlilik URL'si çalışır olmalı, "veri toplanmıyor" beyanı doğru olmalı |
| 2.3 Meta veri | — | Ekran görüntüleri gerçek oyun ekranlarıdır; adda başka markanın adı yok |

## Teknik notlar

- **Capacitor 8.6.0:** Swift Package Manager kullanır, CocoaPods yok. Eklentiler: Haptics (titreşim motoru), Preferences (kayıtlar iOS'un silmediği uygulama deposuna da yazılır), Status Bar.
- **Cihaz ve sürüm:** Yalnızca iPhone, dikey ekran, karanlık tema. En düşük iOS sürümü 15.
- **Şifreleme:** `ITSAppUsesNonExemptEncryption = false` olduğu için her yüklemede şifreleme sorusu çıkmaz.
- **Yerelde web kısmını derlemek:** `cd DemirBaronu/app && npm ci && npm run sync`
- **İkon ve açılış ekranını yeniden üretmek:** `resources/icon.png` (1024×1024, saydamlıksız) ve `resources/splash.png` dosyalarını değiştirip `npm run assets`.
