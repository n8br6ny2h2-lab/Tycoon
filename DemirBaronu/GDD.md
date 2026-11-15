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

### Şehirler (15)

İstanbul (İkitelli), Kocaeli (Dilovası), Bursa (Demirtaş OSB), Sakarya (Hendek), Eskişehir, Ankara (Ostim), İzmir (Çiğli AOSB), Manisa, Antalya, Konya (Büsan), Kayseri, Adana (Hacı Sabancı OSB), Mersin (Liman), Gaziantep (Başpınar), Samsun (Tekkeköy). Her şehrin bir talep büyüklüğü vardır (İstanbul 1,6, Sakarya 0,5). Altı şehir **liman** şehridir: İstanbul, Kocaeli, İzmir, Antalya, Mersin, Samsun.

Mesafe koordinatlardan hesaplanır (kuş uçuşu × 1,25 yol katsayısı). Örnek: İstanbul–Ankara ≈ 440 km.

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

## 8b. Nakit akışı, risk ve vergi

- **Alacaklar:** Vadeli satışlar alacak defterine yazılır. Vade günü tahsil edilir ya da karşılıksız çıkar.
- **Faktoring:** Alacak bankaya kırdırılıp para hemen alınır. Kesinti aylık %3 (muhasebeciyle %1,5). Risk bankaya geçer.
- **Banka kredisi:** Limit = unvan tabanı + net değerin %30'u. Aylık faiz %3,5.
- **Net değer** = kasa + stok (piyasa fiyatıyla, yoldaki mal dahil) + alacaklar + tesis, ekipman, makine ve araçların ikinci el değeri − kredi.
- **Hırsızlık:** Tesis başına günde %0,6 olasılık (gece bekçisiyle üçte biri). Bir ürünün %10–30'u çalınır.
- **Yangın:** Tesis başına günde %0,15 olasılık. Tüm stoğun %40'ı yanar.
- **Sigorta:** Zararın %90'ını öder. Prim aylık stok değerinin %0,2'si (en az 1.000 ₺).
- **Ay sonu (her 30 gün):** kira, ekipman bakımı, tezgâhtar ve personel maaşı, araç gideri, kredi faizi, sigorta primi, eksi bakiye faizi (%5) ve **geçici vergi**. Vergi, ayın kârından sabit giderler düşüldükten sonra kalanın %20'sidir.
- **İflas:** Ay sonunda kasa, kredi limitinin %25'i + 10.000 ₺'nin altına düşerse oyun biter.

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

### Başarımlar (17)

İlk iş, kendi yerin, ilk dükkan, 25 perakende satış, ilk araç, ilk makine, 100 t, 3 şehirde tesis, 10 sadık müşteri, sigorta, 1.000 t, 1 Mn ₺, ilk ithalat, fiber lazer, 10 Mn ₺, 10.000 t, 100 Mn ₺. Her başarım nakit ödül (1.000 ₺ – 2 Mn ₺) ve +2 itibar verir.

## 10. Prototipte olmayanlar (sonraki sürümler)

1. **Şehirler arası fiyat farkı:** Her şehirde ayrı piyasa fiyatı. Ucuz şehirden alıp pahalı şehre satma (arbitraj).
2. **Rakip tüccarlar (yapay zekâ):** Aynı teklife rakipler de girer. Fiyat kırma ve müşteri kapma devreye girer.
3. **İhaleler:** Büyük projeler (köprü, stadyum, OSB). Kapalı zarfla teklif verilir, teminat mektubu gerekir.
4. **Kur riski:** İthalat dolar bazlı olur, ödeme gemi geldiğinde yapılır.
5. **Bobin dilme / boyuna kesme hattı:** Bobin alıp sac ve şerit üretilir (dikey büyüme).
6. **Fire ve hurda:** Kesimde %2–3 fire oluşur ve hurdacıya satılır.
7. **Pas ve stok yaşı:** Açık sahada uzun bekleyen sac değer kaybeder.
8. **Müşteri ilişkileri:** Bayram hediyesi, tahsilat görüşmesi mini oyunu, müşteri kaybı.
9. **Haftalık lig (Game Center):** Herkes aynı rastgele tohumla 60 gün oynar, en yüksek net değer kazanır.

## 11. Para kazanma (App Store)

İlke: **para ile kazanılmaz, zaman kazanılır.** Ticaret becerisi her zaman belirleyicidir.

