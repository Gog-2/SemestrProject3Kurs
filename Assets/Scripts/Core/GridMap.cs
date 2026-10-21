using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Alchemy.Inspector;
using Game.Buildings;

namespace Game.Core
{
    public class GridMap : MonoBehaviour
    {
        [Title("Visuals & References")]
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TerrainType _defaultTerrain;

        [Title("Grid Settings")]
        [SerializeField, Min(1)] private int _width = 20;
        [SerializeField, Min(1)] private int _height = 15;
        [SerializeField] private bool _generateOnStart = true;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public Bounds MapBounds => Bounds;

        private TerrainType[] _terrain;
        private IBuilding[] _occupants;

        public event Action Changed;

        public int Width => _width;
        public int Height => _height;

        public Bounds Bounds
        {
            get
            {
                Vector3 min = _tilemap.CellToWorld(new Vector3Int(0, 0, 0));
                Vector3 max = _tilemap.CellToWorld(new Vector3Int(_width, _height, 0));
                var bounds = new Bounds();
                bounds.SetMinMax(Vector3.Min(min, max), Vector3.Max(min, max));
                return bounds;
            }
        }

        private void Awake()
        {
            if (_generateOnStart)
            {
                Generate();
            }
        }

        private void OnValidate()
        {
            if (_width < 1) _width = 1;
            if (_height < 1) _height = 1;
        }

        [Button]
        public void Generate()
        {
            int length = _width * _height;
            _terrain = new TerrainType[length];
            _occupants = new IBuilding[length];

            TileBase tileToDraw = _defaultTerrain.Tile;
            var tileArray = new TileBase[length];

            for (int i = 0; i < length; i++)
            {
                _terrain[i] = _defaultTerrain;
                tileArray[i] = tileToDraw;
            }

            _tilemap.ClearAllTiles();
            var bounds = new BoundsInt(0, 0, 0, _width, _height, 1);
            _tilemap.SetTilesBlock(bounds, tileArray);
        }

        [Button]
        public void Clear()
        {
            _terrain = null;
            _occupants = null;
            _tilemap.ClearAllTiles();
        }

        public bool InBounds(int x, int y)
        {
            return (uint)x < (uint)_width && (uint)y < (uint)_height;
        }

        public TerrainType GetTerrain(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return _terrain[y * _width + x];
        }

        public void SetTerrain(int x, int y, TerrainType newTerrain)
        {
            if (!InBounds(x, y)) return;

            _terrain[y * _width + x] = newTerrain;
            _tilemap.SetTile(new Vector3Int(x, y, 0), newTerrain != null ? newTerrain.Tile : null);
            Changed?.Invoke();
        }

        public bool IsTerrainWalkable(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return _terrain[y * _width + x].Walkable;
        }

        public float TerrainCost(int x, int y)
        {
            if (!InBounds(x, y)) return float.PositiveInfinity;
            TerrainType t = _terrain[y * _width + x];
            return t.Walkable ? t.MoveCost : float.PositiveInfinity;
        }

        public float Cost(int x, int y)
        {
            if (!InBounds(x, y)) return float.PositiveInfinity;

            int idx = y * _width + x;
            TerrainType t = _terrain[idx];
            if (!t.Walkable) return float.PositiveInfinity;

            float cost = t.MoveCost;
            IBuilding occ = _occupants[idx];
            if (occ != null)
            {
                cost += occ.BreakCost;
            }

            return cost;
        }

        public bool CanPlace(Vector2Int origin, Vector2Int size)
        {
            if (origin.x < 0 || origin.y < 0 || origin.x + size.x > _width || origin.y + size.y > _height)
            {
                return false;
            }

            int endX = origin.x + size.x;
            int endY = origin.y + size.y;

            for (int y = origin.y; y < endY; y++)
            {
                int row = y * _width;
                for (int x = origin.x; x < endX; x++)
                {
                    int idx = row + x;
                    if (!_terrain[idx].Walkable || _occupants[idx] != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryPlace(IBuilding building)
        {
            if (!CanPlace(building.Origin, building.Size)) return false;

            int endX = building.Origin.x + building.Size.x;
            int endY = building.Origin.y + building.Size.y;

            for (int y = building.Origin.y; y < endY; y++)
            {
                int row = y * _width;
                for (int x = building.Origin.x; x < endX; x++)
                {
                    _occupants[row + x] = building;
                }
            }

            building.Destroyed += OnBuildingDestroyed;
            Changed?.Invoke();
            return true;
        }

        public void Remove(IBuilding building)
        {
            if (building == null || _occupants == null) return;

            int endX = building.Origin.x + building.Size.x;
            int endY = building.Origin.y + building.Size.y;

            for (int y = building.Origin.y; y < endY; y++)
            {
                for (int x = building.Origin.x; x < endX; x++)
                {
                    if (!InBounds(x, y)) continue;

                    int idx = y * _width + x;
                    if (_occupants[idx] == building)
                    {
                        _occupants[idx] = null;
                    }
                }
            }

            building.Destroyed -= OnBuildingDestroyed;
            Changed?.Invoke();
        }

        private void OnBuildingDestroyed(IDamageable damageable)
        {
            if (damageable is IBuilding building)
            {
                Remove(building);
            }
        }

        public IBuilding GetOccupant(int x, int y)
        {
            if (!InBounds(x, y) || _occupants == null) return null;
            return _occupants[y * _width + x];
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return _tilemap.WorldToCell(worldPosition);
        }

        public Vector3 CellToWorld(int x, int y)
        {
            return _tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }

        private void OnDrawGizmosSelected()
        {
            if (_tilemap == null) return;
            Gizmos.color = Color.green;
            Bounds bounds = Bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
