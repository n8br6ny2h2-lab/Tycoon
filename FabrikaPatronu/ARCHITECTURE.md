# FABRİKA PATRONU — Hammadde İmparatorluğu

## Mimari, klasör yapısı, veri modelleri ve aşamalar

## 1. Teknoloji kararı

Geliştirme ortamında Unity, Mac ve Xcode yok. Unity projesi burada açılamaz, derlenemez ve test edilemez. Bu yüzden talimattaki "Unity yoksa iOS odaklı uygun teknoloji" seçeneği kullanıldı:

| Katman | Seçim | Gerekçe |
|---|---|---|
| Oyun çekirdeği | Saf JavaScript (ES modülleri), motordan bağımsız | Her yönetici sistemi tek başına Node.js ile test edilir. Uzun ekonomi simülasyonları saniyeler içinde koşar |
| Görüntü | HTML Canvas 2D (izometrik harita, animasyon) + DOM arayüz | Görsel dosya yok. Bütün binalar, araçlar ve ikonlar kodla çizilir, bu yüzden aynı tasarım dilindedir. Uygulama küçük kalır |
| iOS | Capacitor 8 (WKWebView) + StoreKit 2 eklentisi | Demir Baronu'da kurulup App Store hattı hazırlanmış altyapı. Mac olmadan bulutta derlenir |
| Kayıt | Capacitor Preferences (iOS) / localStorage (tarayıcı), sürümlü JSON + sağlama toplamı + yedek | Bulut kayıt için `SaveManager` arayüzü hazırdır |
| Paketleme | esbuild ile tek dosya (`dist/index.html`) | Tarayıcıda ve uygulamada aynı dosya çalışır |

Çekirdek, arayüzden tamamen ayrıdır. Arayüz durumu yalnızca okur ve eylemleri çekirdek fonksiyonlarıyla yapar. İleride Unity ya da SwiftUI istemcisine geçilirse çekirdek kurallar ve veriler aynen taşınabilir.

## 2. Klasör yapısı

```
FabrikaPatronu/
  ARCHITECTURE.md        bu belge
  GDD.md                 oyun tasarımı ve ekonomi dengesi
  src/
    data/                VERİ (kodun içinde sabit sayı yok)
      resources.js       47 hammadde ve ürün
      factories.js       15 fabrika + 4 çıkarma/enerji tesisi, reçeteler
      warehouses.js      6 depo tipi
      vehicles.js        6 taşıma aracı, nakliyeci
      regions.js         6 bölge, arsa düzeni, bölge avantajları
      employees.js       8 pozisyon
      research.js        teknoloji ağacı
      competitors.js     10 rakip şirket
      customers.js       müşteri tipleri ve firmalar
      events.js          dinamik olaylar
      quests.js          günlük/haftalık görevler, başarımlar, etkinlikler
      balance.js         ekonomi sabitleri (vergi, komisyon, faiz, zaman ölçeği)
    core/                SİSTEMLER (her biri saf fonksiyonlar, state alır)
      game.js            GameManager: zaman, tik döngüsü, çevrimdışı ilerleme
      market.js          MarketManager: arz-talep fiyatı, emirler, grafik geçmişi
      economy.js         EconomyManager: enerji şebekesi, faiz ortamı, ekonomik döngü
      inventory.js       InventoryManager: bölge stokları, ortalama maliyet, kalite
      warehouse.js       WarehouseManager: depo kapasitesi, depolama gideri, otomatik stok
      factory.js         FactoryManager: kurma, yükseltme, makine, bakım
      production.js      ProductionManager: reçete çalıştırma, öncelik, durumlar
      logistics.js       LogisticsManager: sevkiyat, filo, otomatik rotalar
      customers.js       CustomerManager: müşteri teklifleri
      contracts.js       ContractManager: sözleşme, teslimat, ihale
      employees.js       EmployeeManager: işe alım, eğitim, verim
      research.js        ResearchManager: teknoloji ağacı
      competitors.js     CompetitorManager: yapay zekâ rakipler
      finance.js         FinanceManager: defter, kredi, vergi, şirket değeri, iflas
      events.js          EventManager: olay seçimi ve etkileri
      quests.js          QuestManager: görevler, başarımlar, etkinlikler
      save.js            SaveManager: sürüm, sağlama, yedek, göç
      state.js           yeni oyun durumu
      rng.js, util.js    tohumlu rastgele, yardımcılar
    ui/                  ARAYÜZ (UIManager ve ekranlar)
      ui.js              UIManager: üst bar, alt menü, ekran yönlendirme, bildirimler
      map.js             izometrik harita çizici, yakınlaştırma, dokunma
      art.js             binalar, araçlar, işçiler, gemi, tren çizimleri
      icons.js           47 ürün ikonu (SVG)
      screens/*.js       piyasa, fabrikalar, depolar, lojistik, şirket...
      style.css
    main.js              açılış
  tests/                 node --test (her sistem ayrı test dosyası)
  scripts/build.mjs      tek dosya paket
  scripts/sim.mjs        uzun süreli ekonomi simülasyonu (bot oyuncu)
  app/                   Capacitor iOS kabuğu (Aşama 9)
```

## 3. Veri modelleri

