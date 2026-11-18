# DEMİR BARONU — Oyun Tasarım Belgesi

**Tür:** Ticaret / işletme simülasyonu (tek oyunculu, gün tabanlı)
**Platform:** iPhone ve iPad (App Store). Sonra Android.
**Dil:** Türkçe (ana dil) + İngilizce
**Hedef kitle:** 18–45 yaş, işletme ve tycoon oyunu sevenler, sanayi ve inşaat sektörüyle ilgilenenler, esnaf kültürünü bilenler
**Oynanabilir prototip:** [`web/play.html`](web/play.html) (telefonda tarayıcıda açılır)

---

## 1. İsim

| Alan | Değer | Karakter |
|---|---|---|
| App Store adı (TR) | **Demir Baronu: Çelik Ticareti** | 28 / 30 |
| Alt başlık (TR) | Boru, profil, sac al-sat | 24 / 30 |
| App Store adı (EN) | **Iron Baron: Steel Trade** | 23 / 30 |
| Alt başlık (EN) | Trade pipes, beams & sheets | 27 / 30 |

Kasım 2026'da App Store'da "Demir Baronu", "Iron Baron" veya "Steel Baron" adında bir oyun bulunamadı. Benzer temalı oyunlar var: *Metal Tycoon* ve *Steel Mill Manager* (ikisi de üretim/idle), *Оружейный Барон* (silah ticareti). Bu oyunlardan hiçbiri boru, profil ve sac **ticareti** yapmıyor. Kesin kontrol, App Store Connect'te ad rezerve edilirken yapılır.

Yedek isimler: *Sac & Profil Tycoon*, *Hurdadan Holdinge*, *Çelik Pazarı*, *Demirci Ticaret*.

## 2. Tek cümlelik fikir

Cebinde 5.000 ₺ ve bir telefonla başlarsın. Hedef 12.000 ton satış ve 40 milyon ₺ net değer. Komisyonculuk yaparak para biriktirir; 15 şehirde depo ve dükkan açar, kamyon ve makine alır, vadeli satışın ve kredinin riskini yönetirsin. Sonunda Türkiye'nin en büyük boru, profil ve sac tüccarı olursun.

## 3. Sıfırdan başlangıç

Oyuncu ilk gün şunlara sahiptir:

- 5.000 ₺ nakit
- Depo yok (stok tutamaz)
- Araç yok
- Kredi yok (banka tanımıyor)
- İtibar 50/100

İlk günlerde tek yol **aracılıktır**. Müşteri siparişini al, fabrikaya direkt sevk ettir, aradaki farkı kazan. Bunun için sermaye gerekmez ama marj düşüktür (%2–8).

İlk ~4 gün içinde depo köşesi kiralanacak kadar para birikir. Böylece stoktan satış ve fiyat spekülasyonu açılır.

## 4. Çekirdek döngü

```
Piyasayı izle ─▶ Ucuzken stok al ─▶ Teklifleri değerlendir ─▶ Teslim et (stoktan / aracılıkla)
      ▲                                                               │
      └──── Günü bitir: fiyatlar değişir, tahsilat, haberler ◀─ Tahsilat / vade ─┘
```

Her gün oyuncu dört karar verir:

1. **Ne alayım?** Fiyat grafiğine, habere ve eldeki siparişlere bakarak stok alır.
2. **Hangi teklifi alayım?** Tonaj, fiyat, vade, müşteri riski ve kesim ihtiyacı farklıdır.
3. **Nasıl teslim edeyim?** Stoktan teslim yüksek marj sağlar ama depo ve nakliye ister. Aracılık sermaye istemez ama marjı düşüktür.
4. **Nakit akışını nasıl yöneteyim?** Vadeli satış, kredi ve faktoring arasında denge kurar.

## 5. Ürünler (48 ürün, 6 kategori)

Fiyatlar oyun içi temsilî ₺/ton değerleridir, gerçek piyasa verisi kullanılmaz. Ağırlıklar gerçek kg/m değerlerinden hesaplanmış yaklaşık değerlerdir ve oyunda ürün satırında görünür.

| Kategori | Ürünler (açılış unvanı) | Oynaklık / bağlı olduğu olaylar |
|---|---|---|
| **Sac & Levha** (11) | HRP 3 mm, DKP 1 mm *(başlangıç)* · DKP 2 mm, kalın levha 10 mm, galvaniz 0,8 mm, baklavalı 4/5 mm *(Esnaf)* · ST52/S355 20 mm, boyalı trapez *(Toptancı)* · HRP bobin, delikli sac *(Bayi)* · Hardox 450 *(Distribütör)* | Yüksek: ithalat, gümrük, otomotiv |
| **Profil** (14) | Kutu 20×20 ve 40×40, dikdörtgen 60×40, köşebent 30 ve 50 *(başlangıç)* · kutu 80×80, lama 40×5, NPU 100 *(Esnaf)* · dikdörtgen 100×50, T demir, NPI 120, IPE 200 *(Toptancı)* · HEA 200 *(Bayi)* · HEB 300 *(Distribütör)* | Orta: inşaat sezonu, haddehane duruşları |
| **Boru** (9) | Sanayi borusu Ø21,3 ve Ø48,3 *(başlangıç)* · doğalgaz DN25 *(Esnaf)* · doğalgaz DN50, galvaniz 1" *(Toptancı)* · kazan borusu, dikişsiz API 5L, sondaj Ø219 *(Bayi)* · spiral kaynaklı Ø508 *(Distribütör)* | Orta: altyapı ihaleleri |
| **İnşaat demiri** (6) | Nervürlü Ø8 ve Ø12 *(başlangıç)* · Ø16, hasır çelik Q188 *(Esnaf)* · Ø32, filmaşin *(Toptancı)* | Çok yüksek: konut kredisi, kentsel dönüşüm, deprem bölgesi ihaleleri |
| **Dolu mil** (3) | Yuvarlak dolu Ø50 *(Esnaf)* · kare dolu 20×20 *(Toptancı)* · C45 transmisyon mili Ø80 *(Bayi)* | Düşük: makine imalatı |
| **Paslanmaz & Alüminyum** (5) | Paslanmaz 304 sac, alüminyum 5754 levha *(Bayi)* · 316L sac, 304 boru, alüminyum kutu profil *(Distribütör)* | En yüksek: LME nikel fiyatı. Ton fiyatı 4–5 kat, sipariş tonajı ¼ |

Her ürünün bir **talep ağırlığı** vardır: kutu profil 40×40, HRP sac ve Ø12 nervürlü en sık istenir; Hardox ve 316L nadirdir.

## 6. Piyasa modeli

