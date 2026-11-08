# Geliştirme Aşamaları — Durum

Bu belge her aşamada **gerçekten** neyin yapıldığını, neyin doğrulandığını ve neyin eksik olduğunu kaydeder. Tamamlanmamış hiçbir özellik tamamlanmış gibi gösterilmez.

Durum işaretleri:
- ✅ yapıldı ve otomatik testle doğrulandı
- 🟢 yazıldı ve UnityEngine referanslarına karşı derlendi, Unity'de henüz çalıştırılmadı
- 🟡 yazıldı ama bu ortamda derlenemiyor (UnityEditor, URP, Unity IAP veya Xcode gerektirir)
- ⬜ yapılmadı / sizin hesabınızı ya da cihazınızı gerektiriyor

## Ortam

- Geliştirme ortamı bir Linux bulut konteyneri. **Unity Editor, macOS, Xcode ve Apple hesabı yok.**
- Oyun mantığı Unity'den bağımsız C# çekirdeğinde (netstandard2.1, C# 9). .NET 8 ile derlenip test ediliyor.
- Unity runtime kodu `UnityEngine.Modules 2021.3.33` referanslarına karşı derleniyor; bu API Unity 6'nın bir alt kümesi.
- Editor betikleri, IAP adaptörü, iOS native eklentisi ve PlayMode testleri burada derlenemiyor.
- Kontrol komutu: `FactoryKing/Tools/run-checks.sh`. Son sonuç: **184/184 oyun testi ve 5/5 sunucu testi geçti, derleme uyarısı yok.**

## Aşama 1 — Proje, sahne, kamera
| İş | Durum |
|---|---|
| Unity 6 proje iskeleti, assembly definition'lar, paket manifesti | 🟡 |
| Tek tıkla kurulum: URP, iOS ayarları, sahne, ikon, UI panel ayarları, shader'ların derlemeye dahil edilmesi | 🟡 |
| İzometrik dokunmatik kamera, prosedürel 3D fabrika (15 makine tipi), bantlar, forklift, kamyon, işçiler, gündüz/gece | 🟢 |
| Optimizasyon: paylaşılan mesh, SRP Batcher/instancing, LOD, nesne havuzu, tek Update döngüsü | 🟢 |

## Aşama 2 — Üretim motoru ve ekonomi
| İş | Durum |
|---|---|
| BigDouble, TR/EN sayı biçimi, 5 fabrika, 30 makine, gerçek tamponlu deterministik simülasyon | ✅ |
| Seviye 1–100, giderler, net kâr, şirket değeri, kilitlenmeyen borç modeli, darboğaz analizi | ✅ |

## Aşama 3 — Idle ve çevrimdışı kazanç, kayıt
| İş | Durum |
|---|---|
| Çevrimdışı kazanç (kapalı form, 8 sa sınır + araştırma/VIP, ×2 alma) | ✅ |
| Saat hilesi koruması (monotonik güvenilir zaman, geri alma tespiti, günlük ödül koruması) | ✅ |
| JSON kayıt: sürümleme, sağlama toplamı, yedek dosya, bozuk kayıt kurtarma, v1→v2 geçişi | ✅ |
| Dosya kaydı, otomatik kayıt (20 sn, arka plana geçiş, çıkış) | 🟢 |

