# Geliştirme Aşamaları — Durum

Bu belge her aşamada **gerçekten** neyin yapıldığını, neyin doğrulandığını ve neyin eksik olduğunu kaydeder. Tamamlanmamış hiçbir özellik tamamlanmış gibi gösterilmez.

Durum işaretleri: ✅ yapıldı ve otomatik testle doğrulandı · 🟡 yazıldı, Unity Editor'de henüz çalıştırılmadı · ⬜ yapılmadı

---

## Ortam incelemesi (başlangıç)

- Depo boştu: başka proje yok, silinen ya da üzerine yazılan bir şey yok. Proje ayrı bir `FactoryKing/` klasöründe.
- Geliştirme ortamı Linux bulut konteyneri. **Unity Editor, Xcode, macOS ve Blender yok**, kurulamaz (Unity lisans/indirme ve Apple araçları gerekir).
- Kurulan araç: .NET 8 SDK (Ubuntu deposundan). Unity bağımlılığı olmayan çekirdek bununla derlenip test ediliyor.
- Unity runtime betikleri NuGet'teki `UnityEngine.Modules 2021.3.33` referans derlemelerine karşı derleme kontrolünden geçiyor. Bu API, Unity 6'nın bir alt kümesidir.
- Hedef Unity sürümü: **6000.0.58f2 (Unity 6 LTS)**. Paketler: URP 17.0.4, Test Framework 1.4.5, uGUI 2.0.0 ve yerleşik modüller.

## AŞAMA 1 — Proje, klasör yapısı, kamera, temel 3D sahne

