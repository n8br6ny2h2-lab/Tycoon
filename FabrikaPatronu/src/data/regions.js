// Bölgeler. x,y: dünya haritasındaki konum (km). plots: arsa sayısı (ızgara cols x rows).
// mines: maden ocağında çıkabilen reçeteler. feeMod: piyasa teslimat ücretine çarpan (kaynak id ya da "*"). hourMod: teslim süresi çarpanı.
export const REGIONS = [
  { id: "anadolu", name: "Anadolu Sanayi Bölgesi", short: "Anadolu", x: 0, y: 0, cost: 0, level: 1, plotPrice: 1500000, cols: 4, rows: 3, rail: true, port: false, airport: false,
    perk: "Sanayi bölgesi: fabrika kurulumu %15 ucuz", factoryMod: 0.85, elecMod: 1, researchMod: 1, commissionMod: 1,
    mines: ["m_kirec", "m_agrega", "m_kum"], farm: true, wells: [], feeMod: {}, color: "#5d8a4e" },
  { id: "maden", name: "Maden Vadisi", short: "Maden", x: 320, y: -260, cost: 18000000, level: 2, plotPrice: 1200000, cols: 4, rows: 3, rail: true, port: false, airport: false,
    perk: "Maden bölgesi: cevher teslimatı %60 ucuz, madenler burada", factoryMod: 1, elecMod: 1, researchMod: 1, commissionMod: 1,
    mines: ["m_demir", "m_bakir", "m_boksit", "m_krom", "m_komur", "m_nikel", "m_cinko", "m_altin", "m_gumus", "m_kirec"], farm: false, wells: [],
    feeMod: { demir_cevheri: 0.4, bakir_cevheri: 0.4, boksit: 0.4, krom: 0.4, komur: 0.4, nikel: 0.4, cinko: 0.4 }, color: "#8a6d4e" },
  { id: "liman", name: "Liman Şehri", short: "Liman", x: -400, y: 140, cost: 25000000, level: 3, plotPrice: 3000000, cols: 4, rows: 3, rail: true, port: true, airport: false,
    perk: "Liman bölgesi: ithalat teslimatı yarı fiyat ve iki kat hızlı, ihracat, gemi", factoryMod: 1, elecMod: 1, researchMod: 1, commissionMod: 0.8,
    mines: [], farm: false, wells: [], feeMod: { "*": 0.5 }, hourMod: 0.5, export: true, color: "#3f7fa8" },
  { id: "petrol", name: "Petrol Bölgesi", short: "Petrol", x: 640, y: 280, cost: 35000000, level: 4, plotPrice: 2000000, cols: 4, rows: 3, rail: true, port: false, airport: false,
    perk: "Enerji bölgesi: elektrik %25 ucuz, petrol, gaz ve lityum burada", factoryMod: 1, elecMod: 0.75, researchMod: 1, commissionMod: 1,
    mines: ["m_lityum"], farm: false, wells: ["k_petrol", "k_gaz"], feeMod: { petrol: 0.4, dogalgaz: 0.4, yakit: 0.5 }, color: "#a8853f" },
  { id: "teknoloji", name: "Teknoloji Şehri", short: "Teknoloji", x: -170, y: -340, cost: 60000000, level: 6, plotPrice: 5000000, cols: 3, rows: 3, rail: true, port: false, airport: true,
    perk: "Teknoloji bölgesi: araştırma %50 hızlı, silisyum ve kuvars", factoryMod: 1, elecMod: 1, researchMod: 1.5, commissionMod: 1,
    mines: ["m_silisyum"], farm: false, wells: [], feeMod: { silisyum: 0.5, elektronik: 0.5 }, color: "#6a5fb0" },
  { id: "uluslararasi", name: "Uluslararası Ticaret Merkezi", short: "Uluslararası", x: -700, y: -60, cost: 120000000, level: 8, plotPrice: 8000000, cols: 3, rows: 3, rail: false, port: true, airport: true,
    perk: "Ticaret merkezi: komisyon yarı yarıya, ihracatta %10 prim, uçak ve gemi", factoryMod: 1.1, elecMod: 1.1, researchMod: 1, commissionMod: 0.5,
    mines: [], farm: false, wells: [], feeMod: { "*": 0.6 }, hourMod: 0.6, export: true, exportBonus: 0.1, color: "#b0475f" },
];
export const RG = Object.fromEntries(REGIONS.map(r => [r.id, r]));
// Müşterilerin bulunduğu dış şehirler (teslimat mesafesi için).
export const MARKET_CITIES = [
  { id: "istanbul", name: "İstanbul", x: -330, y: -120 }, { id: "ankara", name: "Ankara", x: 60, y: -60 }, { id: "izmir", name: "İzmir", x: -480, y: 160 },
  { id: "bursa", name: "Bursa", x: -300, y: -40 }, { id: "kocaeli", name: "Kocaeli", x: -250, y: -110 }, { id: "konya", name: "Konya", x: 40, y: 140 },
  { id: "adana", name: "Adana", x: 330, y: 260 }, { id: "gaziantep", name: "Gaziantep", x: 470, y: 220 }, { id: "kayseri", name: "Kayseri", x: 250, y: 40 },
  { id: "yurtdisi", name: "Yurt dışı (ihracat)", x: -1100, y: -200, export: true },
];
export const CITY = Object.fromEntries(MARKET_CITIES.map(c => [c.id, c]));
export const distKm = (a, b) => Math.round(Math.hypot(a.x - b.x, a.y - b.y)) || 15;
