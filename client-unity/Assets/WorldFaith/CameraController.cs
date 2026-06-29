using UnityEngine;

namespace WorldFaith.Client
{
    /// <summary>
    /// Điều khiển camera isometric bằng touch (mobile) và mouse (PC).
    /// - 1 ngón: pan (kéo map)
    /// - 2 ngón: pinch zoom
    /// - Tap nhanh: select tile/entity
    /// - Mouse: click drag pan, scroll wheel zoom
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 0.5f;
        [SerializeField] private float panSmoothing = 8f;
        [SerializeField] private Vector2 panLimitMin = new(-10f, -10f);
        [SerializeField] private Vector2 panLimitMax = new(74f, 74f);

        [Header("Zoom Settings")]
        [SerializeField] private float zoomMin = 5f;
        [SerializeField] private float zoomMax = 40f;
        [SerializeField] private float zoomSpeed = 0.05f;
        [SerializeField] private float zoomSmoothing = 8f;
        [SerializeField] private float scrollZoomSpeed = 3f;

        [Header("Tap Settings")]
        [SerializeField] private float tapMaxDuration = 0.2f;
        [SerializeField] private float tapMaxDelta = 10f;

        // Events
        public event System.Action<Vector3> OnTileSelected;
        public event System.Action<Vector3> OnTileLongPressed;

        private Camera _cam;
        private Vector3 _targetPosition;
        private float _targetZoom;

        // Touch state
        private Vector2 _lastPanPos;
        private bool _isPanning;
        private float _touchStartTime;
        private Vector2 _touchStartPos;
        private float _lastPinchDist;
        private bool _isPinching;

        // Mouse state
        private Vector3 _mouseLastPos;
        private bool _isMousePanning;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _targetPosition = transform.position;
            _targetZoom = _cam.orthographicSize > 0
                ? _cam.orthographicSize
                : (_cam.fieldOfView > 0 ? _cam.fieldOfView : 20f);
        }

        private void Update()
        {
            if (Application.isMobilePlatform || Input.touchCount > 0)
                HandleTouch();
            else
                HandleMouse();

            ApplySmoothing();
        }

        // ─── Touch Input ─────────────────────────────────────────

        private void HandleTouch()
        {
            int touchCount = Input.touchCount;

            if (touchCount == 0)
            {
                _isPanning = false;
                _isPinching = false;
                return;
            }

            if (touchCount == 1)
            {
                var touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _lastPanPos = touch.position;
                        _touchStartPos = touch.position;
                        _touchStartTime = Time.time;
                        _isPanning = true;
                        _isPinching = false;
                        break;

                    case TouchPhase.Moved:
                        if (_isPanning && !_isPinching)
                        {
                            var delta = touch.position - _lastPanPos;
                            Pan(-delta * panSpeed * (_targetZoom / 20f));
                            _lastPanPos = touch.position;
                        }
                        break;

                    case TouchPhase.Ended:
                        float duration = Time.time - _touchStartTime;
                        float moved = Vector2.Distance(touch.position, _touchStartPos);

                        if (duration < tapMaxDuration && moved < tapMaxDelta)
                            FireTileSelected(touch.position);
                        else if (duration >= 0.6f && moved < tapMaxDelta)
                            FireTileLongPressed(touch.position);

                        _isPanning = false;
                        break;
                }
            }
            else if (touchCount == 2)
            {
                _isPanning = false;
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);
                float dist = Vector2.Distance(t0.position, t1.position);

                if (!_isPinching)
                {
                    _lastPinchDist = dist;
                    _isPinching = true;
                    return;
                }

                float delta = _lastPinchDist - dist;
                _targetZoom = Mathf.Clamp(_targetZoom + delta * zoomSpeed, zoomMin, zoomMax);
                _lastPinchDist = dist;

                // Pan bằng midpoint của 2 ngón
                var midCurr = (t0.position + t1.position) * 0.5f;
                var midPrev = ((t0.position - t0.deltaPosition) + (t1.position - t1.deltaPosition)) * 0.5f;
                var midDelta = midCurr - midPrev;
                Pan(-midDelta * panSpeed * (_targetZoom / 20f));
            }
        }

        // ─── Mouse Input ─────────────────────────────────────────

        private void HandleMouse()
        {
            // Scroll zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
                _targetZoom = Mathf.Clamp(_targetZoom - scroll * scrollZoomSpeed, zoomMin, zoomMax);

            // Middle click hoặc Right click để pan
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _mouseLastPos = Input.mousePosition;
                _isMousePanning = true;
            }

            if (_isMousePanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                var delta = Input.mousePosition - _mouseLastPos;
                Pan(new Vector2(-delta.x, -delta.y) * panSpeed * (_targetZoom / 20f));
                _mouseLastPos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                _isMousePanning = false;

            // Left click để select
            if (Input.GetMouseButtonDown(0))
            {
                _mouseLastPos = Input.mousePosition;
                _touchStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(0))
            {
                float duration = Time.time - _touchStartTime;
                float moved = Vector2.Distance(Input.mousePosition, _mouseLastPos);
                if (duration < tapMaxDuration && moved < tapMaxDelta)
                    FireTileSelected(Input.mousePosition);
            }
        }

        // ─── Smooth Apply ────────────────────────────────────────

        private void ApplySmoothing()
        {
            transform.position = Vector3.Lerp(transform.position, _targetPosition, panSmoothing * Time.deltaTime);

            if (_cam.orthographic)
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, zoomSmoothing * Time.deltaTime);
            else
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetZoom, zoomSmoothing * Time.deltaTime);
        }

        // ─── Helpers ─────────────────────────────────────────────

        private void Pan(Vector2 delta)
        {
            var newPos = _targetPosition + new Vector3(delta.x, 0, delta.y) * Time.deltaTime;
            _targetPosition = new Vector3(
                Mathf.Clamp(newPos.x, panLimitMin.x, panLimitMax.x),
                _targetPosition.y,
                Mathf.Clamp(newPos.z, panLimitMin.y, panLimitMax.y)
            );
        }

        private void FireTileSelected(Vector2 screenPos)
        {
            var ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit))
                OnTileSelected?.Invoke(hit.point);
            else
            {
                // Fallback: project lên plane y=0
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float enter))
                    OnTileSelected?.Invoke(ray.GetPoint(enter));
            }
        }

        private void FireTileLongPressed(Vector2 screenPos)
        {
            var ray = _cam.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
                OnTileLongPressed?.Invoke(ray.GetPoint(enter));
        }

        public void CenterOn(Vector3 worldPos)
        {
            _targetPosition = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        }

        public void SetZoom(float zoom) => _targetZoom = Mathf.Clamp(zoom, zoomMin, zoomMax);
    }
}
