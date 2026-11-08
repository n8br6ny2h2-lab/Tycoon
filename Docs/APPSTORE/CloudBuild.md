# Mac olmadan TestFlight: bulut derleme kurulumu

GitHub Actions oyunu iki adımda derler:
1. **Linux üzerinde Unity**, projeyi Xcode projesine çevirir. Bunu GameCI açık kaynak aracı yapar.
2. **Bulutta macOS**, Xcode projesini App Store Connect API anahtarıyla otomatik imzalar ve TestFlight'a yükler.

Bu kurulum bir kez yapılır. Sonraki her derleme tek tıktır.

## 1. Apple Developer: uygulama kimliği
1. https://developer.apple.com/account → **Certificates, Identifiers & Profiles → Identifiers → +**
2. **App IDs → App** seçin.
   - Açıklama: `Factory King`
   - Bundle ID: **Explicit**, ör. `com.adiniz.factoryking`. Başka bir uygulamanın kimliğini kullanmayın.
3. Capabilities listesinde **Game Center** ve **iCloud** kutularını işaretleyin.
   - iCloud'u "Compatible with Xcode 5" seçeneği olmadan bırakın.
   - In-App Purchase varsayılan olarak açıktır.
4. **Register**.

## 2. App Store Connect: uygulama kaydı
1. https://appstoreconnect.apple.com → **Uygulamalar → + → Yeni Uygulama**
2. Platform **iOS**, ad **Factory King: Idle Tycoon**, birincil dil **Türkçe** veya **İngilizce**.
   - Bundle ID: 1. adımdaki kimlik.
   - SKU: ör. `FACTORYKING001`.

## 3. App Store Connect API anahtarı
1. App Store Connect → **Kullanıcılar ve Erişim → Entegrasyonlar → App Store Connect API → Takım Anahtarları → +**
2. Ad: `GitHub Build`, erişim: **App Manager** → **Oluştur**.
3. Şu üç bilgiyi alın:
   - **Issuer ID** (sayfanın üstünde)
   - **Key ID** (anahtarın satırında)
   - **.p8 dosyası** (**İndir**; yalnızca bir kez indirilebilir, güvenli yerde saklayın)
4. **Team ID**: https://developer.apple.com/account → **Membership details → Team ID**

## 4. Unity lisansı (ücretsiz Personal lisans)
Bilgisayarınızda Unity Hub ile giriş yaptığınız için lisans dosyası zaten var:
`C:\ProgramData\Unity\Unity_lic.ulf`
(ProgramData gizli bir klasördür. Dosya Gezgini'nde adres çubuğuna yazarak açabilirsiniz.)
Bu dosyayı Not Defteri ile açın; içeriğin tamamını 5. adımda `UNITY_LICENSE` olarak gireceksiniz.

## 5. GitHub'a gizli bilgileri girin
Depo: https://github.com/n8br6ny2h2-lab/Tycoon → **Settings → Secrets and variables → Actions**

**Secrets** sekmesi (gizli; kimse göremez, sohbete yazmayın):

| Ad | Değer |
|---|---|
| `UNITY_LICENSE` | `Unity_lic.ulf` dosyasının tüm içeriği |
| `UNITY_EMAIL` | Unity hesabınızın e-postası |
| `UNITY_PASSWORD` | Unity hesabınızın şifresi |
| `ASC_API_KEY_P8` | İndirdiğiniz `.p8` dosyasının tüm içeriği (`-----BEGIN PRIVATE KEY-----` dahil) |

**Variables** sekmesi:

| Ad | Değer |
|---|---|
| `FK_BUNDLE_ID` | ör. `com.adiniz.factoryking` |
| `FK_APPLE_TEAM_ID` | Team ID (10 karakter) |
| `ASC_KEY_ID` | Key ID |
| `ASC_ISSUER_ID` | Issuer ID |

## 6. Derlemeyi başlatın
1. GitHub → depo → **Actions → iOS TestFlight → Run workflow**
2. Dal olarak `claude/zealous-thompson-930ma0`'ı seçin.
   - "Development build" ilk testler için açık kalsın: test mağazası, test reklamları ve geliştirici araçları çalışır.
3. Süre: ilk derleme 40–70 dakika, sonrakiler önbellek sayesinde daha kısa.
4. Bitince App Store Connect → **TestFlight**'ta derleme görünür. Apple'ın işlemesi 5–30 dakika sürer.
   - "Eksik uyumluluk" sorusu çıkmaz; şifreleme beyanı derlemede "Hayır" olarak ayarlı.

## 7. iPhone'a yükleyin
1. App Store Connect → TestFlight → **Dahili Test → +** → kendinizi test kullanıcısı olarak ekleyin.
2. iPhone'a App Store'dan **TestFlight** uygulamasını kurun.
3. E-postadaki davetle ya da TestFlight uygulamasından **Factory King**'i yükleyin.

## Ücretler
- GitHub Actions, özel depolarda aylık ücretsiz dakika verir. macOS dakikaları bu kotadan 10 kat sayılır; bir derleme kotanın önemli bir kısmını kullanabilir. Güncel limitleri GitHub → Settings → Billing'den kontrol edin.
- Unity Personal ücretsizdir.

## Sorun çıkarsa
- **Unity adımı lisans hatası verirse:** `UNITY_LICENSE`, `UNITY_EMAIL` ve `UNITY_PASSWORD` değerlerini kontrol edin. Gerekirse Unity Hub'da çıkış yapıp tekrar girin; bu lisans dosyasını yeniler.
- **İmzalama hatası verirse:** Bundle ID'nin App Store Connect'teki uygulamayla aynı olduğunu ve API anahtarının **App Manager** yetkisinde olduğunu kontrol edin.
- **Hata devam ederse:** Actions sayfasındaki kırmızı adımın günlüğünü (log) kopyalayıp bana gönderin.
