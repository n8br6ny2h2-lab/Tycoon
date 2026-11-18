// Hammaddeler ve ürünler. base: ₺/birim taban fiyat. prod/cons: dünyanın günlük üretim/tüketimi (birim).
// storage: hangi depo sınıfında durduğu. vol: bir birimin depoda kapladığı hacim. shape/color: ikon çizimi.
// Piyasa derinliği: dünya hacmi çarpanı (oyuncunun tek başına fiyatı ne kadar ittiğini belirler).
const DEPTH = 4;
const R = (id, name, cat, unit, vol, storage, base, prod, shape, color, extra = {}) =>
  ({ id, name, cat, unit, vol, storage, base, min: Math.round(base * 0.45), max: Math.round(base * 2.4), prod: prod * DEPTH, cons: prod * DEPTH, elastic: 0.55, quality: 60, shape, color, ...extra });

export const CATEGORIES = {
  metal: "Metaller", enerji: "Enerji", endustriyel: "Endüstriyel", orman: "Orman ve tarım", ara: "Ara ürünler", mamul: "Mamul ürünler",
};

export const RESOURCES = [
  // METALLER
  R("demir_cevheri", "Demir cevheri", "metal", "t", 1, "dokme", 3500, 6000, "ore", "#8b5a3c"),
  R("bakir_cevheri", "Bakır cevheri", "metal", "t", 1, "dokme", 62000, 1200, "ore", "#c0733a"),
  R("boksit", "Boksit", "metal", "t", 1, "dokme", 2600, 5000, "ore", "#c9785a"),
  R("altin", "Altın", "metal", "kg", 0.001, "metal", 2800000, 25, "bar", "#f0c53f", { elastic: 0.35 }),
  R("gumus", "Gümüş", "metal", "kg", 0.002, "metal", 32000, 400, "bar", "#cfd6dd", { elastic: 0.4 }),
  R("nikel", "Nikel", "metal", "t", 1, "metal", 560000, 60, "nugget", "#9aa79f"),
  R("cinko", "Çinko", "metal", "t", 1, "metal", 95000, 160, "nugget", "#aab6c0"),
  R("krom", "Krom cevheri", "metal", "t", 1, "dokme", 9000, 700, "ore", "#5e6d7c"),
  R("lityum", "Lityum", "metal", "t", 1, "kimya", 420000, 45, "powder", "#e7edf6"),
  // ENERJİ
  R("komur", "Kömür", "enerji", "t", 1, "dokme", 4200, 9000, "ore", "#2a2a2c"),
  R("petrol", "Ham petrol", "enerji", "t", 1, "tank", 19000, 6000, "barrel", "#24211e"),
  R("dogalgaz", "Doğalgaz", "enerji", "bin m³", 1, "tank", 11000, 4000, "flame", "#6fb7ff"),
  R("elektrik", "Elektrik", "enerji", "MWh", 0, "enerji", 2600, 60000, "bolt", "#ffd23c", { elastic: 0.7 }),
  R("hidrojen", "Hidrojen", "enerji", "t", 1, "tank", 140000, 35, "h2", "#9fe3ff"),
  R("yakit", "Rafine yakıt", "enerji", "t", 1, "tank", 28000, 3500, "jerrycan", "#d0472f"),
  // ENDÜSTRİYEL
  R("plastik_granul", "Plastik granül", "endustriyel", "t", 1, "kimya", 42000, 1500, "pellets", "#47bdf5"),
  R("kaucuk", "Kauçuk", "endustriyel", "t", 1, "kimya", 58000, 450, "tire", "#3a3a3e"),
  R("silisyum", "Silisyum", "endustriyel", "t", 1, "genel", 90000, 160, "wafer", "#7d8bab"),
  R("cam_kumu", "Cam hammaddesi (kum)", "endustriyel", "t", 1, "dokme", 1400, 3500, "sand", "#e6d29a"),
  R("kimyasal", "Kimyasal madde", "endustriyel", "t", 1, "kimya", 28000, 1400, "flask", "#7fdc63"),
  R("kirectasi", "Kireçtaşı", "endustriyel", "t", 1, "dokme", 600, 10000, "stone", "#cfc9b6"),
  R("agrega", "Agrega (mıcır)", "endustriyel", "t", 1, "dokme", 450, 14000, "gravel", "#9b968a"),
  // ORMAN VE TARIM
  R("kereste", "Kereste", "orman", "t", 1, "genel", 8500, 3500, "logs", "#a36f3d"),
  R("pamuk", "Pamuk", "orman", "t", 1, "genel", 58000, 500, "cotton", "#f3efe4"),
  R("bugday", "Buğday", "orman", "t", 1, "dokme", 9500, 7000, "wheat", "#e2b458"),
  R("bitkisel_yag", "Bitkisel yağ", "orman", "t", 1, "tank", 42000, 900, "bottle", "#e8c444"),
  // ARA ÜRÜNLER
  R("ham_demir", "Ham demir", "ara", "t", 1, "metal", 12500, 3500, "pig", "#6f6a66"),
  R("celik", "Çelik", "ara", "t", 1, "metal", 21000, 3200, "coil", "#9fb3c6"),
  R("islenmis_bakir", "İşlenmiş bakır", "ara", "t", 1, "metal", 300000, 280, "wire", "#d9814a"),
  R("aluminyum", "Alüminyum külçe", "ara", "t", 1, "metal", 82000, 1100, "ingot", "#cdd6de"),
  R("islenmis_ahsap", "İşlenmiş ahşap", "ara", "t", 1, "genel", 15500, 1800, "plank", "#c88d53"),
  R("cam", "Düz cam", "ara", "t", 1, "genel", 13500, 1500, "glass", "#9fd8e8"),
  R("cimento", "Çimento", "ara", "t", 1, "dokme", 2500, 9000, "bag", "#b9b5ab"),
  R("kumas", "Kumaş", "ara", "t", 1, "genel", 92000, 380, "fabric", "#c95b7a"),
  R("un", "Un", "ara", "t", 1, "genel", 15500, 4000, "sack", "#efe6cf"),
  R("kagit", "Kâğıt", "ara", "t", 1, "genel", 31000, 900, "paper", "#f2f2ee"),
  // MAMUL ÜRÜNLER
  R("mobilya", "Mobilya", "mamul", "adet", 0.4, "genel", 1800, 6000, "chair", "#b07a45"),
  R("elektronik", "Elektronik bileşen", "mamul", "adet", 0.002, "genel", 340, 120000, "chip", "#2fa36b"),
  R("batarya", "Batarya modülü", "mamul", "adet", 0.01, "genel", 2300, 25000, "battery", "#4cd893"),
  R("makine", "Endüstriyel makine", "mamul", "adet", 2, "genel", 75000, 90, "gear", "#ffb02e"),
  R("otomobil", "Otomobil", "mamul", "adet", 6, "genel", 100000, 200, "car", "#e0473c"),
  R("insaat_malzemesi", "İnşaat malzemesi", "mamul", "t", 1, "dokme", 2700, 12000, "bricks", "#c26a45"),
  R("hazir_gida", "Hazır gıda", "mamul", "t", 1, "soguk", 24000, 2500, "can", "#e8553f"),
];

export const RES = Object.fromEntries(RESOURCES.map(r => [r.id, r]));
