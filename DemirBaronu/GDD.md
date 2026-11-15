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

Cebinde 5.000 ₺ ve bir telefonla başlarsın. Hedef 12.000 ton satış ve 40 milyon ₺ net değer. Komisyonculuk yaparak para biriktirir, depo kiralar, kamyon alır, vadeli satışın ve kredinin riskini yönetirsin. Sonunda Türkiye'nin en büyük boru, profil ve sac tüccarı olursun.

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

## 5. Ürünler

Fiyatlar oyun içi temsilî ₺/ton değerleridir. Gerçek piyasa verisi kullanılmaz.

| Kategori | Ürün | Örnek ölçü / norm | Taban ₺/t | Açılış |
|---|---|---|---|---|
| Sac | Siyah sac (HRP) | S235JR · 3 mm · 1500×6000 | 27.500 | Başlangıç |
| Sac | DKP sac | DC01 · 1 mm · 1000×2000 | 31.500 | Başlangıç |
| Sac | Baklavalı sac | S235 · 4/5 mm | 33.000 | Esnaf |
| Sac | Galvaniz sac | DX51D Z100 · 0,8 mm | 37.000 | Toptancı |
| Sac | Paslanmaz sac | AISI 304 · 2 mm | 128.000 | Distribütör |
| Profil | Kutu profil | 40×40×2 mm · 6 m | 30.000 | Başlangıç |
| Profil | Köşebent | 50×50×5 mm · 6 m | 29.000 | Başlangıç |
| Profil | NPU profil | NPU 100 · 12 m | 31.500 | Esnaf |
| Profil | HEA profil | HEA 200 · 12 m | 35.000 | Bayi |
| Boru | Sanayi borusu | Ø48,3×3 mm · 6 m | 30.500 | Başlangıç |
| Boru | Doğalgaz borusu | EN 10255 · DN25 | 36.500 | Toptancı |
| Boru | Galvaniz boru | Ø33,7×3,2 mm · 6 m | 40.500 | Bayi |

Oynaklık: sac en oynak kategori (ithalata ve otomotive bağlı). Profil inşaat sezonuna, boru altyapı ihalelerine bağlıdır.

## 6. Piyasa modeli

- Her ürünün fiyatı = taban × e^(genel endeks + ürün sapması).
- **Genel endeks** (kur, hurda ve enerji fiyatlarının ortak etkisi) yavaş bir rastgele yürüyüşle değişir ve tabana geri çekilir.
- **Ürün sapması** günlük gürültüyle değişir, %4/gün hızla sıfıra döner ve kategori olaylarından etkilenir.
- **Haber olayları** (her gün ~%18 olasılık, 4–8 gün sürer):
  - Dolar/TL yükseldi: tüm ürünler ↑
  - Çin'den ucuz sac ithalatı: sac ↓
  - İnşaat sezonu açıldı: profil ↑, profil talebi ↑
  - Haddehanede bakım duruşu: profil ↑↑
  - Doğalgaz altyapı ihalesi: boru ↑, boru talebi ↑
  - AB sınırda karbon vergisi (ihracat zorlaşır, iç piyasaya mal kalır): tümü ↓
  - Hurda fiyatları düştü: tümü ↓
  - Kentsel dönüşüm teşviki: profil ↑, boru ↑
  - Otomotiv üretiminde düşüş: sac ↓
  - Faiz indirimi beklentisi: tümü ↑
- Haber, etkisinden önce görünür. Haberi okuyup hemen stoklayan oyuncu kazanır. Bu, oyunun beceri katmanıdır.

## 7. Siparişler ve müşteriler

Her gün yeni teklifler gelir. Teklif sayısı seviyeye, itibara ve satış elemanı sayısına bağlıdır. Bir teklifin özellikleri:

| Özellik | Etki |
|---|---|
| Ürün ve tonaj | Seviye arttıkça tonaj büyür (0,5 t'dan 150 t'ya kadar) |
| Fiyat | Piyasanın %4–11,5 üstü, itibar ve satış elemanı fiyatı artırır |
| Ödeme | Peşin, 30 gün vade (+%3) veya 60 gün vade (+%6). Vadeli satış Esnaf seviyesinde açılır |
| Müşteri riski | A (sorunsuz), B (%4 karşılıksız çek riski), C (%12 risk, +%2 fiyat) |
| Ölçüye kesim | %22 olasılık, +%7 fiyat. Makine yoksa fason kesim maliyeti %5 |
| Teslim süresi | 2–6 gün. Geç kalınırsa sipariş iptal olur ve itibar −6 düşer |
| Teklif ömrü | 2 gün. Kabul edilmezse düşer |

