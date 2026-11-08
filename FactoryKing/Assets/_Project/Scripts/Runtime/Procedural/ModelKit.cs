using System;
using System.Collections.Generic;
using FactoryKing.Runtime.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace FactoryKing.Runtime.Procedural
{
    /// <summary>
    /// Instantiates procedural geometry as GameObjects. Meshes are cached by key and shared between all instances
    /// (all three lines of a factory and all factories reuse the same mesh objects); only materials differ.
    /// </summary>
    public sealed class ModelKit
    {
        private readonly struct CachedMesh
        {
            public readonly Mesh Mesh;
            public readonly int[] Slots;
            public CachedMesh(Mesh mesh, int[] slots) { Mesh = mesh; Slots = slots; }
        }

        private readonly Dictionary<string, CachedMesh> _cache = new Dictionary<string, CachedMesh>();
        private readonly MeshBuilder _builder = new MeshBuilder();

        public ModelKit(MaterialLibrary materials) => Materials = materials;

        public MaterialLibrary Materials { get; }

        /// <summary>Theme materials (indexed by <see cref="Mat"/>) used for the next parts.</summary>
        public Material[] Theme { get; set; }

        public int CachedMeshCount => _cache.Count;

        public Mesh GetMesh(string key, Action<MeshBuilder> build, out int[] slots)
        {
            if (!_cache.TryGetValue(key, out var cached))
            {
                _builder.Clear();
                build(_builder);
                var mesh = _builder.ToMesh(key, out var used);
                cached = new CachedMesh(mesh, used);
                _cache[key] = cached;
            }
            slots = cached.Slots;
            return cached.Mesh;
        }

        /// <summary>Creates a part whose sub-meshes use theme materials by slot.</summary>
        public MeshRenderer Part(Transform parent, string key, Vector3 localPosition, Action<MeshBuilder> build,
            bool castShadows = true)
        {
            var mesh = GetMesh(key, build, out var slots);
            var mats = new Material[Math.Max(1, slots.Length)];
            for (int i = 0; i < slots.Length; i++) mats[i] = Theme[Mathf.Clamp(slots[i], 0, Theme.Length - 1)];
            if (slots.Length == 0) mats[0] = Theme[0];
            return Spawn(parent, key, localPosition, mesh, mats, castShadows);
        }

        /// <summary>Creates a part rendered with a single explicit material (product colours, status lights...).</summary>
        public MeshRenderer Solid(Transform parent, string key, Vector3 localPosition, Material material,
            Action<MeshBuilder> build, bool castShadows = false)
        {
            var mesh = GetMesh(key, build, out var slots);
            var mats = new Material[Math.Max(1, slots.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            return Spawn(parent, key, localPosition, mesh, mats, castShadows);
        }

        public static Transform Pivot(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static MeshRenderer Spawn(Transform parent, string name, Vector3 localPosition, Mesh mesh, Material[] mats, bool castShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return r;
        }

        public void DestroyMeshes()
        {
            foreach (var c in _cache.Values)
            {
                if (c.Mesh == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(c.Mesh);
                else UnityEngine.Object.DestroyImmediate(c.Mesh);
            }
            _cache.Clear();
        }
    }
}
