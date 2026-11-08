using FactoryKing.Core.Simulation;
using FactoryKing.Runtime.Procedural;
using UnityEngine;

namespace FactoryKing.Runtime.Visuals
{
    /// <summary>The cargo drone: crosses the factory while <c>Drone.IsVisible</c>; tapping its collider catches it.</summary>
    public sealed class DroneView : MonoBehaviour
    {
        private GameSession _s;
        private Transform _model;
        private Transform[] _rotors;
        private Bounds _area;
        private float _t;
        private bool _wasVisible;

        public static DroneView Create(Transform parent, ModelKit kit, GameSession session)
        {
            var go = new GameObject("CargoDrone");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<DroneView>();
            v._s = session;
            v._model = ModelKit.Pivot(go.transform, "Model", Vector3.zero);
            kit.Part(v._model, "Drone.Body", Vector3.zero, b =>
            {
                b.Slot = (int)Mat.Hazard;
                b.Box(Vector3.zero, new Vector3(1.0f, 0.3f, 1.0f), 0.08f);
                b.Slot = (int)Mat.Metal;
                b.Box(new Vector3(0, 0, 0), new Vector3(2.2f, 0.08f, 0.12f), 0f);
                b.Box(new Vector3(0, 0, 0), new Vector3(0.12f, 0.08f, 2.2f), 0f);
                b.Slot = (int)Mat.Wood;
                b.Box(new Vector3(0, -0.45f, 0), new Vector3(0.6f, 0.5f, 0.6f), 0.04f); // the crate it carries
            }, true);
            v._rotors = new Transform[4];
            int i = 0;
            foreach (var p in new[] { new Vector3(1.1f, 0.08f, 0), new Vector3(-1.1f, 0.08f, 0), new Vector3(0, 0.08f, 1.1f), new Vector3(0, 0.08f, -1.1f) })
            {
                var r = kit.Part(v._model, "Drone.Rotor", p, b => { b.Slot = (int)Mat.Chrome; b.Box(Vector3.zero, new Vector3(0.9f, 0.02f, 0.1f), 0f); }, false);
                v._rotors[i++] = r.transform;
            }
            var col = go.AddComponent<SphereCollider>();
            col.radius = 2.2f; // generous touch target
            go.SetActive(true);
            v._model.gameObject.SetActive(false);
            col.enabled = false;
            return v;
        }

        public void SetArea(Bounds area) => _area = area;

        private void Update()
        {
            if (_s == null) return;
            bool visible = _s.Drone.IsVisible;
            if (visible != _wasVisible)
            {
                _wasVisible = visible;
                _model.gameObject.SetActive(visible);
                GetComponent<Collider>().enabled = visible;
                _t = 0f;
            }
            if (!visible) return;
            _t += Time.deltaTime / (float)Core.Meta.DroneService.VisibleSeconds;
            var a = new Vector3(_area.min.x - 4f, 9f, _area.max.z - 2f);
            var b = new Vector3(_area.max.x + 4f, 9f, _area.min.z + 2f);
            transform.position = Vector3.Lerp(a, b, Mathf.Repeat(_t, 1f)) + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.4f;
            foreach (var r in _rotors) r.Rotate(0f, 1440f * Time.deltaTime, 0f, Space.Self);
        }
    }
}