Teslim yolları:

- **Stoktan teslim:** Mal depodan çıkar. Kendi araç kapasiten bedavadır, aşan tonaj nakliyeciyle 600 ₺/t'a gider.
- **Aracılık:** Fabrika maliyeti piyasa × 1,035 + 500 ₺/t nakliyedir. Peşin siparişte sermaye gerekmez. Vadeli siparişte fabrikaya peşin ödeyip müşteriden vadede tahsil edersin. Fabrika aracıya **günde en fazla 3 sevkiyat** yapar ve **sevkiyat başına en fazla 8 t** verir. Bu yüzden büyümek için depo ve stok şarttır.

## 8. Nakit akışı ve risk

- **Alacaklar:** Vadeli satışlar alacak defterine yazılır. Vade günü tahsil edilir veya karşılıksız çıkar.
- **Faktoring:** Bir alacağı bankaya kırdırıp parayı hemen alırsın. Kesinti aylık %3 (muhasebeciyle %1,5). Risk bankaya geçer.
- **Banka kredisi:** Limit = seviye tabanı + net değerin %30'u. Aylık faiz %3,5 ay sonunda kesilir.
- **Eksi bakiye:** Ay sonu giderleri kasayı eksiye düşürebilir. Eksi bakiyeye aylık %5 faiz işler. Bakiye, kredi limitinin %25'i + 10.000 ₺'nin altına düşerse **iflas** olur.
- **Ay sonu giderleri (her 30 günde bir):** depo kirası, araç giderleri, personel maaşı, kredi faizi.

## 9. İlerleme

### Unvanlar (satılan tonaj **ve** net değer şartı; kazanılan unvan kaybedilmez)

| Unvan | Ton | Net değer | Açılanlar | Kredi tabanı | Sipariş tonajı |
|---|---|---|---|---|---|
| Komisyoncu | 0 | — | Aracılık, temel 6 ürün | 0 | 0,5–2,5 t |
| Esnaf | 25 | 20.000 ₺ | Vadeli satış, kredi, baklavalı sac, NPU | 50.000 ₺ | 1–6 t |
| Toptancı | 150 | 250.000 ₺ | Galvaniz sac, doğalgaz borusu | 250.000 ₺ | 3–18 t |
| Bayi | 800 | 1,5 Mn ₺ | HEA, galvaniz boru | 1.000.000 ₺ | 6–40 t |
| Distribütör | 3.000 | 8 Mn ₺ | Paslanmaz sac | 4.000.000 ₺ | 12–80 t |
| **Demir Baronu** | 12.000 | 40 Mn ₺ | Oyunun hedefi, sonra serbest oyun | 15.000.000 ₺ | 20–150 t |

### Yatırımlar

| Tür | Kademe | Bedel | Aylık gider | Etki |
|---|---|---|---|---|
| Depo | Kiralık depo köşesi | 15.000 ₺ | 2.500 ₺ | 12 t |
| Depo | Küçük depo | 90.000 ₺ | 8.000 ₺ | 40 t |
| Depo | Depo + açık saha | 350.000 ₺ | 20.000 ₺ | 120 t |
| Depo | Lojistik depo | 1.400.000 ₺ | 55.000 ₺ | 400 t |
| Depo | Liman deposu | 6.000.000 ₺ | 160.000 ₺ | 1.500 t |
| Araç | Kamyonet | 180.000 ₺ | 9.000 ₺ | 3 t/gün |
| Araç | Kamyon | 650.000 ₺ | 26.000 ₺ | 12 t/gün |
| Araç | TIR | 1.600.000 ₺ | 52.000 ₺ | 25 t/gün |
| Makine | Giyotin makas | 120.000 ₺ | — | Sac kesim primi |
| Makine | Şerit testere | 80.000 ₺ | — | Boru/profil kesim primi |
| Personel | Satış elemanı (en fazla 8) | — | 35.000 ₺ | +1 teklif, +%0,4 fiyat |
| Personel | Muhasebeci (1) | — | 40.000 ₺ | Karşılıksız çek riski ½, faktoring ½ |

