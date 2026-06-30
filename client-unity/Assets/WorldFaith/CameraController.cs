using UnityEngine;
using UnityEngine.InputSystem;

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
    ///   - Requires the Input System package (com.unity.inputsystem). Works correctly
    ///     regardless of the project's Active Input Handling setting (Input Manager (Old),
    ///     Input System Package (New), or Both) — it never touches the Legacy
    ///     UnityEngine.Input API, so there's nothing to misconfigure.
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
        [SerializeField] private float scrollZoomSpeed  = 0.02f; // InputSystem scroll deltas are larger than legacy GetAxis

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

        // Touch (per-finger tracking via Touchscreen.current.touches)
        private int     _activeTouchId     = -1;
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
            bool hasActiveTouch = Touchscreen.current != null && Touchscreen.current.touches.Count > 0
                && AnyTouchInProgress();

            if (hasActiveTouch)
                HandleTouch();
            else
                HandleMouse();

            HandleKeyboard();
            ApplySmoothing();
        }

        private static bool AnyTouchInProgress()
        {
            var ts = Touchscreen.current;
            if (ts == null) return false;
            foreach (var t in ts.touches)
            {
                var phase = t.phase.ReadValue();
                if (phase is UnityEngine.InputSystem.TouchPhase.Began
                    or UnityEngine.InputSystem.TouchPhase.Moved
                    or UnityEngine.InputSystem.TouchPhase.Stationary
                    or UnityEngine.InputSystem.TouchPhase.Ended)
                    return true;
            }
            return false;
        }

        // ─── Touch Input ──────────────────────────────────────

        private void HandleTouch()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;

            // Collect currently active touches (Began/Moved/Stationary/Ended this frame)
            var active = new System.Collections.Generic.List<TouchControl>();
            foreach (var t in ts.touches)
            {
                var phase = t.phase.ReadValue();
                if (phase is UnityEngine.InputSystem.TouchPhase.Began
                    or UnityEngine.InputSystem.TouchPhase.Moved
                    or UnityEngine.InputSystem.TouchPhase.Stationary
                    or UnityEngine.InputSystem.TouchPhase.Ended)
                    active.Add(t);
            }

            if (active.Count == 0)
            {
                _activeTouchId = -1;
                _isPinching    = false;
                return;
            }

            // ── Single finger ──
            if (active.Count == 1)
            {
                _isPinching = false;
                var touch  = active[0];
                int id     = touch.touchId.ReadValue();
                var phase  = touch.phase.ReadValue();
                Vector2 pos = touch.position.ReadValue();

                switch (phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        _activeTouchId    = id;
                        _touchStartScreen = pos;
                        _touchLastScreen  = pos;
                        _touchStartTime   = Time.unscaledTime;
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Moved:
                    case UnityEngine.InputSystem.TouchPhase.Stationary:
                        if (id == _activeTouchId)
                        {
                            PanByScreenDelta(pos - _touchLastScreen);
                            _touchLastScreen = pos;
                        }
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        if (id == _activeTouchId)
                        {
                            float dt    = Time.unscaledTime - _touchStartTime;
                            float moved = Vector2.Distance(pos, _touchStartScreen);

                            if (moved < tapMaxDelta)
                            {
                                if (dt < tapMaxDuration)
                                    FireSelection(pos);
                                else if (dt >= 0.5f)
                                    FireLongPress(pos);
                            }
                            _activeTouchId = -1;
                        }
                        break;
                }
            }
            // ── Two fingers: pinch-zoom + two-finger pan ──
            else if (active.Count >= 2)
            {
                _activeTouchId = -1;   // cancel any single-finger action

                Vector2 p0 = active[0].position.ReadValue();
                Vector2 p1 = active[1].position.ReadValue();
                Vector2 d0 = active[0].delta.ReadValue();
                Vector2 d1 = active[1].delta.ReadValue();

                float dist = Vector2.Distance(p0, p1);

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
                Vector2 midNow  = (p0 + p1) * 0.5f;
                Vector2 midPrev = ((p0 - d0) + (p1 - d1)) * 0.5f;
                PanByScreenDelta(midNow - midPrev);
            }
        }

        // ─── Mouse Input ──────────────────────────────────────

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Scroll wheel zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                _targetZoom = Mathf.Clamp(_targetZoom - scroll * scrollZoomSpeed, zoomMin, zoomMax);

            // Right / Middle mouse = pan
            bool rightDown  = mouse.rightButton.wasPressedThisFrame;
            bool middleDown = mouse.middleButton.wasPressedThisFrame;
            bool rightHeld  = mouse.rightButton.isPressed;
            bool middleHeld = mouse.middleButton.isPressed;
            bool rightUp    = mouse.rightButton.wasReleasedThisFrame;
            bool middleUp   = mouse.middleButton.wasReleasedThisFrame;

            if (rightDown || middleDown)
            {
                _mousePanStart  = mouse.position.ReadValue();
                _isMousePanning = true;
            }
            if (_isMousePanning && (rightHeld || middleHeld))
            {
                Vector3 current = mouse.position.ReadValue();
                Vector3 delta   = current - _mousePanStart;
                PanByScreenDelta(delta);
                _mousePanStart = current;
            }
            if (rightUp || middleUp)
                _isMousePanning = false;

            // Left mouse = select tile on release
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _mouseClickStart = mouse.position.ReadValue();
                _mouseClickTime  = Time.unscaledTime;
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                Vector3 current = mouse.position.ReadValue();
                float dt    = Time.unscaledTime - _mouseClickTime;
                float moved = Vector2.Distance(current, _mouseClickStart);
                if (dt < tapMaxDuration && moved < tapMaxDelta)
                    FireSelection(current);
            }
        }

        // ─── Keyboard Pan ─────────────────────────────────────

        private void HandleKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float h = 0f, v = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;

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
