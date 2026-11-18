// Dinamik olaylar. effect: { prod: {res|cat|"*": çarpan}, cons: {...}, price: {...}, interest: puan, transport: çarpan, grid: false, port: çarpan, research: çarpan }
// weight: seçilme ağırlığı. days: süre [min,max]. good: oyuncu için genel olarak olumlu mu.
export const EVENTS = [
  { id: "kitlik", name: "Hammadde kıtlığı", weight: 3, days: [5, 10], target: "raw", desc: "{res} üretiminde büyük düşüş. Fiyat yükseliyor.", effect: { prod: { $: 0.55 } } },
  { id: "petrol_krizi", name: "Petrol fiyatı krizi", weight: 2, days: [6, 12], desc: "Ham petrol arzı daraldı; petrol, yakıt ve plastik pahalanıyor.", effect: { prod: { petrol: 0.6, yakit: 0.8 } } },
  { id: "enerji_zammi", name: "Enerji zammı", weight: 2, days: [8, 15], desc: "Elektrik ve doğalgaz tarifelerine zam geldi.", effect: { price: { elektrik: 1.3, dogalgaz: 1.15 } } },
  { id: "durgunluk", name: "Ekonomik durgunluk", weight: 1, days: [15, 25], desc: "Talep düştü, faizler yükseldi.", effect: { cons: { "*": 0.82 }, interest: 0.02, offers: 0.6 } },
  { id: "talep", name: "Talep patlaması", weight: 3, days: [5, 9], target: "cat", desc: "{cat} talebi patladı.", effect: { cons: { $cat: 1.45 } } },
  { id: "buyuk_musteri", name: "Büyük müşteri anlaşması", weight: 2, days: [3, 3], desc: "Büyük bir firma özel tedarik teklifi gönderdi.", effect: { bigOffer: true }, good: true },
  { id: "teknoloji_kesfi", name: "Yeni teknoloji keşfi", weight: 1, days: [10, 10], desc: "Araştırmalar bu süre boyunca %30 ucuz ve hızlı.", effect: { research: 0.7 }, good: true },
  { id: "liman", name: "Liman yoğunluğu", weight: 2, days: [4, 8], desc: "Limanlarda bekleme: gemi seferleri uzun sürüyor, ithal mallar pahalı.", effect: { port: 2, price: { metal: 1.04, enerji: 1.03 } } },
  { id: "nakliye", name: "Nakliye maliyet artışı", weight: 2, days: [6, 10], desc: "Akaryakıt zammıyla nakliye %30 pahalandı.", effect: { transport: 1.3 } },
  { id: "rakip_iflas", name: "Rakip şirketin iflası", weight: 1, days: [1, 1], desc: "{rival} iflas etti. Pazar payı boşaldı.", effect: { rivalBankrupt: true }, good: true },
  { id: "kesinti", name: "Enerji kesintisi", weight: 1, days: [1, 1], hours: [4, 8], desc: "Şebekede arıza: kendi santrali olmayan fabrikalar duruyor.", effect: { grid: false } },
  { id: "bolluk", name: "Hasat bolluğu", weight: 2, days: [6, 10], desc: "Tarım ürünlerinde bol arz; buğday, pamuk ve yağ ucuzladı.", effect: { prod: { orman: 1.35 } }, good: true },
];
export const EV = Object.fromEntries(EVENTS.map(e => [e.id, e]));
