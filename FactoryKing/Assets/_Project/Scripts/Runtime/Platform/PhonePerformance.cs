using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.InputHandling;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Battery-friendly frame pacing for phones: 60 FPS while the player interacts, 30 FPS after 20 s without touches,
    /// and always 30 FPS in battery-saver mode or when iOS Low Power Mode is on. The economy is unaffected
    /// (it runs on a fixed step independent of frame rate).
    /// </summary>
    public sealed class PhonePerformance : MonoBehaviour
    {
        private const float IdleSeconds = 20f;
        private GameSession _session;
        private float _idle;
        private float _lowPowerCheck;
        private bool _lowPower;

        public void Init(GameSession session) => _session = session;

        private void Update()
        {
            if (_session == null) return;
            if (PointerInput.PointerCount > 0 || Mathf.Abs(PointerInput.ScrollDelta) > 0) _idle = 0f;
            else _idle += Time.unscaledDeltaTime;

            _lowPowerCheck -= Time.unscaledDeltaTime;
            if (_lowPowerCheck <= 0f)
            {
                _lowPowerCheck = 5f;
                _lowPower = NativeBridge.LowPowerMode;
            }

            bool slow = _session.State.Settings.BatterySaver || _lowPower || _idle > IdleSeconds;
            int target = slow ? 30 : 60;
            if (Application.targetFrameRate != target) Application.targetFrameRate = target;
        }
    }
}
