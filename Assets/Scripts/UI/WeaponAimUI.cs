using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Alchemy.Inspector;
using Game.Weapons;
using Game.Core;

namespace Game.UI
{
    public class WeaponAimUI : MonoBehaviour, IPointerClickHandler
    {
        [Title("References")]
        [SerializeField] private CastleWeapon _weapon;
        [SerializeField] private Camera _mainCamera;

        [Title("Aim Visuals")]
        [SerializeField] private LineRenderer _aimLine;
        [SerializeField] private Transform _aimReticle;
        [SerializeField] private Color _aimColor = new(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private float _reticleRadius = 0.45f;
        [SerializeField] private int _reticleSegments = 48;

        [Title("Cooldown Display")]
        [SerializeField] private TMP_Text _cooldownText;

        private bool _inputRegistered;
        private bool _visualsVisible;
        private GameObject _aimVisualsRoot;
        private Material _aimMaterial;
        private LineRenderer _reticleLine;
        private LineRenderer _rangeLine;
        private float _lastBuiltRange = -1f;
        private float _lastBuiltReticleRadius = -1f;

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_cooldownText != null)
            {
                _cooldownText.raycastTarget = false;
            }
        }

        private void Start()
        {
            if (_weapon == null)
            {
                Debug.LogError($"[{nameof(WeaponAimUI)}] '_weapon' is not assigned in Inspector or bound via SceneBootstrapper!", this);
            }
            else
            {
                _weapon.CooldownChanged -= OnCooldownChanged;
                _weapon.CooldownChanged += OnCooldownChanged;
                _weapon.AimingToggled -= SetAimVisuals;
                _weapon.AimingToggled += SetAimVisuals;
            }

            EnsureAimVisuals();
            SetAimVisuals(false);
            RegisterInput();
            UpdateCooldownText();
        }

        public void BindWeapon(CastleWeapon weapon)
        {
            if (_weapon == weapon && _weapon != null)
            {
                UpdateCooldownText();
                return;
            }

            if (_weapon != null)
            {
                _weapon.CooldownChanged -= OnCooldownChanged;
                _weapon.AimingToggled -= SetAimVisuals;
            }

            _weapon = weapon;

            if (_weapon != null)
            {
                _weapon.CooldownChanged -= OnCooldownChanged;
                _weapon.CooldownChanged += OnCooldownChanged;
                _weapon.AimingToggled -= SetAimVisuals;
                _weapon.AimingToggled += SetAimVisuals;
            }

            if (_cooldownText != null)
            {
                _cooldownText.raycastTarget = false;
            }

            EnsureAimVisuals();
            SetAimVisuals(_weapon != null && _weapon.IsAiming);
            UpdateCooldownText();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }

            if (_weapon != null)
            {
                _weapon.CooldownChanged -= OnCooldownChanged;
                _weapon.CooldownChanged += OnCooldownChanged;
                _weapon.AimingToggled -= SetAimVisuals;
                _weapon.AimingToggled += SetAimVisuals;
            }