- Her ürünün fiyatı = taban × e^(genel endeks + ürün sapması).
- **Genel endeks** (kur, hurda ve enerji fiyatlarının ortak etkisi) yavaş bir rastgele yürüyüşle değişir ve tabana geri çekilir.
- **Ürün sapması** günlük gürültüyle değişir, %4/gün hızla sıfıra döner ve kategori olaylarından etkilenir.
- **Haber olayları** (17 olay, her gün ~%20 olasılık, 4–8 gün sürer):
  - Tüm piyasa: Dolar/TL yükseldi ↑, AB sınırda karbon vergisi ↓, hurda fiyatları geriledi ↓, faiz indirimi beklentisi ↑
  - Sac: Çin'den ucuz ithalat ↓, otomotivde yavaşlama ↓, bobine ek gümrük vergisi ↑
  - Profil: inşaat sezonu ↑ (inşaat demiri de ↑), haddehane bakım duruşu ↑↑
  - Boru: doğalgaz altyapı ihalesi ↑, fabrikalar iskonto yaptı ↓
  - İnşaat demiri: kentsel dönüşüm teşviki ↑, konut kredisi faizleri arttı ↓, deprem bölgesinde yeniden yapım ↑
  - Paslanmaz & alüminyum: LME'de nikel fırladı ↑, Endonezya nikel ihracatını serbest bıraktı ↓
  - Dolu mil: makine imalatçıları kapasite artırıyor ↑
- Yükselişteki kategoriden gelen sipariş sayısı da iki katına çıkar.
- Haber, etkisinden önce görünür. Haberi okuyup hemen stoklayan oyuncu kazanır. Bu, oyunun beceri katmanıdır.

## 7. Siparişler, müşteriler ve işleme

Her gün yeni teklifler gelir. Teklif sayısı seviyeye, itibara ve satış elemanı sayısına bağlıdır. Bir teklifin özellikleri:

| Özellik | Etki |
|---|---|
| Şehir | 15 şehirden biri. Merkez şehir ×3, tesisin olan şehirler ×2, diğerleri nüfusa göre ağırlıklı |
| Ürün ve tonaj | Seviye arttıkça tonaj büyür (0,5 t'dan 150 t'ya). Yükselişteki kategoriler daha sık istenir |
| Fiyat | Piyasanın %4–11,5 üstü. İtibar, satış elemanı ve sadık müşteri (+%1,5) fiyatı artırır |
| Ödeme | Peşin, 30 gün vade (+%3) veya 60 gün vade (+%6). Vadeli satış Esnaf'ta açılır |
| Müşteri riski | A (sorunsuz), B (%4 karşılıksız çek), C (%12, +%2 fiyat). Sadık müşterinin riski bir kademe düşer |
| İşleme (%28 olasılık) | Aşağıdaki tablo |
| Teslim süresi | 2–6 gün. Geç kalınırsa sipariş iptal olur, itibar −6 |

### İşleme türleri ve makineler

| İşleme | Hangi ürünler | Fiyat primi | Makine yoksa fason | Makine | Bedel |
|---|---|---|---|---|---|
| Ölçüye kesim | Sac, paslanmaz | +%7 | %5 | Giyotin makas | 120.000 ₺ |
| Ölçüye kesim | Profil, boru, mil | +%7 | %5 | Şerit testere | 80.000 ₺ |
| Kesme-bükme (şantiye) | İnşaat demiri | +%8 | %6 | Demir kesme-bükme | 150.000 ₺ |
| Abkant büküm | Sac, paslanmaz | +%10 | %7 | Abkant pres | 380.000 ₺ |
| Lazer kesim | Sac, paslanmaz | +%14 | %9 | Fiber lazer | 1.500.000 ₺ |

### Teslim yolları

- **Stoktan teslim:** Oyun, malı bulunduran ve bugün yükleme kapasitesi kalan tesisler arasından nakliyesi en ucuz olanı seçer. Ücret mesafeye göre hesaplanır (bkz. 8. bölüm).
- **Aracılık:** Fabrika maliyeti piyasa × 1,035 + 500 ₺/t nakliyedir; işleme varsa fason + %1 eklenir. Peşin siparişte sermaye gerekmez. Fabrika aracıya **günde en fazla 3 sevkiyat** ve **sevkiyat başına en fazla 8 t** verir.

### Sadık müşteri

Aynı firmaya 3 kez teslimat yapılınca firma sadık müşteri olur. Sonraki tekliflerinde fiyat +%1,5 olur ve risk bir kademe düşer. Oyunda 40 firma var.

## 8. Tesisler, lojistik ve tedarik

### Şehirler (30) ve sanayi profilleri

Her şehrin bir talep büyüklüğü, liman bilgisi ve sanayi profili vardır. Profil o şehirden gelen siparişlerin ürün dağılımını belirler. Örneğin otomotiv şehrinden sac ×1,8 ve dolu mil ×1,4 daha sık istenir.

| Bölge | Şehir (OSB) · sanayi |
|---|---|
| Marmara | İstanbul (İkitelli) makine, inşaat, otomotiv · Tekirdağ (Çorlu) tekstil, gıda · Kocaeli (Dilovası) otomotiv, tersane, çelik · Sakarya (Hendek) otomotiv · Bursa (Demirtaş) otomotiv, tekstil · Balıkesir gıda, makine |
| İç Anadolu | Eskişehir beyaz eşya, makine · Ankara (Ostim) savunma, makine, inşaat · Kırıkkale savunma, çelik · Konya (Büsan) tarım makineleri · Kayseri mobilya, makine · Sivas makine, enerji · Çorum makine, tarım |
| Ege | İzmir (Çiğli) çelik, makine, gıda · Manisa beyaz eşya · Denizli tekstil, makine |
| Karadeniz | Karabük çelik · Zonguldak (Ereğli) çelik, tersane · Samsun (Tekkeköy) tarım, inşaat · Trabzon (Arsin) inşaat, tersane |
| Akdeniz | Antalya inşaat, gıda · Mersin (Liman) inşaat, enerji · Adana (Hacı Sabancı) tarım, gıda · Osmaniye çelik, inşaat · Hatay (İskenderun) çelik, enerji |
| Doğu ve Güneydoğu | Gaziantep (Başpınar) makine, tekstil · Kahramanmaraş tekstil, inşaat · Malatya inşaat · Diyarbakır inşaat, enerji · Erzurum inşaat |

Liman şehirleri (10): İstanbul, Tekirdağ, Kocaeli, İzmir, Antalya, Zonguldak, Samsun, Trabzon, Hatay, Mersin.

### Çelik üretim merkezleri

| Merkez | Şehir | Ürünler |
|---|---|---|
| Ereğli yassı çelik | Zonguldak | Sac |
| İskenderun entegre çelik | Hatay | Sac, profil, inşaat demiri |
| Karabük haddehanesi | Karabük | Profil, inşaat demiri, dolu mil |
| Aliağa ark ocakları | İzmir | İnşaat demiri, dolu mil, profil |
| Dilovası çelik ve boru | Kocaeli | Boru, sac, profil |
| Osmaniye haddehaneleri | Osmaniye | İnşaat demiri |
| Kırıkkale dövme çelik | Kırıkkale | Dolu mil |
| İstanbul boru fabrikaları | İstanbul | Boru |
| Bursa paslanmaz servis merkezi | Bursa | Paslanmaz ve alüminyum |

