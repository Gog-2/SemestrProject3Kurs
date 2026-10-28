using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Alchemy.Inspector;
using Game.Weapons;
using Game.Core;

namespace Game.UI
{
    public class WeaponAimUI : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private CastleWeapon _weapon;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LineRenderer _aimLine;
        [SerializeField] private Transform _aimReticle;

        [Title("Cooldown Display")]
        [SerializeField] private Image _cooldownFillImage;
        [SerializeField] private TMP_Text _cooldownText;
        [SerializeField] private GameObject _readyIndicator;

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

            SetAimVisuals(false);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
                GameManager.Instance.StateChanged += OnGameStateChanged;
            }

            if (_weapon != null)
            {
                _weapon.CooldownChanged += OnCooldownChanged;
                _weapon.AimingToggled += SetAimVisuals;
            }
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
            SetAimVisuals(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnGameStateChanged;
            }
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

        private void Update()
        {
            // Do not handle weapon input when game is paused or over
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (_weapon != null && _weapon.IsAiming)
                {
                    _weapon.CancelAiming();
                }
                SetAimVisuals(false);
                return;
            }

            if (_weapon == null) return;

            // Toggle aiming with 'V'
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                _weapon.ToggleAiming();
            }

            if (!_weapon.IsAiming) return;

            // Cancel aiming with RMB or Escape
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                _weapon.CancelAiming();
                return;
            }

            Vector3 mouseWorldPos = GetMouseWorldPosition();

            // Update aim reticle position
            if (_aimReticle != null)
            {
                _aimReticle.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0f);
            }

            // Update aim line from weapon/castle to reticle
            if (_aimLine != null)
            {
                Vector3 origin = _weapon.transform.position;
                _aimLine.SetPosition(0, origin);
                _aimLine.SetPosition(1, new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0f));
            }

            // Fire on LMB
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _weapon.TryFire(mouseWorldPos);
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_mainCamera == null || Mouse.current == null) return Vector3.zero;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -_mainCamera.transform.position.z));
            worldPos.z = 0f;
            return worldPos;
        }

        private void SetAimVisuals(bool active)
        {
            if (_aimLine != null)
            {
                _aimLine.enabled = active;
            }

            if (_aimReticle != null)
            {
                _aimReticle.gameObject.SetActive(active);
            }
        }

        private void OnCooldownChanged(float current, float max)
        {
            bool ready = current <= 0f;

            if (_cooldownFillImage != null)
            {
                _cooldownFillImage.fillAmount = max > 0f ? (current / max) : 0f;
            }

            if (_cooldownText != null)
            {
                _cooldownText.text = ready ? "READY (V)" : $"{current:F1}s";
            }

            if (_readyIndicator != null)
            {
                _readyIndicator.SetActive(ready);
            }
        }
    }
}