## Aşama 4 — Yöneticiler, görevler, prestij, araştırma
| İş | Durum |
|---|---|
| 5 yönetici (4 nadirlik, seviye, fabrikaya atama, 5 farklı yetenek) | ✅ |
| Günlük ve haftalık görevler, siparişler, 12 başarım, 7 günlük giriş takvimi, hafta sonu etkinliği, hızlandırma | ✅ |
| Prestij ("Taç"): önizleme, kazanılan/korunan/sıfırlanan listesi, rozet ve dekorasyon, başlangıç bonusları | ✅ |
| Araştırma ağacı (6 dal, kalıcı) | ✅ |
| Denge koruması: "ideal oyuncu" botu testi | ✅ |
| 20 fabrika (120 makine), 30 günlük sezonlar, kalıcı ödüller ve miras yıldızları, Sezon Bileti, 27 dekorasyon (zemin/duvar/dış alan/gökyüzü); sezon temposu testi | ✅ çekirdek / 🟢 UI ve 3D |
| Benzer oyunlardan uyarlanan tüm öneriler: kargo dronu, haftalık etkinlik fabrikası (3 dönüşümlü) ve jeton ödülleri, Game Center etkinlik tablosu, 20 yönetici + kartlar + oranları gösterilen sandıklar, keşif seferleri, tedarik zinciri, araç filosu, kalıcı mühendislik ağacı, rozet bonusu, 50 kademeli sezon yolu (ücretsiz + bilet), otomatik yükseltme, tümünü topla, istatistikler, erişilebilirlik (yazı boyutu, renk körlüğü, hareketi azalt), kumbara, başlangıç teklifi, haftalık VIP aboneliği | ✅ çekirdek / 🟢 UI |
| Çevrimiçi (isteğe bağlı): referans sunucu (zaman, uzaktan etkinlik takvimi, co-op kontratları, kulüpler, bildirme, veri silme) + oyun istemcisi | ✅ sunucu testleri / 🟢 istemci / ⬜ barındırma ve moderasyon sizden |
| Telefon odaklı giriş: açılış → ilk kurulum (dil + şirket adı) → canlı 3D arka planlı ana menü; duraklatma menüsü, "Nasıl oynanır", tüm fabrikayı gösterme düğmesi | 🟢 |
| Telefon özellikleri: titreşim (haptik), pil tasarrufu ve iOS Düşük Güç Modu'nda 30 FPS, dokunulmayınca 30 FPS, uygun anda App Store puanlama isteği, iPad tam ekran, durum çubuğu gizli, alt kenar hareketi koruması | 🟢 / 🟡 native |
| Hesap sistemi yok (bilinçli karar): ilerleme cihaz + iCloud + Game Center'da. Böylece Apple'ın hesap silme ve "Sign in with Apple" zorunlulukları gerekmiyor | ✅ |
| Benzer oyunlardan uyarlanan yeni mekanikler: santral/elektrik şebekesi, yaşayan piyasa, yönetici yetenekleri, artırılmış zorluk eğrisi | ✅ çekirdek / 🟢 UI ve 3D santral |

## Aşama 5 — Arayüz, ses, animasyon
| İş | Durum |
|---|---|
| UI Toolkit arayüzü (kodla): HUD, makine detayı, eğitim, 13 sayfa, açılış ekranı, ana menü, çevrimdışı özeti, onay penceresi, prestij onayı, bildirim baloncukları, güvenli alan | 🟢 |
| 16 ekranın karşılığı: açılış, menü, fabrika, makine detayı, yöneticiler, depo, siparişler, araştırma, harita, görevler, günlük ödüller, başarımlar ve koleksiyon, mağaza, etkinlikler, ayarlar, çevrimdışı özeti | 🟢 |
| TR/EN metinler (~345 satır); kapsam testi koddaki her anahtarı iki dilde doğruluyor | ✅ |
| Prosedürel ses: 8 efekt, çalışan makine sayısına göre değişen fabrika ambiyansı, üretken müzik; ses ayarları | 🟢 |
| Koleksiyon dekorasyonlarının 3D sahnede görünmesi, yükseltmede parıltı ve büyüme efekti | 🟢 |

## Aşama 6 — Reklam, satın alma, kayıt, Game Center
| İş | Durum |
|---|---|
| Satın alma kuralları: her işlem bir kez, sahip olunan tekrar satılmaz, geri yükleme yalnızca kalıcı ürünler, geçersiz makbuz reddedilir | ✅ |
| Mağaza katmanı: test mağazası (iptal ve red simülasyonu), grant → kaydet → onayla akışı | 🟢 |
| Unity IAP 4.x adaptörü (paket kurulunca otomatik derlenir), makbuz doğrulama kancası | 🟡 |
| Ödüllü reklam kuralları: yalnızca isteğe bağlı, izlenince ödül, günlük sınır, bekleme süresi, "reklamsız" ürünü | ✅ |
| Gerçek reklam SDK'sı | ⬜ ağ seçimi ve kimlikler sizden |
| iCloud anahtar-değer eşitleme (native .mm), çakışma çözümü (satın alımlar asla kaybolmaz) | ✅ çözücü / 🟡 native |
| Game Center (giriş, başarımlar, 2 liderlik tablosu), isteğe bağlı yerel bildirimler | 🟢 / 🟡 native |
| Xcode yetenekleri (iCloud, Game Center, IAP), Info.plist, gizlilik manifesti | 🟡 |
| Onaya bağlı analitik (onay yoksa hiçbir şey gönderilmez; ATT gerekmez) | ✅ |

