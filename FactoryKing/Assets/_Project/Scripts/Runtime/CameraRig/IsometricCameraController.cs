using System;
using FactoryKing.Runtime.InputHandling;
using UnityEngine;

namespace FactoryKing.Runtime.CameraRig
{
    /// <summary>
    /// Isometric-style perspective camera: one-finger pan, pinch / wheel zoom, inertia, soft bounds, tap-to-select
    /// and smooth focus. The camera orbits a ground-plane target at a fixed yaw/pitch.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraController : MonoBehaviour
    {
        [SerializeField] private float yaw = -35f;
        [SerializeField] private float pitch = 42f;
        [SerializeField] private float fieldOfView = 30f;
        [SerializeField] private float minDistance = 14f;
        [SerializeField] private float maxDistance = 75f;
        [SerializeField] private float zoomSmoothing = 10f;
        [SerializeField] private float panSmoothing = 14f;
        [SerializeField] private float inertiaDamping = 4f;
        [SerializeField] private float tapMaxPixels = 18f;
        [SerializeField] private float tapMaxSeconds = 0.35f;

        private Camera _camera;
        private Vector3 _target;
        private Vector3 _targetGoal;
        private float _distance = 40f;
        private float _distanceGoal = 40f;
        private Vector3 _velocity;
        private Bounds _bounds = new Bounds(Vector3.zero, new Vector3(60, 1, 60));

        private bool _dragging;
        private Vector3 _dragWorldStart;
        private Vector2 _pressPos;
        private float _pressTime;
        private bool _maybeTap;
        private float _lastPinchDistance;
        private Vector3 _lastTarget;

        /// <summary>Returns true when the given screen position is over UI (input is then ignored).</summary>
        public Func<Vector2, bool> IsPointerOverUi;

        /// <summary>Raised on a tap with the collider hit (null when tapping empty space).</summary>
        public event Action<Collider> Tapped;

        public Camera Camera => _camera;

        /// <summary>Title screen: slow cinematic orbit, input ignored.</summary>
        public bool AttractMode { get; set; }
        private float _attractYaw;

        /// <summary>Re-frames the whole factory (HUD "home" button).</summary>
        public void ResetView() => SetBounds(_bounds, true);

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.fieldOfView = fieldOfView;
            _camera.nearClipPlane = 0.5f;
            _camera.farClipPlane = 400f;
            ApplyTransform();
        }

        public void SetBounds(Bounds bounds, bool frame)
        {
            _bounds = bounds;
            if (!frame) return;
            _targetGoal = _target = new Vector3(bounds.center.x, 0, bounds.center.z);
            // Fit the larger horizontal extent, taking portrait aspect into account.
            float aspect = Mathf.Max(0.3f, _camera != null ? _camera.aspect : 0.56f);
            float extent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float fit = extent / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Clamp(aspect * 1.6f, 0.6f, 1.6f);
            _distanceGoal = _distance = Mathf.Clamp(fit, minDistance, maxDistance);
            ApplyTransform();
        }

