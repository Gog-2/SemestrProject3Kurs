using UnityEngine;
using Game.Core;
using Game.WaveSystem;

namespace Game.Spells
{
    public class SpellCaster : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;

        private LineRenderer _previewCircle;
        private Material _previewMaterial;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.PrimaryClicked += OnPrimaryClicked;
                InputManager.Instance.SecondaryClicked += OnCancelInput;
                InputManager.Instance.CancelPressed += OnCancelInput;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.PrimaryClicked -= OnPrimaryClicked;
                InputManager.Instance.SecondaryClicked -= OnCancelInput;
                InputManager.Instance.CancelPressed -= OnCancelInput;
            }
            SetPreviewVisible(false);
        }

        private void OnDestroy()
        {
            if (_previewMaterial != null) Destroy(_previewMaterial);
        }

        private void Update()
        {
            SpellScrollManager mgr = SpellScrollManager.Instance;
            if (mgr == null || !mgr.HasSelection ||
                (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing))
            {
                SetPreviewVisible(false);
                return;
            }

            if (InputManager.Instance == null) return;
            Vector3 worldPos = InputManager.Instance.GetPointerWorldPosition(_mainCamera);
            worldPos.z = 0f;

            SpellScrollInstance scroll = mgr.Scrolls[mgr.SelectedIndex];
            float radius = Mathf.Max(0.75f, scroll.Data.GetStats(scroll.Level).areaRadius);
            UpdatePreviewCircle(worldPos, radius, scroll.Data.AccentColor);
        }

        private void OnCancelInput()
        {
            SpellScrollManager.Instance?.CancelSelection();
        }

        private void OnPrimaryClicked(Vector2 screenPos)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            SpellScrollManager mgr = SpellScrollManager.Instance;
            if (mgr == null || !mgr.HasSelection) return;
            if (InputManager.Instance != null && InputManager.Instance.IsPointerOverUI(screenPos)) return;

            if (WaveController.Instance != null)
            {
                WaveState ws = WaveController.Instance.CurrentState;
                if (ws != WaveState.WaveActive && ws != WaveState.WaveClearing)
                {
                    return;
                }
            }

            Vector3 targetPos = InputManager.Instance != null
                ? InputManager.Instance.GetPointerWorldPosition(_mainCamera)
                : Vector3.zero;
            targetPos.z = 0f;

            if (mgr.TryConsumeSelectedScroll(out SpellScrollInstance scroll))
            {
                CastSpell(scroll.Data, scroll.Level, targetPos);
            }
        }

        public void CastSpell(SpellData data, int level, Vector3 targetWorldPos)
        {
            if (data == null) return;
            if (data.Effect == null)
            {
                Debug.LogError($"[SpellCaster] SpellData '{data.SpellName}' has no SpellEffect assigned.", data);
                return;
            }

            SpellLevelStats stats = data.GetStats(level);
            float dmgMult = RunProgressionManager.Instance != null
                ? RunProgressionManager.Instance.GetEffectiveSpellDamageMultiplier()
                : 1f;

            data.Effect.Cast(stats, dmgMult, targetWorldPos);
        }

        private void EnsurePreviewCircle()
        {
            if (_previewCircle != null) return;

            var go = new GameObject("SpellPreviewCircle");
            go.transform.SetParent(transform, false);
            _previewCircle = go.AddComponent<LineRenderer>();
            _previewCircle.useWorldSpace = false;
            _previewCircle.loop = true;
            _previewCircle.positionCount = 36;
            _previewCircle.startWidth = 0.06f;
            _previewCircle.endWidth = 0.06f;
            _previewCircle.sortingLayerName = "Effects";
            _previewCircle.sortingOrder = 102;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                _previewMaterial = new Material(shader);
                _previewCircle.material = _previewMaterial;
            }
        }

        private void UpdatePreviewCircle(Vector3 center, float radius, Color color)
        {
            EnsurePreviewCircle();
            _previewCircle.enabled = true;
            _previewCircle.transform.position = center;
            _previewCircle.startColor = color;
            _previewCircle.endColor = color;

            int count = _previewCircle.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                _previewCircle.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void SetPreviewVisible(bool visible)
        {
            if (_previewCircle != null)
            {
                _previewCircle.enabled = visible;
            }
        }
    }
}
