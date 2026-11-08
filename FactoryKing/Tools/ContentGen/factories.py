"""Single source for the 15 additional factories (orders 5..19).
Generates Assets/_Project/Scripts/Core/Content/ExtraFactories.cs and the matching localisation rows.
Run: python3 factories.py   (then re-run the .NET tests)."""
import os

# id, EN, TR, theme (body, accent, floor, wall),
# products: [(id, EN, TR, color)] x4 (raw, intermediate, product, packed final)
# machines: [(suffix, EN, TR, category, visual)] x6 (raw, processing, production, packaging, storage, shipping)
F = [
 ("glass","Glassworks","Cam Fabrikası",("#5FB3C9","#F28C38","#CFD9DD","#EEF4F6"),
  [("sand","Silica Sand","Silis Kumu","#E8D9A8"),("molten_glass","Molten Glass","Erimiş Cam","#FF9C4A"),("glass_pane","Glass Pane","Cam Levha","#BFE6F2"),("glass_crate","Glass Crate","Cam Kasası","#9ED3E6")],
  [("sand_hopper","Sand Hopper","Kum Bunkeri","RawProcessing","Silo"),("melting_furnace","Melting Furnace","Ergitme Fırını","Smelting","Furnace"),("float_cutter","Float Line Cutter","Float Hattı Kesici","Cutting","Cutter"),("crate_packer","Crate Packer","Kasa Paketleyici","Packaging","Packer"),("warehouse","Glass Warehouse","Cam Deposu","Storage","Warehouse"),("dock","Glass Dock","Cam Rampası","Shipping","Dock")]),
 ("paper","Paper Mill","Kâğıt Fabrikası",("#7A9E5B","#D9A441","#D8D2C4","#F1EDE4"),
  [("logs","Logs","Tomruk","#8B5A2B"),("pulp","Pulp","Kâğıt Hamuru","#E8E1CF"),("paper_roll","Paper Roll","Kâğıt Bobini","#F5F2EA"),("paper_ream","Paper Ream","Kâğıt Topu","#FFFFFF")],
  [("log_crane","Log Crane","Tomruk Vinci","Transport","Unloader"),("pulper","Pulper","Hamurlaştırıcı","RawProcessing","Mill"),("paper_machine","Paper Machine","Kâğıt Makinesi","Assembly","Loom"),("ream_wrapper","Ream Wrapper","Top Sarıcı","Packaging","Packer"),("warehouse","Paper Warehouse","Kâğıt Deposu","Storage","Warehouse"),("dock","Paper Dock","Kâğıt Rampası","Shipping","Dock")]),
 ("furniture","Furniture Factory","Mobilya Fabrikası",("#A0673A","#3C6E71","#D6C7B2","#F3EADB"),
  [("timber","Timber","Kereste","#A87545"),("plank","Plank","Tahta","#C99A6B"),("furniture_part","Furniture Part","Mobilya Parçası","#B5835A"),("flatpack","Flat-pack Furniture","Paketli Mobilya","#D9B48F")],
  [("timber_loader","Timber Loader","Kereste Yükleyici","Transport","Unloader"),("sawmill","Sawmill","Hızar","Cutting","Cutter"),("cnc_router","CNC Router","CNC Freze","Assembly","Press"),("flatpack_packer","Flat-pack Packer","Paketleme Hattı","Packaging","Packer"),("warehouse","Furniture Warehouse","Mobilya Deposu","Storage","Warehouse"),("dock","Furniture Dock","Mobilya Rampası","Shipping","Dock")]),
 ("beverage","Beverage Plant","İçecek Fabrikası",("#E85D75","#5BC0EB","#E2D4D8","#FBEFF2"),
  [("fruit","Fruit","Meyve","#F25F5C"),("juice","Juice","Meyve Suyu","#FFB627"),("bottle","Bottled Juice","Şişe Meyve Suyu","#FF7F11"),("juice_case","Juice Case","Meyve Suyu Kolisi","#F9C784")],
  [("fruit_intake","Fruit Intake","Meyve Kabulü","RawProcessing","Silo"),("juicer","Industrial Juicer","Endüstriyel Sıkacak","RawProcessing","Mill"),("bottler","Bottling Line","Dolum Hattı","Assembly","RobotArm"),("case_packer","Case Packer","Koli Paketleyici","Packaging","Packer"),("cold_store","Cold Store","Soğuk Depo","Storage","Warehouse"),("dock","Beverage Dock","İçecek Rampası","Shipping","Dock")]),
 ("cement","Cement Works","Çimento Fabrikası",("#8D99AE","#EF8354","#C4C6C8","#E5E6E8"),
  [("limestone","Limestone","Kireçtaşı","#D9D4C7"),("clinker","Clinker","Klinker","#5C5C5C"),("cement","Cement","Çimento","#A9A9A9"),("cement_bag","Cement Bag","Çimento Torbası","#C2B280")],
  [("quarry_loader","Quarry Loader","Ocak Yükleyici","Transport","Unloader"),("rotary_kiln","Rotary Kiln","Döner Fırın","Smelting","Extruder"),("cement_mill","Cement Mill","Çimento Değirmeni","RawProcessing","Mill"),("bagger","Bagging Machine","Torbalama Makinesi","Packaging","Packer"),("silo_store","Bulk Silo","Dökme Silo","Storage","Warehouse"),("dock","Cement Dock","Çimento Rampası","Shipping","Dock")]),
 ("chemical","Chemical Plant","Kimya Fabrikası",("#4CB944","#FFD23F","#CBD5C0","#EAF2E3"),
  [("brine","Brine","Tuzlu Su","#B8E0F6"),("solution","Chemical Solution","Kimyasal Çözelti","#7BD389"),("detergent","Detergent","Deterjan","#3DCCC7"),("detergent_pack","Detergent Pack","Deterjan Paketi","#68D8D6")],
  [("brine_pump","Brine Pump","Tuzlu Su Pompası","Transport","Pump"),("reactor","Reactor","Reaktör","Smelting","Oven"),("mixer","Formulation Mixer","Formül Karıştırıcı","Assembly","Press"),("filler","Pack Filler","Paket Dolum","Packaging","Packer"),("tank_farm","Tank Farm","Tank Sahası","Storage","Warehouse"),("dock","Chemical Dock","Kimya Rampası","Shipping","Dock")]),
 ("pharma","Pharmaceutical Plant","İlaç Fabrikası",("#FFFFFF","#3A86FF","#E3E8EF","#F7F9FC"),
  [("compound","Raw Compound","Ham Bileşik","#E0E7FF"),("active_ingredient","Active Ingredient","Etken Madde","#A0C4FF"),("tablet","Tablet","Tablet","#FFFFFF"),("medicine_box","Medicine Box","İlaç Kutusu","#BDE0FE")],
  [("compound_loader","Compound Loader","Bileşik Yükleyici","Transport","Unloader"),("synthesis","Synthesis Reactor","Sentez Reaktörü","Smelting","Oven"),("tablet_press","Tablet Press","Tablet Presi","Assembly","Press"),("blister_packer","Blister Packer","Blister Paketleyici","Packaging","Packer"),("clean_store","Clean Store","Temiz Depo","Storage","Warehouse"),("dock","Pharma Dock","İlaç Rampası","Shipping","Dock")]),
 ("rubber","Tyre Factory","Lastik Fabrikası",("#3D3D3D","#F2C14E","#BDBDBD","#E0E0E0"),
  [("latex","Latex","Lateks","#F3EED9"),("compound_rubber","Rubber Compound","Kauçuk Karışımı","#505050"),("tyre","Tyre","Lastik","#222222"),("tyre_stack","Tyre Stack","Lastik Yığını","#3A3A3A")],
  [("latex_tank","Latex Tank","Lateks Tankı","RawProcessing","Silo"),("banbury","Banbury Mixer","Banbury Karıştırıcı","RawProcessing","Crusher"),("tyre_press","Curing Press","Vulkanizasyon Presi","Assembly","Press"),("stacker","Tyre Stacker","Lastik İstifleyici","Packaging","Packer"),("warehouse","Tyre Warehouse","Lastik Deposu","Storage","Warehouse"),("dock","Tyre Dock","Lastik Rampası","Shipping","Dock")]),
 ("appliance","Appliance Factory","Beyaz Eşya Fabrikası",("#E9ECEF","#E63946","#CED4DA","#F8F9FA"),
  [("sheet_metal","Sheet Metal","Sac Levha","#ADB5BD"),("casing","Casing","Gövde","#DEE2E6"),("washer","Washing Machine","Çamaşır Makinesi","#F8F9FA"),("boxed_appliance","Boxed Appliance","Kutulu Beyaz Eşya","#D4A373")],
  [("coil_loader","Coil Loader","Rulo Yükleyici","Transport","Unloader"),("stamping","Stamping Press","Pres Hattı","Cutting","Press"),("assembly_cell","Assembly Cell","Montaj Hücresi","Assembly","RobotArm"),("boxer","Boxing Line","Kutulama Hattı","Packaging","Packer"),("warehouse","Appliance Warehouse","Beyaz Eşya Deposu","Storage","Warehouse"),("dock","Appliance Dock","Beyaz Eşya Rampası","Shipping","Dock")]),
 ("automotive","Car Factory","Otomobil Fabrikası",("#1D3557","#E63946","#C9CED6","#E9EEF4"),
  [("steel_coil","Steel Coil","Çelik Rulo","#8D99AE"),("car_body","Car Body","Kaporta","#457B9D"),("car","Car","Otomobil","#E63946"),("car_load","Car Carrier Load","Oto Taşıyıcı Yükü","#A8DADC")],
  [("coil_feeder","Coil Feeder","Rulo Besleyici","Transport","Unloader"),("body_press","Body Press","Kaporta Presi","Cutting","Press"),("assembly_robots","Assembly Robots","Montaj Robotları","Assembly","RobotArm"),("inspection","Final Inspection","Son Kontrol","Packaging","Packer"),("car_park","Car Park","Araç Parkı","Storage","Warehouse"),("carrier_dock","Car Carrier Dock","Oto Taşıyıcı Rampası","Shipping","Dock")]),
 ("solar","Solar Panel Factory","Güneş Paneli Fabrikası",("#264653","#E9C46A","#C5D1D6","#E8EEF0"),
  [("quartz","Quartz","Kuvars","#E5E5E5"),("solar_cell","Solar Cell","Güneş Hücresi","#1D3557"),("solar_panel","Solar Panel","Güneş Paneli","#264653"),("panel_pallet","Panel Pallet","Panel Paleti","#2A9D8F")],
  [("quartz_crusher","Quartz Crusher","Kuvars Kırıcı","RawProcessing","Crusher"),("ingot_furnace","Ingot Furnace","Külçe Fırını","Smelting","Furnace"),("laminator","Panel Laminator","Panel Laminatörü","Assembly","Press"),("palletizer","Palletizer","Paletleyici","Packaging","Packer"),("warehouse","Panel Warehouse","Panel Deposu","Storage","Warehouse"),("dock","Solar Dock","Güneş Paneli Rampası","Shipping","Dock")]),
 ("battery","Battery Factory","Batarya Fabrikası",("#2EC4B6","#FF9F1C","#C8D6D5","#E9F4F3"),
  [("lithium_ore","Lithium Ore","Lityum Cevheri","#CBC0D3"),("cathode","Cathode Material","Katot Malzemesi","#6C757D"),("cell","Battery Cell","Batarya Hücresi","#2EC4B6"),("battery_pack","Battery Pack","Batarya Paketi","#0B6E4F")],
  [("ore_loader","Ore Loader","Cevher Yükleyici","Transport","Unloader"),("calciner","Calciner","Kalsinasyon Fırını","Smelting","Extruder"),("cell_line","Cell Line","Hücre Hattı","Assembly","RobotArm"),("pack_assembly","Pack Assembly","Paket Montajı","Packaging","Packer"),("dry_room","Dry Room Store","Kuru Oda Deposu","Storage","Warehouse"),("dock","Battery Dock","Batarya Rampası","Shipping","Dock")]),
 ("aerospace","Aerospace Plant","Havacılık Fabrikası",("#ADB5BD","#0077B6","#CFD6DD","#EDF1F5"),
  [("aluminium","Aluminium Ingot","Alüminyum Külçe","#C0C0C0"),("alloy","Aero Alloy","Havacılık Alaşımı","#9FA8B3"),("wing_part","Wing Part","Kanat Parçası","#E0E1DD"),("aero_crate","Aero Crate","Havacılık Sandığı","#778DA9")],
  [("ingot_loader","Ingot Loader","Külçe Yükleyici","Transport","Unloader"),("alloy_furnace","Alloy Furnace","Alaşım Fırını","Smelting","Furnace"),("five_axis","5-Axis Machining","5 Eksen İşleme","Cutting","Cutter"),("crate_builder","Crate Builder","Sandık Yapıcı","Packaging","Packer"),("hangar","Hangar Store","Hangar Deposu","Storage","Warehouse"),("dock","Aerospace Dock","Havacılık Rampası","Shipping","Dock")]),
 ("robotics","Robotics Factory","Robotik Fabrikası",("#6A4C93","#8AC926","#D1CCDC","#EEEAF5"),
  [("microchip","Microchip","Mikroçip","#3A3A3A"),("servo","Servo Motor","Servo Motor","#8AC926"),("robot","Robot","Robot","#6A4C93"),("robot_crate","Robot Crate","Robot Sandığı","#C3B1E1")],
  [("chip_loader","Chip Loader","Çip Yükleyici","Transport","Unloader"),("servo_winder","Servo Winder","Servo Sarıcı","Assembly","Loom"),("robot_assembly","Robot Assembly","Robot Montajı","Assembly","RobotArm"),("crater","Robot Crater","Robot Sandıklayıcı","Packaging","Packer"),("warehouse","Robot Warehouse","Robot Deposu","Storage","Warehouse"),("dock","Robotics Dock","Robotik Rampası","Shipping","Dock")]),
 ("space","Space Technology","Uzay Teknolojisi",("#14213D","#FCA311","#BCC4D0","#E5E9F0"),
  [("titanium","Titanium","Titanyum","#B0B7C3"),("component","Space Component","Uzay Bileşeni","#E5E5E5"),("satellite","Satellite","Uydu","#FCA311"),("launch_container","Launch Container","Fırlatma Konteyneri","#14213D")],
  [("titanium_loader","Titanium Loader","Titanyum Yükleyici","Transport","Unloader"),("vacuum_furnace","Vacuum Furnace","Vakum Fırını","Smelting","Oven"),("clean_assembly","Clean-Room Assembly","Temiz Oda Montajı","Assembly","RobotArm"),("container_packer","Container Packer","Konteyner Paketleyici","Packaging","Packer"),("integration_hall","Integration Hall","Entegrasyon Salonu","Storage","Warehouse"),("launch_dock","Launch Dock","Fırlatma Rampası","Shipping","Dock")]),
]

