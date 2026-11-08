using System;
using System.Collections.Generic;

namespace FactoryKing.Core.Economy
{
    public enum ModifierStat
    {
        /// <summary>Multiplies machine speed (cycles per second).</summary>
        ProductionSpeed,
        /// <summary>Multiplies the sale price of finished goods.</summary>
        SellPrice,
        /// <summary>Multiplies operating costs (raw, power, wages, maintenance). Below 1 = cheaper.</summary>
        OperatingCost,
        /// <summary>Multiplies output buffer / warehouse capacity.</summary>
        StorageCapacity,
        /// <summary>Multiplies offline earnings.</summary>
        OfflineEarnings,
        /// <summary>Multiplies upgrade prices. Below 1 = cheaper.</summary>
        UpgradeCost,
        /// <summary>Multiplies the truck load per shipping trip.</summary>
        ShippingCapacity,
    }

    /// <summary>
    /// Named multiplicative modifiers. Managers, prestige, research and temporary boosts register here with a unique
    /// source id and an optional scope (factory id, or null for global), so nothing needs to know about anything else.
    /// </summary>
    public sealed class ModifierStack
    {
        private struct Entry
        {
            public string Scope;
            public ModifierStat Stat;
            public double Multiplier;
        }

        private readonly Dictionary<string, List<Entry>> _bySource = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);

        public event Action Changed;

        /// <summary>Replaces every modifier from <paramref name="sourceId"/> with a single new value.</summary>
        public void Set(string sourceId, ModifierStat stat, double multiplier, string scopeFactoryId = null)
        {
            if (string.IsNullOrEmpty(sourceId)) throw new ArgumentException("sourceId required", nameof(sourceId));
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            _bySource[sourceId] = new List<Entry> { new Entry { Scope = scopeFactoryId, Stat = stat, Multiplier = multiplier } };
            Changed?.Invoke();
        }

        /// <summary>Like <see cref="Set"/> but without raising <see cref="Changed"/> (for values refreshed every second).</summary>
        public void SetQuiet(string sourceId, ModifierStat stat, double multiplier, string scopeFactoryId = null)
        {
            if (_bySource.TryGetValue(sourceId, out var list) && list.Count == 1)
            {
                list[0] = new Entry { Scope = scopeFactoryId, Stat = stat, Multiplier = multiplier };
                return;
            }
            _bySource[sourceId] = new List<Entry> { new Entry { Scope = scopeFactoryId, Stat = stat, Multiplier = multiplier } };
        }

        /// <summary>Adds another modifier to an existing source (e.g. a manager with two perks).</summary>
        public void Add(string sourceId, ModifierStat stat, double multiplier, string scopeFactoryId = null)
        {
            if (!_bySource.TryGetValue(sourceId, out var list)) _bySource[sourceId] = list = new List<Entry>();
            list.Add(new Entry { Scope = scopeFactoryId, Stat = stat, Multiplier = multiplier });
            Changed?.Invoke();
        }

        public bool Remove(string sourceId)
        {
            bool removed = _bySource.Remove(sourceId);
            if (removed) Changed?.Invoke();
            return removed;
        }

        public bool HasSource(string sourceId) => _bySource.ContainsKey(sourceId);

        /// <summary>Product of all global modifiers and those scoped to <paramref name="factoryId"/>.</summary>
        public double Get(ModifierStat stat, string factoryId)
        {
            double result = 1d;
            foreach (var list in _bySource.Values)
            {
                foreach (var e in list)
                {
                    if (e.Stat != stat) continue;
                    if (e.Scope != null && !string.Equals(e.Scope, factoryId, StringComparison.Ordinal)) continue;
                    result *= e.Multiplier;
                }
            }
            return result;
        }
    }
}