- **Fiyat:** Her şehrin fiyatı o ürünün en yakın üreticisine olan mesafeye doğru çekilir: üreticinin yanında yaklaşık −%3, uzaklaştıkça artar (en fazla +%3,5). Ortalama şehir ulusal fiyattadır. Bunun üstüne günlük dalgalanma eklenir.
- **Fabrikadan alım:** Teslim süresi en yakın üreticiye olan mesafeye göre 1–5 gündür (her 350 km +1 gün). Al-sat panelinde en yakın üretici ve uzaklığı yazar.
- **Deprem Bölgesi senaryosu:** Gaziantep, Adana, Kayseri, Mersin, Kahramanmaraş, Malatya, Hatay, Osmaniye ve Diyarbakır'da inşaat demiri talebi üç kattır.

### Harita

Türkiye'nin **81 ili** sınırlarıyla çizilir; göller (Van, Tuz, Beyşehir, Eğirdir…) il şekillerinde boşluk olarak görünür, kıyılar hafif parlar, deniz adları yazılıdır. Bir **ile dokununca** o il turuncu çerçeveyle seçilir ve kartın altında açılır:

- plaka numarası ve il adı
- ilin başlıca sanayileri (çelik, döküm ve hadde etiketleri turuncu)
- il oyundaki 30 sanayi şehrinden biriyse o şehrin bilgisi: OSB, sektörler, üretim tesisleri, tesislerin, rakipler, kategori fiyat farkları; yeni tesis şehri de o olur
- değilse "tesis açılamaz" ve en yakın sanayi şehri ile yaklaşık yol mesafesi, tek dokunuşla o şehre geçiş düğmesi

Simgeler:

- çelik üretim merkezleri baca simgesiyle
- liman şehirleri mavi, senin tesislerin turuncu
- rakip şubeleri renkli noktalarla
- merkezinden diğer tesislerine akan sevkiyat rotaları

İl sınırları turkey-map-react 2.3.0 paketinin verisinden alınmıştır (MIT Lisansı, © 2020 Erdi Gökçe); kaynak kodda belirtilir.

**Görünümler:** "Sanayi" ve 6 ürün kategorisinin "fiyat" görünümü. Fiyat görünümünde her il, en yakın sanayi şehrinin fiyatına göre en ucuz (yeşil) ile en pahalı (kırmızı) arasında boyanır; sanayi şehri olan iller daha koyu renklidir. **Büyüt** ile harita 2,6 kat büyür, seçilen ile kayar, 81 ilin ve bütün şehirlerin adları görünür.

### Tesis türleri

Her tesisin kendi stoğu, **kapasitesi (t)** ve **günlük yükleme kapasitesi (t/gün)** vardır. Teslimat, transfer ve dükkan satışları yükleme kapasitesini harcar.

| Tür | Kademe | Bedel | Kira/ay | Stok | Yükleme | Kapı müşterisi |
|---|---|---|---|---|---|---|
| Depo | Kiralık depo köşesi | 15.000 ₺ | 2.500 ₺ | 12 t | 10 t/gün | — |
| Depo | Küçük depo | 90.000 ₺ | 8.000 ₺ | 40 t | 25 t/gün | — |
| Depo | Depo + açık saha | 350.000 ₺ | 20.000 ₺ | 120 t | 60 t/gün | — |
| Depo | Lojistik depo | 1.400.000 ₺ | 55.000 ₺ | 400 t | 150 t/gün | — |
| Depo | Liman deposu *(yalnız liman şehri)* | 6.000.000 ₺ | 160.000 ₺ | 1.500 t | 400 t/gün | — |
| Dükkan | Küçük dükkan | 40.000 ₺ | 7.000 ₺ | 8 t | 8 t/gün | 3/gün |
| Dükkan | Dükkan + arka depo | 160.000 ₺ | 16.000 ₺ | 25 t | 15 t/gün | 6/gün |
| Dükkan | Yapı market | 1.200.000 ₺ | 45.000 ₺ | 60 t | 30 t/gün | 12/gün |

- **Kapı müşterisi (perakende):** Dükkana her gün gelen müşteri sayısı = kademe × şehir büyüklüğü × (1 + 0,5 × tezgâhtar) × itibar/60. Her biri dükkandaki stoktan 20–200 kg alır ve piyasanın %8–16 üstünde öder.
- **Ekipman (tesis başına):** Forklift +20 t/gün (120.000 ₺, en fazla 3, bakım 4.000 ₺/ay). Tavan vinci +80 t/gün (450.000 ₺, en fazla 2, yalnızca "depo + açık saha" ve üstü).
- **Tezgâhtar (dükkan başına, en fazla 3):** 25.000 ₺/ay. Her biri kapı müşterisini %50 artırır.
- **Tesis sınırı:** Komisyoncu 1, Esnaf 2, Toptancı 3, Bayi 5, Distribütör 8, Baron 12.
- **Devretme:** Boş tesis, kademe bedelinin yarısına devredilebilir.

### Nakliye

| Taşıyan | Aynı şehir | Şehirler arası |
|---|---|---|
| Kendi filon (günlük kapasite kadar) | 40 ₺/t | 40 ₺ + 0,45 ₺/km (yakıt) |
| Nakliyeci (kapasite aşınca) | 150 ₺/t | 250 ₺ + 1,1 ₺/km |

Filo: Kamyonet 3 t/gün (180.000 ₺), Kamyon 12 t/gün (650.000 ₺), TIR 25 t/gün (1.600.000 ₺). İkinci ele satış değeri %60.

**Transfer:** Tesisler arasında mal taşınabilir. Nakliye, taşınan malın maliyetine eklenir.

### Tedarik yolları

| Yol | İndirim | En az | Teslim | Açılış |
|---|---|---|---|---|
| Piyasadan | — | — | Hemen | Başlangıç |
| Fabrikadan | −%4 | 20 t | 3 gün | Toptancı |
| İthalat (gemi) | −%9 | 100 t | 20 gün, yalnızca liman şehrindeki tesise | Bayi |

Toplu alım indirimi ayrıca uygulanır: 10 t −%1, 50 t −%2, 200 t −%3. Yoldaki mal, hedef tesisin kapasitesinden düşülür.

## 8b. Nakit akışı, tahsilat ve mahkeme

### Vadeli satışın akıbeti (vade günü)

| Risk | Ödemeyi erteler | Kısmen öder | Çeki karşılıksız çıkar |
|---|---|---|---|
| A | %9 | %2 | %1 |
| B | %18 | %8 | %6 |
| C | %25 | %15 | %15 |

