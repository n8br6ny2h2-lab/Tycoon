using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>Lazily created particle materials with a generated soft-dot texture (no texture assets needed).</summary>
    public static class ParticleMaterials
    {
        private static Material _smoke;
        private static Material _sparkle;
        private static Texture2D _soft;

        public static Material Smoke => _smoke != null ? _smoke : (_smoke = Create("FK.Smoke", new Color(1, 1, 1, 0.6f), false));
        public static Material Sparkle => _sparkle != null ? _sparkle : (_sparkle = Create("FK.Sparkle", new Color(1f, 0.85f, 0.35f, 1f), true));

        private static Material Create(string name, Color color, bool additive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = name };
            var tex = SoftDot();
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            // URP particle shaders: transparent surface, alpha or additive blend.
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        private static Texture2D SoftDot()
        {
            if (_soft != null) return _soft;
            const int size = 32;
            _soft = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "FK.SoftDot", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
            _soft.SetPixels32(px);
            _soft.Apply(false, true);
            return _soft;
        }
    }
}