## 10. Prototipte olmayanlar (sonraki sürümler)

1. **Şehirler ve şubeler:** İstanbul, Ankara, İzmir, Bursa, Kocaeli, Konya, Gaziantep, Kayseri. Şehirler arası fiyat farkı ile arbitraj yapılır ve şube açılır.
2. **Rakip tüccarlar (yapay zekâ):** Aynı teklife rakipler de girer. Fiyat kırma ve müşteri sadakati devreye girer.
3. **İhaleler:** Büyük projeler (köprü, stadyum, OSB). Kapalı zarfla teklif verilir, teminat mektubu gerekir.
4. **İthalat:** Gemi ile Çin/Rusya/Ukrayna'dan bobin getirilir. 20–40 gün yolda kalır, kur riski taşır.
5. **Bobin dilme / boyuna kesme hattı:** Bobin alıp sac ve şerit üretilir (dikey büyüme).
6. **Müşteri ilişkileri:** Kalıcı müşteriler, sadakat, bayram hediyesi, tahsilat görüşmesi mini oyunu.
7. **Haftalık lig (Game Center):** Herkes aynı rastgele tohumla 60 gün oynar, en yüksek net değer kazanır.

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

1. **Açılış:** şirket adı, kısa hikâye ("Cebinde 5.000 ₺ ve bir telefon…")
2. **Piyasa:** kategori filtresi, ürün satırı (fiyat, günlük değişim, 30 günlük çizgi grafik, stok), al/sat paneli
3. **Siparişler:** aktif siparişler + yeni teklifler, her teklifte aracılık kârı ve stoktan kâr önizlemesi
4. **Depo:** doluluk çubuğu, ortalama maliyet, gerçekleşmemiş kâr/zarar, depo büyütme
5. **Şirket:** unvan ilerlemesi, filo, makineler, personel, banka, alacaklar, istatistik
6. **Üst bar:** kasa, gün/ay, unvan, itibar, haber bandı. Alt kısımda büyük **Günü Bitir** düğmesi

## 13. Teknik plan

- **Prototip (bitti):** tek dosyalık HTML/JS, `DemirBaronu/web/play.html`. Tüm kurallar burada çalışıyor, kayıt tarayıcıda tutuluyor.
- **Üretim sürümü:** Factory King ile aynı altyapı. Kurallar Unity'den bağımsız C# çekirdeğinde (`DemirBaronu.Core`, netstandard2.1) ve NUnit testleriyle yazılır. Arayüz UI Toolkit ile kurulur. Mevcut GitHub Actions → TestFlight hattı (`.github/workflows/ios-testflight.yml`) yeniden kullanılır.
- **Kayıt:** sürümlü JSON, sağlama toplamı, iCloud. Hesap sistemi yok.
- **Rastgelelik:** tohumlu RNG. Haftalık lig ve tekrar oynatma için gerekli.
- **Denge testi:** otomatik "akıllı tüccar" ve "rastgele tüccar" botları oynar. Prototipte ölçülen değerler (6 deneme): haberleri kullanmayan sade akıllı bot **277–377. günde** baron oldu ve hiç iflas etmedi (Esnaf ~12. gün, Toptancı ~46. gün, Bayi ~110. gün). Rastgele aracılık yapan bot 200 günde Toptancı'da kaldı. Haberleri okuyup stoklayan gerçek oyuncunun daha hızlı ilerlemesi beklenir.

## 14. Yol haritası

| Aşama | İçerik | Süre (tahmini) |
|---|---|---|
| 0 | Tasarım belgesi + oynanabilir web prototipi | ✅ bitti |
| 1 | C# çekirdek: piyasa, sipariş, depo, banka, kayıt + testler | 2 hafta |
| 2 | Unity arayüzü (5 ekran), ses, haptik | 2 hafta |
| 3 | Şehirler, rakipler, ihaleler | 3 hafta |
| 4 | IAP, ödüllü reklam, Game Center ligi | 1 hafta |
| 5 | TestFlight beta, denge ayarı, App Store metinleri ve görselleri | 2 hafta |

## 15. Başarı ölçütleri

- 1. gün tutma ≥ %40, 7. gün ≥ %15
- İlk oturumda ilk depoyu kiralayan oyuncu oranı ≥ %70 (eğitim başarısı)
- Ortalama oturum 8–12 dakika (≈ 15–25 oyun günü)
