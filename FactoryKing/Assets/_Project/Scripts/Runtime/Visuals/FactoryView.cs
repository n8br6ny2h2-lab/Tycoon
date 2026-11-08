using System;
using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Economy;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Pooling;
using FactoryKing.Runtime.Procedural;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>
    /// Presents the active factory: hall, production lines, machines, belts, workers, locked plots.
    /// One Update drives every visual of the factory (no per-machine MonoBehaviour Update calls).
    /// </summary>
    public sealed class FactoryView : MonoBehaviour
    {
        public const float MachineSpacing = 4.0f;
        public const float LineSpacing = 6.5f;
        private const int WorkersPerLine = 2;

        private sealed class LineVisual
        {
            public int Index;
            public Transform Root;
            public MachineView[] Machines;
            public ConveyorView[] Conveyors;
            public Material BeltMaterial;
            public float BeltOffset;
            public float BeltSpeed;
            public int[] Capacity;
            public int[] Tier;
            public float[] SpeedVisual;
            public readonly List<WorkerView> Workers = new List<WorkerView>();
            public Transform LockedPlot;
        }

        private GameSession _session;
        private MaterialLibrary _materials;
        private ModelKit _kit;
        private MachineModelFactory _machines;
        private EnvironmentBuilder _environment;
        private Material[] _theme;
        private FactoryDef _factory;
        private Transform _root;
        private Transform _poolStorage;
        private readonly List<LineVisual> _lines = new List<LineVisual>();
        private readonly Dictionary<string, ComponentPool<MeshRenderer>> _itemPools = new Dictionary<string, ComponentPool<MeshRenderer>>();
        private ParticleSystem _sparkles;
        private float _night;

        public string FactoryId => _factory?.Id;
        public Bounds WorldBounds { get; private set; }

        public event Action<string> Rebuilt;

        public void Init(GameSession session, MaterialLibrary materials, ModelKit kit)
        {
            _session = session;
            _materials = materials;
            _kit = kit;
            _machines = new MachineModelFactory(kit);
            _environment = new EnvironmentBuilder(kit);

            _session.Upgrades.MachineUpgraded += OnMachineUpgraded;
            _session.Factories.LineUnlocked += OnLineUnlocked;
            _session.Factories.ActiveFactoryChanged += Build;
        }

        private void OnDestroy()
        {
            if (_session == null) return;
            _session.Upgrades.MachineUpgraded -= OnMachineUpgraded;
            _session.Factories.LineUnlocked -= OnLineUnlocked;
            _session.Factories.ActiveFactoryChanged -= Build;
        }

        public void Build(string factoryId)
        {
            Teardown();
            _factory = _session.Content.GetFactory(factoryId);
            _theme = _materials.ForTheme(_factory.ThemeId);
            _kit.Theme = _theme;

            _root = ModelKit.Pivot(transform, "Factory." + factoryId, Vector3.zero);
            _poolStorage = ModelKit.Pivot(_root, "Pool", Vector3.zero);
            _poolStorage.gameObject.SetActive(true);

            int machineCount = _factory.MachineChain.Count;
            int lineCount = _factory.LineCount;
            var hallMin = new Vector2(-2.8f, -(lineCount - 1) * LineSpacing - 3.1f);
            var hallMax = new Vector2((machineCount - 1) * MachineSpacing - 1.6f, 3.0f);
            _environment.Build(_root, hallMin, hallMax);
            _environment.BuildDecorations(_root, _session.State.Collection, hallMin, hallMax);

            var fs = _session.State.GetFactory(factoryId);
            for (int l = 0; l < lineCount; l++)
            {
                var lv = new LineVisual { Index = l, Root = ModelKit.Pivot(_root, "Line" + l, new Vector3(0, 0, -l * LineSpacing)) };
                _lines.Add(lv);
                if (fs.Lines[l].Unlocked) BuildLine(lv, fs.Lines[l]);
                else BuildLockedPlot(lv);
            }

            CreateSparkles();
            WorldBounds = new Bounds(
                new Vector3((hallMin.x + hallMax.x) * 0.5f + 4f, 0, (hallMin.y + hallMax.y) * 0.5f),
                new Vector3(hallMax.x - hallMin.x + 16f, 4f, hallMax.y - hallMin.y + 8f));
            _environment.SetNight(_materials, _theme, _night);
            Rebuilt?.Invoke(factoryId);
        }

        private void BuildLine(LineVisual lv, LineState state)
        {
            if (lv.LockedPlot != null) { Destroy(lv.LockedPlot.gameObject); lv.LockedPlot = null; }
            int n = _factory.MachineChain.Count;
            lv.Machines = new MachineView[n];
            lv.Conveyors = new ConveyorView[n - 1];
            lv.Capacity = new int[n];
            lv.Tier = new int[n];
            lv.SpeedVisual = new float[n];
            lv.BeltMaterial = _materials.Clone(_materials.BeltMaterial, $"Belt.{_factory.Id}.{lv.Index}");

            var stations = new float[n];
            for (int i = 0; i < n; i++)
            {
                var def = _session.Content.GetMachine(_factory.MachineChain[i]);
                var inMat = def.InputProductId != null ? ProductMaterial(def.InputProductId) : ProductMaterial(def.OutputProductId);
                var outMat = ProductMaterial(def.OutputProductId);
                var view = _machines.Build(lv.Root, new MachineRef(_factory.Id, lv.Index, i), def, inMat, outMat);
                view.transform.localPosition = new Vector3(i * MachineSpacing, 0, 0);
                lv.Machines[i] = view;
                stations[i] = i * MachineSpacing;
                RefreshMachineCache(lv, i, state.Machines[i].Level);
                SetupLod(view);
            }

            for (int i = 0; i < n - 1; i++)
            {
                float x0 = i * MachineSpacing + MachineModelFactory.HalfLength;
                float x1 = (i + 1) * MachineSpacing - MachineModelFactory.HalfLength;
                BuildBelt(lv, x0, x1);
                var itemProduct = _session.Content.GetMachine(_factory.MachineChain[i]).OutputProductId;
                lv.Conveyors[i] = new ConveyorView(lv.Root, ItemPool(itemProduct),
                    new Vector3(x0, MachineModelFactory.BeltHeight + 0.14f, 0), new Vector3(x1, MachineModelFactory.BeltHeight + 0.14f, 0));
            }

            for (int w = 0; w < WorkersPerLine; w++)
                lv.Workers.Add(new WorkerView(_kit, lv.Root, stations, -2.0f, _factory.Order * 100 + lv.Index * 10 + w));
        }

        private void BuildBelt(LineVisual lv, float x0, float x1)
        {
            float len = x1 - x0;
            float mid = (x0 + x1) * 0.5f;
            _kit.Part(lv.Root, $"Belt.Frame.{len:0.00}", new Vector3(mid, 0, 0), b =>
            {
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, MachineModelFactory.BeltHeight - 0.02f, -0.36f), new Vector3(len, 0.16f, 0.07f), 0.01f);
                b.Box(new Vector3(0, MachineModelFactory.BeltHeight - 0.02f, 0.36f), new Vector3(len, 0.16f, 0.07f), 0.01f);
                b.Slot = (int)Mat.Hazard;
                for (float x = -len * 0.5f + 0.2f; x < len * 0.5f; x += 0.9f)
                {
                    b.Box(new Vector3(x, MachineModelFactory.BeltHeight * 0.5f, -0.3f), new Vector3(0.07f, MachineModelFactory.BeltHeight, 0.07f), 0f);
                    b.Box(new Vector3(x, MachineModelFactory.BeltHeight * 0.5f, 0.3f), new Vector3(0.07f, MachineModelFactory.BeltHeight, 0.07f), 0f);
                }
            });
            var belt = _kit.Solid(lv.Root, $"Belt.Surface.{len:0.00}", new Vector3(mid, 0, 0), lv.BeltMaterial, b =>
            {
                float y = MachineModelFactory.BeltHeight + 0.05f, h = len * 0.5f;
                // UV.x along the belt in world units so all belts share the same rib spacing.
                b.QuadUV(new Vector3(-h, y, -0.33f), new Vector3(h, y, -0.33f), new Vector3(h, y, 0.33f), new Vector3(-h, y, 0.33f),
                    new Vector2(0, 0), new Vector2(len, 0), new Vector2(len, 1), new Vector2(0, 1), Vector3.up);
            }, false);
            belt.receiveShadows = true;
        }

        private void BuildLockedPlot(LineVisual lv)
        {
            float length = (_factory.MachineChain.Count - 1) * MachineSpacing + 2.8f;
            var plot = _kit.Part(lv.Root, $"LockedPlot.{length:0.0}", new Vector3(length * 0.5f - 1.4f, 0, 0), b =>
            {
                b.Slot = (int)Mat.Concrete;
                b.Box(new Vector3(0, 0.02f, 0), new Vector3(length, 0.04f, 3.2f), 0f);
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.05f, -1.6f), new Vector3(length, 0.02f, 0.12f), 0f);
                b.Box(new Vector3(0, 0.05f, 1.6f), new Vector3(length, 0.02f, 0.12f), 0f);
                b.Box(new Vector3(-length * 0.5f, 0.05f, 0), new Vector3(0.12f, 0.02f, 3.2f), 0f);
                b.Box(new Vector3(length * 0.5f, 0.05f, 0), new Vector3(0.12f, 0.02f, 3.2f), 0f);
                for (float x = -length * 0.5f + 1f; x < length * 0.5f; x += 2.5f)
                {
                    b.Slot = (int)Mat.Hazard;
                    b.Cylinder(new Vector3(x, 0.02f, -1.6f), 0.08f, 0.7f, 8);
                    b.Slot = (int)Mat.Accent;
                    b.Cylinder(new Vector3(x, 0.72f, -1.6f), 0.1f, 0.06f, 8);
                }
                // "For sale" sign board
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, 0.7f, 0), new Vector3(0.1f, 1.4f, 0.1f), 0f);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, 1.6f, 0), new Vector3(2.4f, 0.9f, 0.1f), 0.04f);
                b.Slot = (int)Mat.White;
                b.Box(new Vector3(0, 1.6f, -0.06f), new Vector3(2.0f, 0.12f, 0.02f), 0f);
                b.Box(new Vector3(0, 1.6f, -0.06f), new Vector3(0.12f, 0.6f, 0.02f), 0f); // "+" mark
            });
            lv.LockedPlot = plot.transform;
        }

        private void SetupLod(MachineView view)
        {
            var renderers = view.GetComponentsInChildren<Renderer>(true);
            var bodyOnly = new List<Renderer>();
            foreach (var r in renderers)
                if (r.name.EndsWith(".Body") || r.name == "Plinth" || r.name.EndsWith(".Racks")) bodyOnly.Add(r);
            var lod = view.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[]
            {
                new LOD(0.10f, renderers),
                new LOD(0.015f, bodyOnly.ToArray()),
            });
            lod.RecalculateBounds();
        }

        private Material ProductMaterial(string productId) => _materials.ForColor(_session.Content.GetProduct(productId).ColorHex);

        private ComponentPool<MeshRenderer> ItemPool(string productId)
        {
            if (_itemPools.TryGetValue(productId, out var pool)) return pool;
            var mat = ProductMaterial(productId);
            pool = new ComponentPool<MeshRenderer>(() =>
            {
                var r = _kit.Solid(_poolStorage, "Item", Vector3.zero, mat, b => b.Box(Vector3.zero, new Vector3(0.3f, 0.24f, 0.3f), 0.04f), false);
                return r;
            }, _poolStorage);
            _itemPools[productId] = pool;
            return pool;
        }

        private void CreateSparkles()
        {
            var go = new GameObject("UpgradeSparkles");
            go.transform.SetParent(_root, false);
            _sparkles = go.AddComponent<ParticleSystem>();
            _sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _sparkles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 120;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = _sparkles.emission;
            emission.enabled = false;
            var shape = _sparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = ParticleMaterials.Sparkle;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void RefreshMachineCache(LineVisual lv, int i, int level)
        {
            var def = _session.Content.GetMachine(_factory.MachineChain[i]);
            var t = _session.Tuning;
            lv.Capacity[i] = UpgradeMath.Capacity(def, level, t, _session.Modifiers.Get(ModifierStat.StorageCapacity, _factory.Id));
            lv.Tier[i] = UpgradeMath.VisualTier(level, t);
            lv.SpeedVisual[i] = (float)Math.Sqrt(UpgradeMath.SpeedMultiplier(level, t));

            float slowest = float.MaxValue;
            for (int k = 0; k < lv.SpeedVisual.Length; k++)
                if (lv.SpeedVisual[k] > 0) slowest = Mathf.Min(slowest, lv.SpeedVisual[k]);
            lv.BeltSpeed = Mathf.Clamp(1.1f * (slowest == float.MaxValue ? 1f : slowest), 0.8f, 5f);
        }

        private void OnMachineUpgraded(MachineRef r, int oldLevel, int newLevel)
        {
            if (_factory == null || r.FactoryId != _factory.Id) return;
            var lv = _lines[r.Line];
            if (lv.Machines == null) return;
            RefreshMachineCache(lv, r.Index, newLevel);
            var view = lv.Machines[r.Index];
            view.PlayUpgradeEffect();
            if (_sparkles != null)
            {
                _sparkles.transform.position = view.transform.position + Vector3.up * 1.5f;
                _sparkles.Emit(UpgradeMath.VisualTier(newLevel, _session.Tuning) > UpgradeMath.VisualTier(oldLevel, _session.Tuning) ? 90 : 35);
            }
        }

        private void OnLineUnlocked(string factoryId, int line)
        {
            if (_factory == null || factoryId != _factory.Id) return;
            BuildLine(_lines[line], _session.State.GetFactory(factoryId).Lines[line]);
        }

        public MachineView GetMachineView(MachineRef r)
        {
            if (_factory == null || r.FactoryId != _factory.Id || r.Line >= _lines.Count) return null;
            var m = _lines[r.Line].Machines;
            return m != null && r.Index < m.Length ? m[r.Index] : null;
        }

        /// <summary>World position of a locked line's plot (for camera focus / UI anchors).</summary>
        public Vector3 LinePosition(int line) => new Vector3((_factory.MachineChain.Count - 1) * MachineSpacing * 0.5f, 0, -line * LineSpacing);

        public void SetNightAmount(float night)
        {
            _night = night;
            if (_theme != null) _environment.SetNight(_materials, _theme, night);
        }

        private void Update()
        {
            if (_session == null || _factory == null) return;
            float dt = Time.deltaTime;
            var fs = _session.State.GetFactory(_factory.Id);

            for (int l = 0; l < _lines.Count; l++)
            {
                var lv = _lines[l];
                if (lv.Machines == null) continue;
                var line = fs.Lines[l];

                bool anyWorking = false;
                for (int i = 0; i < lv.Machines.Length; i++)
                {
                    var ms = line.Machines[i];
                    lv.Machines[i].Tick(ms, lv.Capacity[i], lv.Tier[i], lv.SpeedVisual[i], dt);
                    anyWorking |= ms.Status == MachineStatus.Working;
                }
                for (int i = 0; i < lv.Conveyors.Length; i++) lv.Conveyors[i].Tick(line.Machines[i], lv.BeltSpeed, dt);
                foreach (var w in lv.Workers) w.Tick(line, dt);

                if (anyWorking)
                {
                    // Belt texture repeats once per world unit (32 px rib texture, ribs 0.5 apart).
                    lv.BeltOffset = Mathf.Repeat(lv.BeltOffset - lv.BeltSpeed * dt, 1f);
                    lv.BeltMaterial.mainTextureOffset = new Vector2(lv.BeltOffset, 0f);
                }
            }
        }

        private void Teardown()
        {
            foreach (var lv in _lines)
            {
                if (lv.Conveyors != null) foreach (var c in lv.Conveyors) c?.Clear();
                foreach (var w in lv.Workers) w.Destroy();
                if (lv.BeltMaterial != null) Destroy(lv.BeltMaterial);
            }
            _lines.Clear();
            _itemPools.Clear();
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
            _factory = null;
        }
    }
}
