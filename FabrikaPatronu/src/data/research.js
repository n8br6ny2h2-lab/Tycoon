// Teknoloji ağacı. cost: ₺, days: oyun günü (mühendis ve bölgeyle hızlanır). req: önce bitmesi gerekenler.
export const RESEARCH = [
  { id: "otomasyon", name: "Otomatik üretim", cost: 6000000, days: 4, req: [], desc: "Otomasyon seviyesi 3'e kadar açılır (işçi ihtiyacı −%20/seviye)." },
  { id: "kalite_kontrol", name: "Üretim kalite kontrolü", cost: 9000000, days: 5, req: [], desc: "Bütün ürünlerde kalite +6, fire −%15." },
  { id: "enerji_tasarrufu", name: "Enerji tasarrufu", cost: 10000000, days: 6, req: [], desc: "Fabrikaların elektrik tüketimi −%15." },
  { id: "lojistik_opt", name: "Lojistik optimizasyonu", cost: 12000000, days: 6, req: [], desc: "Nakliye gideri −%15, otomatik rotalar günde iki kez çalışır." },
  { id: "derin_maden", name: "Derin madencilik", cost: 16000000, days: 8, req: ["otomasyon"], desc: "Maden ve kuyu çıktısı +%20." },
  { id: "alasim", name: "Yeni alaşımlar", cost: 15000000, days: 7, req: ["kalite_kontrol"], desc: "Çelik ve alüminyumda kalite +10, metal satış fiyatı +%3." },
  { id: "elektronik", name: "Elektronik üretimi", cost: 20000000, days: 8, req: ["kalite_kontrol"], desc: "Elektronik fabrikası açılır." },
  { id: "robotik", name: "Robotik makineler", cost: 25000000, days: 10, req: ["otomasyon"], desc: "Bütün üretim hızı +%10." },
  { id: "yenilenebilir", name: "Yenilenebilir enerji", cost: 18000000, days: 8, req: ["enerji_tasarrufu"], desc: "Santralde girdisiz güneş enerjisi reçetesi." },
  { id: "batarya", name: "Gelişmiş batarya", cost: 35000000, days: 12, req: ["elektronik", "alasim"], desc: "Batarya fabrikası açılır." },
  { id: "makine", name: "Ağır makine imalatı", cost: 30000000, days: 10, req: ["robotik", "elektronik"], desc: "Makine fabrikası açılır." },
  { id: "kargo_ucak", name: "Kargo uçakları", cost: 40000000, days: 10, req: ["lojistik_opt"], desc: "Havalimanı olan bölgeler arasında kargo uçağı." },
  { id: "yz_kontrol", name: "Yapay zekâ üretim kontrolü", cost: 60000000, days: 14, req: ["robotik", "elektronik"], desc: "Fire −%30, bakım ihtiyacı −%25, hız +%5." },
  { id: "otomotiv", name: "Otomotiv üretimi", cost: 80000000, days: 15, req: ["makine", "batarya"], desc: "Otomotiv fabrikası açılır." },
];
export const RS = Object.fromEntries(RESEARCH.map(r => [r.id, r]));
