# FACTORY KING — Fabrika Kralı

iOS (iPhone ve iPad) için 3D idle fabrika tycoon oyunu. Unity 6 LTS, URP ve C# ile geliştiriliyor. Oyuncu küçük bir atölyeyle başlar ve beş fabrikadan oluşan bir endüstri imparatorluğu kurar: Demir-Çelik, Gıda, Plastik, Tekstil ve Elektronik.

> **Durum:** Sekiz aşamanın tamamı kod ve doküman olarak yazıldı; 137 otomatik test geçiyor.
> Proje henüz Unity Editor'de açılıp bir iPhone'da çalıştırılmadı, çünkü geliştirme ortamında Unity, Mac ve Xcode yok.
> Yayından önce gerekenler: Unity'de ilk açılış ve düzeltmeler, cihaz testleri, Apple hesabı, reklam SDK'sı ve ekran görüntüleri.
> Ayrıntılar: [Docs/PHASES.md](Docs/PHASES.md) · Sizden gerekenler: [Docs/USER_ACTIONS.md](Docs/USER_ACTIONS.md)

## Klasör yapısı

```
FactoryKing/                         Unity projesi (Unity Hub ile bu klasörü açın)
  Assets/_Project/
    Scripts/Core/                    Motordan bağımsız oyun çekirdeği (saf C#, noEngineReferences)
      Numerics/                      BigDouble büyük sayı tipi, TR/EN sayı biçimlendirme
      Content/                       Ürün/makine/fabrika tanımları (5 fabrika x 6 makine), doğrulayıcı
      Economy/                       Yükseltme formülleri, modifikatörler, cüzdan, gelir defteri, hat analizi
      Simulation/                    GameState, üretim simülatörü, servisler, GameSession
      Localization/                  TSV tabanlı TR/EN dil tablosu
      Persistence/                   JSON, sürümlü/sağlamalı kayıt, yedekten kurtarma, iCloud çakışma çözümü
      Time/                          Saat hilesi koruması (güvenilir zaman)
      Meta/                          Çevrimdışı kazanç, yöneticiler, prestij, araştırma, görevler, siparişler,
                                     başarımlar, günlük ödül, etkinlik, hızlandırma
      Monetization/                  Mağaza kataloğu, satın alma işleme, ödüllü reklam, analitik
    Scripts/Runtime/                 Unity katmanı: 3D görseller, kamera, UI Toolkit arayüzü, ses, kayıt,
                                     iCloud, Game Center, bildirimler, mağaza/reklam adaptörleri
    Scripts/Purchasing/              Unity IAP adaptörü (paket kurulunca derlenir)
    Scripts/Editor/                  Kurulum, iOS derleme, Xcode yetenekleri, ekran görüntüsü aracı
    Tests/EditMode/                  NUnit testleri (Unity Test Runner ve dotnet test ortak kullanır)
    Tests/PlayMode/                  Unity içinde oyun açılış testleri
    Art/AppIcon.png                  1024x1024 uygulama ikonu
    Resources/Localization/strings.txt   Türkçe/İngilizce metinler
  Assets/Plugins/iOS/                iCloud ve bildirim native eklentisi, gizlilik manifesti
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt
  Tools/run-checks.sh                Unity olmadan derleme ve test
  Tools/DotNet/                      Core derleme, test ve runtime derleme kontrolü projeleri
  Tools/Icon/make_icon.py            İkon üreteci
Docs/                                Mimari, aşama durumu, sizden gerekenler
Docs/APPSTORE/                       Mağaza metinleri, gizlilik, yaş derecesi, IAP, Game Center, reklam, TestFlight
Docs/web/                            Gizlilik politikası ve destek sayfası (TR/EN, yayınlanacak)
```

## Hızlı başlangıç (Mac + Unity)

1. Unity Hub'dan **Unity 6 LTS (6000.0.x)** sürümünü **iOS Build Support** modülüyle kurun. Xcode 16 veya üstünü kurun.
2. Unity Hub'da **Add → Add project from disk** seçip `FactoryKing/` klasörünü gösterin. Farklı bir 6000.0.x yaması kuruluysa Hub projeyi o sürümle açmayı önerir.
3. Proje açılınca menüden **Factory King → 1. Setup Project (URP + iOS + Scene)** komutunu çalıştırın.
   - Bu komut URP pipeline varlığını, iOS Player Settings ayarlarını, `Assets/_Project/Scenes/Factory.unity` sahnesini ve derleme için gereken shader materyallerini oluşturur.
4. `Factory` sahnesini açıp **Play** tuşuna basın. Boş bir sahnede bile `GameBootstrap` oyunu otomatik başlatır.
5. Testleri **Window → General → Test Runner → EditMode → Run All** ile çalıştırın.
6. iOS Xcode projesini **Factory King → Build iOS (Xcode project)** ile üretin. Çıktı `FactoryKing/Builds/iOS` klasörüne yazılır.

> URP kurulumu sorun çıkarırsa: Unity Hub'da **Universal 3D** şablonuyla yeni bir proje oluşturup `FactoryKing/Assets/_Project` ve `FactoryKing/Assets/Plugins` klasörlerini içine kopyalayın.

## Unity olmadan kontroller

```bash
FactoryKing/Tools/run-checks.sh     # .NET 8 SDK gerekir
```

Bu betik üç şey yapar: çekirdeği Unity ile aynı ayarlarla (netstandard2.1 / C# 9) derler, 137 NUnit testini çalıştırır ve Unity runtime betiklerini UnityEngine referans derlemelerine karşı derleme kontrolünden geçirir.

## Belgeler

- [Docs/PHASES.md](Docs/PHASES.md): her aşamada neyin bittiği, test sonuçları, eksikler ve sonraki adımlar
- [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md): sistem mimarisi ve ekonomi modeli
- [Docs/USER_ACTIONS.md](Docs/USER_ACTIONS.md): sizden gereken hesaplar, kimlikler ve kurulum adımları
- [Docs/APPSTORE/](Docs/APPSTORE/): App Store ve TestFlight hazırlığı
