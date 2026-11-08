using System;
using UnityEngine;

namespace FactoryKing.Runtime.EnvironmentFx
{
    /// <summary>
    /// Rotates the sun, blends light colour/intensity and ambient light over a configurable day length and reports a
    /// 0..1 "night amount" so windows and lamps can switch on. Purely cosmetic; does not affect the economy.
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private float dayLengthSeconds = 480f;
        [Range(0f, 1f)] [SerializeField] private float timeOfDay = 0.35f; // 0 = midnight, 0.5 = noon

        private Light _sun;
        private Camera _camera;
        private Gradient _sunColor;
        private Gradient _skyColor;
        private Gradient _equatorColor;
        private Gradient _groundColor;
        private float _lastNight = -1f;

        public event Action<float> NightAmountChanged;

        public float TimeOfDay
        {
            get => timeOfDay;
            set => timeOfDay = Mathf.Repeat(value, 1f);
        }

        public float DayLengthSeconds
        {
            get => dayLengthSeconds;
            set => dayLengthSeconds = Mathf.Max(10f, value);
        }

        public float NightAmount { get; private set; }

        /// <summary>Sky cosmetic tint (null = natural sky).</summary>
        public Color? SkyTint { get; set; }

        public void Init(Light sun, Camera backgroundCamera)
        {
            _sun = sun;
            _camera = backgroundCamera;
            _sunColor = Make(("#2B3A67", 0f), ("#FF9A5A", 0.24f), ("#FFF4E0", 0.35f), ("#FFFFFF", 0.5f), ("#FFE6C4", 0.68f), ("#FF7B4A", 0.76f), ("#2B3A67", 1f));
            _skyColor = Make(("#0E1630", 0f), ("#5D6FA8", 0.24f), ("#A9CBEF", 0.35f), ("#B9DAF7", 0.5f), ("#A9C4E8", 0.68f), ("#6E5C9A", 0.77f), ("#0E1630", 1f));
            _equatorColor = Make(("#141B33", 0f), ("#B98A7A", 0.24f), ("#C9D4DE", 0.4f), ("#D6DEE6", 0.6f), ("#C99A85", 0.76f), ("#141B33", 1f));
            _groundColor = Make(("#0B0E16", 0f), ("#5B4E46", 0.25f), ("#6E6A62", 0.5f), ("#5B4E46", 0.75f), ("#0B0E16", 1f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            Apply();
        }

        private void Update()
        {
            if (_sun == null) return;
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / dayLengthSeconds, 1f);
            Apply();
        }

        private void Apply()
        {
            // Sun elevation: -90 at midnight, +90 at noon. Keep a fixed azimuth that suits the isometric camera.
            float elevation = Mathf.Sin((timeOfDay - 0.25f) * Mathf.PI * 2f) * 70f;
            float azimuth = 150f + (timeOfDay - 0.5f) * 120f;
            _sun.transform.rotation = Quaternion.Euler(Mathf.Max(elevation, 8f), azimuth, 0f);

            float daylight = Mathf.Clamp01((elevation + 6f) / 30f);
            _sun.color = _sunColor.Evaluate(timeOfDay);
            _sun.intensity = Mathf.Lerp(0.18f, 1.25f, daylight);
            _sun.shadowStrength = Mathf.Lerp(0.35f, 0.85f, daylight);

            RenderSettings.ambientSkyColor = _skyColor.Evaluate(timeOfDay);
            RenderSettings.ambientEquatorColor = _equatorColor.Evaluate(timeOfDay);
            RenderSettings.ambientGroundColor = _groundColor.Evaluate(timeOfDay);
            RenderSettings.fogColor = _equatorColor.Evaluate(timeOfDay);
            var sky = Color.Lerp(RenderSettings.ambientSkyColor, RenderSettings.fogColor, 0.5f);
            if (SkyTint.HasValue)
            {
                var tint = SkyTint.Value * Mathf.Lerp(0.35f, 1f, daylight);
                sky = Color.Lerp(sky, tint, 0.7f);
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, tint, 0.5f);
            }
            if (_camera != null) _camera.backgroundColor = sky;

            NightAmount = 1f - daylight;
            // Notify only when crossing thresholds to avoid per-frame material swaps.
            float bucket = NightAmount > 0.5f ? 1f : 0f;
            if (!Mathf.Approximately(bucket, _lastNight))
            {
                _lastNight = bucket;
                NightAmountChanged?.Invoke(NightAmount);
            }
        }

        private static Gradient Make(params (string hex, float t)[] keys)
        {
            var colors = new GradientColorKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                ColorUtility.TryParseHtmlString(keys[i].hex, out var c);
                colors[i] = new GradientColorKey(c, keys[i].t);
            }
            var g = new Gradient();
            g.SetKeys(colors, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            return g;
        }
    }
}
