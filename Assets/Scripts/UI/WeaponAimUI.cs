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

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_weapon == null)
            {
                _weapon = FindAnyObjectByType<CastleWeapon>();
            }
        }

        private void Start()
        {
            if (_weapon == null)
            {
                _weapon = FindAnyObjectByType<CastleWeapon>();
            }

            if (_weapon != null)
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

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }

            if (_weapon == null)
            {
                _weapon = FindAnyObjectByType<CastleWeapon>();
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

            if (_aimReticle != null)
            {
                _aimReticle.position = target;
            }

            if (_aimLine != null)
            {
                Vector3 origin = _weapon.AimOrigin;
                origin.z = 0f;

                _aimLine.positionCount = 2;
                _aimLine.SetPosition(0, origin);
                _aimLine.SetPosition(1, target);
            }
        }

        private void EnsureAimVisuals()
        {
            if (_aimLine != null && _aimReticle != null) return;
            if (_weapon == null) return;

            if (_aimVisualsRoot == null)
            {
                _aimVisualsRoot = new GameObject("WeaponAimVisuals");
                _aimVisualsRoot.transform.SetParent(_weapon.transform, false);
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
                _aimLine.sortingLayerName = "Default";
                _aimLine.sortingOrder = 100;

                if (_aimMaterial != null)
                {
                    _aimLine.material = _aimMaterial;
                }
            }

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
                reticleLine.sortingLayerName = "Default";
                reticleLine.sortingOrder = 101;

                if (_aimMaterial != null)
                {
                    reticleLine.material = _aimMaterial;
                }

                int segments = reticleLine.positionCount;
                for (int i = 0; i < segments; i++)
                {
                    float angle = (i / (float)segments) * Mathf.PI * 2f;
                    reticleLine.SetPosition(i, new Vector3(
                        Mathf.Cos(angle) * _reticleRadius,
                        Mathf.Sin(angle) * _reticleRadius,
                        0f));
                }
            }

            SetAimVisuals(false);
        }
    }
}
