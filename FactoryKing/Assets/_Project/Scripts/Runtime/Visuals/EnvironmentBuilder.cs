using System.Collections.Generic;
using FactoryKing.Runtime.Procedural;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>Static surroundings of a factory: hall, walls with windows, yard, road, trees and lamps.</summary>
    public sealed class EnvironmentBuilder
    {
        private readonly ModelKit _kit;

        public EnvironmentBuilder(ModelKit kit) => _kit = kit;

        public readonly List<Renderer> Windows = new List<Renderer>();
        public readonly List<Renderer> Lamps = new List<Renderer>();
        public readonly List<Light> LampLights = new List<Light>();

        /// <summary>Builds around a hall spanning <paramref name="hallMin"/>..<paramref name="hallMax"/> on XZ.</summary>
        public Transform Build(Transform parent, Vector2 hallMin, Vector2 hallMax)
        {
            Windows.Clear();
            Lamps.Clear();
            LampLights.Clear();
            var root = ModelKit.Pivot(parent, "Environment", Vector3.zero);
            Vector2 size = hallMax - hallMin;
            Vector2 center = (hallMin + hallMax) * 0.5f;

            _kit.Part(root, "Env.Ground", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Ground;
                b.Box(new Vector3(0, -0.06f, 0), new Vector3(260f, 0.1f, 260f), 0f);
            }, false);

            // Hall floor and outer yard (keys include size so different layouts don't share wrong meshes).
            string key = $"{size.x:0.0}x{size.y:0.0}";
            _kit.Part(root, "Env.Hall." + key, new Vector3(center.x, 0, center.y), b =>
            {
                b.Slot = (int)Mat.Floor;
                b.Box(new Vector3(0, -0.02f, 0), new Vector3(size.x, 0.08f, size.y), 0.02f);
                b.Slot = (int)Mat.Concrete;
                b.Box(new Vector3(0, -0.04f, 0), new Vector3(size.x + 1.2f, 0.07f, size.y + 1.2f), 0.02f);
                // Back (north) wall and west wall, low enough to keep the isometric view clear.
                b.Slot = (int)Mat.Wall;
                float h = 3.6f;
                b.Box(new Vector3(0, h * 0.5f, size.y * 0.5f + 0.15f), new Vector3(size.x + 0.6f, h, 0.3f), 0.05f);
                b.Box(new Vector3(-size.x * 0.5f - 0.15f, h * 0.5f, 0), new Vector3(0.3f, h, size.y + 0.6f), 0.05f);
                b.Slot = (int)Mat.Accent;
                b.Box(new Vector3(0, h + 0.1f, size.y * 0.5f + 0.15f), new Vector3(size.x + 0.8f, 0.2f, 0.42f), 0.04f);
                b.Box(new Vector3(-size.x * 0.5f - 0.15f, h + 0.1f, 0), new Vector3(0.42f, 0.2f, size.y + 0.8f), 0.04f);
                // Steel columns and roof trusses along the open sides.
                b.Slot = (int)Mat.Metal;
                for (float x = -size.x * 0.5f; x <= size.x * 0.5f + 0.01f; x += size.x / 6f)
                {
                    b.Box(new Vector3(x, 2.4f, size.y * 0.5f - 0.1f), new Vector3(0.22f, 4.8f, 0.22f), 0.02f);
                    b.Box(new Vector3(x, 4.85f, 0), new Vector3(0.16f, 0.16f, size.y), 0.02f);
                }
                b.Box(new Vector3(0, 4.85f, size.y * 0.5f - 0.1f), new Vector3(size.x, 0.18f, 0.18f), 0.02f);
                // Floor lane markings between lines.
                b.Slot = (int)Mat.Hazard;
                b.Box(new Vector3(0, 0.025f, -size.y * 0.5f + 0.25f), new Vector3(size.x - 0.4f, 0.01f, 0.08f), 0f);
            });

            // Windows on the walls (separate renderers so they can light up at night).
            float wallH = 2.2f;
            for (int i = 0; i < 6; i++)
            {
                float x = hallMin.x + (i + 0.5f) * size.x / 6f;
                var w = _kit.Part(root, "Env.Window", new Vector3(x, wallH, hallMax.y - 0.01f), b =>
                {
                    b.Slot = (int)Mat.Glass;
                    b.Box(Vector3.zero, new Vector3(2.2f, 1.1f, 0.06f), 0f);
                }, false);
                Windows.Add(w);
            }
            for (int i = 0; i < 4; i++)
            {
                float z = hallMin.y + (i + 0.5f) * size.y / 4f;
                var w = _kit.Part(root, "Env.WindowSide", new Vector3(hallMin.x + 0.01f, wallH, z), b =>
                {
                    b.Slot = (int)Mat.Glass;
                    b.Box(Vector3.zero, new Vector3(0.06f, 1.1f, 2.2f), 0f);
                }, false);
                Windows.Add(w);
            }

            // Truck yard + road leaving to the east.
            _kit.Part(root, "Env.Road." + key, new Vector3(hallMax.x, 0, center.y), b =>
            {
                b.Slot = (int)Mat.Road;
                b.Box(new Vector3(14f, -0.01f, 0), new Vector3(28f, 0.06f, size.y + 1f), 0f);
                b.Box(new Vector3(26f, -0.01f, 0), new Vector3(6f, 0.06f, 80f), 0f);
                b.Slot = (int)Mat.White;
                for (float z = -38f; z < 38f; z += 3f) b.Box(new Vector3(26f, 0.025f, z), new Vector3(0.18f, 0.01f, 1.4f), 0f);
            }, false);

            // Lamp posts along the yard.
            for (int i = 0; i < 4; i++)
            {
                var pos = new Vector3(hallMax.x + 4f + i * 6f, 0, hallMax.y + 1.2f);
                _kit.Part(root, "Env.LampPost", pos, b =>
                {
                    b.Slot = (int)Mat.Metal;
                    b.Cylinder(Vector3.zero, 0.08f, 4.2f, 8);
                    b.Box(new Vector3(0, 4.15f, -0.4f), new Vector3(0.1f, 0.08f, 0.9f), 0f);
                }, false);
                var lamp = _kit.Part(root, "Env.LampHead", pos + new Vector3(0, 4.05f, -0.8f), b =>
                {
                    b.Slot = (int)Mat.LampOff;
                    b.Box(Vector3.zero, new Vector3(0.3f, 0.1f, 0.4f), 0.02f);
                }, false);
                Lamps.Add(lamp);
                if (i % 2 == 0)
                {
                    var lightGo = new GameObject("LampLight");
                    lightGo.transform.SetParent(lamp.transform, false);
                    lightGo.transform.localPosition = new Vector3(0, -0.3f, 0);
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 11f;
                    light.intensity = 2.2f;
                    light.color = new Color(1f, 0.86f, 0.62f);
                    light.shadows = LightShadows.None;
                    light.enabled = false;
                    LampLights.Add(light);
                }
            }

            // Deterministic trees around the site.
            var rnd = new System.Random(7);
            for (int i = 0; i < 46; i++)
            {
                float angle = (float)(rnd.NextDouble() * Mathf.PI * 2);
                float radius = 22f + (float)rnd.NextDouble() * 26f;
                var pos = new Vector3(center.x + Mathf.Cos(angle) * radius, 0, center.y + Mathf.Sin(angle) * radius);
                if (pos.x > hallMax.x + 18f && pos.x < hallMax.x + 34f) continue; // keep the road clear
                int variant = rnd.Next(3);
                var tree = _kit.Part(root, "Env.Tree" + variant, pos, b => Tree(b, variant));
                float s = 0.8f + (float)rnd.NextDouble() * 0.6f;
                tree.transform.localScale = new Vector3(s, s, s);
                tree.transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
            }
            return root;
        }

        /// <summary>
        /// Collection items earned from prestige, events, the login calendar and cosmetic packs appear as props in
        /// front of the hall, so progress is visible in the world.
        /// </summary>
        public void BuildDecorations(Transform parent, IEnumerable<string> collection, Vector2 hallMin, Vector2 hallMax)
        {
            var gold = _kit.Materials.Tier(3);
            int slot = 0;
            foreach (var id in collection)
            {
                var pos = new Vector3(hallMin.x + 2f + slot * 3.2f, 0, hallMin.y - 4.5f);
                slot++;
                if (id.StartsWith("decor_monument"))
                {
                    _kit.Part(parent, "Decor.Plinth", pos, b => { b.Slot = (int)Mat.Concrete; b.Box(new Vector3(0, 0.3f, 0), new Vector3(1.4f, 0.6f, 1.4f), 0.06f); });
                    _kit.Solid(parent, "Decor.Crown", pos + Vector3.up * 0.6f, gold, b =>
                    {
                        b.Cylinder(Vector3.zero, 0.5f, 0.35f, 10);
                        for (int i = 0; i < 5; i++)
                        {
                            float a = i * Mathf.PI * 2f / 5f;
                            b.Frustum(new Vector3(Mathf.Cos(a) * 0.42f, 0.35f, Mathf.Sin(a) * 0.42f), 0.12f, 0f, 0.45f, 4, false, true, false);
                        }
                    }, true);
                }
                else if (id.Contains("flag"))
                {
                    _kit.Part(parent, "Decor.FlagPole", pos, b => { b.Slot = (int)Mat.Chrome; b.Cylinder(Vector3.zero, 0.05f, 4f, 8); });
                    _kit.Part(parent, "Decor.Flag", pos + new Vector3(0.6f, 3.4f, 0), b => { b.Slot = (int)Mat.Accent; b.Box(Vector3.zero, new Vector3(1.1f, 0.7f, 0.04f), 0f); });
                }
                else
                {
                    _kit.Part(parent, "Decor.Pedestal", pos, b => { b.Slot = (int)Mat.Metal; b.Cylinder(Vector3.zero, 0.6f, 0.4f, 12); });
                    _kit.Solid(parent, "Decor.Gear", pos + Vector3.up * 0.4f, gold, b =>
                    {
                        b.CylinderAlong(new Vector3(0, 0.8f, -0.1f), new Vector3(0, 0.8f, 0.1f), 0.6f, 12);
                        for (int i = 0; i < 8; i++)
                        {
                            b.PushTRS(new Vector3(0, 0.8f, 0), Quaternion.Euler(0, 0, i * 45f), Vector3.one);
                            b.Box(new Vector3(0, 0.68f, 0), new Vector3(0.22f, 0.22f, 0.2f), 0.02f);
                            b.PopMatrix();
                        }
                    }, true);
                }
            }
        }

        private static void Tree(MeshBuilder b, int variant)
        {
            b.Slot = (int)Mat.Trunk;
            b.Cylinder(Vector3.zero, 0.16f, 1.0f, 6);
            b.Slot = (int)Mat.Foliage;
            if (variant == 0)
            {
                b.Frustum(new Vector3(0, 0.8f, 0), 1.1f, 0f, 1.6f, 7, false, true, false);
                b.Frustum(new Vector3(0, 1.8f, 0), 0.8f, 0f, 1.3f, 7, false, true, false);
            }
            else if (variant == 1)
            {
                b.Box(new Vector3(0, 1.7f, 0), new Vector3(1.6f, 1.4f, 1.6f), 0.45f);
            }
            else
            {
                b.Frustum(new Vector3(0, 0.9f, 0), 0.9f, 0.2f, 2.2f, 6, true, true, false);
            }
        }

        public void SetNight(MaterialLibrary lib, Material[] theme, float nightAmount)
        {
            bool night = nightAmount > 0.5f;
            var glass = night ? lib.GlassNight : theme[(int)Mat.Glass];
            var lamp = night ? lib.LampOn : theme[(int)Mat.LampOff];
            foreach (var w in Windows) if (w != null) w.sharedMaterial = glass;
            foreach (var l in Lamps) if (l != null) l.sharedMaterial = lamp;
            foreach (var l in LampLights) if (l != null) l.enabled = night;
        }
    }
}