- **Erteleme:** Müşteri bir bahane söyler ("Şantiye hakedişi gecikti", "Çek yarın bankada, söz") ve 7–15 gün yeni vade alır. Her erteleme bir sonraki seferde ödememe olasılığını artırır (×1,6). En fazla 3 kez ertelenir.
- **Kısmi ödeme:** Borcun %30–70'i ödenir, kalanı için 10–20 gün sonrasına söz verilir.
- Muhasebeci tüm olumsuz olasılıkları yarıya indirir.
- **Karşılıksız:** Alacak **takibe** düşer, tutarı o ay kârdan karşılık olarak düşülür (vergiyi de azaltır) ve firma **kara listeye** girer. Sonraki tekliflerinde "Daha önce ödemedi" yazar ve riski C olur.

### Takipteki alacak için seçenekler

| Seçenek | Masraf | Süre | Sonuç |
|---|---|---|---|
| Ara / ziyaret et | — | 5 günde bir | %35 ihtimalle borcun %20–50'si ödenir |
| Uzlaş | — | Hemen | A %75, B %65, C %55 tahsil edilir, kalanından vazgeçilir |
| İcra takibi | Tutarın %4'ü (en az 3.000 ₺) | 20–35 gün | %50: %60–100 tahsil (haciz). %28: borçlu itiraz eder, dava gerekir. %22: haczedilecek mal yoktur |
| Dava aç | Tutarın %8'i (en az 8.000 ₺) | 60–150 gün | %68 kazanma: %50–100 + faiz. %12 ihtimalle borçlu bu arada konkordato ilan eder: yalnızca %15 tahsil edilir |
| Zarar yaz | — | Hemen | Alacak kapanır |

- **Hukuk müşaviri** (45.000 ₺/ay): icra ve dava masrafını yarıya indirir, kazanma şansını +%15 artırır.
- **Kredi sigortası:** Prim aylık açık alacakların %0,4'ü. Müşteri ödemezse 30 gün içinde alacağın %80'i ödenir.
- **Net değerde** takip, icra ve davadaki alacaklar %20 değerle sayılır; sigortadaki alacak sigortanın ödeyeceği tutarla.

### Aleyhine davalar

- **Kalite tazminatı:** Her stok teslimatında %1,2 (fasona giden işlemede %4,2) ihtimalle müşteri 3–10 gün sonra "ölçü/kalite eksik" diyerek siparişin %10–25'i kadar tazminat ister. Seçenekler:
  - **Uzlaş:** talebin yarısını öde.
  - **Mahkemede savun:** avukat masrafı talebin %10'u; 30–60 gün sürer. Kazanma şansı %55 (müşavirle %70). Kaybedersen talebin tamamını ödersin ve itibar −3.
- **İş kazası:** Forklift ve vinç başına günde %0,12 olasılık (iş güvenliği uzmanıyla dörtte biri). Tazminat 150–400 bin ₺. Ödenmezse dava açılır; %50 ihtimalle iki katı ve itibar −5.

### Diğer

- **Faktoring:** Normal alacak bankaya kırdırılıp para hemen alınır. Kesinti aylık %3 (muhasebeciyle %1,5).
- **Banka kredisi:** Limit = unvan tabanı + net değerin %30'u. Aylık faiz zorluğa göre %2,5 / %3,5 / %4,5.
- **Net değer** = kasa + stok + alacaklar + tesis, ekipman, makine ve araçların ikinci el değeri − kredi.
- **Hırsızlık / yangın:** tesis başına günde %0,6 / %0,15. Stok sigortası zararın %90'ını öder; gece bekçisi hırsızlığı üçte bire indirir.
- **Fabrika gecikmesi:** Fabrikadan alımların %15'i 2–5 gün gecikir.
- **Ay sonu:** kira, bakım, maaşlar, araç gideri, faiz, sigorta primleri, eksi bakiye faizi ve geçici vergi. Kasa eksiye düşerse banka önce alacakları kırdırır, sonra otomatik kredi açar. Bunlar yetmezse iflas olur.

## 8c. Şehir fiyatları, kur ve üretim

- **Şehir fiyat farkı:** Her şehrin her kategori için kendi fiyat sapması vardır (±%7, her gün yavaşça değişir). Alım, satış, dükkan satışı ve müşteri teklifleri o şehrin fiyatıyla yapılır. Al-sat paneli en ucuz ve en pahalı şehri gösterir. Haritada şehre dokununca kategori farkları görünür. Ucuz şehirden alıp transferle pahalı şehirde satmak (arbitraj) mümkündür.
- **Dolar/TL:** Günde ortalama +%0,04 değişir, oynaklığı %0,4'tür. Kur artışının yarısı çelik fiyatlarına yansır. "Dolar yükseldi" haberi kuru da iter.
- **İthalatta kur riski:** Alımın %30'u peşin ödenir, kalan %70 mal geldiğinde o günkü kurla ödenir. İstenirse +%1,5 bedelle kur sabitlenir (vadeli kur).
- **Fire:** Kendi makinenle işlemede fasonun yerine yalnızca %1,2 net fire maliyeti vardır (hurda satışı düşülmüş).
- **Bobin dilme hattı** (2,5 Mn ₺, Bayi): HRP bobini 3 mm siyah sac levhaya çevirir. Günde en fazla 30 t, 300 ₺/t işçilik, %1 fire.

## 9. İlerleme

### Unvanlar (satılan tonaj **ve** net değer şartı; kazanılan unvan kaybedilmez)

| Unvan | Ton | Net değer | Açılanlar | Kredi tabanı | Tesis | Sipariş tonajı |
|---|---|---|---|---|---|---|
| Komisyoncu | 0 | — | Aracılık, 14 temel ürün | 0 | 1 | 0,5–2,5 t |
| Esnaf | 25 | 20.000 ₺ | Vadeli satış, kredi, 12 ürün | 50.000 ₺ | 2 | 1–6 t |
| Toptancı | 150 | 250.000 ₺ | Fabrikadan alım, 12 ürün | 250.000 ₺ | 3 | 3–18 t |
| Bayi | 800 | 1,5 Mn ₺ | İthalat, paslanmaz ve alüminyum | 1 Mn ₺ | 5 | 6–40 t |
| Distribütör | 3.000 | 8 Mn ₺ | HEB, spiral boru, Hardox, 316L | 4 Mn ₺ | 8 | 12–80 t |
| **Demir Baronu** | 12.000 | 40 Mn ₺ | Oyunun hedefi, sonra serbest oyun | 15 Mn ₺ | 12 | 20–150 t |

Perakende satışlar da tonaja sayılır.

### Personel

