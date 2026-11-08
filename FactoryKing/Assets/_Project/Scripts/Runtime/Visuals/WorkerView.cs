using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Procedural;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>
    /// A line worker. Walks the aisle and prefers machines that need attention (blocked/starved), otherwise inspects
    /// working machines. Purely presentational but reads the real machine statuses.
    /// </summary>
    public sealed class WorkerView
    {
        private readonly Transform _t;
        private readonly Transform _body;
        private readonly float[] _stationX;
        private readonly float _aisleZ;
        private readonly System.Random _rnd;
        private float _targetX;
        private float _wait;
        private float _walkPhase;

        public WorkerView(ModelKit kit, Transform parent, float[] stationX, float aisleZ, int seed)
        {
            _stationX = stationX;
            _aisleZ = aisleZ;
            _rnd = new System.Random(seed);
            _t = ModelKit.Pivot(parent, "Worker", new Vector3(stationX[_rnd.Next(stationX.Length)], 0.12f, aisleZ));
            _body = ModelKit.Pivot(_t, "Body", Vector3.zero);
            kit.Part(_body, "Worker.Mesh", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Metal; // trousers
                b.Box(new Vector3(-0.08f, 0.3f, 0), new Vector3(0.12f, 0.6f, 0.14f), 0.03f);
                b.Box(new Vector3(0.08f, 0.3f, 0), new Vector3(0.12f, 0.6f, 0.14f), 0.03f);
                b.Slot = (int)Mat.Hazard; // hi-vis vest
                b.Box(new Vector3(0, 0.85f, 0), new Vector3(0.36f, 0.5f, 0.22f), 0.07f);
                b.Slot = (int)Mat.Wood; // skin tone (neutral)
                b.Box(new Vector3(0, 1.22f, 0), new Vector3(0.2f, 0.22f, 0.2f), 0.06f);
                b.Slot = (int)Mat.Accent; // hard hat
                b.Frustum(new Vector3(0, 1.3f, 0), 0.15f, 0.08f, 0.12f, 10);
                b.Cylinder(new Vector3(0, 1.3f, 0), 0.18f, 0.025f, 10);
            }, true);
            _targetX = _t.localPosition.x;
            _wait = (float)_rnd.NextDouble() * 2f;
        }

        public void Tick(LineState line, float dt)
        {
            Vector3 p = _t.localPosition;
            float dx = _targetX - p.x;
            if (Mathf.Abs(dx) < 0.05f)
            {
                _body.localPosition = Vector3.zero;
                _t.localRotation = Quaternion.identity; // face the machine (+Z) while inspecting
                _wait -= dt;
                if (_wait <= 0f) PickTarget(line);
                return;
            }
            float step = Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), 1.4f * dt);
            _t.localPosition = new Vector3(p.x + step, p.y, _aisleZ);
            _t.localRotation = Quaternion.Euler(0, dx > 0 ? 90f : -90f, 0);
            _walkPhase += dt * 9f;
            _body.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_walkPhase)) * 0.05f, 0);
        }

        private void PickTarget(LineState line)
        {
            int pick = -1;
            for (int i = 0; i < line.Machines.Count && i < _stationX.Length; i++)
            {
                var s = line.Machines[i].Status;
                if ((s == MachineStatus.Blocked || s == MachineStatus.Starved) && _rnd.NextDouble() < 0.5) { pick = i; break; }
            }
            if (pick < 0) pick = _rnd.Next(_stationX.Length);
            _targetX = _stationX[pick] + (float)(_rnd.NextDouble() - 0.5) * 0.8f;
            _wait = 1.5f + (float)_rnd.NextDouble() * 3f;
        }

        public void Destroy()
        {
            if (_t != null) Object.Destroy(_t.gameObject);
        }
    }
}