# Limited-time event factories (rotate weekly, Thursday-Monday). Same structure, flagged as events.
EVENTS = [
 ("evt_chocolate","Chocolate Factory","Çikolata Fabrikası",("#6F4E37","#F2C14E","#D7C4B5","#F4EBE3"),
  [("cocoa","Cocoa Beans","Kakao Çekirdeği","#5C4033"),("cocoa_mass","Cocoa Mass","Kakao Kütlesi","#7B4B2A"),("chocolate_bar","Chocolate Bar","Çikolata","#4E2A1E"),("chocolate_box","Chocolate Box","Çikolata Kutusu","#C0392B")],
  [("bean_hopper","Bean Hopper","Çekirdek Bunkeri","RawProcessing","Silo"),("roaster","Roaster","Kavurma Fırını","Smelting","Oven"),("conche","Conche Mixer","Konç Karıştırıcı","Assembly","Mill"),("wrapper","Bar Wrapper","Tablet Paketleyici","Packaging","Packer"),("cool_store","Cool Store","Serin Depo","Storage","Warehouse"),("dock","Sweet Dock","Tatlı Rampası","Shipping","Dock")]),
 ("evt_toys","Toy Factory","Oyuncak Fabrikası",("#FF6B6B","#4ECDC4","#E9DCD0","#FFF4E6"),
  [("toy_pellet","Toy Pellets","Oyuncak Granülü","#FFD166"),("toy_part","Toy Parts","Oyuncak Parçası","#06D6A0"),("toy","Toy Robot","Oyuncak Robot","#EF476F"),("toy_box","Toy Box","Oyuncak Kutusu","#118AB2")],
  [("pellet_loader","Pellet Loader","Granül Yükleyici","Transport","Unloader"),("molder","Toy Molder","Oyuncak Kalıbı","Smelting","Extruder"),("toy_assembly","Toy Assembly","Oyuncak Montajı","Assembly","RobotArm"),("gift_wrapper","Gift Wrapper","Hediye Paketleyici","Packaging","Packer"),("toy_store","Toy Store","Oyuncak Deposu","Storage","Warehouse"),("dock","Toy Dock","Oyuncak Rampası","Shipping","Dock")]),
 ("evt_fireworks","Fireworks Workshop","Havai Fişek Atölyesi",("#22223B","#F4A261","#CFC8D8","#EDE7F6"),
  [("powder","Colour Powder","Renk Tozu","#E76F51"),("charge","Star Charge","Yıldız Dolgusu","#F4A261"),("rocket","Firework Rocket","Havai Fişek","#E63946"),("show_crate","Show Crate","Gösteri Sandığı","#264653")],
  [("powder_silo","Powder Silo","Toz Silosu","RawProcessing","Silo"),("star_press","Star Press","Yıldız Presi","Assembly","Press"),("rocket_line","Rocket Line","Fişek Hattı","Assembly","Cutter"),("crate_packer","Show Packer","Gösteri Paketleyici","Packaging","Packer"),("bunker","Safety Bunker","Güvenlik Sığınağı","Storage","Warehouse"),("dock","Show Dock","Gösteri Rampası","Shipping","Dock")]),
]