| Rol | Maaş/ay | En fazla | Etki |
|---|---|---|---|
| Satış elemanı | 35.000 ₺ | 8 | +1 teklif/gün, +%0,4 fiyat, biraz daha büyük siparişler |
| Muhasebeci | 40.000 ₺ | 1 | Karşılıksız çek riski ve faktoring maliyeti yarıya |
| Gece bekçisi | 22.000 ₺ | 1 | Hırsızlık olasılığı üçte bire |
| Tezgâhtar | 25.000 ₺ | Dükkan başına 3 | Kapı müşterisi +%50 |
| Hukuk müşaviri | 45.000 ₺ | 1 | İcra ve dava masrafı yarıya, kazanma şansı +%15 |
| İş güvenliği uzmanı | 30.000 ₺ | 1 | İş kazası olasılığı dörtte bire |

### Başarımlar (23)

İlk iş, kendi yerin, ilk dükkan, 25 perakende satış, ilk araç, ilk makine, 100 t, 3 şehirde tesis, 10 sadık müşteri, sigorta, 1.000 t, 1 Mn ₺, ilk ithalat, fiber lazer, 10 Mn ₺, 10.000 t, 100 Mn ₺, fuarcı, ilk ihale, ilk üçte, sektör lideri, dava kazan, rakip satın al. Her başarım nakit ödül (1.000 ₺ – 2 Mn ₺) ve +2 itibar verir.

### Eğitim ve günlük görevler

- **İlk adımlar (8 adım, ödüllü):** ilk aracılık → ilk depo → stok al → stoktan teslim → Esnaf ol → vadeli satış → dükkan aç → ihaleye gir.
- **Günlük görevler:** Her gün 3 görev gelir: sipariş kapat, X t kategori sat, X ₺ kâr et, dükkanda perakende satış. Her biri (unvan+1) × 2.000 ₺ ve itibar +1 verir.

## 9b. Rekabet: rakip şirketler

Oyuncu sektörde yalnız değildir. 7 bot şirket kendi başına büyür, şube açar, ihaleye girer ve müşteri kapar.

| Şirket | Merkez | Karakter | Başlangıç | Özellik |
|---|---|---|---|---|
| Özler Demir Çelik | İstanbul | Sektör devi | ~25 Mn ₺ | Yavaş ama sürekli büyür, yenmesi en zor rakip |
| Kaya Metal | Ankara | Agresif | ~2 Mn ₺ | Hızlı büyür, ihalede düşük teklif verir, iflas riski en yüksek |
| Ege Çelik Ticaret | İzmir | Temkinli | ~6 Mn ₺ | İhalede yüksek teklif verir, istikrarlı |
| Marmara Profil | Kocaeli | Uzman (profil) | ~3 Mn ₺ | Profil işlerinde daha agresif |
| Güneydoğu Boru | Gaziantep | Uzman (boru) | ~1,2 Mn ₺ | Boru işlerinde daha agresif |
| Anadolu Yapı Demir | Konya | Perakendeci | ~400 bin ₺ | Dükkan odaklı, ihalede pahalı |
| Karadeniz Sac | Samsun | Temkinli (sac) | ~800 bin ₺ | Sac uzmanı |

Her rakibin bir patronu vardır (Hikmet Özler, Ferhat Kaya, Nur Aydemir…). Patronlar iş kaptıklarında, ihale kazandıklarında ve fiyat savaşı açtıklarında karakterlerine uygun sözler söyler ("Bu şehir ikimize dar.").

İflas eden rakibin yerine 20–40 gün sonra yeni bir şirket girer (Yıldırım Metal, Atlas Demir, Toros Çelik…).