## Aşama 7 — Performans ve test
| İş | Durum |
|---|---|
| Azami yük (5 fabrika × 3 hat × seviye 100): masaüstünde tik başına **0,23 ms** (sınır 1,5 ms) | ✅ |
| Kayıt+yükleme 2 ms, 8,5 KB; çevrimdışı hesap anında | ✅ |
| Test grupları: ekonomi, üretim, yükseltme, çevrimdışı, kayıt, saat değişikliği, satın alma, reklam, prestij, dil, performans, denge | ✅ (127 test) |
| PlayMode duman testleri (oyun açılır, üretir, satar, hat açar, kare süresi) | 🟡 Unity Test Runner'da çalıştırılmalı |
| Cihazda kare hızı, pil ve ısınma ölçümü; çözünürlük ve güvenli alan testleri | ⬜ cihaz gerekli |

## Aşama 8 — App Store materyalleri ve TestFlight
| İş | Durum |
|---|---|
| 1024×1024 özgün ikon (alfa kanalı yok); kurulumda otomatik atanıyor | ✅ |
| TR/EN mağaza metinleri, anahtar kelimeler, kategori | ✅ `Docs/APPSTORE/Metadata.md` |
| Gizlilik politikası ve destek sayfası (TR/EN, KVKK) | ✅ şablon `Docs/web/`, şirket bilgileriniz eklenmeli |
| App Privacy cevapları, yaş derecelendirmesi (4+), IAP, Game Center ve reklam yapılandırması | ✅ `Docs/APPSTORE/` |
| TestFlight adımları ve cihaz test listesi, App Review notu | ✅ `Docs/APPSTORE/TestFlight.md` |
| Ekran görüntüleri | ⬜ oyun Unity'de çalışınca yakalanacak (Editor menüsünde araç var) |
| Bundle ID, imzalama, arşivleme, yükleme | ⬜ Apple hesabı gerekli |

## Nihai kabul kriterleri
| Kriter | Durum |
|---|---|
| Gerçek 3D fabrika sahnesi çalışmalı | 🟢 kod hazır ve derleniyor; Unity'de görülmedi |
| Üretim gerçek hesaplamalarla işlemeli | ✅ |
| Para kazanma, makine yükseltme | ✅ |
| Çevrimdışı kazanç | ✅ |
| Beş fabrika oynanabilir | ✅ mantık / 🟢 görsel |
| Yönetici sistemi | ✅ |
| Kayıt sistemi | ✅ çekirdek / 🟢 dosya, 🟡 iCloud |
| Türkçe ve İngilizce | ✅ |
| Reklam ve satın alma test edilebilir | ✅ birim testleri + test mağazası/reklam; gerçek SDK ⬜ |
| iOS derlemesi alınabilmeli | 🟡 betik ve Xcode ayarları hazır; Mac + Unity + Xcode'da denenmeli |
| Kritik hata yok | Otomatik testler temiz. Unity'de ilk açılışta derleme veya görsel düzeltme gerekebilir |

## Bilinen riskler ve ilk Unity açılışında beklenenler
1. **Editor/IAP/native kod burada derlenmedi.** İlk açılışta küçük API düzeltmeleri gerekebilir (URP 17, Unity IAP 4.x, Xcode API'leri).
2. **Görsel ayar:** kamera açısı, renkler, ışık ve UI boyutları gerçek ekranda ince ayar isteyecek.
3. **UI temasının yazı tipi:** UI Toolkit'e yerleşik `LegacyRuntime.ttf` atanıyor. Türkçe karakterler iOS sistem yazı tipine düşerek gösterilir; cihazda kontrol edilmeli.
4. **Premium grafik:** modeller kodla üretiliyor. Mağazada rekabetçi bir görünüm için 3D sanatçı modelleri önerilir.
