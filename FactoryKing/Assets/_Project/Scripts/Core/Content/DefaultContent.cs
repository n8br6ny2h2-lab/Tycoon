using System;
using System.Collections.Generic;

namespace FactoryKing.Core.Content
{
    /// <summary>
    /// Built-in game catalogue: 5 factories x 6 machines (30 machine variants), each factory with 3 production lines.
    /// All numbers are tuning data; the Unity layer may override them from a ScriptableObject/JSON later.
    /// </summary>
    public static partial class DefaultContent
    {
        public const string Steel = "steel";
        public const string Food = "food";
        public const string Plastic = "plastic";
        public const string Textile = "textile";
        public const string Electronics = "electronics";

        private static readonly double[] BaseLineCosts = { 0, 750, 40000 };

        /// <summary>Total number of factories (5 hand-made + 15 generated from <see cref="ExtraFactories"/>).</summary>
        public const int FactoryCount = 20;
        /// <summary>Value scale between consecutive factories.</summary>
        public const double ScaleRatio = 30;
        /// <summary>
        /// Unlock price growth between consecutive factories from the 6th on, tuned with the season pacing test so an
        /// average player (4 short sessions a day) reaches the 20th factory near the end of a 30-day season.
        /// </summary>
        public const double UnlockRatioStart = 85, UnlockRatioDecay = 0.96;

