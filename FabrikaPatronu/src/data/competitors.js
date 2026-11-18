// Yapay zekâ rakipler. strategy: ucuz (düşük fiyat, hacim), kalite (yüksek kalite, prim), hacim (alım-satım tüccarı), uzman (tek zincir).
// focus: ürettiği ürünler. size: günlük toplam üretim kapasitesi (ton eşdeğeri değer olarak). capital: başlangıç sermayesi.
export const COMPETITORS = [
  { id: "kartal", name: "Kartal Demir Çelik", strategy: "hacim", focus: ["ham_demir", "celik"], capital: 900000000, color: "#e05a47" },
  { id: "ege", name: "Ege Kimya ve Plastik", strategy: "ucuz", focus: ["plastik_granul", "kimyasal", "kaucuk"], capital: 500000000, color: "#47bdf5" },
  { id: "toros", name: "Toros Alüminyum", strategy: "kalite", focus: ["aluminyum", "islenmis_bakir"], capital: 650000000, color: "#cfd6dd" },
  { id: "marmara", name: "Marmara Otomotiv", strategy: "kalite", focus: ["otomobil", "makine"], capital: 1500000000, color: "#ffb02e" },
  { id: "akdeniz", name: "Akdeniz Gıda", strategy: "ucuz", focus: ["un", "hazir_gida", "bitkisel_yag"], capital: 300000000, color: "#e8c444" },
  { id: "karadeniz", name: "Karadeniz Orman Ürünleri", strategy: "uzman", focus: ["kereste", "islenmis_ahsap", "mobilya", "kagit"], capital: 250000000, color: "#a36f3d" },
  { id: "anka", name: "Anka Elektronik", strategy: "kalite", focus: ["elektronik", "batarya"], capital: 1100000000, color: "#2fa36b" },
  { id: "dicle", name: "Dicle Enerji", strategy: "hacim", focus: ["petrol", "dogalgaz", "yakit", "elektrik"], capital: 1300000000, color: "#8a5cd6" },
  { id: "yapi", name: "Yapı Malzemeleri Holding", strategy: "ucuz", focus: ["cimento", "insaat_malzemesi", "cam"], capital: 400000000, color: "#b9b5ab" },
  { id: "global", name: "Global Emtia Ticaret", strategy: "hacim", focus: ["bakir_cevheri", "nikel", "lityum", "altin", "gumus", "cinko"], capital: 2000000000, color: "#3f7fa8" },
];
