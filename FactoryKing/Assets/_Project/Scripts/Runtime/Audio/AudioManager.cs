using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryKing.Runtime.Audio
{
    public enum Sfx { Click, Cash, Upgrade, Success, Unlock, Error, Package, Machine }

    /// <summary>
    /// All sound is synthesised at startup from code (no audio files, no licensing risk): UI clicks, cash register,
    /// upgrade chime, success fanfare, packaging thump, plus an ambient factory loop (hum + conveyor rattle) whose
    /// volume follows how many machines are really working, and a calm generative menu/background music loop.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int Rate = 44100;

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();
        private AudioSource[] _sfx;
        private int _next;
        private AudioSource _ambience;
        private AudioSource _music;
        private float _sfxVolume = 0.8f;
        private float _musicVolume = 0.6f;
        private float _activity;

        private void Awake()
        {
            _sfx = new AudioSource[8];
            for (int i = 0; i < _sfx.Length; i++)
            {
                _sfx[i] = gameObject.AddComponent<AudioSource>();
                _sfx[i].playOnAwake = false;
            }
            _clips[Sfx.Click] = Make("click", 0.06f, (t, d) => Tone(t, 1400, 0) * Env(t, 0.002f, 0.05f) * 0.5f);
            _clips[Sfx.Cash] = Make("cash", 0.45f, (t, d) =>
                (Tone(t, 2093, 0) * Env(t, 0.003f, 0.25f) + Tone(t - 0.08f, 2637, 0) * Env(t - 0.08f, 0.003f, 0.3f)) * 0.35f
                + Noise(t) * Env(t, 0.001f, 0.04f) * 0.2f);
            _clips[Sfx.Upgrade] = Make("upgrade", 0.5f, (t, d) =>
            {
                float f = 523f * Mathf.Pow(2f, Mathf.Floor(t / 0.08f) * 4f / 12f);
                return Tone(t, f, 0.3f) * Env(t, 0.005f, 0.45f) * 0.35f;
            });
            _clips[Sfx.Success] = Make("success", 0.9f, (t, d) =>
                (Tone(t, 523, 0.2f) + Tone(t, 659, 0.2f) * Step(t, 0.12f) + Tone(t, 784, 0.2f) * Step(t, 0.24f) + Tone(t, 1047, 0.2f) * Step(t, 0.36f))
                * Env(t, 0.01f, 0.85f) * 0.18f);
            _clips[Sfx.Unlock] = Make("unlock", 0.7f, (t, d) => (Tone(t, 392, 0.4f) + Tone(t, 587, 0.4f)) * Env(t, 0.01f, 0.65f) * 0.25f);
            _clips[Sfx.Error] = Make("error", 0.25f, (t, d) => Tone(t, 180, 0.6f) * Env(t, 0.005f, 0.22f) * 0.3f);
            _clips[Sfx.Package] = Make("package", 0.18f, (t, d) => (Tone(t, 110 - t * 200, 0.2f) + Noise(t) * 0.4f) * Env(t, 0.002f, 0.15f) * 0.4f);
            _clips[Sfx.Machine] = Make("machine", 0.22f, (t, d) => (Noise(t) * 0.5f + Tone(t, 70, 0.8f)) * Env(t, 0.001f, 0.2f) * 0.35f);

            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.clip = Make("ambience", 4f, (t, d) =>
            {
                float hum = Tone(t, 55, 0.5f) * 0.25f + Tone(t, 110, 0.2f) * 0.08f;
                float rattle = Noise(t) * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 6f)) * 0.06f;
                return hum + rattle;
            }, loopSafe: true);
            _ambience.loop = true;
            _ambience.volume = 0;
            _ambience.Play();

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = MakeMusic();
            _music.loop = true;
            _music.Play();
            ApplyVolumes();
        }

        public void SetVolumes(float sfx, float music)
        {
            _sfxVolume = Mathf.Clamp01(sfx);
            _musicVolume = Mathf.Clamp01(music);
            ApplyVolumes();
        }

        /// <summary>0..1 share of machines currently working on screen; drives the ambience loudness.</summary>
        public void SetFactoryActivity(float activity) => _activity = Mathf.Clamp01(activity);

        public void Play(Sfx sfx, float minInterval = 0.05f, float volume = 1f)
        {
            if (_sfxVolume <= 0f || !_clips.TryGetValue(sfx, out var clip)) return;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sfx, out float last) && now - last < minInterval) return;
            _lastPlayed[sfx] = now;
            var src = _sfx[_next];
            _next = (_next + 1) % _sfx.Length;
            src.pitch = 1f + UnityEngine.Random.Range(-0.04f, 0.04f);
            src.PlayOneShot(clip, _sfxVolume * volume);
        }

        private void Update()
        {
            float target = _sfxVolume * 0.35f * _activity;
            _ambience.volume = Mathf.MoveTowards(_ambience.volume, target, Time.unscaledDeltaTime * 0.5f);
        }

        private void ApplyVolumes()
        {
            if (_music != null) _music.volume = _musicVolume * 0.35f;
        }

        private void OnApplicationPause(bool paused)
        {
            if (_music == null) return;
            if (paused) { _music.Pause(); _ambience.Pause(); }
            else { _music.UnPause(); _ambience.UnPause(); }
        }

        // ---------------- synthesis ----------------

        private static float Tone(float t, float freq, float warmth)
        {
            if (t < 0) return 0;
            float p = t * freq * Mathf.PI * 2f;
            return Mathf.Sin(p) * (1f - warmth) + (Mathf.Sin(p) + Mathf.Sin(p * 2f) * 0.5f + Mathf.Sin(p * 3f) * 0.25f) * 0.57f * warmth;
        }

        private static float Env(float t, float attack, float release)
        {
            if (t < 0) return 0;
            if (t < attack) return t / attack;
            return Mathf.Exp(-(t - attack) / Mathf.Max(0.001f, release) * 4f);
        }

        private static float Step(float t, float at) => t >= at ? 1f : 0f;

        private static uint _seed = 22222;
        private static float Noise(float t)
        {
            _seed = _seed * 1664525u + 1013904223u;
            return ((_seed >> 9) / 8388608f) * 2f - 1f;
        }

        private static AudioClip Make(string name, float seconds, Func<float, float, float> f, bool loopSafe = false)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float v = f(t, seconds);
                if (loopSafe)
                {
                    float fade = Mathf.Min(1f, Mathf.Min(t, seconds - t) / 0.05f);
                    v *= Mathf.Lerp(0.85f, 1f, fade);
                }
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            var clip = AudioClip.Create("FK." + name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>16-bar generative loop: soft pad chords, plucked arpeggio and a light pulse (I–vi–IV–V in C).</summary>
        private static AudioClip MakeMusic()
        {
            const float bpm = 92f;
            float beat = 60f / bpm;
            int[][] chords = { new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 } };
            float bar = beat * 4f;
            float seconds = bar * 16f;
            int n = Mathf.CeilToInt(seconds * 22050);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 22050f;
                int barIndex = (int)(t / bar) % 16;
                var chord = chords[(barIndex / 2) % 4];
                float inBar = t % bar;
                float v = 0f;
                foreach (int note in chord)
                {
                    float f = 440f * Mathf.Pow(2f, (note - 69) / 12f);
                    v += Mathf.Sin(t * f * Mathf.PI * 2f) * 0.06f * (0.8f + 0.2f * Mathf.Sin(t * 0.5f));
                }
                int step = (int)(inBar / (beat / 2f));
                int arpNote = chord[step % 3] + 12;
                float arpT = inBar % (beat / 2f);
                float af = 440f * Mathf.Pow(2f, (arpNote - 69) / 12f);
                v += Mathf.Sin(arpT * af * Mathf.PI * 2f) * Mathf.Exp(-arpT * 9f) * 0.12f;
                float beatT = inBar % beat;
                v += Mathf.Sin(beatT * 60f * Mathf.PI * 2f) * Mathf.Exp(-beatT * 18f) * 0.15f;
                float edge = Mathf.Min(1f, Mathf.Min(t, seconds - t) / 0.02f);
                data[i] = Mathf.Clamp(v * edge, -1f, 1f);
            }
            var clip = AudioClip.Create("FK.music", n, 1, 22050, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