        public static GameContent Create()
        {
            var c = new GameContent();

            // ---------------- 1. Iron & Steel: ore -> molten steel -> beams -> bundles ----------------
            AddFactory(c, Steel, order: 0, scale: 1, unlockCost: 0, rawCost: 1, growth: 1.15,
                products: new[]
                {
                    P("iron_ore", "#7A5C4E", 0),
                    P("molten_steel", "#FF7A2E", 0),
                    P("steel_beam", "#9AA4AE", 0),
                    P("steel_bundle", "#B9C2CB", 12),
                },
                machines: new[]
                {
                    M("steel_ore_unloader", MachineCategory.Transport, StageType.RawInput, MachineVisual.Unloader, null, 1, "iron_ore", 1, 1.0, 12, 8, 1.0, 0.005),
                    M("steel_blast_furnace", MachineCategory.Smelting, StageType.Processing, MachineVisual.Furnace, "iron_ore", 2, "molten_steel", 1, 2.0, 8, 15, 6.0, 0.02),
                    M("steel_beam_cutter", MachineCategory.Cutting, StageType.Production, MachineVisual.Cutter, "molten_steel", 1, "steel_beam", 1, 2.2, 8, 20, 3.0, 0.015),
                    M("steel_bundler", MachineCategory.Packaging, StageType.Packaging, MachineVisual.Packer, "steel_beam", 1, "steel_bundle", 1, 1.6, 8, 12, 1.5, 0.01),
                    M("steel_yard", MachineCategory.Storage, StageType.Storage, MachineVisual.Warehouse, "steel_bundle", 1, "steel_bundle", 1, 0.6, 30, 10, 0.5, 0.005),
                    M("steel_flatbed_dock", MachineCategory.Shipping, StageType.Shipping, MachineVisual.Dock, "steel_bundle", 4, "steel_bundle", 4, 8.0, 0, 25, 0.5, 0.01),
                });

            // ---------------- 2. Food: wheat -> flour -> bread -> bread crates ----------------
            AddFactory(c, Food, order: 1, scale: 30, unlockCost: 40000, rawCost: 0.5, growth: 1.16,
                products: new[]
                {
                    P("wheat", "#E3C567", 0),
                    P("flour", "#F4EEDC", 0),
                    P("bread", "#C98A3D", 0),
                    P("bread_crate", "#D9A35B", 9),
                },
                machines: new[]
                {
                    M("food_grain_silo", MachineCategory.RawProcessing, StageType.RawInput, MachineVisual.Silo, null, 1, "wheat", 1, 0.6, 16, 8, 0.8, 0.005),
                    M("food_flour_mill", MachineCategory.RawProcessing, StageType.Processing, MachineVisual.Mill, "wheat", 2, "flour", 1, 1.4, 10, 14, 4.0, 0.015),
                    M("food_bread_oven", MachineCategory.Smelting, StageType.Production, MachineVisual.Oven, "flour", 1, "bread", 2, 2.6, 12, 22, 5.0, 0.02),
                    M("food_wrapper", MachineCategory.Packaging, StageType.Packaging, MachineVisual.Packer, "bread", 2, "bread_crate", 1, 1.5, 10, 12, 1.2, 0.01),
                    M("food_cold_store", MachineCategory.Storage, StageType.Storage, MachineVisual.Warehouse, "bread_crate", 1, "bread_crate", 1, 0.5, 40, 10, 2.0, 0.006),
                    M("food_reefer_dock", MachineCategory.Shipping, StageType.Shipping, MachineVisual.Dock, "bread_crate", 6, "bread_crate", 6, 7.0, 0, 24, 0.8, 0.01),
                });

            // ---------------- 3. Plastic: crude feed -> granules -> moulded goods -> boxes ----------------
            AddFactory(c, Plastic, order: 2, scale: 900, unlockCost: 6000000, rawCost: 1.2, growth: 1.17,
                products: new[]
                {
                    P("crude_feed", "#2B2B33", 0),
                    P("plastic_granule", "#4FC3F7", 0),
                    P("plastic_part", "#29B6F6", 0),
                    P("plastic_box", "#81D4FA", 15),
                },
                machines: new[]
                {
                    M("plastic_crude_pump", MachineCategory.Transport, StageType.RawInput, MachineVisual.Pump, null, 1, "crude_feed", 1, 0.8, 14, 9, 2.0, 0.006),
                    M("plastic_pelletizer", MachineCategory.Smelting, StageType.Processing, MachineVisual.Extruder, "crude_feed", 1, "plastic_granule", 1, 1.8, 10, 16, 5.5, 0.018),
                    M("plastic_injection_molder", MachineCategory.Assembly, StageType.Production, MachineVisual.Press, "plastic_granule", 2, "plastic_part", 1, 2.4, 10, 21, 6.0, 0.02),
                    M("plastic_box_packer", MachineCategory.Packaging, StageType.Packaging, MachineVisual.Packer, "plastic_part", 1, "plastic_box", 1, 1.4, 10, 12, 1.2, 0.01),
                    M("plastic_warehouse", MachineCategory.Storage, StageType.Storage, MachineVisual.Warehouse, "plastic_box", 1, "plastic_box", 1, 0.55, 36, 10, 0.6, 0.005),
                    M("plastic_container_dock", MachineCategory.Shipping, StageType.Shipping, MachineVisual.Dock, "plastic_box", 5, "plastic_box", 5, 7.5, 0, 25, 0.6, 0.01),
                });

            // ---------------- 4. Textile: cotton -> fabric -> garments -> folded packs ----------------
            AddFactory(c, Textile, order: 3, scale: 27000, unlockCost: 400000000, rawCost: 0.8, growth: 1.18,
                products: new[]
                {
                    P("cotton", "#F5F5F0", 0),
                    P("fabric", "#E57373", 0),
                    P("garment", "#BA68C8", 0),
                    P("garment_pack", "#9575CD", 14),
                },
                machines: new[]
                {
                    M("textile_cotton_gin", MachineCategory.RawProcessing, StageType.RawInput, MachineVisual.Crusher, null, 1, "cotton", 1, 0.7, 16, 9, 1.5, 0.006),
                    M("textile_loom", MachineCategory.Assembly, StageType.Processing, MachineVisual.Loom, "cotton", 2, "fabric", 1, 1.6, 10, 15, 3.5, 0.016),
                    M("textile_cut_and_sew", MachineCategory.Cutting, StageType.Production, MachineVisual.Cutter, "fabric", 1, "garment", 1, 2.3, 10, 21, 3.0, 0.018),
                    M("textile_folder", MachineCategory.Packaging, StageType.Packaging, MachineVisual.Packer, "garment", 1, "garment_pack", 1, 1.5, 10, 12, 1.0, 0.01),
                    M("textile_warehouse", MachineCategory.Storage, StageType.Storage, MachineVisual.Warehouse, "garment_pack", 1, "garment_pack", 1, 0.55, 36, 10, 0.5, 0.005),
                    M("textile_box_truck_dock", MachineCategory.Shipping, StageType.Shipping, MachineVisual.Dock, "garment_pack", 5, "garment_pack", 5, 7.5, 0, 25, 0.5, 0.01),
                });

            // ---------------- 5. Electronics: silicon -> circuits -> devices -> retail boxes ----------------
            AddFactory(c, Electronics, order: 4, scale: 810000, unlockCost: 30000000000, rawCost: 1.5, growth: 1.19,
                products: new[]
                {
                    P("silicon_wafer", "#90A4AE", 0),
                    P("circuit_board", "#43A047", 0),
                    P("smart_device", "#37474F", 0),
                    P("device_box", "#ECEFF1", 18),
                },
                machines: new[]
                {
                    M("elec_wafer_loader", MachineCategory.Transport, StageType.RawInput, MachineVisual.Unloader, null, 1, "silicon_wafer", 1, 0.9, 12, 10, 1.5, 0.006),
                    M("elec_pcb_etcher", MachineCategory.Cutting, StageType.Processing, MachineVisual.Press, "silicon_wafer", 1, "circuit_board", 1, 2.0, 8, 17, 4.5, 0.02),
                    M("elec_robot_assembly", MachineCategory.Assembly, StageType.Production, MachineVisual.RobotArm, "circuit_board", 2, "smart_device", 1, 2.5, 8, 24, 7.0, 0.025),
                    M("elec_retail_packer", MachineCategory.Packaging, StageType.Packaging, MachineVisual.Packer, "smart_device", 1, "device_box", 1, 1.5, 8, 13, 1.2, 0.01),
                    M("elec_cleanroom_store", MachineCategory.Storage, StageType.Storage, MachineVisual.Warehouse, "device_box", 1, "device_box", 1, 0.6, 30, 11, 1.5, 0.006),
                    M("elec_courier_dock", MachineCategory.Shipping, StageType.Shipping, MachineVisual.Dock, "device_box", 4, "device_box", 4, 8.0, 0, 26, 0.5, 0.01),
                });

            // ---------------- 6..20: generated industries ----------------
            double unlock = 30000000000;
            for (int i = 0; i < ExtraFactories.Length; i++)
            {
                var spec = ExtraFactories[i];
                int order = 5 + i;
                unlock *= UnlockRatioStart * System.Math.Pow(UnlockRatioDecay, i); // steep mid-game, gentler finish
                double scale = System.Math.Pow(ScaleRatio, order);
                double cycleMul = 1 + 0.04 * (i % 4);
                double kwMul = 1 + 0.12 * order;
                bool doubleProduction = i % 2 == 0;
                string[] p = { spec.Products[0].id, spec.Products[1].id, spec.Products[2].id, spec.Products[3].id };
                AddFactory(c, spec.Id, order, scale, unlock, rawCost: 0.8 + 0.1 * (i % 5), growth: 1.20,
                    products: new[]
                    {
                        P(p[0], spec.Products[0].color, 0),
                        P(p[1], spec.Products[1].color, 0),
                        P(p[2], spec.Products[2].color, 0),
                        P(p[3], spec.Products[3].color, doubleProduction ? 9 + i % 4 : 14 + i % 5),
                    },
                    machines: new[]
                    {
                        M(spec.Machines[0].id, spec.Machines[0].cat, StageType.RawInput, spec.Machines[0].visual, null, 1, p[0], 1, 0.8 * cycleMul, 14, 9, 1.5 * kwMul, 0.006),
                        M(spec.Machines[1].id, spec.Machines[1].cat, StageType.Processing, spec.Machines[1].visual, p[0], 2, p[1], 1, 1.8 * cycleMul, 10, 16, 5 * kwMul, 0.018),
                        M(spec.Machines[2].id, spec.Machines[2].cat, StageType.Production, spec.Machines[2].visual, p[1], 1, p[2], doubleProduction ? 2 : 1, 2.4 * cycleMul, 12, 22, 5 * kwMul, 0.02),
                        M(spec.Machines[3].id, spec.Machines[3].cat, StageType.Packaging, spec.Machines[3].visual, p[2], doubleProduction ? 2 : 1, p[3], 1, 1.5 * cycleMul, 10, 12, 1.2 * kwMul, 0.01),
                        M(spec.Machines[4].id, spec.Machines[4].cat, StageType.Storage, spec.Machines[4].visual, p[3], 1, p[3], 1, 0.55, 36, 10, 0.6 * kwMul, 0.005),
                        M(spec.Machines[5].id, spec.Machines[5].cat, StageType.Shipping, spec.Machines[5].visual, p[3], 5, p[3], 5, 7.5 * cycleMul, 0, 25, 0.5 * kwMul, 0.01),
                    });
                c.Factories[c.Factories.Count - 1].ThemeId = spec.Id;
            }

            c.InvalidateLookups();
            return c;
        }

