// Fabrika tipleri ve reçeteler. Reçete değerleri makine başına, çevrim başınadır:
// in/out: birim, hours: oyun saati, mwh: elektrik, labor: ₺ işçilik dışı değişken gider (sarf, bakım malzemesi), fire: çıktı kaybı oranı.
const rec = (id, name, inp, out, hours, mwh, labor, fire = 0.02, extra = {}) => ({ id, name, in: inp, out, hours, mwh, labor, fire, ...extra });

export const FACTORY_TYPES = [
  // Çıkarma, tarım ve enerji tesisleri (bölgeye bağlı)
  { id: "maden", name: "Maden ocağı", kind: "extract", cost: 9000000, workers: 12, maint: 9000, idleMwh: 0.2, unlock: { level: 1 }, look: "mine",
    desc: "Bölgedeki cevheri çıkarır. Hangi madenlerin çıktığı bölgeye bağlıdır.", recipes: [
      rec("m_demir", "Demir cevheri", {}, { demir_cevheri: 6 }, 6, 1.2, 8000, 0, { res: "demir_cevheri" }),
      rec("m_bakir", "Bakır cevheri", {}, { bakir_cevheri: 0.5 }, 6, 2, 16000, 0, { res: "bakir_cevheri" }),
      rec("m_boksit", "Boksit", {}, { boksit: 8 }, 6, 1.2, 10000, 0, { res: "boksit" }),
      rec("m_krom", "Krom cevheri", {}, { krom: 4 }, 6, 1.2, 18000, 0, { res: "krom" }),
      rec("m_komur", "Kömür", {}, { komur: 8 }, 6, 1.0, 17000, 0, { res: "komur" }),
      rec("m_nikel", "Nikel", {}, { nikel: 0.06 }, 6, 2, 18000, 0, { res: "nikel" }),
      rec("m_cinko", "Çinko", {}, { cinko: 0.35 }, 6, 2, 17000, 0, { res: "cinko" }),
      rec("m_altin", "Altın", {}, { altin: 0.009 }, 8, 2, 9000, 0, { res: "altin" }),
      rec("m_gumus", "Gümüş", {}, { gumus: 0.9 }, 8, 1.5, 10000, 0, { res: "gumus" }),
      rec("m_lityum", "Lityum (tuzlu su)", {}, { lityum: 0.08 }, 12, 4, 14000, 0, { res: "lityum" }),
      rec("m_kirec", "Kireçtaşı", {}, { kirectasi: 16 }, 6, 0.6, 4000, 0, { res: "kirectasi" }),
      rec("m_agrega", "Agrega", {}, { agrega: 18 }, 6, 0.6, 3000, 0, { res: "agrega" }),
      rec("m_kum", "Cam kumu", {}, { cam_kumu: 10 }, 6, 0.6, 6000, 0, { res: "cam_kumu" }),
      rec("m_silisyum", "Silisyum (kuvars)", {}, { silisyum: 0.12 }, 8, 1, 3000, 0, { res: "silisyum" }),
    ] },
  { id: "kuyu", name: "Petrol ve gaz kuyusu", kind: "extract", cost: 14000000, workers: 6, maint: 12000, idleMwh: 0.2, unlock: { level: 1 }, look: "well",
    desc: "Petrol Bölgesi'nde ham petrol ve doğalgaz çıkarır.", recipes: [
      rec("k_petrol", "Ham petrol", {}, { petrol: 4 }, 6, 2, 45000, 0, { res: "petrol" }),
      rec("k_gaz", "Doğalgaz", {}, { dogalgaz: 5 }, 6, 1.5, 32000, 0, { res: "dogalgaz" }),
    ] },
  { id: "santral", name: "Elektrik santrali", kind: "power", cost: 20000000, workers: 10, maint: 10000, idleMwh: 0, unlock: { level: 2 }, look: "power",
    desc: "Kendi elektriğini üretir. Fazlası şebekeye satılır, fabrikaların önce buradan beslenir.", recipes: [
      rec("e_komur", "Kömürden elektrik", { komur: 4.2 }, { elektrik: 10 }, 1, 0, 1800, 0),
      rec("e_gaz", "Doğalgazdan elektrik", { dogalgaz: 1.9 }, { elektrik: 10 }, 1, 0, 900, 0),
      rec("e_gunes", "Güneşten elektrik", {}, { elektrik: 1 }, 1, 0, 400, 0, { research: "yenilenebilir" }),
    ] },
  { id: "ciftlik", name: "Tarım ve orman işletmesi", kind: "extract", cost: 4000000, workers: 8, maint: 3000, idleMwh: 0.1, unlock: { level: 1 }, look: "farm",
    desc: "Buğday, pamuk, yağlık tohum ve kereste üretir.", recipes: [
      rec("c_bugday", "Buğday", {}, { bugday: 8 }, 12, 0.5, 50000, 0, { res: "bugday" }),
      rec("c_pamuk", "Pamuk", {}, { pamuk: 1.2 }, 12, 0.5, 44000, 0, { res: "pamuk" }),
      rec("c_yag", "Bitkisel yağ", {}, { bitkisel_yag: 1 }, 12, 0.6, 25000, 0, { res: "bitkisel_yag" }),
      rec("c_kereste", "Kereste", {}, { kereste: 6 }, 12, 0.5, 30000, 0, { res: "kereste" }),
    ] },
  // 15 fabrika
  { id: "celik", name: "Demir-çelik fabrikası", kind: "factory", cost: 25000000, workers: 14, maint: 14000, idleMwh: 0.5, unlock: { level: 1 }, look: "steel",
    desc: "Cevher ve kömürden ham demir, ham demirden çelik.", recipes: [
      rec("ham_demir", "Ham demir", { demir_cevheri: 16, komur: 6 }, { ham_demir: 10 }, 2, 4, 8000, 0.02),
      rec("celik", "Çelik", { ham_demir: 10.5 }, { celik: 10 }, 3, 8, 12000, 0.015),
    ] },
  { id: "plastik", name: "Plastik fabrikası", kind: "factory", cost: 12000000, workers: 8, maint: 7000, idleMwh: 0.3, unlock: { level: 2 }, look: "plastic",
    desc: "Ham petrolden plastik granül.", recipes: [
      rec("plastik", "Plastik granül", { petrol: 6.25, dogalgaz: 0.75 }, { plastik_granul: 5 }, 3, 6, 10000, 0.02),
    ] },
  { id: "aluminyum", name: "Alüminyum fabrikası", kind: "factory", cost: 22000000, workers: 10, maint: 12000, idleMwh: 0.5, unlock: { level: 2 }, look: "smelter",
    desc: "Boksitten alüminyum, bakır cevherinden işlenmiş bakır.", recipes: [
      rec("aluminyum", "Alüminyum külçe", { boksit: 8, kimyasal: 0.8 }, { aluminyum: 2 }, 4, 28, 6000, 0.02),
      rec("bakir", "İşlenmiş bakır", { bakir_cevheri: 4 }, { islenmis_bakir: 1 }, 4, 3, 3000, 0.01),
    ] },
  { id: "cam", name: "Cam fabrikası", kind: "factory", cost: 9000000, workers: 8, maint: 5000, idleMwh: 0.3, unlock: { level: 1 }, look: "glass",
    desc: "Kum, kimyasal ve doğalgazla düz cam.", recipes: [
      rec("cam", "Düz cam", { cam_kumu: 6, kimyasal: 0.75, dogalgaz: 1.5 }, { cam: 5 }, 3, 1, 4500, 0.03),
    ] },
  { id: "mobilya", name: "Mobilya fabrikası", kind: "factory", cost: 3500000, workers: 10, maint: 2500, idleMwh: 0.1, unlock: { level: 1 }, look: "furniture",
    desc: "Kereste işler, ahşap ve çelikten mobilya yapar.", recipes: [
      rec("ahsap", "İşlenmiş ahşap", { kereste: 6.5 }, { islenmis_ahsap: 5 }, 2, 1, 3000, 0.02),
      rec("mobilya", "Mobilya", { islenmis_ahsap: 1, celik: 0.08, kimyasal: 0.06 }, { mobilya: 20 }, 3, 0.4, 8000, 0.02),
    ] },
  { id: "tekstil", name: "Tekstil fabrikası", kind: "factory", cost: 6000000, workers: 12, maint: 4000, idleMwh: 0.2, unlock: { level: 1 }, look: "textile",
    desc: "Pamuktan kumaş.", recipes: [
      rec("kumas", "Kumaş", { pamuk: 1.1, kimyasal: 0.05 }, { kumas: 1 }, 3, 1, 4000, 0.03),
    ] },
  { id: "kimya", name: "Kimya fabrikası", kind: "factory", cost: 15000000, workers: 8, maint: 9000, idleMwh: 0.4, unlock: { level: 2 }, look: "chem",
    desc: "Doğalgazdan kimyasal madde ve hidrojen, petrolden sentetik kauçuk.", recipes: [
      rec("kimyasal", "Kimyasal madde", { dogalgaz: 8 }, { kimyasal: 5 }, 3, 4, 10000, 0.02),
      rec("hidrojen", "Hidrojen", { dogalgaz: 3 }, { hidrojen: 0.5 }, 4, 6, 5000, 0.01),
      rec("kaucuk", "Sentetik kauçuk", { petrol: 3.2, kimyasal: 0.8 }, { kaucuk: 2 }, 4, 2, 6000, 0.02),
    ] },
  { id: "elektronik", name: "Elektronik fabrikası", kind: "factory", cost: 30000000, workers: 10, maint: 15000, idleMwh: 0.6, unlock: { level: 3, research: "elektronik" }, look: "electronics",
    desc: "Silisyum, bakır ve plastikten elektronik bileşen.", recipes: [
      rec("elektronik", "Elektronik bileşen", { silisyum: 0.2, islenmis_bakir: 0.16, plastik_granul: 0.24 }, { elektronik: 400 }, 3, 0.8, 24000, 0.03),
    ] },
  { id: "batarya", name: "Batarya fabrikası", kind: "factory", cost: 40000000, workers: 8, maint: 18000, idleMwh: 0.6, unlock: { level: 5, research: "batarya" }, look: "battery",
    desc: "Lityum, nikel ve alüminyumdan batarya modülü.", recipes: [
      rec("batarya", "Batarya modülü", { lityum: 0.2, nikel: 0.1, aluminyum: 0.2 }, { batarya: 100 }, 4, 1, 15000, 0.02),
    ] },
  { id: "otomotiv", name: "Otomotiv fabrikası", kind: "factory", cost: 90000000, workers: 30, maint: 40000, idleMwh: 1, unlock: { level: 7, research: "otomotiv" }, look: "auto",
    desc: "Çelik, alüminyum, plastik, kauçuk, cam ve elektronikten otomobil.", recipes: [
      rec("otomobil", "Otomobil", { celik: 4, aluminyum: 0.6, plastik_granul: 0.48, kaucuk: 0.2, cam: 0.12, elektronik: 100 }, { otomobil: 4 }, 6, 6, 80000, 0.01),
    ] },
  { id: "makine", name: "Makine fabrikası", kind: "factory", cost: 35000000, workers: 16, maint: 16000, idleMwh: 0.6, unlock: { level: 4, research: "makine" }, look: "machine",
    desc: "Çelik, elektronik ve plastikten endüstriyel makine.", recipes: [
      rec("makine", "Endüstriyel makine", { celik: 2.4, elektronik: 80, plastik_granul: 0.3 }, { makine: 2 }, 6, 1, 18000, 0.01),
    ] },
  { id: "gida", name: "Gıda fabrikası", kind: "factory", cost: 5000000, workers: 10, maint: 3500, idleMwh: 0.2, unlock: { level: 1 }, look: "food",
    desc: "Buğdaydan un, un ve yağdan hazır gıda.", recipes: [
      rec("un", "Un", { bugday: 6.5 }, { un: 5 }, 2, 0.5, 1500, 0.01),
      rec("gida", "Hazır gıda", { un: 1.2, bitkisel_yag: 0.3 }, { hazir_gida: 2 }, 3, 0.4, 5000, 0.02),
    ] },
  { id: "rafineri", name: "Rafineri", kind: "factory", cost: 45000000, workers: 12, maint: 20000, idleMwh: 0.8, unlock: { level: 4 }, look: "refinery",
    desc: "Ham petrolü rafine yakıt ve kimyasal ürünlere ayırır.", recipes: [
      rec("rafine", "Rafine yakıt + kimyasal", { petrol: 10 }, { yakit: 6.5, kimyasal: 2.5 }, 3, 3, 6000, 0.01),
    ] },
  { id: "kagit", name: "Kâğıt fabrikası", kind: "factory", cost: 10000000, workers: 9, maint: 5000, idleMwh: 0.3, unlock: { level: 2 }, look: "paper",
    desc: "Kereste ve kimyasaldan kâğıt.", recipes: [
      rec("kagit", "Kâğıt", { kereste: 6.6, kimyasal: 0.3 }, { kagit: 3 }, 3, 1.8, 4500, 0.02),
    ] },
  { id: "cimento", name: "Çimento fabrikası", kind: "factory", cost: 8000000, workers: 9, maint: 4500, idleMwh: 0.3, unlock: { level: 1 }, look: "cement",
    desc: "Kireçtaşı ve kömürden çimento; çimento, demir ve agregadan inşaat malzemesi.", recipes: [
      rec("cimento", "Çimento", { kirectasi: 14, komur: 1.2 }, { cimento: 10 }, 2, 1, 3000, 0.02),
      rec("insaat", "İnşaat malzemesi", { cimento: 3, ham_demir: 0.5, agrega: 7 }, { insaat_malzemesi: 10 }, 2, 0.5, 2500, 0.02),
    ] },
];
export const FT = Object.fromEntries(FACTORY_TYPES.map(f => [f.id, f]));
export const RECIPE = Object.fromEntries(FACTORY_TYPES.flatMap(f => f.recipes.map(r => [r.id, { ...r, factory: f.id }])));
