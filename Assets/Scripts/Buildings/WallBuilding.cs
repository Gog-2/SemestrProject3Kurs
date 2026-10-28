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
            if (_gridMap == null || _spriteRenderer == null) return;

            bool north = IsWallAt(_originCell + Vector2Int.up);
            bool south = IsWallAt(_originCell + Vector2Int.down);
            bool east = IsWallAt(_originCell + Vector2Int.right);
            bool west = IsWallAt(_originCell + Vector2Int.left);

            int connectionCount = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

            if (connectionCount >= 3 && _crossSprite != null)
            {
                _spriteRenderer.sprite = _crossSprite;
            }
            else if ((east || west) && !(north || south) && _horizontalSprite != null)
            {
                _spriteRenderer.sprite = _horizontalSprite;
            }
            else if ((north || south) && !(east || west) && _verticalSprite != null)
            {
                _spriteRenderer.sprite = _verticalSprite;
            }
            else if (_singleSprite != null)
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