        private static ProductDef P(string id, string color, double value) =>
            new ProductDef { Id = id, NameKey = "product." + id, ColorHex = color, BaseValue = value };

        private static MachineDef M(string id, MachineCategory cat, StageType stage, MachineVisual visual,
            string input, int inPer, string output, int outPer, double cycle, int capacity, double upgradeCost,
            double powerKw, double maintenance) =>
            new MachineDef
            {
                Id = id, NameKey = "machine." + id, Category = cat, Stage = stage, Visual = visual,
                InputProductId = input, InputPerCycle = inPer, OutputProductId = output, OutputPerCycle = outPer,
                BaseCycleSeconds = cycle, BaseCapacity = capacity, BaseUpgradeCost = upgradeCost,
                PowerKw = powerKw, MaintenancePerSecond = maintenance,
            };

        /// <summary>Registers a factory; monetary values of products/machines are multiplied by <paramref name="scale"/>.</summary>
        private static void AddFactory(GameContent c, string id, int order, double scale, double unlockCost, double rawCost,
            double growth, ProductDef[] products, MachineDef[] machines)
        {
            var lineCosts = new double[BaseLineCosts.Length];
            for (int i = 0; i < lineCosts.Length; i++) lineCosts[i] = BaseLineCosts[i] * scale;

            var factory = new FactoryDef
            {
                Id = id,
                NameKey = "factory." + id,
                ThemeId = id,
                Order = order,
                UnlockCost = unlockCost,
                LineUnlockCosts = lineCosts,
                RawMaterialCost = rawCost * scale,
                ElectricityPricePerKwSecond = 0.01 * scale,
                WagePerMachineSecond = 0.02 * scale,
            };

            foreach (var p in products)
            {
                p.BaseValue *= scale;
                c.Products.Add(p);
            }
            double chainKw = 0;
            foreach (var m in machines)
            {
                chainKw += m.PowerKw;
                m.UpgradeCostGrowth = growth; // later factories get steeper upgrade curves
                m.BaseUpgradeCost *= scale;
                m.MaintenancePerSecond *= scale;
                c.Machines.Add(m);
                factory.MachineChain.Add(m.Id);
            }
            // The power plant comfortably feeds one level-1 line; more lines and upgrades need plant upgrades.
            factory.BasePowerCapacityKw = Math.Round(chainKw * 1.6, 1);
            factory.PowerUpgradeBaseCost = 150 * scale;
            c.Factories.Add(factory);
        }
    }
}

namespace FactoryKing.Core.Content
{
    /// <summary>Compact description of a generated factory (see Tools/ContentGen/factories.py).</summary>
    internal sealed class ExtraFactorySpec
    {
        public readonly string Id;
        public readonly string[] Theme;
        public readonly (string id, string color)[] Products;
        public readonly (string id, MachineCategory cat, MachineVisual visual)[] Machines;

        public ExtraFactorySpec(string id, string[] theme, (string id, string color)[] products,
            (string id, MachineCategory cat, MachineVisual visual)[] machines)
        {
            Id = id;
            Theme = theme;
            Products = products;
            Machines = machines;
        }
    }
}