**Rakiplerin oyuncuya etkisi:**
- **Müşteri kapma:** Rakibin şubesi olan şehirden gelen tekliflerin bir kısmı "Rakip" etiketlidir ve yalnızca o gün açık kalır. Gün biterse işi rakip alır.
- **Pazarlık:** Her teklifte bir kez %3 zam istenebilir. Başarı şansı %55'tir; itibarla ve sadık müşteride artar, rakipli işte azalır. Rakipli işte pazarlık tutmazsa iş rakibe gider.
- **Dükkan müşterisi:** Aynı şehirdeki her rakip şube, dükkana gelen müşteriyi azaltır.
- **Fiyat savaşı:** Bir rakip bir kategoride 6 gün fiyat kırar. O kategoride marjlar −%3 düşer ve rakip daha sık araya girer.
- **Eleman ayartma:** Rakip, satış elemanına teklif yapar. 60.000 ₺ prim verilmezse eleman gider.
- **Konkordato:** Rakip iflas ederse deposu (120 t, forklift dahil) normal fiyatın %40'ına satışa çıkar.
- **Müşteri kaybı:** Sadık müşterinin siparişini geciktirirsen rakibe geçer. Rakibin kaptığı her işte sadakati azalır.
- **Rakip satın alma (Bayi'den itibaren):** Net değeri seninkinin %35'inden az olan rakip, net değerinin 1,3 katına satın alınabilir. Şubeleri sana küçük depo olarak geçer; itibar +5.

**İhaleler (Esnaf'tan itibaren, 6–12 günde bir):** Büyük projeler: otoyol viyadüğü, şehir hastanesi, metro, TOKİ, doğalgaz hattı, tersane… Oyuncu kâr marjı seçer (%2/4/6/8/11) ve tutarın %3'ü kadar teminat yatırır. Rakipler karakterlerine göre gizli teklif verir, en düşük teklif kazanır. Kazanan 10 gün içinde teslim eder, ödeme 30 gün vadelidir. Kaybedenin teminatı iade edilir. Teslim edilmezse teminat yanar ve itibar −10 düşer. Sonuçlar tüm tekliflerle birlikte gösterilir.

**Sıralama:** Net değere göre sektör sıralaması ve son 30 günün tonajına göre pazar payı her zaman görünür. Sıra üst barda da yazar.

## 9c. Takvim, mevsimler ve etkinlikler

- **Takvim:** 1. gün 1 Mart, 1. yıldır. Her ay 30 gündür.
- **Mevsimler:**

| Mevsim | Sipariş | Talep |
|---|---|---|
| İlkbahar | ×1,15 | İnşaat demiri ×1,3, profil ×1,2 |
| Yaz | ×1,2 | İnşaat demiri ×1,5, profil ×1,3, boru ×0,8 |
| Sonbahar | ×1,1 | Boru ×1,3 |
| Kış | ×0,95 | İnşaat demiri ×0,5, profil ×0,8, boru ×1,4, sac ×1,1 |

- **Etkinlikler:** Aynı anda bir etkinlik olur. Bitiminden 6–14 gün sonra yenisi gelir.
  - **Fuar** (TÜYAP, Ankara, İzmir, Konya, Bursa, Gaziantep): Stand kiralamak net değerin %1'i kadardır (20 bin – 2 Mn ₺). 5 gün boyunca itibar +5, günde +2 teklif ve fuar şehrinden üç kat sipariş getirir.
  - **Bayram** (3 gün): Sipariş gelmez, dükkanlar kapalı olur. Karar kartları çıkar: müşterilere hediye (itibar +4) ve personele ikramiye (verilmezse %40 istifa riski).
  - **Meydan okuma** (7 gün): "X ton Y kategorisi sat." Tamamlanırsa nakit ödül ve itibar +3.
  - **Üretici kampanyası** (5 gün): Bir kategoride tüm alımlar −%5.
  - **Vergi denetimi:** Muhasebeci yoksa karar kartı çıkar: 25.000 ₺ mali müşavir tut ya da %50 ihtimalle net değerin %2'si kadar ceza riskine gir.
  - **Durgunluk** (6 gün): Günde 1 teklif eksik gelir.
- **Yılın Tüccarı (her 1 Mart):** Sıralamada ilk 5'e ödül verilir: 5 Mn / 2 Mn / 1 Mn / 500 bin / 250 bin ₺, ayrıca itibar +5.

## 9d. Menü, zorluk, senaryolar ve miras

- **Ana menü:** Devam et (şirket, tarih, unvan, net değer, sıra), Yeni oyun, Nasıl oynanır, Rekorlar, Ayarlar. Altında canlı fiyat bandı akar. Oyun içinde sol üstteki menü düğmesiyle duraklatma.
- **Yeni oyun:** şirket adı, merkez şehir, zorluk.

| Zorluk | Başlangıç | Rakip saldırganlığı | Kredi faizi | Vergi |
|---|---|---|---|---|
| Kolay | 15.000 ₺ | ×0,7 | %2,5 | %15 |
| Normal | 5.000 ₺ | ×1 | %3,5 | %20 |
| Zor | 2.000 ₺ | ×1,35 | %4,5 | %25 |

- **Senaryolar:**
  - **Kur Krizi (90 gün):** Toptancı olarak başlarsın; 85 t stok, 300 bin ₺ nakit ve 1,5 Mn ₺ kredi borcun vardır. Dolar 20 gün boyunca her gün %1 yükselir, talep 30 gün düşük kalır. Hedef: 90. günde 3 Mn ₺ net değer.
  - **Deprem Bölgesi (120 gün):** Gaziantep'te Esnaf olarak başlarsın; bölgede inşaat demiri talebi üç kattır. Hedef: 1.000 t inşaat demiri satmak.
- **Miras (prestij):** Demir Baronu olunca şirket satılabilir. Her 20 Mn ₺ net değer için 1 miras puanı kazanılır (en az 1, en çok 20). Her puan yeni oyunlarda kalıcı olarak +10.000 ₺ başlangıç, +2 itibar (en çok 70) ve tekliflerde +%0,2 fiyat verir.
- **Ses ve titreşim:** Satış, ihale, uyarı ve gün sonu için sentezlenmiş sesler. Ayarlardan kapatılabilir.
- **Rekorlar:** Baron olunca, her yıl sonunda ve iflasta sonuç kaydedilir (en iyi 10).
- **Ayarlar:** ay sonu raporu, gün sonu bildirimi, tüm verileri sil.
- **İflastan önce kurtarma:** Ay sonunda kasa eksiye düşerse banka önce alacakları otomatik kırdırır, sonra kredi limitinden otomatik kredi açar. İflas ancak bunlar yetmezse olur.
- **Eski kayıtlar:** Yeni alanlar otomatik doldurulur, ilerleme silinmez.

## 10. Prototipte olmayanlar (sonraki sürümler)

2. **Rakiplerle ortaklık:** Büyük ihalede konsorsiyum, rakipten acil stok alıp satma.
3. **Çok oyunculu lig:** Gerçek oyuncularla aynı piyasada haftalık rekabet.
5. **Profil hattı:** Şeritten kutu profil üretimi (dikey büyümenin ikinci adımı).
7. **Pas ve stok yaşı:** Açık sahada uzun bekleyen sac değer kaybeder.
8. **Müşteri ilişkileri:** Bayram hediyesi, tahsilat görüşmesi mini oyunu, müşteri kaybı.
9. **Haftalık lig (Game Center):** Herkes aynı rastgele tohumla 60 gün oynar, en yüksek net değer kazanır.

## 11. Para kazanma (App Store)

Oyun ücretsizdir ve satın almadan baştan sona oynanabilir. Mağazada 21 isteğe bağlı ürün vardır; kimlikler, fiyatlar ve etkiler `APPSTORE.md` (H bölümü) içindedir.

| Grup | Adet | Fiyat aralığı (TL) |
|---|---|---|
| Sermaye paketleri (net değerle ölçeklenir) | 4 | 29,99 – 549,99 |
| Başlangıç paketi (bir kez) | 1 | 49,99 |
| Güçlendiriciler (yalnızca işe yarayacakları anda satılır) | 7 | 14,99 – 59,99 |
| Kalıcı ürünler | 4 | 99,99 – 179,99 |
| Temalar | 3 | 49,99 |
| Baron Kulübü aboneliği | 2 | 49,99 / hafta · 149,99 / ay |

- **Tasarım ilkeleri:**
  - Sermaye paketleri net değerle büyür; böylece oyunun her aşamasında anlamlı kalır.
  - Güçlendiriciler, işe yaramayacakları durumda satın alınamaz (takipte alacak yokken Hukuk Bürosu gibi).
  - Abonelik günlük küçük bir avantaj verir, oyunu kendi başına bitirmez.
- **Teknik:** StoreKit 2 ile uygulamanın kendi eklentisi kullanılır, üçüncü taraf servis yoktur. Tarayıcı sürümünde aynı mağaza "deneme modu"nda çalışır ve ödeme almaz.

## 11b. Görsel tasarım

Görsel dosya kullanılmaz. Her şey kodla çizilir (canvas ve SVG); bu yüzden uygulama küçük kalır ve internetsiz çalışır. Renkler arayüz temasından okunur; satın alınan temalar çizimlere de yansır.

- **Tesis sahneleri:** her depo ve dükkan kartında yandan görünüm.
  - **Bina:** Depo kademeye göre büyür: depo köşesi → depo + açık saha (çit) → lojistik depo (kemerli çatı, 3 kapı) → liman deposu (deniz, gemi, rıhtım vinci). Dükkan; tentesi, vitrini ve şirket adının yazılı tabelasıyla görünür. Yapı market tabelası da vardır.
  - **Stok:** her 1/14 kapasite için bir yığın, ürün kategorisine göre çizilir: sac levha yığını, profil uçları, boru piramidi, nervürlü demir bağı, dolu mil, parlak paslanmaz.
  - **Hareket:** forkliftler mal taşır, tavan vinci yük kaldırır, gelen sevkiyat kamyonu yaklaşır, o gün yükleme yapıldıysa kamyon çıkar, dükkana müşteriler yürür. Bayramda dükkan kapalıdır.
- **Derinlik ve ışık:**
  - Binalar ön yüz, koyu yan yüz ve çatıdan oluşur; cephelerde dikey oluk çizgileri ve pas izleri vardır.
  - Cephe tabelasında şirket adı yazar. Pencereler ışıklıdır, rampa lambaları titreşir.
  - Her nesnenin altına gölge düşer. Stok yığınlarında üst yüz ve parlama, borularda radyal ışık vardır.
  - Gökyüzü mevsim renginde; güneş, süzülen bulutlar, uzak tepeler ve pencereleri yanan şehir silueti görünür. Zemin beton saha ve şeritli yoldur.
- **Araçlar:** Kamyonet, kamyon ve TIR ayrı çizilir (ızgara, far, ayna; TIR'da dorse). Filodaki araçlar ana tesiste park eder. Forkliftte koruma kafesi ve baretli sürücü vardır. Tavan vinci ve rıhtım vinci kafes kirişlidir. Liman gemisinde renkli konteynerler bulunur.
- **Mevsim havası:** Kışın çatılarda, yığınlarda ve zeminde kar olur, kar yağar. Sonbaharda yağmur, yazın sıcak hava dalgası vardır.
- **Ana menü:** gün batımı gökyüzü, iki katmanlı fabrika silueti ve ışıklı pencereler, tüten bacalar, parlayan fırın ağzı, kafes kirişli rıhtım vinci. Yolda iki yönde TIR, kamyon ve kamyonet geçer; kaynak kıvılcımları saçılır.
- **Harita:** 81 il sınırı, dalgalı deniz, gradyanlı kara, seçilen il vurgusu, merkezden diğer tesislere sevkiyat rotaları.
- **Her şeyin kendi resmi:**
  - **Filo:** kamyonet (profil yüklü), kamyon (boru yüklü) ve TIR (dorseli) ayrı küçük resimlerle.
  - **Atölye:** 6 makinenin her biri ayrı çizilir ve kuruluysa çalışır: giyotin makasın bıçağı iner, şerit testerenin kasnakları döner ve kıvılcım saçar, demir bükme tablası etriye büker, abkant koçu iner, fiber lazer kafası sac üstünde gezer, dilme hattı bobini çözüp şeritlere ayırır.
  - **Ekipman:** forklift sac taşır, tavan vinci yük kaldırır.
  - **Personel:** her görevin kendi portresi ve aleti: satış elemanı (telefon, kravat), muhasebeci (gözlük, hesap makinesi), gece bekçisi (kasket, fener), hukuk müşaviri (cübbe, terazi), iş güvenliği uzmanı (baret, reflektörlü yelek), tezgâhtar (önlük).
  - **Tesis kademeleri:** "Yeni tesis aç" kartında seçilen türün bütün kademeleri resimli şerit olarak görünür; "Büyüt" satırında bir sonraki kademenin resmi vardır.
  - **Ürünler:** 48 ürünün her biri kendi kesitiyle üç boyutlu çizilir (kutu, dikdörtgen, köşebent, lama, U, I, H, T, boru, mil, kare, trapez, delikli, bobin, filmaşin, hasır, nervürlü) ve malzemenin gerçek rengini (siyah sac, DKP, galvaniz, paslanmaz, alüminyum…) taşır. Köşede ürünün ana ölçüsü yazar.
  - **Mağaza ve şirket:** her satın alma paketinin, sigortaların ve bankanın kendi çizimi vardır.
- **Arayüz:** fiyat grafiklerinin altı dolgulu, sipariş ve piyasa satırlarında ürünün üç boyutlu kesit resmi.
- **Mevsim efekti (üst bar):** kışın kar, sonbaharda yaprak, ilkbaharda polen, yazın sıcak hava zerreleri. Sahnelerin gökyüzü rengi de mevsime göre değişir.
- **Ekran efektleri:**
  - Teslimatta yüklü bir kamyon ekranı geçer ve "+kâr" yazısı yükselir.
  - Kasa yeni değere sayarak gider.
  - Satın almada para yağmuru, unvan ve ihale kazanımında konfeti çıkar.
  - Karşılıksız çekte kasa titrer.
- **Karakterler:** rakip patronların isimden üretilen portreleri (ten, saç, bıyık, gözlük; kadın patronlar uzun saçlı), oyuncunun baretli portresi, müşteri firmalar için baş harfli renkli logolar.
- **Pencere çizimleri:** fuar standı, bayram, ikramiye, denetim, satılık depo, transfer, dava tokmağı, iş kazası, kupa, madalya, taç, kırık profil (iflas), senaryo bayrağı, takvim, mağaza, el sıkışma.
- **Ürün çizimi:** al-sat panelinde seçilen ürünün yığın resmi.
- **Performans:**
  - Yalnızca ekranda görünen çizimler hareket eder, saniyede en fazla 30 kare.
  - Uygulama arka plandayken çizim durur.
  - Ayarlar → **Animasyonlar** ile tamamen kapatılabilir. "Hareketi azalt" kullananlarda kapalı başlar.

## 12. Ekranlar

**Yatay ekran.** Oyun yalnızca yatay oynanır (iPhone'da sola ve sağa çevrilebilir). Görünüm: koyu lacivert paneller, para işlemleri için altın sarısı, servet için turkuaz-yeşil, iri ve yuvarlak yazı (Nunito).

- **Üst çubuk:** şirket adı ve altında yeşil noktalı satır (unvan · itibar · sıra · son haber), kasa (altın para simgesi ve sermaye paketine giden yeşil +), başarımlar kupası, sarı **Mağaza** düğmesi, ? (nasıl oynanır), ayarlar/menü ve takvim (gün, yıl; altında ay sonuna kalan süre çubuğu).
- **Alt çubuk:** 6 sekme (Ana ekran, Siparişler, Piyasa, Tesisler, Rekabet, Şirket; seçili olanın üstünde sarı çizgi) ve sağda sarı **Günü bitir** düğmesi.
- **Ana ekran:**
  - **Kasa kartı:** büyük kasa rakamı, net değer, net değer grafiği, piyasa durumu ya da etkinlik etiketi; altta borç, alacak, aylık kira ve aylık maaş.
  - **Tesislerim:** her tesis kendi canlı resmiyle kart olarak (DEPO/DÜKKAN etiketi, doluluk çubuğu) ve kesik çizgili "Yeni tesis" kartı.
  - **Bugün:** dolar kuru ve sıradaki eğitim adımı ya da günlük görevler.
  - **Kısayol kutuları:** Teklifler, İhaleler, Piyasa, Banka, Personel, Alacaklar, Filo, Atölye.
  - Altın renkli **Türkiye haritası · 81 il** düğmesi.
- **Şirket:** bütün bölümler (Unvan, Net değer, Filo, Atölye, Personel, Sigorta, Banka, Alacaklar, Ay sonu, Sadık müşteriler, Başarımlar, İstatistik, Haberler, Şirketi sat) simgeli kutular hâlinde. Kutuya dokununca bölüm açılır, "‹ Şirket" ile geri dönülür. Takipteki alacak varsa kutunun alt yazısı kırmızıdır.
- **Diğer sekmeler:** teklifler, ürünler ve tesisler genişliğe göre 2–3 sütuna dizilir. Haritanın yanında il bilgisi durur, Rekabet bölümleri yan yana gelir.
- **Ana menü:** solda logo ve fabrika sahnesi, sağda fiyat bandı, Devam et / Yeni oyun ve simgeli dört kutu.
- **Yeni oyun:** solda form, sağda sahne, açıklama ve başla düğmesi. "Nasıl oynanır" iki sütundur, mağaza kutuları 3–4 sütuna yayılır.
- **Dik oynanmaz.** iPhone uygulaması yatay ekrana kilitlidir. Tarayıcıda dokunmatik cihaz dik tutulursa oyunun yerine yalnızca "Telefonu yan çevir" ekranı çıkar, geçilemez. Masaüstünde pencere dar ve uzun olsa da yatay düzen kullanılır.

1. **Ana menü ve yeni oyun:** devam et, yeni oyun (ad, şehir, zorluk), nasıl oynanır, rekorlar, ayarlar. Karar kartları ve yıl sonu ödülü pencereleri
2. **Siparişler:** aktif siparişler ve yeni teklifler. Her teklifte şehir, en yakın tesise mesafe, aracılık kârı, stoktan kâr ve hangi tesisten gideceği yazar
3. **Piyasa:** 6 kategori filtresi, "stoktakiler" filtresi, ürün satırı (fiyat, günlük değişim, 30 günlük grafik, ağırlık, toplam stok). Al/sat paneli: tesis seçimi, miktar, 3 tedarik yolu
4. **Tesisler:** her tesis için doluluk, yükleme kapasitesi, kira, ekipman, tezgâhtar, büyütme, ürün bazında stok ve kâr/zarar, transfer ve satış. Yoldaki sevkiyatlar. Yeni tesis açma (şehir + tür)
5. **Rekabet:** sektör sıralaması, pazar payı çubuğu, açık ihaleler ve teklif düğmeleri, son ihale sonuçları, etkinlik ve mevsim, rakip haberleri
6. **Şirket:** unvan, net değer grafiği, filo, atölye makineleri, personel, sigorta, banka, alacaklar, ay sonu tahmini (vergi dahil), sadık müşteriler, başarımlar, istatistik, haberler
7. **Üst bar:** menü düğmesi, tarih ve mevsim, kasa, unvan, net değer, stok/kapasite, itibar, sektör sırası, haber bandı. Altta büyük **Günü Bitir** düğmesi

### Ekran düzeni ilkeleri

- **Ana menü:** ekran yüksekliğine göre küçülen logo, iki ana düğme ve 2×2 karo. Fabrika sahnesi akışın sonunda durur, hiçbir düğmenin üstüne binmez. Kısa ekranlarda (≤700 px) tanıtım cümlesi gizlenir ve aralıklar daralır; iPhone SE'de kaydırma gerekmez.
- **Bildirimler:** Gün sonu özeti üst bardaki haber satırına yazılır. Baloncuklar küçük ve en fazla 2 tanedir; 2,8 saniyede kaybolur, dokununca kapanır.
- **Siparişler:** Eğitim, görevler, etkinlik, ihale ve fiyat savaşı tek satırlık kaydırılabilir etiketlerdedir; dokununca ayrıntı açılır. Teklifler ilk ekranda görünür.
- **Tesisler:** Kartta sahne, doluluk, yükleme, kira ve stok özeti görünür. Ekipman, büyütme ve stok satırları "Yönet" altındadır.
- **Şirket:** 13 bölüm özet satırlı açılır başlıklardır. Unvan ve net değer açık başlar; takipte alacak varsa Alacaklar da açılır.
- **Pencereler:** alttan açılan kart, ekranın en fazla %80'i; çizim yüksekliği 64–84 px.
- **Kontrol:** Her sürümde 19 ekran 375×667 ve 430×932 boyutlarında otomatik çekilir; yatay taşma ve konsol hatası denetlenir.

## 13. Teknik plan

- **Prototip (bitti):** tek dosyalık HTML/JS, `DemirBaronu/web/play.html`. Tüm kurallar burada çalışıyor, kayıt tarayıcıda tutuluyor.
- **Üretim sürümü:** Factory King ile aynı altyapı. Kurallar Unity'den bağımsız C# çekirdeğinde (`DemirBaronu.Core`, netstandard2.1) ve NUnit testleriyle yazılır. Arayüz UI Toolkit ile kurulur. Mevcut GitHub Actions → TestFlight hattı (`.github/workflows/ios-testflight.yml`) yeniden kullanılır.
- **Kayıt:** sürümlü JSON, sağlama toplamı, iCloud. Hesap sistemi yok.
- **Rastgelelik:** tohumlu RNG. Haftalık lig ve tekrar oynatma için gerekli.
- **Denge testi:** otomatik "akıllı tüccar" ve "rastgele tüccar" botları oynar. 4. sürümde (tahsilat ve mahkeme, şehir fiyatları, kur, görevler dahil, Normal zorluk) ölçülen değerler, 6 deneme: haberleri kullanmayan sade akıllı bot **357–388. günde** baron oldu ve hiç iflas etmedi. 700 günde 4–11 alacağı karşılıksız çıktı; icra ve uzlaşmayla 7–26 Mn ₺ geri alındı, 0,8–1,8 Mn ₺ hukuk masrafı ödendi. Rastgele aracılık yapan bot 200 günde Toptancı'da kaldı.

## 14. Yol haritası

| Aşama | İçerik | Süre (tahmini) |
|---|---|---|
| 0 | Tasarım belgesi + oynanabilir web prototipi | ✅ bitti |
| 1 | iOS uygulaması: Capacitor sarmalayıcı, bulut derleme → TestFlight, mağaza metinleri, ekran görüntüleri | ✅ hazır, ilk derleme hesap ayarlarından sonra |
| 2 | Unity arayüzü (5 ekran), ses, haptik | 2 hafta |
| 3 | Şehir fiyat farkları, rakipler, ihaleler, kur riski | 3 hafta |
| 4 | IAP, ödüllü reklam, Game Center ligi | 1 hafta |
| 5 | TestFlight beta, denge ayarı, App Store metinleri ve görselleri | 2 hafta |

## 15. Başarı ölçütleri

- 1. gün tutma ≥ %40, 7. gün ≥ %15
- İlk oturumda ilk depoyu kiralayan oyuncu oranı ≥ %70 (eğitim başarısı)
- Ortalama oturum 8–12 dakika (≈ 15–25 oyun günü)
