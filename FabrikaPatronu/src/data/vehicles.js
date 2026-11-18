// Taşıma araçları. cap: hacim birimi, speed: km/sa, fuel: ₺/km, maint: ₺/gün.
// needs: iki bölgede de bulunması gereken altyapı. research: açılması için gereken araştırma.
export const VEHICLES = [
  { id: "kamyon", name: "Küçük kamyon", cap: 8, speed: 60, fuel: 18, price: 1200000, maint: 1500, needs: null, look: "truck_s" },
  { id: "agir", name: "Ağır kamyon", cap: 20, speed: 55, fuel: 28, price: 2800000, maint: 2500, needs: null, look: "truck_l" },
  { id: "tir", name: "Tır", cap: 40, speed: 70, fuel: 36, price: 4500000, maint: 3500, needs: null, unlock: { level: 2 }, look: "tir" },
  { id: "tren", name: "Yük treni", cap: 1200, speed: 50, fuel: 130, price: 45000000, maint: 25000, needs: "rail", unlock: { level: 4 }, look: "train" },
  { id: "gemi", name: "Konteyner gemisi", cap: 8000, speed: 30, fuel: 420, price: 180000000, maint: 80000, needs: "port", unlock: { level: 6 }, look: "ship" },
  { id: "ucak", name: "Kargo uçağı", cap: 100, speed: 700, fuel: 950, price: 250000000, maint: 120000, needs: "airport", research: "kargo_ucak", look: "plane" },
];
export const VH = Object.fromEntries(VEHICLES.map(v => [v.id, v]));
// Filo yoksa kiralık nakliyeci: hacim birimi başına sabit ücret + km başına ücret.
export const CARRIER = { name: "Kiralık nakliyeci", perUnit: 250, perUnitKm: 3.2, speed: 50, minHours: 3 };
// Piyasadan alımda tedarikçiler.
export const SUPPLIERS = [
  { id: "standart", name: "Anadolu Tedarik", priceMod: 0, hours: 24, quality: 0, desc: "Piyasa fiyatı, 1 günde teslim" },
  { id: "hizli", name: "Hızlı Lojistik A.Ş.", priceMod: 0.04, hours: 6, quality: 0, desc: "%4 pahalı, 6 saatte teslim" },
  { id: "ekonomik", name: "Ekonomik Toptan", priceMod: -0.03, hours: 72, quality: -8, desc: "%3 ucuz, 3 günde teslim, kalite düşük" },
  { id: "premium", name: "Premium Kalite", priceMod: 0.07, hours: 24, quality: 12, desc: "%7 pahalı, yüksek kalite" },
];
