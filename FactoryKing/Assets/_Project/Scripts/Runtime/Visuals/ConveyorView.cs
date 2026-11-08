using System.Collections.Generic;
using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Pooling;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>
    /// Belt between two machines. The crates shown are exactly the upstream machine's output buffer (capped by the
    /// number of visible slots): a crate appears when the upstream machine finishes a cycle, rides the belt, queues,
    /// and disappears when the downstream machine takes it.
    /// </summary>
    public sealed class ConveyorView
    {
        private const float Spacing = 0.42f;

        private readonly Transform _root;
        private readonly ComponentPool<MeshRenderer> _pool;
        private readonly Vector3 _start;
        private readonly Vector3 _end;
        private readonly int _maxSlots;
        private readonly List<Transform> _queued = new List<Transform>(8);
        private readonly List<Transform> _moving = new List<Transform>(8);
        private readonly List<float> _movingPos = new List<float>(8);
        private long _lastCycles = -1;

        public ConveyorView(Transform root, ComponentPool<MeshRenderer> itemPool, Vector3 start, Vector3 end)
        {
            _root = root;
            _pool = itemPool;
            _start = start;
            _end = end;
            float length = Vector3.Distance(start, end);
            _maxSlots = Mathf.Max(1, Mathf.FloorToInt(length / Spacing));
        }

        private float Length => Vector3.Distance(_start, _end);

        /// <param name="beltSpeed">Belt surface speed in units per second.</param>
        public void Tick(MachineState upstream, float beltSpeed, float dt)
        {
            int desired = Mathf.Min(upstream.OutputBuffer, _maxSlots);

            if (_lastCycles < 0) _lastCycles = upstream.CyclesCompleted;
            long produced = upstream.CyclesCompleted - _lastCycles;
            _lastCycles = upstream.CyclesCompleted;

            // New output: spawn riding crates at the belt start (as many as fit the real buffer).
            int total = _queued.Count + _moving.Count;
            for (long i = 0; i < produced && total < desired; i++, total++)
            {
                var item = _pool.Get(_root).transform;
                item.localPosition = _start;
                _moving.Add(item);
                _movingPos.Add(-i * Spacing);
            }

            // Downstream consumed: remove from the head of the queue first.
            while (_queued.Count + _moving.Count > desired)
            {
                if (_queued.Count > 0) { Release(_queued[0]); _queued.RemoveAt(0); }
                else { Release(_moving[0]); _moving.RemoveAt(0); _movingPos.RemoveAt(0); }
            }
            // Missed updates (e.g. after resume): fill the queue directly.
            while (_queued.Count + _moving.Count < desired)
            {
                var item = _pool.Get(_root).transform;
                _queued.Add(item);
                item.localPosition = SlotPosition(_queued.Count - 1);
            }

            float length = Length;
            float speed = Mathf.Max(0.6f, beltSpeed);
            for (int i = 0; i < _moving.Count; i++)
            {
                float target = length - Spacing * 0.5f - (_queued.Count + i) * Spacing;
                float p = Mathf.Min(target, _movingPos[i] + speed * dt);
                _movingPos[i] = p;
                _moving[i].localPosition = Vector3.Lerp(_start, _end, Mathf.Clamp01(p / length));
            }
            // Arrivals join the queue (always the front-most moving item first).
            while (_moving.Count > 0 && _movingPos[0] >= length - Spacing * 0.5f - _queued.Count * Spacing - 1e-3f)
            {
                _queued.Add(_moving[0]);
                _moving.RemoveAt(0);
                _movingPos.RemoveAt(0);
            }
            for (int i = 0; i < _queued.Count; i++)
            {
                var t = _queued[i];
                t.localPosition = Vector3.MoveTowards(t.localPosition, SlotPosition(i), speed * dt);
            }
        }

        private Vector3 SlotPosition(int index)
        {
            float length = Length;
            float d = Mathf.Max(0f, length - Spacing * 0.5f - index * Spacing);
            return Vector3.Lerp(_start, _end, d / length);
        }

        private void Release(Transform t) => _pool.Release(t.GetComponent<MeshRenderer>());

        public void Clear()
        {
            foreach (var t in _queued) Release(t);
            foreach (var t in _moving) Release(t);
            _queued.Clear();
            _moving.Clear();
            _movingPos.Clear();
        }
    }
}
