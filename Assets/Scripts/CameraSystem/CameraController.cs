using UnityEngine;
using UnityEngine.InputSystem;
using Alchemy.Inspector;
using Game.Core;

namespace Game.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [BoxGroup("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private GridMap gridMap;
        [Tooltip("Цель для начального фокуса (например Замок). Если не задана, берется центр карты.")]
        [SerializeField] private Transform initialTarget;

        [BoxGroup("Movement")]
        [Tooltip("Телепортировать камеру к цели/в центр карты при старте")]
        [SerializeField] private bool focusOnStart = true;
        [Tooltip("Базовая скорость перемещения камеры в единицах/сек")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 15f;
        [Tooltip("Увеличивать скорость при отдалении камеры")]
        [SerializeField] private bool scaleSpeedWithZoom = true;
        [Tooltip("Базовый зум для расчета множителя скорости")]
        [SerializeField, Min(0.1f)] private float baseZoomReference = 10f;

        [BoxGroup("Zoom")]
        [SerializeField, Min(0.5f)] private float minZoom = 3f;
        [SerializeField, Min(1f)] private float maxZoom = 25f;
        [SerializeField, Min(0.1f)] private float zoomStep = 2f;
        [SerializeField, Min(1f)] private float zoomSmoothSpeed = 15f;
        [Tooltip("Зумировать к точке под курсором мыши")]
        [SerializeField] private bool zoomToCursor = true;

        [BoxGroup("Edge Panning")]
        [SerializeField] private bool useEdgeScrolling = false;
        [SerializeField, Min(1f)] private float edgeThreshold = 20f;

        [BoxGroup("Bounds")]
        [SerializeField] private bool clampToBounds = true;

        [BoxGroup("Debug")]
        [ShowInInspector, ReadOnly]
        public float CurrentZoom => targetCamera != null ? targetCamera.orthographicSize : 0f;

        private Sheme input;
        private float targetZoom;

        private void Awake()
        {
            input = new Sheme();
            targetZoom = targetCamera.orthographicSize;
        }

        private void Start()
        {
            if (focusOnStart)
            {
                FocusOnTarget();
            }
        }

        [Button]
        public void FocusOnTarget()
        {
            Vector3 targetPos = initialTarget != null ? initialTarget.position : gridMap.Bounds.center;
            TeleportTo(targetPos);
        }

        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            if (clampToBounds)
            {
                ClampPositionToBounds();
            }
        }

        private void OnEnable()
        {
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }

        private void OnDestroy()
        {
            input.Dispose();
        }

        private void OnValidate()
        {
            if (minZoom < 0.5f) minZoom = 0.5f;
            if (maxZoom <= minZoom) maxZoom = minZoom + 1f;
            if (baseZoomReference < 0.1f) baseZoomReference = 0.1f;
            if (edgeThreshold < 1f) edgeThreshold = 1f;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            HandleZoom(dt);
            HandleMovement(dt);

            if (clampToBounds)
            {
                ClampPositionToBounds();
            }
        }

        private void HandleMovement(float dt)
        {
            // Чтение направления движения из сгенерированной Sheme (Player/Move)
            Vector2 inputDir = input.Player.Move.ReadValue<Vector2>();

            if (useEdgeScrolling && Application.isFocused)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                if (mousePos.x <= edgeThreshold) inputDir.x -= 1f;
                else if (mousePos.x >= Screen.width - edgeThreshold) inputDir.x += 1f;

                if (mousePos.y <= edgeThreshold) inputDir.y -= 1f;
                else if (mousePos.y >= Screen.height - edgeThreshold) inputDir.y += 1f;
            }

            if (inputDir.sqrMagnitude > 0.0001f)
            {
                inputDir.Normalize();

                float speedMultiplier = scaleSpeedWithZoom ? (targetCamera.orthographicSize / baseZoomReference) : 1f;
                Vector3 moveDelta = new Vector3(inputDir.x, inputDir.y, 0f) * (moveSpeed * speedMultiplier * dt);
                transform.position += moveDelta;
            }
        }

        private void HandleZoom(float dt)
        {
            // Чтение скролла из сгенерированной Sheme (Player/Scroll)
            float scroll = input.Player.Scroll.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetZoom += scroll * zoomStep;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }

            if (Mathf.Abs(targetCamera.orthographicSize - targetZoom) > 0.001f)
            {
                Vector3 mouseWorldBefore = Vector3.zero;
                Vector2 mousePos = Vector2.zero;

                if (zoomToCursor)
                {
                    mousePos = Mouse.current.position.ReadValue();
                    mouseWorldBefore = targetCamera.ScreenToWorldPoint(mousePos);
                }

                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, targetZoom, zoomSmoothSpeed * dt);

                if (zoomToCursor)
                {
                    Vector3 mouseWorldAfter = targetCamera.ScreenToWorldPoint(mousePos);
                    Vector3 diff = mouseWorldBefore - mouseWorldAfter;
                    transform.position += new Vector3(diff.x, diff.y, 0f);
                }
            }
        }

        private void ClampPositionToBounds()
        {
            Bounds bounds = gridMap.Bounds;
            float camVertExtent = targetCamera.orthographicSize;
            float camHorzExtent = camVertExtent * targetCamera.aspect;

            Vector3 pos = transform.position;

            if (camHorzExtent * 2f >= bounds.size.x)
            {
                pos.x = bounds.center.x;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, bounds.min.x + camHorzExtent, bounds.max.x - camHorzExtent);
            }

            if (camVertExtent * 2f >= bounds.size.y)
            {
                pos.y = bounds.center.y;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, bounds.min.y + camVertExtent, bounds.max.y - camVertExtent);
            }

            transform.position = pos;
        }
    }
}