Bütün tanımlar `src/data` altındadır ve kimlikle (`id`) bağlanır.

**Resource**
```
{ id, name, cat, unit, vol, storage, base, min, max, quality, prod, cons, elastic, shape, color }
```
`storage`: genel · dokme · metal · kimya · soguk · tank · enerji. `prod/cons`: dünyanın günlük üretim ve tüketimi. `vol`: depoda bir birimin kapladığı hacim.

**Recipe** (fabrika tipinin içinde)
```
{ id, in: {resId: qty}, out: {resId: qty}, hours, mwh, labor, fire }
```
Değerler makine başına ve çevrim başınadır.

**FactoryType**
```
{ id, name, cost, workers, maint, idleMwh, store, unlock: {level, research}, regions?, recipes[], look }
```

**Durum (`state`)**, tek bir JSON nesnesi olarak kaydedilir:
```
{ v, seed, time: {min, speed, lastReal}, company: {name, cash, xp, level, rep, credit},
  regions: {id: {owned, plots: [{owned, building}]}}, buildings: {id: Factory|Warehouse},
  inventory: {regionId: {resId: {q, cost, ql}}}, market: {resId: {stock, price, hist}},
  orders: [limit emirleri], shipments: [], fleet: [], routes: [], contracts: [], offers: [],
  staff: {pos: {n, lvl}}, research: {done:{}, active}, rivals: [], loans: [], ledger, events: [],
  quests, stats, notifications, log }
```

## 4. Zaman ve döngü

- 1 gerçek saniye = 2 oyun dakikası (hız ×1). ×2, ×5 ve duraklatma vardır. 1 oyun günü ×1 hızda 12 dakika sürer.
- Her oyun saati üretim, enerji, sevkiyat ve piyasa güncellenir. Günlük olarak sözleşmeler, maaş tahakkuku, rakipler, olaylar ve görevler işlenir. Aylık olarak vergi, kredi taksitleri ve maaş ödemesi yapılır.
- **Çevrimdışı:** dönüşte geçen gerçek süre aynı motorla, oyun saatleri hâlinde simüle edilir. En fazla 3 oyun günü sayılır (12 gerçek saatten fazlası hiç sayılmaz). Bunun sonunda ayrıntılı rapor gösterilir.
- **Saat hilesi koruması:** son kayıt anı saklanır. Cihaz saati geri alınmışsa ilerleme verilmez. İleri alınmışsa en fazla 12 saat sayılır, sonrası boşa gider. Çevrimdışı kazanç bir oturumda bir kez verilir.

## 5. Ekonomi modeli (özet; ayrıntı GDD.md'de)

- Her ürünün dünya stoku `S`, hedef stoku `T` vardır. Fiyat `P = base · (T/S)^e`, değer `[min, max]` arasında sınırlanır.
- Üreticiler fiyat yükselince daha çok, düşünce daha az üretir: `prod · (1 + k·(T−S)/T)`. Tüketim fiyata ters tepki verir.
- Oyuncu alırken `S` azalır, satarken artar. Büyük emir fiyatı kendisi kaydırır (kayma), kayma parça parça hesaplanır.
- Alış fiyatı = piyasa × (1 + makas/2), satış = piyasa × (1 − makas/2). Bunun üstüne komisyon ve işlem vergisi eklenir. Hemen alıp satmak her zaman zarardır.
- Rakiplerin üretimi ve tüketimi de `S`'yi değiştirir. Olaylar üretim ve tüketim çarpanlarını değiştirir.
- Mamul ürünlerin taban fiyatı, girdi + enerji + işçilik maliyetinin üstünde ama sınırlı bir marjla tanımlıdır. Testler her reçetenin marjını %5–60 aralığında tutar.

## 6. Geliştirme aşamaları

| Aşama | İçerik | Bitiş ölçütü |
|---|---|---|
| 1 | Proje, veri, durum, kayıt, ana menü, izometrik harita | Yeni oyun açılır, harita çizilir, kayıt/yükleme ve yedekten kurtarma testleri geçer |
| 2 | Piyasa, alım-satım (piyasa/limit/toplu emir, tedarikçi), depo ve stok | Al-sat, kayma, maliyet ortalaması, kapasite testleri geçer |
| 3 | Fabrika kurma, reçeteler, üretim hatları, enerji, durumlar | Üretim zinciri ve durma koşulları testleri geçer |
| 4 | Müşteriler, sözleşmeler, ihaleler, lojistik, filo, rotalar | Teslimat, gecikme cezası, nakliye testleri geçer |
| 5 | Finans, kredi, vergi, çalışanlar, eğitim, iflas | Defter ve kredi testleri geçer |
| 6 | Rakipler, olaylar, araştırma ağacı, bölgeler | Rakip/olay piyasayı etkiler |
| 7 | Görevler, etkinlikler, başarımlar, çevrimdışı ilerleme | Çevrimdışı ve saat hilesi testleri geçer |
| 8 | Grafik (gece-gündüz, mevsim, animasyon), ses, mobil uyum | Ekran turu taşmasız ve hatasız geçer |
| 9 | Test, uzun simülasyon, App Store hazırlığı | 2 oyun yılı simülasyonu: negatif stok yok, kontrolsüz para yok |
