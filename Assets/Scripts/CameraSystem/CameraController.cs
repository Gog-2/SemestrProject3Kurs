using UnityEngine;
using Alchemy.Inspector;
using Game.Core;

namespace Game.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [BoxGroup("References")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private GridMap _gridMap;
        [SerializeField] private Transform _initialTarget;

        [BoxGroup("Movement")]
        [SerializeField] private bool _focusOnStart = true;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 15f;
        [SerializeField] private bool _scaleSpeedWithZoom = true;
        [SerializeField, Min(0.1f)] private float _baseZoomReference = 10f;

        [BoxGroup("Zoom")]
        [SerializeField, Min(0.5f)] private float _minZoom = 3f;
        [SerializeField, Min(1f)] private float _maxZoom = 25f;
        [SerializeField, Min(0.1f)] private float _zoomStep = 2f;
        [SerializeField, Min(1f)] private float _zoomSmoothSpeed = 15f;
        [SerializeField] private bool _zoomToCursor = true;

        [BoxGroup("Edge Panning")]
        [SerializeField] private bool _useEdgeScrolling = false;
        [SerializeField, Min(1f)] private float _edgeThreshold = 20f;

        [BoxGroup("Bounds")]
        [SerializeField] private bool _clampToBounds = true;

        [BoxGroup("Debug")]
        [ShowInInspector, ReadOnly]
        public float CurrentZoom => _targetCamera != null ? _targetCamera.orthographicSize : 0f;
        public bool FocusOnStart => _focusOnStart;

        private float _targetZoom;
        private bool _inputRegistered;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
            }
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_gridMap == null)
            {
                _gridMap = FindAnyObjectByType<GridMap>();
            }

            if (_initialTarget == null)
            {
                var castle = FindAnyObjectByType<Game.Buildings.Castle>();
                if (castle != null)
                {
                    _initialTarget = castle.transform;
                }
            }

            Debug.Assert(_targetCamera != null, "[CameraController] _targetCamera is not assigned!", this);
            _targetZoom = _targetCamera != null ? _targetCamera.orthographicSize : 10f;
        }

        private void Start()
        {
            RegisterInput();
            if (_focusOnStart)
            {
                FocusOnTarget();
            }
        }

        [Button]
        public void FocusOnTarget()
        {
            if (_initialTarget == null)
            {
                var castle = FindAnyObjectByType<Game.Buildings.Castle>();
                if (castle != null)
                {
                    _initialTarget = castle.transform;
                }
            }

            if (_gridMap == null)
            {
                _gridMap = FindAnyObjectByType<GridMap>();
            }

            Vector3 targetPos;
            if (_initialTarget != null)
            {
                targetPos = _initialTarget.position;
            }
            else if (_gridMap != null)
            {
                targetPos = _gridMap.Bounds.center;
            }
            else
            {
                targetPos = transform.position;
            }
            TeleportTo(targetPos);
        }

        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            if (_clampToBounds)
            {
                ClampPositionToBounds();
            }
        }

        private void OnEnable()
        {
            RegisterInput();
        }

        private void OnDisable()
        {
            UnregisterInput();
        }

        private void RegisterInput()
        {
            if (_inputRegistered) return;
            if (InputManager.Instance == null) return;
            InputManager.Instance.TouchPanDelta += OnTouchPanDelta;
            _inputRegistered = true;
        }

        private void UnregisterInput()
        {
            if (!_inputRegistered) return;
            if (InputManager.Instance != null)
            {
                InputManager.Instance.TouchPanDelta -= OnTouchPanDelta;
            }
            _inputRegistered = false;
        }

        private void OnValidate()
        {
            if (_minZoom < 0.5f) _minZoom = 0.5f;
            if (_maxZoom <= _minZoom) _maxZoom = _minZoom + 1f;
            if (_baseZoomReference < 0.1f) _baseZoomReference = 0.1f;
            if (_edgeThreshold < 1f) _edgeThreshold = 1f;
        }

        private void LateUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (InputManager.Instance == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;

            HandleZoom(dt);
            HandleMovement(dt);

            if (_clampToBounds)
            {
                ClampPositionToBounds();
            }
        }

        private void HandleMovement(float dt)
        {
            Vector2 inputDir = InputManager.Instance.MoveInput;

            if (_useEdgeScrolling && Application.isFocused && !InputManager.Instance.HasActiveTouch)
            {
                Vector2 pointerPos = InputManager.Instance.PointerScreenPosition;
                if (pointerPos.x <= _edgeThreshold) inputDir.x -= 1f;
                else if (pointerPos.x >= Screen.width - _edgeThreshold) inputDir.x += 1f;

                if (pointerPos.y <= _edgeThreshold) inputDir.y -= 1f;
                else if (pointerPos.y >= Screen.height - _edgeThreshold) inputDir.y += 1f;
            }

            if (inputDir.sqrMagnitude > 0.0001f)
            {
                inputDir.Normalize();

                float speedMultiplier = _scaleSpeedWithZoom ? (_targetCamera.orthographicSize / _baseZoomReference) : 1f;
                Vector3 moveDelta = new Vector3(inputDir.x, inputDir.y, 0f) * (_moveSpeed * speedMultiplier * dt);
                transform.position += moveDelta;
            }
        }

        private void OnTouchPanDelta(Vector2 screenDelta)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // Convert screen delta to world delta
            Vector3 worldPosBefore = _targetCamera.ScreenToWorldPoint(new Vector3(0f, 0f, -_targetCamera.transform.position.z));
            Vector3 worldPosAfter = _targetCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, -_targetCamera.transform.position.z));
            Vector3 worldDelta = worldPosAfter - worldPosBefore;

            transform.position -= new Vector3(worldDelta.x, worldDelta.y, 0f);

            if (_clampToBounds)
            {
                ClampPositionToBounds();
            }
        }

        private void HandleZoom(float dt)
        {
            float scroll = InputManager.Instance.ZoomDelta;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _targetZoom += scroll * _zoomStep;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }

            if (Mathf.Abs(_targetCamera.orthographicSize - _targetZoom) > 0.001f)
            {
                Vector3 mouseWorldBefore = Vector3.zero;
                Vector2 mousePos = Vector2.zero;
                bool canZoomToCursor = _zoomToCursor && !InputManager.Instance.HasActiveTouch;

                float camDistance = -_targetCamera.transform.position.z;
                if (canZoomToCursor)
                {
                    mousePos = InputManager.Instance.PointerScreenPosition;
                    mouseWorldBefore = _targetCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, camDistance));
                }

                _targetCamera.orthographicSize = Mathf.Lerp(_targetCamera.orthographicSize, _targetZoom, _zoomSmoothSpeed * dt);

                if (canZoomToCursor)
                {
                    Vector3 mouseWorldAfter = _targetCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, camDistance));
                    Vector3 diff = mouseWorldBefore - mouseWorldAfter;
                    transform.position += new Vector3(diff.x, diff.y, 0f);
                }
            }
        }

        private void ClampPositionToBounds()
        {
            if (_gridMap == null)
            {
                _gridMap = FindAnyObjectByType<GridMap>();
            }

            if (_targetCamera == null)
            {
                _targetCamera = GetComponent<Camera>();
                if (_targetCamera == null) _targetCamera = Camera.main;
            }

            if (_gridMap == null || _targetCamera == null) return;

            Bounds bounds = _gridMap.Bounds;
            float camVertExtent = _targetCamera.orthographicSize;
            float camHorzExtent = camVertExtent * _targetCamera.aspect;

            Vector3 pos = transform.position;

            // Allow extra margin when camera view exceeds map size so movement is not locked to center
            float paddingX = Mathf.Max(0f, camHorzExtent - bounds.size.x * 0.5f);
            float paddingY = Mathf.Max(0f, camVertExtent - bounds.size.y * 0.5f);

            float minX = bounds.min.x + camHorzExtent - paddingX;
            float maxX = bounds.max.x - camHorzExtent + paddingX;
            if (minX > maxX) (minX, maxX) = (maxX, minX);
            pos.x = Mathf.Clamp(pos.x, minX, maxX);

            float minY = bounds.min.y + camVertExtent - paddingY;
            float maxY = bounds.max.y - camVertExtent + paddingY;
            if (minY > maxY) (minY, maxY) = (maxY, minY);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            transform.position = pos;
        }
    }
}