STAGES = ["RawInput","Processing","Production","Packaging","Storage","Shipping"]
here = os.path.dirname(os.path.abspath(__file__))
root = os.path.normpath(os.path.join(here, "..", ".."))
cs = []
cs.append("// <auto-generated> by Tools/ContentGen/factories.py — edit the script, not this file. </auto-generated>")
cs.append("namespace FactoryKing.Core.Content\n{\n    public static partial class DefaultContent\n    {")
cs.append("        /// <summary>Factories 6..20: id, theme colours, products and machine chain specs.</summary>")
cs.append("        internal static readonly ExtraFactorySpec[] ExtraFactories =\n        {")
for fid, en, tr, theme, prods, machs in F:
    p = ", ".join(f'("{fid}_{pid}" , "{c}")'.replace(" ,", ",") for pid,_,_,c in prods)
    m = ", ".join(f'("{fid}_{suf}", MachineCategory.{cat}, MachineVisual.{vis})' for suf,_,_,cat,vis in machs)
    cs.append(f'            new ExtraFactorySpec("{fid}", new[] {{ "{theme[0]}", "{theme[1]}", "{theme[2]}", "{theme[3]}" }},')
    cs.append(f'                new[] {{ {p} }},')
    cs.append(f'                new[] {{ {m} }}),')
