using FactoryKing.Core.Simulation;
using UnityEngine;

namespace FactoryKing.Runtime.Sim
{
    /// <summary>
    /// The only place that advances the economy in real time. Uses a fixed step with an accumulator so results are
    /// frame-rate independent, and caps catch-up per frame (long absences are handled by offline earnings, Phase 3).
    /// Uses unscaled time so pausing visuals via timeScale never stops or speeds up the economy.
    /// </summary>
    public sealed class SimulationDriver : MonoBehaviour
    {
        public const float FixedStep = 1f / 20f;
        public const float MaxCatchUpSeconds = 1f;

        private GameSession _session;
        private float _accumulator;

        public bool Paused { get; set; }

        /// <summary>Debug multiplier for testing (1 in release builds).</summary>
        public float DebugSpeed { get; set; } = 1f;

        public void Init(GameSession session) => _session = session;

        private void Update()
        {
            if (_session == null || Paused) return;
            _accumulator += Mathf.Min(Time.unscaledDeltaTime * DebugSpeed, MaxCatchUpSeconds * DebugSpeed);
            int guard = 0;
            while (_accumulator >= FixedStep && guard++ < 400)
            {
                _session.Tick(FixedStep);
                _accumulator -= FixedStep;
            }
            if (guard >= 400) _accumulator = 0f;
        }

        private void OnApplicationPause(bool paused)
        {
            // Drop the frame gap; Phase 3 OfflineEarningsManager credits the time away instead.
            _accumulator = 0f;
        }
    }
}
