// Müşteri tipleri. products: istedikleri ürünler. qty: piyasa günlük tüketiminin oranı olarak sipariş büyüklüğü [min,max].
// markup: piyasa fiyatına göre teklif farkı. minQ: istenen en düşük kalite. kinds: verdikleri sözleşme türleri.
export const CUSTOMER_TYPES = [
  { id: "kucuk", name: "Küçük işletme", products: ["mobilya", "kumas", "hazir_gida", "kagit", "un", "cam", "islenmis_ahsap"], qty: [0.004, 0.012], markup: [0.05, 0.12], minQ: [40, 55], kinds: ["tek", "tek", "haftalik"], level: 1 },
  { id: "orta", name: "Orta ölçekli işletme", products: ["celik", "plastik_granul", "kimyasal", "islenmis_ahsap", "kagit", "cam", "kumas", "un"], qty: [0.01, 0.03], markup: [0.03, 0.09], minQ: [45, 60], kinds: ["tek", "haftalik", "aylik"], level: 1 },
  { id: "buyuk", name: "Büyük fabrika", products: ["celik", "aluminyum", "islenmis_bakir", "plastik_granul", "kimyasal", "ham_demir", "kaucuk", "cam"], qty: [0.03, 0.08], markup: [0.01, 0.06], minQ: [55, 70], kinds: ["tek", "aylik", "uzun"], level: 2 },
  { id: "insaat", name: "İnşaat şirketi", products: ["cimento", "insaat_malzemesi", "celik", "cam", "agrega"], qty: [0.02, 0.06], markup: [0.02, 0.08], minQ: [45, 60], kinds: ["tek", "haftalik", "aylik"], level: 1 },
  { id: "otomotiv", name: "Otomotiv firması", products: ["celik", "aluminyum", "kaucuk", "cam", "elektronik", "batarya", "plastik_granul"], qty: [0.02, 0.05], markup: [0.03, 0.08], minQ: [62, 75], kinds: ["aylik", "uzun"], level: 3 },
  { id: "teknoloji", name: "Teknoloji şirketi", products: ["elektronik", "batarya", "silisyum", "islenmis_bakir", "lityum"], qty: [0.02, 0.05], markup: [0.04, 0.1], minQ: [65, 80], kinds: ["tek", "aylik", "uzun"], level: 4 },
  { id: "devlet", name: "Devlet kurumu", products: ["insaat_malzemesi", "cimento", "makine", "yakit", "otomobil", "celik"], qty: [0.05, 0.12], markup: [0.0, 0.05], minQ: [55, 65], kinds: ["ihale"], level: 2 },
  { id: "uluslararasi", name: "Uluslararası şirket", products: ["otomobil", "makine", "celik", "aluminyum", "kumas", "hazir_gida", "batarya"], qty: [0.03, 0.08], markup: [0.08, 0.15], minQ: [65, 80], kinds: ["tek", "aylik", "uzun"], level: 5, export: true },
];
export const CT = Object.fromEntries(CUSTOMER_TYPES.map(c => [c.id, c]));
export const CONTRACT_KINDS = {
  tek: { name: "Tek seferlik sipariş", deliveries: 1, every: 0, days: [3, 6] },
  haftalik: { name: "Haftalık sipariş", deliveries: 4, every: 7, days: [4, 6] },
  aylik: { name: "Aylık tedarik", deliveries: 3, every: 30, days: [8, 12] },
  uzun: { name: "Uzun vadeli anlaşma", deliveries: 6, every: 30, days: [10, 14] },
  ihale: { name: "Büyük ölçekli ihale", deliveries: 1, every: 0, days: [8, 14] },
};
const N = s => s.split(",").map(x => x.trim());
export const CUSTOMER_NAMES = {
  kucuk: N("Yıldız Mobilya Atölyesi, Güneş Ev Tekstili, Lezzet Lokantaları, Kırtasiye Dünyası, Ahşap Sanat, Mavi Fırın, Usta Cam Çerçeve, Dostlar Market"),
  orta: N("Demirtaş Makine, Özgür Ambalaj, Kaya Boya, Atlas Kablo, Beyaz Kâğıtçılık, Pınar Tekstil, Ekin Değirmen, Erdem Plastik"),
  buyuk: N("Anadolu Döküm, Birleşik Metal, Kuzey Plastik Sanayi, Boğaziçi Kimya, Atak Alüminyum Profil, Zirve Lastik, Sistem Cam"),
  insaat: N("Temel İnşaat, Kule Yapı, Köprü Altyapı, Mega Konut, Hızlı Prefabrik, Çınar Yapı"),
  otomotiv: N("Ankara Otomotiv Yan Sanayi, Ege Oto Parça, Bursa Karoser, Volt Elektrikli Araç"),
  teknoloji: N("Nova Teknoloji, Çip Vadisi Elektronik, Kuantum Batarya, Piksel Donanım"),
  devlet: N("Karayolları Bölge Müdürlüğü, Toplu Konut İdaresi, Belediye Altyapı Daire Başkanlığı, Devlet Demiryolları, Enerji Bakanlığı Projeleri"),
  uluslararasi: N("EuroAuto GmbH, Orient Machinery Ltd, Atlantic Metals Inc, Nordic Batteries AB, Gulf Foods Co"),
};
