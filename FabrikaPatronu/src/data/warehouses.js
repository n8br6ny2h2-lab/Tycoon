// Depo tipleri. accepts: kabul ettiği depolama sınıfları. cap: 1. seviye hacim. rate: hacim birimi başına günlük depolama gideri (₺).
export const WAREHOUSE_TYPES = [
  { id: "genel", name: "Genel depo", accepts: ["genel", "dokme"], cap: 600, cost: 1200000, maint: 1500, rate: 2, unlock: { level: 1 }, look: "shed",
    desc: "Kuru yük, dökme malzeme ve mamul ürünler için." },
  { id: "metal", name: "Metal deposu", accepts: ["metal"], cap: 800, cost: 1800000, maint: 2000, rate: 2, unlock: { level: 1 }, look: "metal",
    desc: "Külçe, çelik rulo ve kıymetli metaller için tavan vinçli depo." },
  { id: "kimya", name: "Kimyasal depo", accepts: ["kimya"], cap: 400, cost: 2500000, maint: 3000, rate: 5, unlock: { level: 2 }, look: "chemstore",
    desc: "Plastik, kauçuk, lityum ve kimyasallar için havalandırmalı, yangın korumalı depo." },
  { id: "soguk", name: "Soğuk hava deposu", accepts: ["soguk"], cap: 300, cost: 2800000, maint: 4000, rate: 8, unlock: { level: 2 }, look: "cold",
    desc: "Hazır gıda için soğutmalı depo. Elektrik tüketir." },
  { id: "tank", name: "Petrol tankı", accepts: ["tank"], cap: 1000, cost: 3500000, maint: 3500, rate: 3, unlock: { level: 2 }, look: "tank",
    desc: "Ham petrol, yakıt, doğalgaz, hidrojen ve sıvı yağ için." },
  { id: "lojistik", name: "Büyük lojistik deposu", accepts: ["genel", "dokme", "metal"], cap: 4000, cost: 9000000, maint: 9000, rate: 1.5, unlock: { level: 3 }, look: "logistics",
    desc: "Genel, dökme ve metal yükler için büyük hacimli, rampalı depo." },
];
export const WT = Object.fromEntries(WAREHOUSE_TYPES.map(w => [w.id, w]));
export const STORAGE_CLASSES = { genel: "Genel", dokme: "Dökme", metal: "Metal", kimya: "Kimyasal", soguk: "Soğuk", tank: "Sıvı/Gaz", enerji: "Şebeke" };
