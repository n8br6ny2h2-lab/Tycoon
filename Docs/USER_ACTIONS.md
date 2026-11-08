# Sizden Gerekenler

Aşağıdakiler hesap, cihaz veya kimlik bilgisi gerektirdiği için kod tarafında tamamlanamaz. Gerçek kimlikler verilene kadar projede **sahte SDK kimliği kullanılmayacak** ve yayın yapıldığı iddia edilmeyecek.

## Şimdi (Aşama 1–2'yi denemek için)

1. **Mac + Unity 6 LTS (6000.0.x) + iOS Build Support modülü + Xcode 16 veya üstü** kurun.
2. Projeyi açıp **Factory King → 1. Setup Project** komutunu çalıştırın, sonra Play'e basın. Gördüğünüzü ve aldığınız hataları (Console) bize bildirin. Ekran görüntüsü çok işe yarar.
3. Unity'nin ürettiği `.meta` dosyalarını ve `Assets/_Project/Settings`, `Scenes`, `Resources/ShaderKeepAlive` varlıklarını commit edin.

## Onaylamanız gereken kimlikler

| Değer | Şu anki durum | Nereye girilecek |
|---|---|---|
| **Bundle ID** (ör. `com.sirketiniz.factoryking`) | Yer tutucu: `com.CHANGEME.factoryking` | Player Settings veya `FK_BUNDLE_ID` ortam değişkeni |
| **SKU** (App Store Connect) | Yok | App Store Connect |
| **Şirket / geliştirici adı** | Yer tutucu: "Factory King Studio" | Player Settings |
| **Apple Developer Team ID** | Yok | Player Settings veya `FK_APPLE_TEAM_ID` |

Başka bir uygulamanın Bundle ID değerini kullanmayın. Kimliği önce Apple Developer hesabınızda kaydedin.

## Sonraki aşamalarda gerekecekler

- **Apple Developer Program** üyeliği (yıllık ücretli), App Store Connect erişimi, imzalama sertifikaları ve provisioning (Xcode otomatik imzalama yeterli).
- **IAP ürün kimlikleri:** elmas paketleri, reklam kaldırma, başlangıç paketi, VIP, kozmetik, sezon bileti. Ürünleri App Store Connect'te siz oluşturacaksınız. Önerilen kimlik listesini Aşama 6'da vereceğiz.
- **Reklam ağı seçimi ve kimlikleri** (ör. Google AdMob / Unity LevelPlay / AppLovin MAX): App ID ve ödüllü reklam birim kimlikleri. Tercih ettiğiniz ağı belirtin.
- **Game Center:** App Store Connect'te liderlik tablosu ve başarım kimlikleri.
- **iCloud:** Xcode capability'si için iCloud container kimliği.
- **Destek ve gizlilik politikası URL'leri** (yayınlanabilir bir web adresi).
- **KVKK veri sorumlusu bilgileri** (gizlilik metni için şirket unvanı ve iletişim adresi).
- **Test cihazları:** en az bir eski iPhone (performans tabanı), bir güncel iPhone ve bir iPad.
- **Sanat yönü:** "premium" görünüm için 3D sanatçı/Blender modelleri isteyip istemediğiniz ya da prosedürel stilin geliştirilmesiyle devam edilip edilmeyeceği.