cs.append("        };")
cs.append("        /// <summary>Limited-time event factories (see LiveEvents).</summary>")
cs.append("        internal static readonly ExtraFactorySpec[] EventFactories =\n        {")
for fid, en, tr, theme, prods, machs in EVENTS:
    p = ", ".join(f'("{fid}_{pid}", "{c}")' for pid,_,_,c in prods)
    m = ", ".join(f'("{fid}_{suf}", MachineCategory.{cat}, MachineVisual.{vis})' for suf,_,_,cat,vis in machs)
    cs.append(f'            new ExtraFactorySpec("{fid}", new[] {{ "{theme[0]}", "{theme[1]}", "{theme[2]}", "{theme[3]}" }},')
    cs.append(f'                new[] {{ {p} }},')
    cs.append(f'                new[] {{ {m} }}),')
cs.append("        };\n    }\n}")
open(os.path.join(root, "Assets/_Project/Scripts/Core/Content/ExtraFactories.cs"), "w").write("\n".join(cs) + "\n")

rows = []
for fid, en, tr, theme, prods, machs in F + EVENTS:
    rows.append((f"factory.{fid}", en, tr))
    for pid, pen, ptr, _ in prods: rows.append((f"product.{fid}_{pid}", pen, ptr))
    for suf, men, mtr, _, _ in machs: rows.append((f"machine.{fid}_{suf}", men, mtr))
