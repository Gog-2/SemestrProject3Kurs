using UnityEngine;
using Alchemy.Inspector;
using Game.Core;

namespace Game.Buildings
{
    public class BuildingGhostView : MonoBehaviour
    {
        [Title("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _validColor = new(0.2f, 0.9f, 0.2f, 0.65f);
        [SerializeField] private Color _invalidColor = new(0.9f, 0.2f, 0.2f, 0.65f);

        private BuildingData _currentData;
        private GridMap _gridMap;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            _spriteRenderer.sortingLayerName = "Ghost";
            _spriteRenderer.sortingOrder = 25;
            gameObject.SetActive(false);
        }

        public void Show(BuildingData data, GridMap map)
        {
            _currentData = data;
            _gridMap = map;

            if (data == null)
            {
                Hide();
                return;
            }

            if (_spriteRenderer != null)
            {
                if (data.Icon != null)
                {
                    _spriteRenderer.sprite = data.Icon;
                }
                else if (data.Prefab != null)
                {
                    var prefabSr = data.Prefab.GetComponentInChildren<SpriteRenderer>();
                    if (prefabSr != null) _spriteRenderer.sprite = prefabSr.sprite;
                }

                _spriteRenderer.color = _validColor;
                transform.localScale = new Vector3(data.Size.x, data.Size.y, 1f);
            }

            gameObject.SetActive(true);
        }

        public void UpdatePosition(Vector2Int cell, bool isValid)
        {
            if (_gridMap == null || _currentData == null) return;

            Vector2Int size = _currentData.Size;
            Vector3 worldPos = _gridMap.CellToWorld(cell.x, cell.y) +
                               new Vector3((size.x - 1) * 0.5f, (size.y - 1) * 0.5f, 0f);

            transform.position = worldPos;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = isValid ? _validColor : _invalidColor;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _currentData = null;
        }
    }
}
