using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Game.Core
{
    [DefaultExecutionOrder(-1000)]
    public class InputManager : MonoBehaviour
    {
        private static InputManager _instance;
        public static InputManager Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        [Header("Touch Settings")]
        [SerializeField, Min(1f)] private float _tapThresholdPixels = 20f;
        [SerializeField, Min(0.05f)] private float _maxTapDuration = 0.35f;
        [SerializeField, Min(0.001f)] private float _pinchSensitivity = 0.01f;

        private Sheme _sheme;

        // Touch tracking
        private bool _touchStartedOverUI;
        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private bool _hasPanned;
        private float _touchPinchDelta;

        // Events
        public event Action<Vector2> PrimaryClicked;
        public event Action SecondaryClicked;
        public event Action CancelPressed;
        public event Action PausePressed;
        public event Action<int> SlotSelected;
        public event Action SellPressed;
        public event Action MoveBuildingPressed;
        public event Action UpgradePressed;
        public event Action ToggleAimPressed;
        public event Action SkipPressed;
        public event Action<Vector2> TouchPanDelta;

        // Properties
        public Vector2 MoveInput { get; private set; }
        public float ZoomDelta { get; private set; }
        public Vector2 PointerScreenPosition { get; private set; }
        public bool IsShiftHeld { get; private set; }
        public bool HasActiveTouch => Touch.activeTouches.Count > 0;
        public bool IsReady => _sheme != null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnhancedTouchSupport.Enable();

            if (_sheme == null)
            {
                _sheme = new Sheme();
                RegisterActions();
            }
        }

        private void OnEnable()
        {
            if (_sheme == null)
            {
                _sheme = new Sheme();
                RegisterActions();
            }

            _sheme.Enable();

            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }
        }

        private void OnDisable()
        {
            _sheme?.Disable();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (_sheme != null)
            {
                UnregisterActions();
                _sheme.Dispose();
                _sheme = null;
            }
        }

        private void RegisterActions()
        {
            if (_sheme == null) return;

            UnregisterActions();

            if (_sheme.Player.ToggleAim == null)
            {
                Debug.LogError("[InputManager] Input action 'ToggleAim' was not found in Sheme.", this);
                return;
            }

            _sheme.Player.PrimaryAction.performed += OnPrimaryActionPerformed;
            _sheme.Player.SecondaryAction.performed += OnSecondaryActionPerformed;
            _sheme.Player.Cancel.performed += OnCancelActionPerformed;
            _sheme.Player.Pause.performed += OnPauseActionPerformed;

            _sheme.Player.SelectSlot1.performed += OnSelectSlot1;
            _sheme.Player.SelectSlot2.performed += OnSelectSlot2;
            _sheme.Player.SelectSlot3.performed += OnSelectSlot3;
            _sheme.Player.SelectSlot4.performed += OnSelectSlot4;
            _sheme.Player.SelectSlot5.performed += OnSelectSlot5;

            _sheme.Player.Sell.performed += OnSell;
            _sheme.Player.MoveBuilding.performed += OnMoveBuilding;
            _sheme.Player.Upgrade.performed += OnUpgrade;
            _sheme.Player.ToggleAim.performed += OnToggleAim;
            _sheme.Player.Skip.performed += OnSkip;
        }

        private void UnregisterActions()
        {
            if (_sheme == null) return;

            _sheme.Player.PrimaryAction.performed -= OnPrimaryActionPerformed;
            _sheme.Player.SecondaryAction.performed -= OnSecondaryActionPerformed;
            _sheme.Player.Cancel.performed -= OnCancelActionPerformed;
            _sheme.Player.Pause.performed -= OnPauseActionPerformed;

            _sheme.Player.SelectSlot1.performed -= OnSelectSlot1;
            _sheme.Player.SelectSlot2.performed -= OnSelectSlot2;
            _sheme.Player.SelectSlot3.performed -= OnSelectSlot3;
            _sheme.Player.SelectSlot4.performed -= OnSelectSlot4;
            _sheme.Player.SelectSlot5.performed -= OnSelectSlot5;

            _sheme.Player.Sell.performed -= OnSell;
            _sheme.Player.MoveBuilding.performed -= OnMoveBuilding;
            _sheme.Player.Upgrade.performed -= OnUpgrade;
            _sheme.Player.ToggleAim.performed -= OnToggleAim;
            _sheme.Player.Skip.performed -= OnSkip;
        }

        private void OnSelectSlot1(InputAction.CallbackContext ctx) => SlotSelected?.Invoke(0);
        private void OnSelectSlot2(InputAction.CallbackContext ctx) => SlotSelected?.Invoke(1);
        private void OnSelectSlot3(InputAction.CallbackContext ctx) => SlotSelected?.Invoke(2);
        private void OnSelectSlot4(InputAction.CallbackContext ctx) => SlotSelected?.Invoke(3);
        private void OnSelectSlot5(InputAction.CallbackContext ctx) => SlotSelected?.Invoke(4);
        private void OnSell(InputAction.CallbackContext ctx) => SellPressed?.Invoke();
        private void OnMoveBuilding(InputAction.CallbackContext ctx) => MoveBuildingPressed?.Invoke();
        private void OnUpgrade(InputAction.CallbackContext ctx) => UpgradePressed?.Invoke();
        private void OnToggleAim(InputAction.CallbackContext ctx) => ToggleAimPressed?.Invoke();
        private void OnSkip(InputAction.CallbackContext ctx) => SkipPressed?.Invoke();

        private void OnPrimaryActionPerformed(InputAction.CallbackContext context)
        {
            // If primary action is triggered by mouse or gamepad (not touch gesture)
            if (Touch.activeTouches.Count == 0)
            {
                Vector2 screenPos = _sheme != null ? _sheme.Player.PointerPosition.ReadValue<Vector2>() : PointerScreenPosition;
                if (screenPos == Vector2.zero && PointerScreenPosition != Vector2.zero)
                {
                    screenPos = PointerScreenPosition;
                }
                PointerScreenPosition = screenPos;

                if (!IsPointerOverUI(screenPos))
                {
                    PrimaryClicked?.Invoke(screenPos);
                }
            }
        }

        private void OnSecondaryActionPerformed(InputAction.CallbackContext context)
        {
            SecondaryClicked?.Invoke();
        }

        private void OnCancelActionPerformed(InputAction.CallbackContext context)
        {
            CancelPressed?.Invoke();
        }

        private void OnPauseActionPerformed(InputAction.CallbackContext context)
        {
            PausePressed?.Invoke();
        }

        private void Update()
        {
            UpdateContinuousInputs();
            HandleTouchGestures();
        }

        private void UpdateContinuousInputs()
        {
            if (_sheme == null) return;

            // Move
            MoveInput = _sheme.Player.Move.ReadValue<Vector2>();

            // Pointer position
            Vector2 ptr = _sheme.Player.PointerPosition.ReadValue<Vector2>();
            if (Touch.activeTouches.Count > 0)
            {
                ptr = Touch.activeTouches[0].screenPosition;
            }
            PointerScreenPosition = ptr;

            // Shift modifier
            IsShiftHeld = _sheme.Player.ShiftModifier.IsPressed();

            // Zoom
            float scroll = _sheme.Player.Scroll.ReadValue<float>();
            ZoomDelta = scroll + _touchPinchDelta;
            _touchPinchDelta = 0f; // Reset pinch delta after consumption

            // Hotkeys 6-9 for expanded slot bars
            if (_sheme.Player.enabled && Keyboard.current != null)
            {
                if (Keyboard.current.digit6Key.wasPressedThisFrame) SlotSelected?.Invoke(5);
                else if (Keyboard.current.digit7Key.wasPressedThisFrame) SlotSelected?.Invoke(6);
                else if (Keyboard.current.digit8Key.wasPressedThisFrame) SlotSelected?.Invoke(7);
                else if (Keyboard.current.digit9Key.wasPressedThisFrame) SlotSelected?.Invoke(8);
            }
        }

        private void HandleTouchGestures()
        {
            var touches = Touch.activeTouches;

            if (touches.Count == 1)
            {
                Touch touch = touches[0];

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _touchStartPos = touch.screenPosition;
                        _touchStartTime = Time.unscaledTime;
                        _touchStartedOverUI = IsPointerOverUI(touch.screenPosition);
                        _hasPanned = false;
                        break;

                    case TouchPhase.Moved:
                        if (!_touchStartedOverUI)
                        {
                            Vector2 delta = touch.delta;
                            if (!_hasPanned && (touch.screenPosition - _touchStartPos).sqrMagnitude > (_tapThresholdPixels * _tapThresholdPixels))
                            {
                                _hasPanned = true;
                            }

                            if (_hasPanned)
                            {
                                TouchPanDelta?.Invoke(delta);
                            }
                        }
                        break;

                    case TouchPhase.Ended:
                        if (!_touchStartedOverUI && !_hasPanned)
                        {
                            float duration = Time.unscaledTime - _touchStartTime;
                            if (duration <= _maxTapDuration && (touch.screenPosition - _touchStartPos).sqrMagnitude <= (_tapThresholdPixels * _tapThresholdPixels))
                            {
                                if (!IsPointerOverUI(touch.screenPosition))
                                {
                                    PrimaryClicked?.Invoke(touch.screenPosition);
                                }
                            }
                        }
                        _touchStartedOverUI = false;
                        _hasPanned = false;
                        break;

                    case TouchPhase.Canceled:
                        _touchStartedOverUI = false;
                        _hasPanned = false;
                        break;
                }
            }
            else if (touches.Count >= 2)
            {
                Touch touch0 = touches[0];
                Touch touch1 = touches[1];

                // Pinch gesture
                Vector2 prevPos0 = touch0.screenPosition - touch0.delta;
                Vector2 prevPos1 = touch1.screenPosition - touch1.delta;

                float prevDist = Vector2.Distance(prevPos0, prevPos1);
                float currDist = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

                float deltaDist = currDist - prevDist;
                // Pinch out (fingers apart) zooms in (negative zoom delta in camera controller's convention or positive)
                // Note: Scroll action in Sheme: negative = scroll up (zoom in), positive = scroll down (zoom out)
                _touchPinchDelta = -deltaDist * _pinchSensitivity;
                _hasPanned = true; // Avoid accidental taps after pinch
            }
        }

        public Vector3 GetPointerWorldPosition(Camera targetCamera = null)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return Vector3.zero;

            Vector2 screenPos = PointerScreenPosition;
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            worldPos.z = 0f;
            return worldPos;
        }

        public bool IsPointerOverUI()
        {
            return IsPointerOverUI(PointerScreenPosition);
        }

        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        // Manual triggers for UI buttons (e.g. mobile HUD buttons)
        public void TriggerPause() => PausePressed?.Invoke();
        public void TriggerCancel() => CancelPressed?.Invoke();
        public void TriggerToggleAim() => ToggleAimPressed?.Invoke();
        public void TriggerSlot(int index) => SlotSelected?.Invoke(index);
    }
}