path = os.path.join(root, "Assets/_Project/Resources/Localization/strings.txt")
existing = open(path, encoding="utf-8").read()
start, end = "# --- generated factories begin ---", "# --- generated factories end ---"
block = start + "\n" + "\n".join(f"{k}\t{e}\t{t}" for k, e, t in rows) + "\n" + end + "\n"
if start in existing:
    a = existing.index(start); b = existing.index(end) + len(end) + 1
    existing = existing[:a] + block + existing[b:]
else:
    existing = existing.rstrip("\n") + "\n" + block
open(path, "w", encoding="utf-8").write(existing)
# Theme table for the Unity layer
th = ["// <auto-generated> by Tools/ContentGen/factories.py </auto-generated>",
      "namespace FactoryKing.Runtime.Visuals\n{\n    public readonly partial struct Theme\n    {",
      "        private static bool TryExtra(string id, out Theme theme)\n        {\n            switch (id)\n            {"]
for fid, en, tr, theme, prods, machs in F + EVENTS:
    th.append(f'                case "{fid}": theme = new Theme("{theme[0]}", "{theme[1]}", "{theme[2]}", "{theme[3]}"); return true;')
th.append("                default: theme = default; return false;\n            }\n        }\n    }\n}")
open(os.path.join(root, "Assets/_Project/Scripts/Runtime/Visuals/ThemeExtra.cs"), "w").write("\n".join(th) + "\n")
print(len(F), "factories,", len(rows), "strings")
