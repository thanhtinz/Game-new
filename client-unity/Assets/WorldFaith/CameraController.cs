using UnityEngine;

namespace WorldFaith.Client
{
    /// <summary>
    /// 2D top-down orthographic camera controller for WorldFaith.
    ///
    /// Setup:
    ///   - Attach to the Main Camera GameObject.
    ///   - Camera must be set to Orthographic projection in the Inspector.
    ///   - Camera Z position should be -10 (looks at Z=0 where tiles live).
    ///   - Camera rotation must be (0, 0, 0) — looking straight down the -Z axis.
    ///
    /// Controls:
    ///   Mobile  : 1 finger drag = pan  |  2 finger pinch = zoom  |  tap = select tile
    ///   PC      : Right/Middle drag = pan  |  Scroll wheel = zoom  |  Left click = select tile
    ///   Keyboard: WASD / Arrow Keys = pan
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed         = 5f;
        [SerializeField] private float panSmoothing     = 10f;
        [SerializeField] private float keyboardPanSpeed = 8f;
        [SerializeField] private Vector2 panLimitMin    = new(-2f,   -2f);
        [SerializeField] private Vector2 panLimitMax    = new(130f, 130f);

        [Header("Zoom")]
        [SerializeField] private float zoomMin          = 3f;
        [SerializeField] private float zoomMax          = 60f;
        [SerializeField] private float zoomSpeed        = 0.05f;
        [SerializeField] private float zoomSmoothing    = 10f;
        [SerializeField] private float scrollZoomSpeed  = 4f;

        [Header("Tap / Click")]
        [SerializeField] private float tapMaxDuration   = 0.2f;
        [SerializeField] private float tapMaxDelta      = 12f;

        // Events
        public event System.Action<Vector2Int> OnTileSelected;      // grid coordinates
        public event System.Action<Vector2Int> OnTileLongPressed;
        public event System.Action<Vector2>    OnWorldPointSelected; // world-space XY

        // ─── Private state ────────────────────────────────────

        private Camera _cam;
        private Vector3 _targetPosition;
        private float   _targetZoom;

        // Touch
        private int     _activeFingerId    = -1;
        private Vector2 _touchStartScreen;
        private Vector2 _touchLastScreen;
        private float   _touchStartTime;
        private float   _lastPinchDist;
        private bool    _isPinching;

        // Mouse
        private Vector3 _mousePanStart;
        private Vector3 _mouseClickStart;
        private float   _mouseClickTime;
        private bool    _isMousePanning;

        // tile size — sync with WorldRenderer.tileSize
        private float _tileSize = 1f;

        // ─── Unity lifecycle ──────────────────────────────────

        private void Awake()
        {
            _cam = GetComponent<Camera>();

            // Enforce orthographic 2D setup
            _cam.orthographic = true;
            _cam.transform.rotation = Quaternion.identity;  // no rotation needed for top-down 2D

            _targetPosition = transform.position;
            _targetZoom     = _cam.orthographicSize > 0 ? _cam.orthographicSize : 10f;
        }

        private void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
                HandleTouch();
            else
                HandleMouse();

