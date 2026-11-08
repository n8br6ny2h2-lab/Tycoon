# Factory King — Mimari

## Katmanlar

| Katman | Assembly | Unity bağımlılığı | Görev |
|---|---|---|---|
| Core | `FactoryKing.Core` | Yok (`noEngineReferences: true`) | Tüm oyun kuralları: sayılar, içerik, simülasyon, ekonomi, yerelleştirme |
| Runtime | `FactoryKing.Runtime` | UnityEngine | Görselleştirme, kamera, girdi, gün/gece, sürücü, geçici HUD |
| Editor | `FactoryKing.Editor` | UnityEditor + URP | Proje kurulumu, iOS derleme, içerik doğrulama |
| Tests | `FactoryKing.Tests.EditMode` | Test Runner | Core testleri (aynı dosyalar `dotnet test` ile de çalışır) |

Kural: **Oyun mantığı Core dışında yazılmaz.** Unity katmanı yalnızca Core durumunu okur ve Core servislerini çağırır. Bu sayede ekonomi, üretim, kayıt ve çevrimdışı hesaplar Unity açmadan test edilebilir.

## Core sistemleri (Aşama 1–2)

```
GameSession (bileşim kökü, kural içermez)
├── GameState            Kaydedilebilir ilerleme: para, borç, elmas, fabrikalar/hatlar/makineler, istatistikler
├── Wallet               Tüm para/elmas işlemleri (satın alma nakit ister; işletme gideri borca dönüşebilir)
├── ModifierStack        Kaynak kimliğiyle çarpanlar (yönetici, prestij, araştırma, reklam bonusu...)
├── EconomyLedger        60 sn kayan pencere: gelir/sn, gider kalemleri, satış/dk, saatlik kâr
├── ProductionSimulator  Deterministik ayrık üretim simülasyonu
├── UpgradeService       Makine seviyesi 1–100, x1/x10/MAKS fiyat teklifi
└── FactoryService       Fabrika/hat satın alma (sıralı), aktif fabrika
LineAnalyzer             Kapalı form kararlı-durum analizi (darboğaz, gelir/gider/sn)
UpgradeMath              Saf formüller (hız, kapasite, bakım, maliyet serisi, görsel kademe)
ContentValidator         İçerik hatalarını ve kârsız seviyeleri yayından önce yakalar
```

Talimattaki yönetici sınıfları şöyle karşılanıyor: EconomyManager → `Wallet` + `EconomyLedger`, ProductionManager → `ProductionSimulator`, UpgradeManager → `UpgradeService`, FactoryManager → `FactoryService`, LocalizationManager → `LocalizationTable`. Tek bir dev GameManager yok; `GameBootstrap` yalnızca nesneleri birbirine bağlıyor. Sonraki aşamalardaki yöneticiler (Save, Offline, IAP, Ads, Mission, Achievement, Audio, Analytics, CloudSave, Employee) aynı desenle ayrı servisler olarak eklenecek.

## Üretim modeli

Her hat altı aşamadan geçer: **Hammadde girişi → İşleme → Üretim → Paketleme → Depolama → Satış/Sevkiyat**.

- Her makinenin bir döngü süresi, döngü başına girdi/çıktı miktarı ve bir çıkış tamponu (kapasite) vardır.
- Hammadde makinesi her birim için hammadde maliyeti öder. Sonraki makineler girdisini bir önceki makinenin tamponundan alır. Depo yalnızca malı taşır, sevkiyat rampası ise tır yükü kadar malı satar.
- Bir makine çıkış tamponu doluysa **Blocked**, girdi yoksa **Starved** olur. Sıkışan bir makine darboğazı görünür kılar.
- Simülasyon, her hatı en hızlı makinesinin döngüsünden kısa alt adımlarla ve yukarı akıştan aşağı akışa doğru ilerletir. Bu yüzden sonuç kare hızından bağımsızdır (test edildi).
- Görseller bu durumu birebir okur. Bant üzerindeki kasa sayısı gerçek tampon sayısıdır, tır gerçek yükü taşır, depo raf doluluğu gerçek stoğu gösterir, makine hareketleri yalnızca gerçek döngü sürerken oynar.

