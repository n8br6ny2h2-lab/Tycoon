using System.Collections.Generic;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>Material slots used by procedural geometry. Theme-dependent slots resolve per factory.</summary>
    public enum Mat
    {
        Body = 0,       // theme main colour
        Accent = 1,     // theme accent colour
        Metal = 2,      // dark painted steel
        Chrome = 3,     // bright metal
        Rubber = 4,     // tyres, belts sides
        Hazard = 5,     // yellow safety paint
        Glass = 6,      // windows (emissive at night)
        Floor = 7,
        Wall = 8,
        Roof = 9,
        Ground = 10,
        Road = 11,
        Wood = 12,
        Concrete = 13,
        Glow = 14,      // furnace/oven heat glow
        Foliage = 15,
        Trunk = 16,
        LampOff = 17,
        White = 18,
        Count = 19,
    }

    /// <summary>Colour set of one factory theme.</summary>
    public readonly partial struct Theme
    {
        public readonly Color Body, Accent, Floor, Wall;

        public Theme(string body, string accent, string floor, string wall)
        {
            Body = Hex(body);
            Accent = Hex(accent);
            Floor = Hex(floor);
            Wall = Hex(wall);
        }

        public static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static Theme For(string themeId)
        {
            if (TryExtra(themeId, out var extra)) return extra;
            switch (themeId)
            {
                case "food": return new Theme("#F2B134", "#E5533D", "#D9CBB4", "#F3E9DA");
                case "plastic": return new Theme("#2EA8E0", "#F25C54", "#C9D6DF", "#E8F0F5");
                case "textile": return new Theme("#9B6BD3", "#F2A541", "#D8CFE3", "#F1ECF6");
                case "electronics": return new Theme("#3DDC97", "#3A86FF", "#BFC6CF", "#E9EDF2");
                default: return new Theme("#E2683C", "#2F4858", "#B9B4AA", "#E7E1D6"); // steel
            }
        }
    }

    /// <summary>
    /// Creates and caches every runtime material. URP/Lit is used when the Universal pipeline is active; otherwise it
    /// falls back to the built-in Standard shader so the scene still renders. Materials are shared (SRP-Batcher
    /// friendly) and GPU instancing is enabled.
    /// </summary>
    public sealed class MaterialLibrary
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Shader _shader;
        private readonly Dictionary<string, Material[]> _themes = new Dictionary<string, Material[]>();
        private readonly Dictionary<string, Material> _byColor = new Dictionary<string, Material>();
        private readonly Material[] _status = new Material[4];
        private readonly Material[] _tiers = new Material[6];
        private Material _glassNight;
        private Material _lampOn;

        public MaterialLibrary()
        {
            _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = Shader.Find("Standard");
            if (_shader == null) _shader = Shader.Find("Diffuse");

            _status[0] = Create("Status.Idle", Theme.Hex("#8A939B"), 0.3f, 0, Theme.Hex("#30363B"));
            _status[1] = Create("Status.Working", Theme.Hex("#39E06B"), 0.6f, 0, Theme.Hex("#1FD15A") * 2.2f);
            _status[2] = Create("Status.Starved", Theme.Hex("#FFC531"), 0.6f, 0, Theme.Hex("#FFB300") * 2.0f);
            _status[3] = Create("Status.Blocked", Theme.Hex("#FF4A3D"), 0.6f, 0, Theme.Hex("#FF2A1A") * 2.0f);

            string[] tierColors = { "#7C8A96", "#B87333", "#C9D1D9", "#FFC83D", "#7FE7E0", "#B98CFF" };
            for (int i = 0; i < _tiers.Length; i++)
                _tiers[i] = Create("Tier" + i, Theme.Hex(tierColors[i]), 0.85f, i == 0 ? 0.2f : 0.9f,
                    i >= 3 ? Theme.Hex(tierColors[i]) * 0.35f : Color.black);

            _glassNight = Create("Glass.Night", Theme.Hex("#FFD37A"), 0.9f, 0, Theme.Hex("#FFB648") * 1.6f);
            _lampOn = Create("Lamp.On", Theme.Hex("#FFF1C1"), 0.9f, 0, Theme.Hex("#FFE3A0") * 3f);
            BeltMaterial = CreateBeltMaterial();
        }

        public Material BeltMaterial { get; }
        public Material Status(int status) => _status[Mathf.Clamp(status, 0, 3)];
        public Material Tier(int tier) => _tiers[Mathf.Clamp(tier, 0, _tiers.Length - 1)];
        public Material GlassNight => _glassNight;
        public Material LampOn => _lampOn;

        /// <summary>Materials indexed by <see cref="Mat"/> for the given theme.</summary>
        public Material[] ForTheme(string themeId)
        {
            if (_themes.TryGetValue(themeId, out var mats)) return mats;
            var t = Theme.For(themeId);
            mats = new Material[(int)Mat.Count];
            mats[(int)Mat.Body] = Create(themeId + ".Body", t.Body, 0.45f, 0.1f);
            mats[(int)Mat.Accent] = Create(themeId + ".Accent", t.Accent, 0.5f, 0.15f);
            mats[(int)Mat.Floor] = Create(themeId + ".Floor", t.Floor, 0.25f, 0f);
            mats[(int)Mat.Wall] = Create(themeId + ".Wall", t.Wall, 0.15f, 0f);
            // Shared, theme-independent slots.
            mats[(int)Mat.Metal] = Shared("Metal", "#3B4148", 0.55f, 0.6f);
            mats[(int)Mat.Chrome] = Shared("Chrome", "#C7CED6", 0.85f, 0.9f);
            mats[(int)Mat.Rubber] = Shared("Rubber", "#22252A", 0.2f, 0f);
            mats[(int)Mat.Hazard] = Shared("Hazard", "#F5C518", 0.45f, 0.1f);
            mats[(int)Mat.Glass] = Shared("Glass", "#9CC9E8", 0.95f, 0.1f);
            mats[(int)Mat.Roof] = Shared("Roof", "#5C6670", 0.35f, 0.3f);
            mats[(int)Mat.Ground] = Shared("Ground", "#7DB35A", 0.1f, 0f);
            mats[(int)Mat.Road] = Shared("Road", "#4A4E55", 0.15f, 0f);
            mats[(int)Mat.Wood] = Shared("Wood", "#B98552", 0.2f, 0f);
            mats[(int)Mat.Concrete] = Shared("Concrete", "#A7A9AC", 0.15f, 0f);
            mats[(int)Mat.Glow] = SharedEmissive("Glow", "#FF8A2A", "#FF6A00", 3.0f);
            mats[(int)Mat.Foliage] = Shared("Foliage", "#4E9F4A", 0.15f, 0f);
            mats[(int)Mat.Trunk] = Shared("Trunk", "#7A5536", 0.1f, 0f);
            mats[(int)Mat.LampOff] = Shared("LampOff", "#E8E2D0", 0.6f, 0f);
            mats[(int)Mat.White] = Shared("White", "#F4F6F8", 0.4f, 0f);
            _themes[themeId] = mats;
            return mats;
        }

        private readonly Dictionary<string, Material> _cosmetics = new Dictionary<string, Material>();

        /// <summary>
        /// Material for an equipped cosmetic (floor, wall, ground). Patterns are generated textures tinted with the
        /// cosmetic's two colours; geometry UVs are world-planar, so tiles keep a constant size everywhere.
        /// Returns <paramref name="fallback"/> for the default (theme) cosmetic.
        /// </summary>
        public Material ForCosmetic(FactoryKing.Core.Meta.CosmeticDef def, Material fallback)
        {
            if (def == null || string.IsNullOrEmpty(def.ColorHex)) return fallback;
            if (_cosmetics.TryGetValue(def.Id, out var m)) return m;
            var a = Theme.Hex(def.ColorHex);
            var b = string.IsNullOrEmpty(def.SecondaryHex) ? a * 0.85f : Theme.Hex(def.SecondaryHex);
            bool shiny = def.Id.Contains("gold") || def.Id.Contains("marble") || def.Id.Contains("glass");
            if (def.Pattern == FactoryKing.Core.Meta.CosmeticPattern.Solid)
            {
                m = Create("Cosmetic." + def.Id, a, shiny ? 0.8f : 0.25f, def.Id.Contains("gold") ? 0.8f : 0f);
            }
            else
            {
                m = Create("Cosmetic." + def.Id, Color.white, shiny ? 0.75f : 0.3f, def.Id.Contains("gold") ? 0.6f : 0f);
                m.mainTexture = PatternTexture(def.Pattern, a, b);
                m.mainTextureScale = new Vector2(0.5f, 0.5f); // one tile per 2 world units
            }
            _cosmetics[def.Id] = m;
            return m;
        }

        private static Texture2D PatternTexture(FactoryKing.Core.Meta.CosmeticPattern pattern, Color a, Color b)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 4 };
            var px = new Color32[size * size];
            Color32 ca = a, cb = b, grout = Color.Lerp(b, Color.black, 0.35f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color32 c;
                switch (pattern)
                {
                    case FactoryKing.Core.Meta.CosmeticPattern.Checker:
                        c = ((x / 32) + (y / 32)) % 2 == 0 ? ca : cb;
                        break;
                    case FactoryKing.Core.Meta.CosmeticPattern.Stripes:
                        c = (y % 16) < 11 ? ca : cb;
                        break;
                    default: // tiles with grout lines
                        c = (x % 32 < 2 || y % 32 < 2) ? grout : ((x / 32 + y / 32) % 2 == 0 ? ca : (Color32)Color.Lerp(a, b, 0.4f));
                        break;
                }
                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Material for a product/crate colour (cached by hex).</summary>
        public Material ForColor(string hex) => Shared("Product" + hex, hex, 0.35f, 0.05f);

        private Material Shared(string key, string hex, float smoothness, float metallic)
        {
            if (_byColor.TryGetValue(key, out var m)) return m;
            m = Create(key, Theme.Hex(hex), smoothness, metallic);
            _byColor[key] = m;
            return m;
        }

        private Material SharedEmissive(string key, string hex, string emissionHex, float intensity)
        {
            if (_byColor.TryGetValue(key, out var m)) return m;
            m = Create(key, Theme.Hex(hex), 0.5f, 0f, Theme.Hex(emissionHex) * intensity);
            _byColor[key] = m;
            return m;
        }

        public Material Create(string name, Color color, float smoothness, float metallic, Color? emission = null)
        {
            var m = new Material(_shader) { name = "FK." + name, enableInstancing = true };
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, color);
            if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, smoothness);
            if (m.HasProperty(GlossinessId)) m.SetFloat(GlossinessId, smoothness);
            if (m.HasProperty(MetallicId)) m.SetFloat(MetallicId, metallic);
            if (emission.HasValue && emission.Value.maxColorComponent > 0)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, emission.Value);
            }
            return m;
        }

        /// <summary>Dark belt with light cross-ribs; scrolled per line by <see cref="ConveyorView"/>.</summary>
        private Material CreateBeltMaterial()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "FK.BeltTex", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 2,
            };
            var dark = new Color32(38, 41, 46, 255);
            var rib = new Color32(92, 98, 106, 255);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = (x % 16) < 3 ? rib : dark;
            tex.SetPixels32(px);
            tex.Apply(true, true);

            var m = Create("Belt", Color.white, 0.3f, 0f);
            m.mainTexture = tex;
            return m;
        }

        /// <summary>Creates an independent copy (e.g. one belt material per line so each scrolls at its own speed).</summary>
        public Material Clone(Material source, string name)
        {
            var m = new Material(source) { name = "FK." + name };
            return m;
        }
    }
}