| İş | Durum |
|---|---|
| Unity proje iskeleti (`Packages/manifest.json`, `ProjectVersion.txt`, assembly definition'lar) | 🟡 |
| Tek tıkla kurulum: URP varlığı ve renderer, iOS ayarları (IL2CPP, iOS 15+, iPhone+iPad, dikey, Linear), sahne, build listesi | 🟡 |
| İzometrik kamera: kaydırma ve atalet, pinch/tekerlek zoom, sınırlar, dokunarak seçme, yumuşak odaklanma | 🟡 (derleme ✅) |
| Prosedürel 3D: pahlı kenarlı mesh üretici, 15 makine arketipi, fabrika salonu, duvar/pencere, avlu, yol, ağaçlar, lambalar | 🟡 (derleme ✅) |
| Animasyonlu makineler, hareketli bantlar, robot kol, forklift, kamyon, işçiler | 🟡 (derleme ✅) |
| Gündüz/gece döngüsü, gölgeler, gece pencere ve lamba ışıkları | 🟡 (derleme ✅) |
| Optimizasyon: paylaşılan mesh, SRP Batcher/instancing, LODGroup, nesne havuzu, tek Update döngüsü, kare başı tahsissiz bant | 🟡 (derleme ✅) |
| iOS Xcode projesi üreten derleme betiği (`BuildScript.BuildIOS`) | 🟡 |

**Oluşturulan dosyalar:** `FactoryKing/Assets/_Project/Scripts/Runtime/**` (17 dosya), `Scripts/Editor/ProjectSetup.cs`, `Scripts/Editor/BuildScript.cs`, 4 `.asmdef`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`.

**Eksikler / riskler:**
- Sahne ve görseller hiç **görülmedi**. Unity'de ilk açılışta renk, ölçek, kamera açısı ve performans ayarı gerekecek.
- Grafikler kod ile üretilen stilize modeller. "Premium" görünüm için sanatçı veya Blender modelleri, dokular ve post-processing gerekir. Mimari, `MachineModelFactory` yerine FBX prefab kullanmaya hazır. Blender otomasyon hattı henüz yazılmadı.
- `ProjectSetup.SetupUrp()` URP 17 API'si ile yazıldı ama Editor'de denenmedi. Sorun çıkarsa README'deki "Universal 3D şablonu" yolunu kullanın.
- `.meta` dosyaları ilk açılışta Unity tarafından üretilecek. İlk açılıştan sonra bunlar commit edilmeli.

## AŞAMA 2 — Üretim motoru, makineler, ürünler, ekonomi

| İş | Durum |
|---|---|
| `BigDouble` büyük sayı tipi (taşma ve NaN güvenli, metin dönüşümlü) | ✅ |
| TR/EN sayı biçimlendirme (1,23 B / 1.23K, aa-ab-… son ekleri) | ✅ |
| 5 fabrika (Çelik, Gıda, Plastik, Tekstil, Elektronik), her biri 6 aşamalı zincir ve 3 hat | ✅ |
| 30 makine varyasyonu, 8 kategorinin tamamı (≥25 şartı) | ✅ |
| Gerçek tamponlu deterministik üretim simülasyonu (Starved/Blocked durumları) | ✅ |
| Seviye 1–100: hız, kapasite, bakım, maliyet; x1/x10/MAKS | ✅ |
| Giderler: hammadde, elektrik, işçi ücreti, bakım; net kâr, gelir/sn, satış/dk, saatlik kâr, şirket değeri | ✅ |
| Kilitlenmeyen borç modeli | ✅ |
| Kaynak kimlikli modifikatör yığını (yönetici, prestij ve araştırma için hazır) | ✅ |
| Analitik hat raporu (darboğaz), simülasyonla tutarlılığı testli | ✅ |
| İçerik doğrulayıcı (zincir uyumu, tampon/parti, her seviyede kârlılık) | ✅ |
| TR/EN yerelleştirme tablosu (90 satır; tüm içerik anahtarları iki dilde) | ✅ |
| Geçici geliştirici HUD'u: para, gelir, fabrika ve hat satın alma, makine detayı ve yükseltme, dil değiştirme | 🟡 (derleme ✅) |

**Test sonuçları (`FactoryKing/Tools/run-checks.sh`):**
- Core derleme (netstandard2.1, C# 9, uyarılar hata sayılır): **başarılı, 0 uyarı**
- NUnit: **76/76 geçti**: BigDouble (12), biçimlendirme (17), yükseltme formülleri (8), içerik (6), üretim (15), servisler (13), yerelleştirme (5)
  - Simülasyon ile analitik model, 5 fabrikada seviye 1/10/25/60/100'de %5 içinde örtüşüyor
  - Birim korunumu, tampon sınırları, determinizm, kare hızından bağımsızlık, sıfır parada kilitlenmeme, büyük/NaN/negatif adım güvenliği
- Runtime derleme kontrolü (UnityEngine 2021.3 referansları): **başarılı, 0 uyarı**
- Editor betikleri (`Scripts/Editor`) bu ortamda derlenemiyor (UnityEditor ve URP derlemesi yok).

**Eksikler:**
- Ekonomi değerleri şimdilik `DefaultContent.cs` (koddaki veri) ve `EconomyTuning` içinde. ScriptableObject veya JSON ile düzenleme Aşama 3'te eklenecek.
- İlerleme hızı ayarlanmadı. Referans değerler `Docs/ARCHITECTURE.md` içinde.

## AŞAMA 3 — Idle kazanç, çevrimdışı kazanç, yükseltmeler (sıradaki)

⬜ SaveManager (JSON, sürümleme, yedek dosya, bozuk kayıt kurtarma) — ilerlemenin kaybolmaması için öncelikli
⬜ OfflineEarningsManager: `LineAnalyzer` ile kapalı form hesap, 8 saat sınırı, özet ekranı verisi
⬜ Saat hilesi koruması: son görülen UTC, monotonik süre ve geri giden saat tespiti
⬜ Ekonomi verisinin ScriptableObject/JSON'a taşınması
⬜ 2 kat çevrimdışı ödül için reklam arayüzü (kanca; gerçek SDK Aşama 6'da)

## AŞAMA 4–8

⬜ Aşama 4: yöneticiler (5 karakter, nadirlik, seviye, atama), görevler (günlük/haftalık), başarımlar, prestij, araştırma
⬜ Aşama 5: UI Toolkit ile 16 ekran, ses ve müzik, animasyon cilası
⬜ Aşama 6: reklam (ödüllü), StoreKit/Unity IAP, iCloud, Game Center
⬜ Aşama 7: cihazda performans profilleme, çözünürlük testleri, hata ayıklama
⬜ Aşama 8: App Store materyalleri, gizlilik beyanları, TestFlight

## Nihai kabul kriterlerine göre durum

| Kriter | Durum |
|---|---|
| Gerçek 3D fabrika sahnesi çalışmalı | 🟡 kod hazır, Unity'de doğrulanmadı |
| Üretim gerçek hesaplamalarla işlemeli | ✅ |
| Para kazanılabilmeli, makine yükseltilebilmeli | ✅ (Core) / 🟡 (HUD) |
| Çevrimdışı kazanç | ⬜ Aşama 3 |
| Beş fabrika oynanabilir | ✅ simülasyonda / 🟡 görselde |
| Yönetici sistemi | ⬜ Aşama 4 |
| Kayıt sistemi | ⬜ Aşama 3 |
| Türkçe ve İngilizce | ✅ veri ve dil değiştirme / 🟡 HUD |
| Reklam ve satın alma test edilebilir | ⬜ Aşama 6 |
| iOS derlemesi alınabilmeli | 🟡 betik hazır, Mac + Unity + Xcode gerekir |
| Kritik hata yok | Core testleri temiz, Unity'de doğrulanmadı |
