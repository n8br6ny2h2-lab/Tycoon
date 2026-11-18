# FABRİKA PATRONU — Hammadde İmparatorluğu

Oyun tasarım belgesi. Mimari için bkz. [ARCHITECTURE.md](ARCHITECTURE.md).

Küçük bir hammadde ticaret şirketiyle başlarsın. Hammadde alır, stoklar ve işlersin. Fabrikalar kurar, üretip satar, rakiplerle yarışırsın. Altı sanayi bölgesine yayılan bir sanayi imparatorluğu kurarsın.

Oyun yatay oynanır, iPhone içindir ve çevrimdışı çalışır.

## Durum (sürüm 0.1)

| Aşama | Durum |
|---|---|
| 1. Proje, ana menü, harita, kayıt | Tamam |
| 2. Piyasa, alım-satım, depo | Tamam |
| 3. Fabrika ve üretim zinciri | Tamam |
| 4. Müşteriler, sözleşmeler, lojistik | Tamam |
| 5. Finans, çalışanlar, şirket yönetimi | Tamam |
| 6. Rakipler, olaylar, araştırma | Tamam |
| 7. Görevler, etkinlikler, çevrimdışı ilerleme | Tamam |
| 8. Grafik, animasyon, ses, mobil uyum | Kodla çizilen 2D izometrik grafik, animasyonlar, sentezlenmiş sesler. Müzik yok |
| 9. Test ve App Store | 44 birim testi + 2 yıllık ekonomi simülasyonu + ekran turu. Para kazanma modeli henüz yok (aşağıda) |

## İlk dakikalar

Başlangıç: Anadolu Sanayi Bölgesi'nde 3 arsa, 600 hacimlik genel depo ve 6 Mn ₺.

Haritadaki "İlk adımlar" kartı oyuncuyu sırayla yönlendirir:

1. Hammadde al.
2. Malın depoya gelmesini bekle.
3. İlk satışı yap.
4. İlk fabrikayı kur.
5. İşçi al.
6. İlk üretimi yap.
7. İlk sözleşmeyi kabul et.

Her adım 250 bin ₺ ödül verir. En ucuz fabrikalar ilk gün kurulabilir: mobilya 3 Mn ₺, tarım 3,4 Mn ₺, gıda 4,3 Mn ₺ (Anadolu indirimiyle).

## Ekonomi

- **Piyasa:** 47 ürün var. Her birinin dünya stoku, hedef stoku, günlük üretimi ve tüketimi tutulur.
  - Fiyat `taban × (hedef/stok)^esneklik` formülüyle hesaplanır ve `[min, max]` aralığında kalır.
  - Üreticiler fiyat yükselince daha çok üretir; tüketiciler daha az tüketir.
  - Saatlik gürültü ve yumuşatma vardır. Olaylar üretim, tüketim ya da fiyat çarpanlarını değiştirir.
- **Oyuncu etkisi:** alım stoğu azaltır, satış artırır. Büyük emir sekiz parçada kayma yaratır.
  - Alış-satış makası %2, komisyon %0,4, işlem vergisi %0,3'tür.
  - Alımda ve satımda hacim başına nakliye ücreti alınır.
  - Bu yüzden hemen alıp satmak her zaman zarardır (test edilir).
- **Tedarikçiler:**

  | Tedarikçi | Fiyat | Teslim | Kalite |
  |---|---|---|---|
  | Standart | piyasa | 24 saat | normal |
  | Hızlı | +%4 | 6 saat | normal |
  | Ekonomik | −%3 | 72 saat | −8 |
  | Premium | +%7 | 24 saat | +12 |

- **Emir türleri:** piyasa emri, limit emri (7 gün geçerli), toplu sipariş (bugünkü fiyattan %2 indirimle her gün bir parti).
- **Maliyet takibi:** her stok kalemi ağırlıklı ortalama maliyet ve kalite tutar. Satışta gerçek kâr veya zarar gösterilir.
- **Mamul taban fiyatları:** girdi, enerji ve sarf toplamının üstünde, sınırlı bir marjla tanımlıdır.
  - Test, her reçetenin marjını %3–200 aralığında tutar.
  - Fabrikanın günlük kârı kendi satışının fiyatı itmesiyle azalır; bu yüzden tek ürüne yığılmak cezalandırılır.
