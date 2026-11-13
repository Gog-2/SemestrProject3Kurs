using UnityEngine;
using Alchemy.Inspector;

namespace Game.Buildings
{
    public class WallBuilding : Building
    {
        [Title("Wall Visuals (Auto-connect)")]
        [SerializeField] private Sprite _singleSprite;
        [SerializeField] private Sprite _horizontalSprite;
        [SerializeField] private Sprite _verticalSprite;
        [SerializeField] private Sprite _crossSprite;

        public void SetWallSprites(Sprite single, Sprite horizontal, Sprite vertical, Sprite cross)
        {
            if (single != null) _singleSprite = single;
            if (horizontal != null) _horizontalSprite = horizontal;
            if (vertical != null) _verticalSprite = vertical;
            if (cross != null) _crossSprite = cross;
        }

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(_singleSprite != null, $"[{name}] _singleSprite is not assigned!", this);
            Debug.Assert(_horizontalSprite != null, $"[{name}] _horizontalSprite is not assigned!", this);
            Debug.Assert(_verticalSprite != null, $"[{name}] _verticalSprite is not assigned!", this);
            Debug.Assert(_crossSprite != null, $"[{name}] _crossSprite is not assigned!", this);
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            UpdateConnections();
            NotifyNeighbors();
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            NotifyNeighbors();
        }

        public void UpdateConnections()
        {
            if (_gridMap == null) return;

            bool north = IsWallAt(_originCell + Vector2Int.up);
            bool south = IsWallAt(_originCell + Vector2Int.down);
            bool east = IsWallAt(_originCell + Vector2Int.right);
            bool west = IsWallAt(_originCell + Vector2Int.left);

            int connectionCount = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

            if (connectionCount >= 3)
            {
                _spriteRenderer.sprite = _crossSprite;
            }
            else if ((east || west) && !(north || south))
            {
                _spriteRenderer.sprite = _horizontalSprite;
            }
            else if ((north || south) && !(east || west))
            {
                _spriteRenderer.sprite = _verticalSprite;
            }
            else
            {
                _spriteRenderer.sprite = _singleSprite;
            }
        }

        private bool IsWallAt(Vector2Int cell)
        {
            if (_gridMap == null) return false;
            IBuilding occupant = _gridMap.GetOccupant(cell.x, cell.y);
            return occupant is WallBuilding;
        }

        private void NotifyNeighbors()
        {
            if (_gridMap == null) return;

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                Vector2Int neighborCell = _originCell + d;
                if (_gridMap.GetOccupant(neighborCell.x, neighborCell.y) is WallBuilding wall)
                {
                    wall.UpdateConnections();
                }
            }
        }
    }
}
