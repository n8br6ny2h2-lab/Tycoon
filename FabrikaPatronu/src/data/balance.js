// Ekonomi ve zaman sabitleri. Denge ayarları yalnızca buradan yapılır.
export const BAL = {
  version: 1,
  minutesPerRealSecond: 2,          // ×1 hızda 1 gerçek saniye = 2 oyun dakikası (1 oyun günü = 12 dk)
  speeds: [0, 1, 2, 5],
  offlineMaxRealHours: 12,          // çevrimdışı süre en fazla bu kadar gerçek saat sayılır
  offlineMaxGameHours: 72,          // ve en fazla 3 oyun günü simüle edilir
  startCash: 6000000,
  startRep: 50,
  spread: 0.02,                     // alış-satış makası (toplam)
  commission: 0.004,                // komisyon
  tradeTax: 0.003,                  // işlem vergisi
  corporateTax: 0.2,                // aylık pozitif kâr üzerinden kurumlar vergisi
  slippageChunks: 8,
  targetStockDays: 20,              // dünya hedef stoğu = günlük tüketim × gün
  supplyResponse: 0.6,              // üreticilerin fiyat/stok sapmasına tepkisi
  demandResponse: 0.35,             // tüketicilerin tepkisi
  noise: 0.04,                      // günlük üretim/tüketim gürültüsü
  priceSmoothing: 0.35,             // saatlik fiyat yumuşatma
  gridSellRatio: 0.7,               // fazla elektriği şebekeye satış oranı
  baseInterest: 0.022,              // aylık taban faiz
  overdraftDays: 5,                 // eksi bakiyede iflas uyarısına kadar gün
  overdraftLimit: 0.15,             // şirket değerinin oranı kadar eksi bakiye (en az 2 Mn ₺)
  machineCostRatio: 0.25,           // ek makine fiyatı = fabrika maliyeti × oran
  upgradeCostRatio: 0.6,            // fabrika seviye fiyatı = maliyet × oran × seviye
  maxLevel: 5,
  wearPerDay: 2.5,                  // çalışan makinenin günlük aşınması (puan)
  maintCostRatio: 0.015,            // tam bakım = fabrika maliyeti × oran
  xpLevels: [0, 40000, 150000, 400000, 1000000, 2500000, 5500000, 11000000, 22000000, 45000000],
  levelNames: ["Tüccar", "Küçük işletme", "Atölye sahibi", "Sanayici", "Fabrikatör", "Holding", "Sanayi grubu", "Endüstri devi", "Hammadde imparatoru", "Fabrika Patronu"],
  repMin: 0, repMax: 100,
  lateGraceDays: 3,
  latePenalty: 0.1,
  theftBase: 0.004,                 // günlük hırsızlık olasılığı (depo başına, güvenliksiz)
};