- **Kredili hesap:** hammadde, enerji ve bakım gibi işletme giderleri şirket değerinin %15'ine kadar eksi bakiyeyle ödenebilir. Yatırımlar için nakit gerekir.
- **Simülasyon sonucu:** bot oyuncu 2 oyun yılında şirket değerini 10 Mn ₺'den yaklaşık 530 Mn ₺'ye çıkarır (iki farklı tohumla denendi). Negatif stok, sınır dışı fiyat, iflas ya da kilitlenen fabrika görülmez.

## Depolar

- **Depo tipleri:** genel, metal, kimyasal, soğuk hava, petrol tankı, büyük lojistik.
- **Depolama sınıfları:** her ürünün bir sınıfı vardır: genel, dökme, metal, kimya, soğuk, tank. Elektrik depolanmaz.
- **Yer ayırma:** gelmekte olan mal yer ayırır. Depo doluysa alım ve üretim durur.
- **Geliştirme:** seviye (kapasite +%60), raf sistemi (+%20), güvenlik (hırsızlık −%30/seviye).
- **Giderler:** günlük bakım ve hacim başı depolama ücreti.
- **Otomatik stok:** alt sınırın altında alır, üst sınırın üstünde satar.

## Fabrikalar ve üretim

- **Tesisler:** 15 fabrika, ayrıca maden ocağı, petrol ve gaz kuyusu, elektrik santrali ve tarım-orman işletmesi.
- **Reçeteler:** fabrika başına 1–3 reçete var. Her biri girdi, çıktı, süre, elektrik, sarf gideri ve fire içerir.
  - Örnek zincirler: cevher + kömür → ham demir → çelik; petrol → plastik granül; silisyum + bakır + plastik → elektronik; lityum + nikel + alüminyum → batarya; çelik + alüminyum + plastik + kauçuk + cam + elektronik → otomobil; çimento + ham demir + agrega → inşaat malzemesi.
- **Hat geliştirme:**
  - makine sayısı
  - seviye (hız +%8, makine sınırı +2)
  - otomasyon (işçi −%20, hız +%5)
  - kalite hattı (+6)
  - fire azaltma (−%25)
- **Bakım:** durum günlük aşınır. 40 altında hız −%30, 15 altında üretim durur.
- **Durumlar:** Çalışıyor, Hammadde bekliyor, Bakım gerekiyor, Depo dolu, Enerji kesintisi, Üretim tamamlandı, Durduruldu, İşçi yok.
- **Öncelik:** kıt girdi önce yüksek öncelikli fabrikaya gider.
- **Otomasyon ayarları:** otomatik tedarik (girdiyi ~1 günlük ihtiyaçta tutar), otomatik satış, planlı bakım.
- **Kalite:** taban + kalite hattı + mühendis + araştırma + girdi kalitesi.
- **Enerji:** santraller önce çalışır ve kendi havuzlarını doldurur. Fabrikalar önce bu havuzdan, sonra şebekeden çeker. Fazla enerji şebekeye %70 fiyatla satılır. Kesintide şebeke yoktur.

## Müşteriler, sözleşmeler, lojistik

- **Müşteri tipleri:** 8 tip, seviyeyle açılır.
- **Sözleşme türleri:** 5 tür (tek seferlik, haftalık, aylık, uzun vadeli, ihale).
  - Her biri miktar, birim fiyat, en düşük kalite, teslim süresi ve şehir içerir.
  - İhracat yalnızca liman ya da ticaret merkezinden gönderilebilir.
- **Teslimat:**
  - Zamanında: kalite primiyle ödenir, itibar +1.
  - Gecikmeli: %10 ceza.
  - 3 günden fazla geç: kaçırılmış sayılır, ceza ve itibar −4. İki kaçırma fesih demektir.
- **İhale:** kapalı zarf, %2 teminat. Rakip teklifleriyle karşılaştırılır, en düşük teklif kazanır.
- **Lojistik:**
  - Mesafe bölge koordinatlarından hesaplanır.
  - Kiralık nakliyeci her zaman vardır. Kendi filon: kamyon, ağır kamyon, tır, tren (demiryolu), gemi (liman), uçak (araştırma + havalimanı).
  - Araçlar gidiş-dönüş boyunca meşgul kalır.
  - Otomatik rotalar her N günde bir taşır ve kaynakta bırakılacak miktarı korur.

## Şirket

- **Personel:** 8 pozisyon. İşe alım gideri çeyrek maaş, tazminat yarım maaştır. Maaşlar günlük tahakkuk eder. Eğitim seviye başına etkiyi %10 artırır.
- **Finans paneli:**
  - toplam nakit, şirket değeri, günlük ciro, günlük net kâr
  - 30 günlük gelir ve gider, fabrika, depo, stok, araç ve arsa değeri
  - borçlar, son ay vergisi, pazar payı, kredi notu, faiz ortamı
