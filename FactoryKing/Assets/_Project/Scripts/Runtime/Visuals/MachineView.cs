using System.Collections.Generic;
using FactoryKing.Core.Content;
using FactoryKing.Core.Simulation;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    public enum PartMotion
    {
        /// <summary>Continuous rotation around an axis while the machine is working.</summary>
        Spin,
        /// <summary>Moves along an axis following a down-and-up stroke over each cycle.</summary>
        Stroke,
        /// <summary>Rotates back and forth (degrees) over each cycle.</summary>
        Swing,
        /// <summary>Moves along an axis from 0 to amplitude and back over each cycle.</summary>
        Shuttle,
        /// <summary>Small fast vibration while working.</summary>
        Shake,
        /// <summary>Uniform scale pulse (e.g. heat glow).</summary>
        Pulse,
    }

    public sealed class AnimatedPart
    {
        public Transform Transform;
        public PartMotion Motion;
        public Vector3 Axis;
        public float Amount;   // degrees/sec for Spin, distance for Stroke/Shuttle, degrees for Swing
        public float Phase;    // cycle phase offset 0..1
        public Vector3 BasePosition;
        public Quaternion BaseRotation;
        public Vector3 BaseScale;
    }

    /// <summary>
    /// Visual of one machine. Every motion is driven by the machine's real simulation state (in-cycle, progress,
    /// buffer, truck load) — nothing animates when the machine is idle, starved or blocked.
    /// </summary>
    public sealed class MachineView : MonoBehaviour
    {
        public MachineRef Ref { get; private set; }
        public MachineDef Def { get; private set; }

        internal readonly List<AnimatedPart> Parts = new List<AnimatedPart>();
        internal Renderer StatusLight;
        internal Renderer TierBand;
        internal Transform ScaleRoot;
        internal ParticleSystem Smoke;

        // Robot arm joints (optional)
        internal Transform ArmYaw, ArmShoulder, ArmElbow;
        // Warehouse (optional)
        internal Renderer[] CrateSlots;
        internal Transform Forklift;
        internal Vector3 ForkliftFrom, ForkliftTo;
        // Dock (optional)
        internal Transform Truck;
        internal Renderer[] TruckCrates;
        internal Vector3 TruckParked, TruckAway;

        private MaterialLibrary _materials;
        private MachineStatus _shownStatus = (MachineStatus)(-1);
        private int _shownTier = -1;
        private float _spinAngle;
        private float _punch;
        private float _smokeTimer;

        internal void Init(MachineRef r, MachineDef def, MaterialLibrary materials)
        {
            Ref = r;
            Def = def;
            _materials = materials;
            foreach (var p in Parts)
            {
                p.BasePosition = p.Transform.localPosition;
                p.BaseRotation = p.Transform.localRotation;
                p.BaseScale = p.Transform.localScale;
            }
        }

        public void PlayUpgradeEffect() => _punch = 1f;

        /// <param name="speedVisual">Animation speed factor (cycles per second relative to level 1, clamped).</param>
        public void Tick(MachineState state, int capacity, int tier, float speedVisual, float dt)
        {
            bool working = state.InCycle && state.Status == MachineStatus.Working;
            float progress = (float)state.Progress;

            if (state.Status != _shownStatus && StatusLight != null)
            {
                _shownStatus = state.Status;
                StatusLight.sharedMaterial = _materials.Status((int)state.Status);
            }
            if (tier != _shownTier)
            {
                _shownTier = tier;
                if (TierBand != null) TierBand.sharedMaterial = _materials.Tier(tier);
            }

            if (working) _spinAngle += dt * Mathf.Clamp(speedVisual, 0.5f, 6f);

            foreach (var p in Parts)
            {
                if (!working && p.Motion != PartMotion.Spin) continue; // hold pose when not working
                float t = Mathf.Repeat(progress + p.Phase, 1f);
                switch (p.Motion)
                {
                    case PartMotion.Spin:
                        p.Transform.localRotation = p.BaseRotation * Quaternion.AngleAxis(_spinAngle * p.Amount, p.Axis);
                        break;
                    case PartMotion.Stroke:
                        // fast down, slower up
                        float s = t < 0.35f ? t / 0.35f : 1f - (t - 0.35f) / 0.65f;
                        p.Transform.localPosition = p.BasePosition + p.Axis * (p.Amount * Smooth(s));
                        break;
                    case PartMotion.Swing:
                        p.Transform.localRotation = p.BaseRotation * Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI * 2f) * p.Amount, p.Axis);
                        break;
                    case PartMotion.Shuttle:
                        float u = t < 0.5f ? t * 2f : 2f - t * 2f;
                        p.Transform.localPosition = p.BasePosition + p.Axis * (p.Amount * Smooth(u));
                        break;
                    case PartMotion.Shake:
                        p.Transform.localPosition = p.BasePosition + p.Axis * (Mathf.Sin(Time.time * 60f + p.Phase * 10f) * p.Amount);
                        break;
                    case PartMotion.Pulse:
                        float k = 1f + Mathf.Sin(t * Mathf.PI * 2f) * p.Amount;
                        p.Transform.localScale = p.BaseScale * k;
                        break;
                }
            }

            if (ArmYaw != null) AnimateArm(working ? progress : 0f);
            if (CrateSlots != null) UpdateWarehouse(state, capacity, working, progress);
            if (Truck != null) UpdateTruck(state, working, progress);
            UpdateSmoke(working, dt);

            if (_punch > 0f)
            {
                _punch = Mathf.Max(0f, _punch - dt * 3f);
                float scale = 1f + Mathf.Sin(_punch * Mathf.PI) * 0.15f;
                ScaleRoot.localScale = Vector3.one * (BaseScale(tier) * scale);
            }
            else if (ScaleRoot != null)
            {
                ScaleRoot.localScale = Vector3.one * BaseScale(tier);
            }
        }

        private static float BaseScale(int tier) => 1f + tier * 0.025f;
        private static float Smooth(float x) => x * x * (3f - 2f * x);

        /// <summary>Pick from the input side, swing over, place on the output side, return.</summary>
        private void AnimateArm(float p)
        {
            float yaw = p < 0.5f ? Mathf.Lerp(-80f, 80f, Smooth(p * 2f)) : Mathf.Lerp(80f, -80f, Smooth((p - 0.5f) * 2f));
            float dip = Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2f));
            ArmYaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
            ArmShoulder.localRotation = Quaternion.Euler(0f, 0f, -25f - 20f * (1f - dip));
            ArmElbow.localRotation = Quaternion.Euler(0f, 0f, 70f - 25f * (1f - dip));
        }

        private void UpdateWarehouse(MachineState state, int capacity, bool working, float progress)
        {
            float fill = capacity > 0 ? Mathf.Clamp01(state.OutputBuffer / (float)capacity) : 0f;
            int visible = Mathf.CeilToInt(fill * CrateSlots.Length);
            if (state.OutputBuffer > 0) visible = Mathf.Max(1, visible);
            for (int i = 0; i < CrateSlots.Length; i++)
            {
                bool on = i < visible;
                if (CrateSlots[i].enabled != on) CrateSlots[i].enabled = on;
            }
            if (Forklift != null)
            {
                float u = working ? (progress < 0.5f ? progress * 2f : 2f - progress * 2f) : 0f;
                Forklift.localPosition = Vector3.Lerp(ForkliftFrom, ForkliftTo, Smooth(u));
                Forklift.localRotation = Quaternion.Euler(0f, working && progress >= 0.5f ? 180f : 0f, 0f);
            }
        }

        private void UpdateTruck(MachineState state, bool working, float progress)
        {
            // 0-0.15 loading at bay, 0.15-0.45 drive away, 0.45-0.6 away (delivering), 0.6-0.9 return, 0.9-1 park
            float u;
            if (!working || progress < 0.15f) u = 0f;
            else if (progress < 0.45f) u = Smooth((progress - 0.15f) / 0.3f);
            else if (progress < 0.6f) u = 1f;
            else if (progress < 0.9f) u = 1f - Smooth((progress - 0.6f) / 0.3f);
            else u = 0f;
            Truck.localPosition = Vector3.Lerp(TruckParked, TruckAway, u);
            bool visibleTruck = u < 0.98f;
            if (Truck.gameObject.activeSelf != visibleTruck) Truck.gameObject.SetActive(visibleTruck);

            // Crates on the bed: the real load while delivering; none on the way back.
            int load = working && progress < 0.6f ? state.InProcess : 0;
            for (int i = 0; i < TruckCrates.Length; i++)
            {
                bool on = i < load;
                if (TruckCrates[i].enabled != on) TruckCrates[i].enabled = on;
            }
        }

        private void UpdateSmoke(bool working, float dt)
        {
            if (Smoke == null) return;
            if (!working) return;
            _smokeTimer -= dt;
            if (_smokeTimer <= 0f)
            {
                _smokeTimer = 0.35f;
                Smoke.Emit(1);
            }
        }
    }
}