            RegisterInput();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }

            if (_weapon != null)
            {
                _weapon.CooldownChanged -= OnCooldownChanged;
                _weapon.AimingToggled -= SetAimVisuals;
            }

            UnregisterInput();
            SetAimVisuals(false);
        }

        private void OnDestroy()
        {
            UnregisterInput();

            if (_aimVisualsRoot != null)
            {
                Destroy(_aimVisualsRoot);
            }

            if (_aimMaterial != null)
            {
                Destroy(_aimMaterial);
            }
        }

        private void RegisterInput()
        {
            if (_inputRegistered) return;
            if (InputManager.Instance == null) return;

            InputManager.Instance.ToggleAimPressed += OnToggleAimPressed;
            InputManager.Instance.CancelPressed += OnCancelPressed;
            InputManager.Instance.SecondaryClicked += OnCancelPressed;
            InputManager.Instance.PrimaryClicked += OnPrimaryClicked;
            _inputRegistered = true;
        }

        private void UnregisterInput()
        {
            if (!_inputRegistered) return;

            if (InputManager.Instance == null)
            {
                _inputRegistered = false;
                return;
            }

            InputManager.Instance.ToggleAimPressed -= OnToggleAimPressed;
            InputManager.Instance.CancelPressed -= OnCancelPressed;
            InputManager.Instance.SecondaryClicked -= OnCancelPressed;
            InputManager.Instance.PrimaryClicked -= OnPrimaryClicked;
            _inputRegistered = false;
        }

        private void Update()
        {
            if (_weapon == null) return;

            bool aiming = _weapon.IsAiming &&
                          (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing);

            if (aiming)
            {
                EnsureAimVisuals();

                if (!_visualsVisible)
                {
                    SetAimVisuals(true);
                }

                UpdateAimVisuals();
            }
            else if (_visualsVisible)
            {
                SetAimVisuals(false);
            }
        }

        private void OnToggleAimPressed()
        {
#if UNITY_EDITOR
            Debug.Log("[WeaponAimUI] ToggleAim pressed.", this);
#endif
            ToggleAiming();
        }

        public void ToggleAiming()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_weapon == null) return;

            _weapon.ToggleAiming();
            if (_weapon.IsAiming)
            {
                Game.Buildings.BuildingManager.Instance?.CancelAction();
                Game.Spells.SpellScrollManager.Instance?.CancelSelection();
            }
        }

        private void OnCancelPressed()
        {
            if (_weapon != null && _weapon.IsAiming)
            {
                _weapon.CancelAiming();
            }
        }

        private void OnPrimaryClicked(Vector2 screenPos)
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_weapon == null || !_weapon.IsAiming) return;
            if (InputManager.Instance == null) return;
            if (InputManager.Instance.IsPointerOverUI(screenPos)) return;

            Vector3 mouseWorldPos = GetPointerWorldPosition();
            _weapon.TryFire(mouseWorldPos);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleAiming();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Playing)
            {
                if (_weapon != null && _weapon.IsAiming)
                {
                    _weapon.CancelAiming();
                }

                SetAimVisuals(false);
            }
        }

        private Vector3 GetPointerWorldPosition()
        {
            if (InputManager.Instance == null) return Vector3.zero;

            return InputManager.Instance.GetPointerWorldPosition(_mainCamera);
        }

        private void OnCooldownChanged(float current, float max)
        {
            UpdateCooldownText();
        }

        private void UpdateCooldownText()
        {
            if (_cooldownText == null || _weapon == null) return;

            if (_weapon.IsAiming)
            {
                _cooldownText.text = "AIMING";
                return;
            }

            _cooldownText.text = _weapon.IsReady ? "READY (V)" : $"{_weapon.CooldownTimer:F1}s";
        }

        private void SetAimVisuals(bool active)
        {
            _visualsVisible = active;

            if (_aimLine != null)
            {
                _aimLine.enabled = active;
            }

            if (_aimReticle != null)
            {
                _aimReticle.gameObject.SetActive(active);
            }

            if (_rangeLine != null)
            {
                _rangeLine.enabled = active && _weapon != null && _weapon.MaxRange > 0.01f;
            }

            if (active)
            {
                UpdateAimVisuals();
            }

            UpdateCooldownText();
        }

        private void UpdateAimVisuals()
        {
            if (InputManager.Instance == null || _weapon == null) return;

            Vector3 target = GetPointerWorldPosition();
            target.z = 0f;

            _weapon.RotateTowardsWorldPoint(target);

            bool inRange = _weapon.IsTargetInRange(target);
            Color currentColor = inRange ? _aimColor : new Color(1f, 0.25f, 0.25f, 0.9f);

            float targetReticleRadius = Mathf.Max(0.25f, _weapon.AoERadius);
            if (_reticleLine != null && !Mathf.Approximately(_lastBuiltReticleRadius, targetReticleRadius))
            {
                BuildCirclePoints(_reticleLine, targetReticleRadius);
                _lastBuiltReticleRadius = targetReticleRadius;
            }

            if (_aimReticle != null)
            {
                _aimReticle.position = target;
            }

            if (_reticleLine != null)
            {
                _reticleLine.startColor = currentColor;
                _reticleLine.endColor = currentColor;
            }

            if (_aimLine != null)
            {
                Vector3 origin = _weapon.AimOrigin;
                origin.z = 0f;

                _aimLine.positionCount = 2;
                _aimLine.SetPosition(0, origin);
                _aimLine.SetPosition(1, target);
                _aimLine.startColor = currentColor;
                _aimLine.endColor = currentColor;
            }

            if (_rangeLine != null)
            {
                float maxRange = _weapon.MaxRange;
                if (maxRange > 0.01f)
                {
                    _rangeLine.enabled = true;
                    Vector3 origin = _weapon.AimOrigin;
                    origin.z = 0f;
                    _rangeLine.transform.position = origin;
                    if (!Mathf.Approximately(_lastBuiltRange, maxRange))
                    {
                        BuildCirclePoints(_rangeLine, maxRange);
                        _lastBuiltRange = maxRange;
                    }
                }
                else
                {
                    _rangeLine.enabled = false;
                }
            }
        }

        private void EnsureAimVisuals()
        {
            if (_weapon == null) return;
            if (_aimLine != null && _aimReticle != null && _reticleLine != null && _rangeLine != null) return;

            if (_aimVisualsRoot == null)
            {
                if (_aimLine != null && _aimLine.transform.parent != null)
                {
                    _aimVisualsRoot = _aimLine.transform.parent.gameObject;
                }
                else if (_aimReticle != null && _aimReticle.parent != null)
                {
                    _aimVisualsRoot = _aimReticle.parent.gameObject;
                }
                else
                {
                    _aimVisualsRoot = new GameObject("WeaponAimVisuals");
                    _aimVisualsRoot.transform.SetParent(_weapon.transform, false);
                }
            }

            if (_aimMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                }

                if (shader != null)
                {
                    _aimMaterial = new Material(shader) { color = _aimColor };
                }
                else
                {
                    Debug.LogWarning("[WeaponAimUI] Could not find a shader for the aim indicator.", this);
                }
            }

            if (_aimLine == null)
            {
                var lineGo = new GameObject("AimLine");
                lineGo.transform.SetParent(_aimVisualsRoot.transform, false);

                _aimLine = lineGo.AddComponent<LineRenderer>();
                _aimLine.useWorldSpace = true;
                _aimLine.positionCount = 2;
                _aimLine.startWidth = 0.05f;
                _aimLine.endWidth = 0.05f;
                _aimLine.startColor = _aimColor;
                _aimLine.endColor = _aimColor;
                _aimLine.numCapVertices = 4;
                _aimLine.numCornerVertices = 4;

                if (_aimMaterial != null)
                {
                    _aimLine.material = _aimMaterial;
                }
            }

            _aimLine.sortingLayerName = "Effects";
            _aimLine.sortingOrder = 100;

            if (_aimReticle == null)
            {
                var reticleGo = new GameObject("AimReticle");
                reticleGo.transform.SetParent(_aimVisualsRoot.transform, false);
                _aimReticle = reticleGo.transform;

                var reticleLine = reticleGo.AddComponent<LineRenderer>();
                reticleLine.useWorldSpace = false;
                reticleLine.loop = true;
                reticleLine.positionCount = Mathf.Max(12, _reticleSegments);
                reticleLine.startWidth = 0.05f;
                reticleLine.endWidth = 0.05f;
                reticleLine.startColor = _aimColor;
                reticleLine.endColor = _aimColor;
                reticleLine.numCapVertices = 4;
                reticleLine.numCornerVertices = 4;

                if (_aimMaterial != null)
                {
                    reticleLine.material = _aimMaterial;
                }

                _reticleLine = reticleLine;
                BuildCirclePoints(_reticleLine, _reticleRadius);
                _lastBuiltReticleRadius = _reticleRadius;
            }
            else if (_reticleLine == null)
            {
                _reticleLine = _aimReticle.GetComponent<LineRenderer>();
                if (_reticleLine == null)
                {
                    _reticleLine = _aimReticle.gameObject.AddComponent<LineRenderer>();
                    _reticleLine.useWorldSpace = false;
                    _reticleLine.loop = true;
                    _reticleLine.positionCount = Mathf.Max(12, _reticleSegments);
                    _reticleLine.startWidth = 0.05f;
                    _reticleLine.endWidth = 0.05f;
                    _reticleLine.startColor = _aimColor;
                    _reticleLine.endColor = _aimColor;
                    _reticleLine.numCapVertices = 4;
                    _reticleLine.numCornerVertices = 4;
                    if (_aimMaterial != null) _reticleLine.material = _aimMaterial;
                    BuildCirclePoints(_reticleLine, _reticleRadius);
                    _lastBuiltReticleRadius = _reticleRadius;
                }
            }

            if (_reticleLine != null)
            {
                _reticleLine.sortingLayerName = "Effects";
                _reticleLine.sortingOrder = 101;
            }

            if (_rangeLine == null)
            {
                Transform existingRange = _aimVisualsRoot.transform.Find("AimRangeCircle");
                if (existingRange != null)
                {
                    _rangeLine = existingRange.GetComponent<LineRenderer>();
                }

                if (_rangeLine == null)
                {
                    var rangeGo = existingRange != null ? existingRange.gameObject : new GameObject("AimRangeCircle");
                    if (existingRange == null)
                    {
                        rangeGo.transform.SetParent(_aimVisualsRoot.transform, false);
                    }

                    _rangeLine = rangeGo.AddComponent<LineRenderer>();
                    _rangeLine.useWorldSpace = false;
                    _rangeLine.loop = true;
                    _rangeLine.positionCount = Mathf.Max(24, _reticleSegments);
                    _rangeLine.startWidth = 0.04f;
                    _rangeLine.endWidth = 0.04f;
                    Color rangeCol = new Color(_aimColor.r, _aimColor.g, _aimColor.b, 0.35f);
                    _rangeLine.startColor = rangeCol;
                    _rangeLine.endColor = rangeCol;
                    if (_aimMaterial != null) _rangeLine.material = _aimMaterial;
                }
            }

            _rangeLine.sortingLayerName = "Effects";
            _rangeLine.sortingOrder = 99;

            SetAimVisuals(_weapon.IsAiming);
        }

        private static void BuildCirclePoints(LineRenderer line, float radius)
        {
            if (line == null) return;
            int segments = line.positionCount;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
            }
        }
    }
}
