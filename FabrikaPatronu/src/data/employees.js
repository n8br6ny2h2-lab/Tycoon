// Pozisyonlar. salary: aylık ₺. per: kişi başı etki. cap: toplam etki üst sınırı. Eğitim seviyesi etkiyi seviye başına %10 artırır.
export const POSITIONS = [
  { id: "isci", name: "İşçi", salary: 28000, desc: "Fabrika ve madenlerde makineleri çalıştırır. Yetersizse üretim yavaşlar.", effect: "workers" },
  { id: "muhendis", name: "Mühendis", salary: 75000, per: 0.6, cap: 12, desc: "Ürün kalitesini artırır (+0,6 kişi başı) ve araştırmayı %5 hızlandırır.", effect: "quality" },
  { id: "teknisyen", name: "Teknisyen", salary: 45000, per: 0.08, cap: 0.7, desc: "Makine aşınmasını kişi başı %8 azaltır (en fazla %70).", effect: "wear" },
  { id: "depo", name: "Depo sorumlusu", salary: 40000, per: 0.05, cap: 0.4, desc: "Depo kapasitesini kişi başı %5 artırır (en fazla %40), hırsızlığı azaltır.", effect: "storage" },
  { id: "lojistik", name: "Lojistik yöneticisi", salary: 60000, per: 0.04, cap: 0.3, desc: "Nakliye giderini kişi başı %4 düşürür (en fazla %30).", effect: "transport" },
  { id: "satis", name: "Satış uzmanı", salary: 55000, per: 0.012, cap: 0.08, desc: "Günde +1 müşteri teklifi ve kişi başı %1,2 daha iyi sözleşme fiyatı.", effect: "sales" },
  { id: "finans", name: "Finans yöneticisi", salary: 80000, per: 0.0015, cap: 0.012, desc: "Kredi faizini kişi başı 0,15 puan düşürür, vergi danışmanlığıyla vergiyi %2 azaltır.", effect: "finance" },
  { id: "mudur", name: "Fabrika müdürü", salary: 90000, per: 0.04, cap: 0.25, desc: "Bütün üretim hızını kişi başı %4 artırır (en fazla %25).", effect: "speed" },
];
export const POS = Object.fromEntries(POSITIONS.map(p => [p.id, p]));
export const TRAINING = { maxLevel: 5, costMonths: 1.5, bonusPerLevel: 0.1, days: 3 };
