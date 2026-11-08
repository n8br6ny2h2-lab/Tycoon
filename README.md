# FACTORY KING — Fabrika Kralı

iOS (iPhone ve iPad) için 3D idle fabrika tycoon oyunu. Unity 6 LTS, URP ve C# ile geliştiriliyor. Oyuncu küçük bir atölyeyle başlar ve beş fabrikadan oluşan bir endüstri imparatorluğu kurar: Demir-Çelik, Gıda, Plastik, Tekstil ve Elektronik.

> **Durum:** Aşama 1 (proje, sahne, kamera) ve Aşama 2 (üretim motoru, makineler, ekonomi) tamamlandı.
> Proje henüz Unity Editor'de açılıp çalıştırılmadı, çünkü geliştirme ortamında Unity yok. Ayrıntılar ve eksikler: [Docs/PHASES.md](Docs/PHASES.md).

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
    Scripts/Runtime/                 Unity katmanı (görseller, kamera, girdi, gün/gece, geçici HUD)
    Scripts/Editor/                  Tek tıkla kurulum (URP, iOS ayarları, sahne) ve iOS derleme betiği
    Tests/EditMode/                  NUnit testleri (Unity Test Runner ve dotnet test ortak kullanır)
    Resources/Localization/strings.txt   Türkçe/İngilizce metinler
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt
  Tools/run-checks.sh                Unity olmadan derleme ve test
  Tools/DotNet/                      Core derleme, test ve runtime derleme kontrolü projeleri
Docs/                                Mimari, aşama durumu, sizden gerekenler
```

## Hızlı başlangıç (Mac + Unity)

1. Unity Hub'dan **Unity 6 LTS (6000.0.x)** sürümünü **iOS Build Support** modülüyle kurun. Xcode 16 veya üstünü kurun.
2. Unity Hub'da **Add → Add project from disk** seçip `FactoryKing/` klasörünü gösterin. Farklı bir 6000.0.x yaması kuruluysa Hub projeyi o sürümle açmayı önerir.
3. Proje açılınca menüden **Factory King → 1. Setup Project (URP + iOS + Scene)** komutunu çalıştırın.
   - Bu komut URP pipeline varlığını, iOS Player Settings ayarlarını, `Assets/_Project/Scenes/Factory.unity` sahnesini ve derleme için gereken shader materyallerini oluşturur.
4. `Factory` sahnesini açıp **Play** tuşuna basın. Boş bir sahnede bile `GameBootstrap` oyunu otomatik başlatır.
5. Testleri **Window → General → Test Runner → EditMode → Run All** ile çalıştırın.
6. iOS Xcode projesini **Factory King → Build iOS (Xcode project)** ile üretin. Çıktı `FactoryKing/Builds/iOS` klasörüne yazılır.

> URP kurulumu sorun çıkarırsa: Unity Hub'da **Universal 3D** şablonuyla yeni bir proje oluşturup `FactoryKing/Assets/_Project` klasörünü içine kopyalayın.

## Unity olmadan kontroller

```bash
FactoryKing/Tools/run-checks.sh     # .NET 8 SDK gerekir
```

Bu betik üç şey yapar: çekirdeği Unity ile aynı ayarlarla (netstandard2.1 / C# 9) derler, 76 NUnit testini çalıştırır ve Unity runtime betiklerini UnityEngine referans derlemelerine karşı derleme kontrolünden geçirir.

## Belgeler

- [Docs/PHASES.md](Docs/PHASES.md): her aşamada neyin bittiği, test sonuçları, eksikler ve sonraki adımlar
- [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md): sistem mimarisi ve ekonomi modeli
- [Docs/USER_ACTIONS.md](Docs/USER_ACTIONS.md): sizden gereken hesaplar, kimlikler ve kurulum adımları