## Ekonomi

- **Para:** `BigDouble` (mantis × 10^üs). NaN ve sonsuz değer üretmez, `double` sınırını aşan değerleri taşır, metne dönüştürüp geri okunabilir (kayıt için).
- **Seviye etkisi:** hız = (1 + 0,10·(L−1)) × 2^(ulaşılan kilometre taşı) (taşlar: 10/25/50/75/100). Kapasite seviye başına %5, bakım gideri seviye başına %6 artar.
- **Maliyet:** taban × 1,15^(L−1) (makineye göre ayarlanabilir). Toplu alım geometrik seri ile hesaplanır, MAKS ikili arama kullanır.
- **Sınırlı büyüme:** seviye başına maliyet hızdan çok daha hızlı büyür (test: geç seviyede ek hız birimi başına maliyet en az 100 kat). Seviye üst sınırı 100'dür. Sonraki büyüme yeni fabrikalardan gelir; her fabrika bir öncekinden pahalıdır ve en az 5 kat kazandırır.
- **Giderler:** hammadde, elektrik (kW × fiyat), işçi ücreti ve seviyeyle artan bakım. Giderler yalnızca makine çalışırken oluşur.
- **Kilitlenmeme garantisi:** işletme giderleri nakdi aşarsa fark `Debt` olarak tutulur ve ilk gelirden ödenir. Satın almalar ise her zaman nakit ister. Sıfır parayla bile fabrika çalışmaya devam eder (test edildi).
- **Doğrulama:** Her fabrikanın her tekdüze seviyede (1–100) kârlı olması ve yeni fabrikanın kendini 2–60 dakikada amorti etmesi testlerle zorunlu tutuluyor.
- **Şirket değeri:** nakit − borç + yatırım × 0,6 + net gelir/sn × 600. **Şirket seviyesi:** ömür boyu kazancın 10 tabanında logaritması × 2, artı 1.

### İlerleme referansı (açgözlü bot, çevrimdışı gelir, yönetici ve prestij yokken)

| Süre | Olay |
|---|---|
| 12 dk | Çelik 2. hat |
| 58 dk | Gıda fabrikası |
| 83 dk | Çelik 3. hat, Gıda 2. hat |
| 4,4 sa | Plastik fabrikası |
| 6 sa | ~162B/sn net gelir, şirket seviyesi 18 |

Tekstil ve Elektronik fabrikaları; çevrimdışı gelir, yöneticiler, prestij ve araştırma eklendikten sonra (Aşama 3–4) dengelenecek.

## Unity katmanı

- `GameBootstrap`: bileşim kökü. Sahnede yoksa otomatik oluşur.
- `SimulationDriver`: tek gerçek zamanlı ekonomi saati. 20 Hz sabit adımla çalışır, kare başına en fazla 1 sn yakalar ve `unscaledDeltaTime` kullanır.
- `FactoryView`: aktif fabrikanın tüm görselleri. Tek bir `Update` bütün makineleri, bantları ve işçileri sürer, makine başına `Update` yoktur.
- `MachineModelFactory` + `MeshBuilder` + `ModelKit`: 15 arketip için prosedürel, pahlı kenarlı stilize modeller üretir. Mesh'ler önbelleğe alınır ve paylaşılır.
- `MaterialLibrary`: URP/Lit materyalleri (SRP Batcher uyumlu, GPU instancing açık), tema paletleri, durum ışığı ve seviye kademesi materyalleri.
- `ConveyorView`: bant kasaları için nesne havuzu (`ComponentPool`).
- LOD: her makinede LODGroup var (uzakta yalnızca gövde, çok uzakta gizli).
- `IsometricCameraController`: tek parmakla kaydırma (ataletli), iki parmakla yakınlaştırma, fare tekerleği, dokunarak seçme, yumuşak odaklanma, sınırlar.
- `DayNightCycle`: güneş açısı, renk ve ortam ışığı geçişi. Gece pencereler ve sokak lambaları yanar.
- `DevHud`: geçici IMGUI arayüzü (Aşama 5'te UI Toolkit ekranlarıyla değiştirilecek).
