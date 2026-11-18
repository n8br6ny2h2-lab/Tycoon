// Günlük ve haftalık görev şablonları. n(level): hedef. reward(level): ₺ ödül.
export const DAILY = [
  { id: "buy", text: "{n} {unit} {res} satın al", kind: "buy", pick: "raw", n: l => 20 + l * 15, reward: l => 60000 * l },
  { id: "produce", text: "{n} {unit} {res} üret", kind: "produce", pick: "produced", n: l => 10 + l * 10, reward: l => 90000 * l },
  { id: "contracts", text: "{n} teslimat tamamla", kind: "deliveries", n: l => 1 + Math.floor(l / 3), reward: l => 80000 * l },
  { id: "upgrade", text: "Bir fabrikayı ya da depoyu geliştir", kind: "upgrade", n: () => 1, reward: l => 70000 * l },
  { id: "profit", text: "Bugün {n} ₺ satış kârı elde et", kind: "profit", n: l => 150000 * l, reward: l => 100000 * l },
  { id: "sell", text: "{n} {unit} {res} sat", kind: "sell", pick: "any", n: l => 15 + l * 10, reward: l => 60000 * l },
];
export const WEEKLY = [
  { id: "factory", text: "Yeni bir fabrika kur", kind: "build", n: () => 1, reward: l => 600000 * l },
  { id: "value", text: "Şirket değerini %{n} artır", kind: "value", n: () => 10, reward: l => 800000 * l },
  { id: "export", text: "Bir ihracat teslimatı yap", kind: "export", n: () => 1, reward: l => 900000 * l, level: 3 },
  { id: "chain", text: "Daha önce üretmediğin bir ürün üret", kind: "newProduct", n: () => 1, reward: l => 700000 * l },
  { id: "deliveries", text: "Hafta içinde {n} teslimat tamamla", kind: "deliveries", n: l => 4 + l, reward: l => 700000 * l },
  { id: "research", text: "Bir araştırma tamamla", kind: "research", n: () => 1, reward: l => 500000 * l, level: 2 },
];
export const ACHIEVEMENTS = [
  { id: "ilk_alim", name: "İlk alım", desc: "Piyasadan ilk hammaddeyi al.", test: s => s.stats.bought > 0 },
  { id: "ilk_satis", name: "İlk satış", desc: "İlk satışını yap.", test: s => s.stats.sold > 0 },
  { id: "ilk_fabrika", name: "Fabrikatör", desc: "İlk fabrikanı kur.", test: s => s.stats.built > 0 },
  { id: "ilk_uretim", name: "Bant çalışıyor", desc: "İlk ürününü üret.", test: s => s.stats.produced > 0 },
  { id: "teslimat10", name: "Güvenilir tedarikçi", desc: "10 teslimat tamamla.", test: s => s.stats.deliveries >= 10 },
  { id: "teslimat100", name: "Tedarik zinciri", desc: "100 teslimat tamamla.", test: s => s.stats.deliveries >= 100 },
  { id: "fabrika5", name: "Sanayici", desc: "5 fabrikan olsun.", test: s => Object.values(s.buildings).filter(b => b.kind === "factory").length >= 5 },
  { id: "fabrika15", name: "Sanayi devi", desc: "15 fabrikan olsun.", test: s => Object.values(s.buildings).filter(b => b.kind === "factory").length >= 15 },
  { id: "bolge2", name: "Yeni ufuklar", desc: "İkinci bölgeyi aç.", test: s => Object.values(s.regions).filter(r => r.owned).length >= 2 },
  { id: "bolge6", name: "İmparatorluk", desc: "Bütün bölgeleri aç.", test: s => Object.values(s.regions).filter(r => r.owned).length >= 6 },
  { id: "deger100", name: "Yüz milyonluk şirket", desc: "Şirket değeri 100 milyon ₺.", test: s => s.stats.peakValue >= 1e8 },
  { id: "deger1000", name: "Milyar ₺ kulübü", desc: "Şirket değeri 1 milyar ₺.", test: s => s.stats.peakValue >= 1e9 },
  { id: "arastirma5", name: "Ar-Ge merkezi", desc: "5 araştırma tamamla.", test: s => Object.keys(s.research.done).length >= 5 },
  { id: "filo", name: "Filo sahibi", desc: "5 aracın olsun.", test: s => s.fleet.length >= 5 },
  { id: "ihale", name: "İhale kazanıldı", desc: "Bir devlet ihalesi kazan.", test: s => s.stats.tendersWon > 0 },
  { id: "ihracat", name: "Dünyaya açıl", desc: "İlk ihracatını yap.", test: s => s.stats.exports > 0 },
  { id: "lider", name: "Sektör lideri", desc: "Şirket değerinde 1. sıraya çık.", test: s => s.stats.rank === 1 },
  { id: "elektrik", name: "Kendi enerjisi", desc: "Kendi santralini kur.", test: s => Object.values(s.buildings).some(b => b.type === "santral") },
  { id: "otomobil", name: "Otomobil üreticisi", desc: "İlk otomobilini üret.", test: s => (s.stats.producedBy.otomobil || 0) > 0 },
  { id: "personel50", name: "Büyük aile", desc: "50 çalışana ulaş.", test: s => Object.values(s.staff).reduce((a, x) => a + x.n, 0) >= 50 },
];
// Haftalık dönen özel etkinlikler.
export const SPECIALS = [
  { id: "ticaret", name: "Hammadde ticaret haftası", desc: "Piyasa komisyonu yarı yarıya.", effect: { commission: 0.5 } },
  { id: "fuar", name: "Sanayi fuarı", desc: "Müşteri teklifleri %60 fazla, itibar kazancı iki kat.", effect: { offers: 1.6, rep: 2 } },
  { id: "ihaleler", name: "Büyük şirket ihaleleri", desc: "Her gün yeni bir büyük ihale.", effect: { tenders: 1 } },
  { id: "yarisma", name: "Üretim yarışması", desc: "Haftanın en çok üretilen ürününde rakipleri geçersen 2 milyon ₺ × seviye ödül.", effect: { contest: true } },
  { id: "sakin", name: "Sakin hafta", desc: "Özel etkinlik yok, piyasa olağan.", effect: {} },
];