        public void Focus(Vector3 worldPoint, float distance = -1f)
        {
            _targetGoal = ClampToBounds(new Vector3(worldPoint.x, 0, worldPoint.z));
            if (distance > 0) _distanceGoal = Mathf.Clamp(distance, minDistance, maxDistance);
            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (AttractMode)
            {
                _attractYaw += dt * 6f;
                Quaternion r = Quaternion.Euler(pitch - 6f, yaw + _attractYaw, 0f);
                Vector3 c = new Vector3(_bounds.center.x, 0f, _bounds.center.z);
                transform.SetPositionAndRotation(c - r * Vector3.forward * (_distance * 0.9f), r);
                return;
            }
            HandleInput(dt);

            if (!_dragging && _velocity.sqrMagnitude > 1e-4f)
            {
                _targetGoal += _velocity * dt;
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, 1f - Mathf.Exp(-inertiaDamping * dt));
            }
            _targetGoal = ClampToBounds(_targetGoal);
            _target = Vector3.Lerp(_target, _targetGoal, 1f - Mathf.Exp(-panSmoothing * dt));
            _distance = Mathf.Lerp(_distance, _distanceGoal, 1f - Mathf.Exp(-zoomSmoothing * dt));
            ApplyTransform();
        }

        private void HandleInput(float dt)
        {
            int count = PointerInput.PointerCount;

            // Wheel zoom (editor / iPad trackpad).
            float scroll = PointerInput.ScrollDelta;
            if (Mathf.Abs(scroll) > 0.01f && !(IsPointerOverUi?.Invoke(Input.mousePosition) ?? false))
                _distanceGoal = Mathf.Clamp(_distanceGoal * (1f - scroll * 0.1f), minDistance, maxDistance);

            if (count >= 2)
            {
                // Pinch zoom + two-finger pan.
                Vector2 a = PointerInput.Position(0), b = PointerInput.Position(1);
                float d = Vector2.Distance(a, b);
                if (_lastPinchDistance > 0f && d > 1f)
                    _distanceGoal = Mathf.Clamp(_distanceGoal * (_lastPinchDistance / d), minDistance, maxDistance);
                _lastPinchDistance = d;
                _dragging = false;
                _maybeTap = false;
                return;
            }
            _lastPinchDistance = 0f;

            if (PointerInput.PrimaryDown)
            {
                Vector2 pos = PointerInput.Position(0);
                if (IsPointerOverUi != null && IsPointerOverUi(pos)) return;
                if (GroundPoint(pos, out _dragWorldStart))
                {
                    _dragging = true;
                    _maybeTap = true;
                    _pressPos = pos;
                    _pressTime = Time.unscaledTime;
                    _velocity = Vector3.zero;
                    _lastTarget = _targetGoal;
                }
            }

            if (_dragging && count == 1)
            {
                Vector2 pos = PointerInput.Position(0);
                if ((pos - _pressPos).magnitude > tapMaxPixels) _maybeTap = false;
                if (!_maybeTap && GroundPointFrom(pos, _targetGoal, out var now))
                {
                    Vector3 delta = _dragWorldStart - now;
                    _targetGoal = ClampToBounds(_targetGoal + new Vector3(delta.x, 0, delta.z));
                    if (dt > 0f) _velocity = Vector3.Lerp(_velocity, (_targetGoal - _lastTarget) / dt, 0.5f);
                    _lastTarget = _targetGoal;
                }
            }

            if (_dragging && (PointerInput.PrimaryUp || count == 0))
            {
                _dragging = false;
                if (_maybeTap && Time.unscaledTime - _pressTime <= tapMaxSeconds)
                {
                    _velocity = Vector3.zero;
                    var ray = _camera.ScreenPointToRay(_pressPos);
                    Tapped?.Invoke(Physics.Raycast(ray, out var hit, 500f) ? hit.collider : null);
                }
            }
        }

        private bool GroundPoint(Vector2 screen, out Vector3 world) => GroundPointFrom(screen, _targetGoal, out world);

        /// <summary>Projects a screen point onto the ground using the camera placed at <paramref name="target"/>.</summary>
        private bool GroundPointFrom(Vector2 screen, Vector3 target, out Vector3 world)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 camPos = target - rot * Vector3.forward * _distance;
            Vector3 oldPos = _camera.transform.position;
            Quaternion oldRot = _camera.transform.rotation;
            _camera.transform.SetPositionAndRotation(camPos, rot);
            Ray ray = _camera.ScreenPointToRay(screen);
            _camera.transform.SetPositionAndRotation(oldPos, oldRot);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                world = ray.GetPoint(enter);
                return true;
            }
            world = default;
            return false;
        }

        private Vector3 ClampToBounds(Vector3 p) =>
            new Vector3(Mathf.Clamp(p.x, _bounds.min.x, _bounds.max.x), 0f, Mathf.Clamp(p.z, _bounds.min.z, _bounds.max.z));

        private void ApplyTransform()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(_target - rot * Vector3.forward * _distance, rot);
        }
    }
}
