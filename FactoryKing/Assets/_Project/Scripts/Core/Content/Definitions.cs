using System;
using System.Collections.Generic;

namespace FactoryKing.Core.Content
{
    /// <summary>The six stages every production line passes through.</summary>
    public enum StageType
    {
        RawInput = 0,
        Processing = 1,
        Production = 2,
        Packaging = 3,
        Storage = 4,
        Shipping = 5,
    }

    public enum MachineCategory
    {
        RawProcessing,
        Smelting,
        Cutting,
        Assembly,
        Packaging,
        Transport,
        Storage,
        Shipping,
    }

    /// <summary>Procedural visual archetype used by the Unity layer to build a machine model.</summary>
    public enum MachineVisual
    {
        Unloader,
        Silo,
        Pump,
        Crusher,
        Furnace,
        Oven,
        Extruder,
        Mill,
        Cutter,
        Press,
        Loom,
        RobotArm,
        Packer,
        Warehouse,
        Dock,
    }

    public sealed class ProductDef
    {
        public string Id;
        public string NameKey;
        /// <summary>Base sale value of one unit (only used for the final product of a line).</summary>
        public double BaseValue;
        /// <summary>Hex colour hint for the 3D crate/item visual, e.g. "#C0C6CC".</summary>
        public string ColorHex;
    }

    public sealed class MachineDef
    {
        public string Id;
        public string NameKey;
        public MachineCategory Category;
        public StageType Stage;
        public MachineVisual Visual;

        /// <summary>Product consumed per cycle; null for raw input machines (they buy raw material).</summary>
        public string InputProductId;
        public int InputPerCycle = 1;
        /// <summary>Product produced per cycle. For shipping machines this is the product being sold.</summary>
        public string OutputProductId;
        public int OutputPerCycle = 1;

        public double BaseCycleSeconds = 1;
        /// <summary>Output buffer size at level 1 (storage machines: warehouse size).</summary>
        public int BaseCapacity = 10;

        public double BaseUpgradeCost = 10;
        /// <summary>Geometric cost growth per level; 1.07 - 1.20 is typical.</summary>
        public double UpgradeCostGrowth = 1.15;

        /// <summary>Electrical load while running, kW.</summary>
        public double PowerKw = 1;
        /// <summary>Maintenance cost per running second at level 1.</summary>
        public double MaintenancePerSecond = 0.01;
    }

    public sealed class FactoryDef
    {
        public string Id;
        public string NameKey;
        /// <summary>Visual theme id for the Unity layer (palette, props).</summary>
        public string ThemeId;
        public int Order;

        /// <summary>Cash required to buy this factory. 0 for the starting factory.</summary>
        public double UnlockCost;
        /// <summary>Unlock price of each production line (index 0 is included with the factory).</summary>
        public double[] LineUnlockCosts = { 0 };

        /// <summary>Ordered machine chain of one line: RawInput -> ... -> Shipping.</summary>
        public List<string> MachineChain = new List<string>();

        /// <summary>Price of one raw material unit bought by the RawInput machine.</summary>
        public double RawMaterialCost = 1;
        /// <summary>Electricity price per kW per running second.</summary>
        public double ElectricityPricePerKwSecond = 0.01;
        /// <summary>Operator wage per running machine per second.</summary>
        public double WagePerMachineSecond = 0.02;

        public int LineCount => LineUnlockCosts.Length;
    }

    /// <summary>Immutable-by-convention catalogue of everything the game can contain.</summary>
    public sealed class GameContent
    {
        public readonly List<ProductDef> Products = new List<ProductDef>();
        public readonly List<MachineDef> Machines = new List<MachineDef>();
        public readonly List<FactoryDef> Factories = new List<FactoryDef>();

        private Dictionary<string, ProductDef> _products;
        private Dictionary<string, MachineDef> _machines;
        private Dictionary<string, FactoryDef> _factories;

        public ProductDef GetProduct(string id) => Lookup(ref _products, Products, p => p.Id, id, "product");
        public MachineDef GetMachine(string id) => Lookup(ref _machines, Machines, m => m.Id, id, "machine");
        public FactoryDef GetFactory(string id) => Lookup(ref _factories, Factories, f => f.Id, id, "factory");

        public bool TryGetFactory(string id, out FactoryDef def)
        {
            def = null;
            if (id == null) return false;
            if (_factories == null)
            {
                _factories = new Dictionary<string, FactoryDef>(StringComparer.Ordinal);
                foreach (var f in Factories) _factories[f.Id] = f;
            }
            return _factories.TryGetValue(id, out def);
        }

        /// <summary>Call after mutating the lists so lookups are rebuilt.</summary>
        public void InvalidateLookups()
        {
            _products = null;
            _machines = null;
            _factories = null;
        }

        private static T Lookup<T>(ref Dictionary<string, T> cache, List<T> source, Func<T, string> key, string id, string kind)
        {
            if (cache == null)
            {
                cache = new Dictionary<string, T>(StringComparer.Ordinal);
                foreach (var item in source) cache[key(item)] = item;
            }
            if (id != null && cache.TryGetValue(id, out var value)) return value;
            throw new KeyNotFoundException($"Unknown {kind} id '{id}'.");
        }
    }
}