- **Kredi:** 3 banka var. Faiz şu toplamdır: ekonomik döngü + olaylar + kredi notu primi + banka farkı − finans yöneticisi indirimi. Taksit anüite yöntemiyle hesaplanır. Ay sonunda %20 kurumlar vergisi kesilir.
- **İflas:** kasa sınırın altında 5 gün kalırsa kurtarma seçenekleri açılır: devlet teşvik kredisi (oyunda bir kez) ya da yatırımcı ortaklığı. Kullanılmazsa iflas olur ve yeni oyun başlar.
- **Rakipler:** 10 yapay zekâ şirket, dört strateji: düşük maliyet, kalite, hacimli ticaret, uzman.
  - Fiyat yükselince kapasite artırır, düşünce kısar.
  - Tüccarlar ucuzken stok yapar, pahalıyken satar.
  - Sermayesi biten iflas eder; yerine yeni rakip girer.
  - Şirket değeri sıralaması ve pazar payı gösterilir.
- **Araştırma:** 14 düğümlü ağaç. Elektronik, batarya, makine ve otomotiv fabrikaları ile güneş enerjisi ve kargo uçağı araştırmayla açılır.
- **Bölgeler:**

  | Bölge | Avantaj |
  |---|---|
  | Anadolu | fabrika %15 ucuz |
  | Maden Vadisi | madenler, cevher teslimatı ucuz |
  | Liman Şehri | ithalat yarı fiyat ve hızlı, ihracat, gemi |
  | Petrol Bölgesi | elektrik %25 ucuz, kuyular, lityum |
  | Teknoloji Şehri | araştırma %50 hızlı, silisyum |
  | Uluslararası Ticaret Merkezi | komisyon yarı, ihracat primi, uçak |

- **Olaylar:** hammadde kıtlığı, petrol krizi, enerji zammı, durgunluk, talep patlaması, büyük müşteri, teknoloji keşfi, liman yoğunluğu, nakliye artışı, rakip iflası, enerji kesintisi, hasat bolluğu.
- **Görevler ve başarımlar:** günlük 3 ve haftalık 3 görev, 20 başarım.
- **Haftalık etkinlikler:** ticaret haftası, sanayi fuarı, büyük ihaleler, üretim yarışması.

## Zaman, çevrimdışı ve kayıt

- **Zaman:** ×1 hızda 1 oyun günü 12 gerçek dakikadır. Hız seçenekleri: duraklat, ×1, ×2, ×5. Takvim 1 Mart'ta başlar ve 12 ay × 30 gündür; mevsimler haritaya yansır.
- **Çevrimdışı:** geçen süre aynı motorla, en fazla 3 oyun günü simüle edilir. Dönüşte rapor gösterilir: nakit, değer, üretilenler, gelir ve gider kalemleri. Cihaz saati geri alınmışsa ilerleme verilmez.
- **Kayıt:**
  - 20 saniyede bir ve uygulama arka plana geçince otomatik yapılır.
  - Sürümlü JSON ve sağlama toplamıyla tutulur; bir önceki geçerli kayıt yedekte saklanır.
  - Göç fonksiyonları eksik alanları doldurur.
  - iOS'ta Capacitor Preferences, tarayıcıda localStorage kullanılır.

## Para kazanma (henüz uygulanmadı)

Talimattaki model: isteğe bağlı ödüllü reklam, reklamsız premium, kozmetik fabrika görünümleri, harita temaları, VIP üyelik. Oyuncu ödemeye zorlanmayacak.

- **Bu sürümde:** oyunda satın alma ya da reklam yok. Talimat "görünüp çalışmayan özellik bırakma" diyor; bu yüzden mağaza ekranı eklenmedi.
- **Reklam:** gerçek reklam için bir reklam ağı hesabı ve SDK'sı (ör. AdMob) gerekir.
- **Kozmetik ve VIP:** StoreKit eklentisi `app/ios/App/App/StorePlugin.swift` hazırdır.

## Bilinen sınırlar

- Unity yerine web teknolojisi (Capacitor) kullanıldı; gerekçesi ARCHITECTURE.md'de.
- Grafikler kodla çizilen 2D izometriktir, hazır sanat dosyası yoktur. Müzik yoktur.
- Bulut kayıt arayüzü hazır, bağlantısı yok.