            HandleKeyboard();
            ApplySmoothing();
        }

        // ─── Touch Input ──────────────────────────────────────

        private void HandleTouch()
        {
            int count = Input.touchCount;

            if (count == 0)
            {
                _activeFingerId = -1;
                _isPinching     = false;
                return;
            }

            // ── Single finger ──
            if (count == 1)
            {
                _isPinching = false;
                var touch   = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _activeFingerId   = touch.fingerId;
                        _touchStartScreen = touch.position;
                        _touchLastScreen  = touch.position;
                        _touchStartTime   = Time.unscaledTime;
                        break;

                    case TouchPhase.Moved:
                        if (touch.fingerId == _activeFingerId)
                        {
                            PanByScreenDelta(touch.position - _touchLastScreen);
                            _touchLastScreen = touch.position;
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.fingerId == _activeFingerId)
                        {
                            float dt    = Time.unscaledTime - _touchStartTime;
                            float moved = Vector2.Distance(touch.position, _touchStartScreen);

                            if (moved < tapMaxDelta)
                            {
                                if (dt < tapMaxDuration)
                                    FireSelection(touch.position);
                                else if (dt >= 0.5f)
                                    FireLongPress(touch.position);
                            }
                            _activeFingerId = -1;
                        }
                        break;
                }
            }
            // ── Two fingers: pinch-zoom + two-finger pan ──
            else if (count >= 2)
            {
                _activeFingerId = -1;   // cancel any single-finger action

                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);

                float dist = Vector2.Distance(t0.position, t1.position);

                if (!_isPinching)
                {
                    _lastPinchDist = dist;
                    _isPinching    = true;
                    return;
                }

                // Zoom
                float pinchDelta = (_lastPinchDist - dist) * zoomSpeed;
                _targetZoom      = Mathf.Clamp(_targetZoom + pinchDelta, zoomMin, zoomMax);
                _lastPinchDist   = dist;

                // Pan with midpoint
                Vector2 midNow  = (t0.position + t1.position) * 0.5f;
                Vector2 midPrev = ((t0.position - t0.deltaPosition) + (t1.position - t1.deltaPosition)) * 0.5f;
                PanByScreenDelta(midNow - midPrev);
            }
        }

        // ─── Mouse Input ──────────────────────────────────────

        private void HandleMouse()
        {
            // Scroll wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                _targetZoom = Mathf.Clamp(_targetZoom - scroll * scrollZoomSpeed, zoomMin, zoomMax);

            // Right / Middle mouse = pan
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _mousePanStart  = Input.mousePosition;
                _isMousePanning = true;
            }
            if (_isMousePanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector3 delta  = Input.mousePosition - _mousePanStart;
                PanByScreenDelta(delta);
                _mousePanStart = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                _isMousePanning = false;

            // Left mouse = select tile on release
            if (Input.GetMouseButtonDown(0))
            {
                _mouseClickStart = Input.mousePosition;
                _mouseClickTime  = Time.unscaledTime;
            }
            if (Input.GetMouseButtonUp(0))
            {
                float dt    = Time.unscaledTime - _mouseClickTime;
                float moved = Vector2.Distance(Input.mousePosition, _mouseClickStart);
                if (dt < tapMaxDuration && moved < tapMaxDelta)
                    FireSelection(Input.mousePosition);
            }
        }

        // ─── Keyboard Pan ─────────────────────────────────────

        private void HandleKeyboard()
        {
            float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
            float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                var kDelta = new Vector3(h, v, 0f) * keyboardPanSpeed * _targetZoom / 10f * Time.unscaledDeltaTime;
                _targetPosition = ClampPosition(_targetPosition + kDelta);
            }
        }

        // ─── Smooth Apply ─────────────────────────────────────

        private void ApplySmoothing()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                _targetPosition,
                panSmoothing * Time.unscaledDeltaTime);

            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize,
                _targetZoom,
                zoomSmoothing * Time.unscaledDeltaTime);
        }

        // ─── Pan Helpers ──────────────────────────────────────

        /// <summary>
        /// Pans the camera by a screen-space delta (pixels).
        /// Converts pixels → world units using the current orthographic size.
        /// Works correctly at any zoom level.
        /// </summary>
        private void PanByScreenDelta(Vector2 screenDelta)
        {
            // pixels per world unit  =  screen height / (2 * orthographicSize)
            float pixelsPerUnit = Screen.height / (2f * _cam.orthographicSize);
            var   worldDelta    = new Vector3(
                -screenDelta.x / pixelsPerUnit,
                -screenDelta.y / pixelsPerUnit,
                0f);

            _targetPosition = ClampPosition(_targetPosition + worldDelta);
        }

        private Vector3 ClampPosition(Vector3 pos) => new(
            Mathf.Clamp(pos.x, panLimitMin.x, panLimitMax.x),
            Mathf.Clamp(pos.y, panLimitMin.y, panLimitMax.y),
            pos.z);   // keep Z fixed at whatever it was set to

        // ─── Tile Selection ───────────────────────────────────

        /// <summary>
        /// Converts a screen position to the tile grid coordinate and fires the event.
        /// </summary>
        private void FireSelection(Vector2 screenPos)
        {
            Vector2 worldPos = ScreenToWorld2D(screenPos);
            OnWorldPointSelected?.Invoke(worldPos);

            var tileCoord = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / _tileSize),
                Mathf.FloorToInt(worldPos.y / _tileSize));
            OnTileSelected?.Invoke(tileCoord);
        }

        private void FireLongPress(Vector2 screenPos)
        {
            Vector2 worldPos  = ScreenToWorld2D(screenPos);
            var tileCoord     = new Vector2Int(
                Mathf.FloorToInt(worldPos.x / _tileSize),
                Mathf.FloorToInt(worldPos.y / _tileSize));
            OnTileLongPressed?.Invoke(tileCoord);
        }

        /// <summary>
        /// Converts screen pixel coordinates to world-space XY (Z=0 plane).
        /// </summary>
        private Vector2 ScreenToWorld2D(Vector2 screenPos)
        {
            Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -transform.position.z));
            return new Vector2(wp.x, wp.y);
        }

        // ─── Public API ───────────────────────────────────────

        /// <summary>Centers the camera on a world-space XY position.</summary>
        public void CenterOn(Vector2 worldPos)
            => _targetPosition = new Vector3(worldPos.x, worldPos.y, transform.position.z);

        /// <summary>Centers the camera on a tile grid coordinate.</summary>
        public void CenterOnTile(int tileX, int tileY)
            => CenterOn(new Vector2(tileX * _tileSize, tileY * _tileSize));

        public void SetZoom(float zoom)
            => _targetZoom = Mathf.Clamp(zoom, zoomMin, zoomMax);

        public float CurrentZoom => _cam.orthographicSize;

        /// <summary>
        /// Sync tile size from WorldRenderer so click-to-tile conversion is accurate.
        /// Call this once after WorldRenderer is initialized.
        /// </summary>
        public void SetTileSize(float size) => _tileSize = size;
    }
}