| Ürün | Tür | Fiyat (TR) | İçerik |
|---|---|---|---|
| Reklamsız | Tüketilmeyen | 149,99 ₺ | Tüm geçiş reklamlarını kaldırır |
| Baron Paketi | Tüketilmeyen | 249,99 ₺ | Reklamsız + 2. kayıt yuvası + altın şirket logosu |
| Piyasa Analisti | Aylık abonelik | 99,99 ₺ | Ertesi günün haber başlığını önceden gösterir ve 90 günlük grafik açar |
| Kozmetik | Tüketilen | 29,99–79,99 ₺ | Kamyon boyaları, ofis temaları |

Ödüllü reklam (isteğe bağlı): ay sonunda faizi bir kez sıfırlar, fason kesimi bir kez bedava yapar veya kaybedilen bir teklifi geri getirir. Günde en fazla 5 kez.

Nakit satılmaz. Böylece oyun "kazanmak için öde" olmaz ve Apple'ın 3.1.1 kuralına uyulur.

## 12. Ekranlar

1. **Açılış:** şirket adı, merkez şehir seçimi, kısa hikâye ("Cebinde 5.000 ₺ ve bir telefon…")
2. **Siparişler:** aktif siparişler ve yeni teklifler. Her teklifte şehir, en yakın tesise mesafe, aracılık kârı, stoktan kâr ve hangi tesisten gideceği yazar
3. **Piyasa:** 6 kategori filtresi, "stoktakiler" filtresi, ürün satırı (fiyat, günlük değişim, 30 günlük grafik, ağırlık, toplam stok). Al/sat paneli: tesis seçimi, miktar, 3 tedarik yolu
4. **Tesisler:** her tesis için doluluk, yükleme kapasitesi, kira, ekipman, tezgâhtar, büyütme, ürün bazında stok ve kâr/zarar, transfer ve satış. Yoldaki sevkiyatlar. Yeni tesis açma (şehir + tür)
5. **Şirket:** unvan, net değer grafiği, filo, atölye makineleri, personel, sigorta, banka, alacaklar, ay sonu tahmini (vergi dahil), sadık müşteriler, başarımlar, istatistik, haberler
6. **Üst bar:** kasa, gün/ay, unvan, net değer, stok/kapasite, itibar, haber bandı. Altta büyük **Günü Bitir** düğmesi

## 13. Teknik plan

- **Prototip (bitti):** tek dosyalık HTML/JS, `DemirBaronu/web/play.html`. Tüm kurallar burada çalışıyor, kayıt tarayıcıda tutuluyor.
- **Üretim sürümü:** Factory King ile aynı altyapı. Kurallar Unity'den bağımsız C# çekirdeğinde (`DemirBaronu.Core`, netstandard2.1) ve NUnit testleriyle yazılır. Arayüz UI Toolkit ile kurulur. Mevcut GitHub Actions → TestFlight hattı (`.github/workflows/ios-testflight.yml`) yeniden kullanılır.
- **Kayıt:** sürümlü JSON, sağlama toplamı, iCloud. Hesap sistemi yok.
- **Rastgelelik:** tohumlu RNG. Haftalık lig ve tekrar oynatma için gerekli.
- **Denge testi:** otomatik "akıllı tüccar" ve "rastgele tüccar" botları oynar. 2. sürümde (tesisler, perakende, vergi, sigorta dahil) ölçülen değerler, 8 deneme: haberleri kullanmayan sade akıllı bot **334–412. günde** baron oldu ve hiç iflas etmedi. Ortalama ilerleme: Esnaf ~10. gün, Toptancı ~40. gün, Bayi ~100. gün, Distribütör ~230. gün. Rastgele aracılık yapan bot 200 günde Toptancı'da kaldı. Haberleri okuyup stoklayan gerçek oyuncunun daha hızlı ilerlemesi beklenir.

## 14. Yol haritası

| Aşama | İçerik | Süre (tahmini) |
|---|---|---|
| 0 | Tasarım belgesi + oynanabilir web prototipi | ✅ bitti |
| 1 | C# çekirdek: piyasa, sipariş, depo, banka, kayıt + testler | 2 hafta |
| 2 | Unity arayüzü (5 ekran), ses, haptik | 2 hafta |
| 3 | Şehir fiyat farkları, rakipler, ihaleler, kur riski | 3 hafta |
| 4 | IAP, ödüllü reklam, Game Center ligi | 1 hafta |
| 5 | TestFlight beta, denge ayarı, App Store metinleri ve görselleri | 2 hafta |

## 15. Başarı ölçütleri

- 1. gün tutma ≥ %40, 7. gün ≥ %15
- İlk oturumda ilk depoyu kiralayan oyuncu oranı ≥ %70 (eğitim başarısı)
- Ortalama oturum 8–12 dakika (≈ 15–25 oyun günü)
