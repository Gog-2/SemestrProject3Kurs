using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Alchemy.Inspector;
using Game.Buildings;

namespace Game.Core
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class GridMap : MonoBehaviour
    {
        [Title("Visuals & References")]
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TerrainType defaultTerrain;

        [Title("Grid Settings")]
        [SerializeField, Min(1)] private int width = 20;
        [SerializeField, Min(1)] private int height = 15;
        [SerializeField] private bool generateOnStart = true;

        [Title("Debug")]
        [ShowInInspector, ReadOnly]
        public Bounds MapBounds => Bounds;

        // Источник правды (плоские массивы [y * width + x])
        private TerrainType[] terrain;
        private IBuilding[] occupants;

        public event Action Changed;

        public int Width => width;
        public int Height => height;

        public Bounds Bounds
        {
            get
            {
                Vector3 min = tilemap.CellToWorld(new Vector3Int(0, 0, 0));
                Vector3 max = tilemap.CellToWorld(new Vector3Int(width, height, 0));
                var bounds = new Bounds();
                bounds.SetMinMax(Vector3.Min(min, max), Vector3.Max(min, max));
                return bounds;
            }
        }

        private void Awake()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        private void OnValidate()
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
        }

        [Button]
        public void Generate()
        {
            int length = width * height;
            terrain = new TerrainType[length];
            occupants = new IBuilding[length];

            TileBase tileToDraw = defaultTerrain.Tile;
            TileBase[] tileArray = new TileBase[length];

            for (int i = 0; i < length; i++)
            {
                terrain[i] = defaultTerrain;
                tileArray[i] = tileToDraw;
            }

            tilemap.ClearAllTiles();
            var bounds = new BoundsInt(0, 0, 0, width, height, 1);
            tilemap.SetTilesBlock(bounds, tileArray);
        }

        [Button]
        public void Clear()
        {
            terrain = null;
            occupants = null;
            tilemap.ClearAllTiles();
        }

        public bool InBounds(int x, int y)
        {
            return (uint)x < (uint)width && (uint)y < (uint)height;
        }

        public TerrainType GetTerrain(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return terrain[y * width + x];
        }

        public void SetTerrain(int x, int y, TerrainType newTerrain)
        {
            if (!InBounds(x, y)) return;

            terrain[y * width + x] = newTerrain;
            tilemap.SetTile(new Vector3Int(x, y, 0), newTerrain != null ? newTerrain.Tile : null);
            Changed?.Invoke();
        }

        public bool IsTerrainWalkable(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return terrain[y * width + x].Walkable;
        }

        public float TerrainCost(int x, int y)
        {
            if (!InBounds(x, y)) return float.PositiveInfinity;
            return terrain[y * width + x].MoveCost;
        }

        public float Cost(int x, int y)
        {
            if (!InBounds(x, y)) return float.PositiveInfinity;
            int idx = y * width + x;
            float cost = terrain[idx].MoveCost;
            IBuilding occ = occupants[idx];
            if (occ != null)
            {
                cost += occ.BreakCost;
            }
            return cost;
        }

        public bool CanPlace(Vector2Int origin, Vector2Int size)
        {
            if (origin.x < 0 || origin.y < 0 || origin.x + size.x > width || origin.y + size.y > height)
            {
                return false;
            }

            int endX = origin.x + size.x;
            int endY = origin.y + size.y;

            for (int y = origin.y; y < endY; y++)
            {
                int row = y * width;
                for (int x = origin.x; x < endX; x++)
                {
                    int idx = row + x;
                    if (!terrain[idx].Walkable || occupants[idx] != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryPlace(IBuilding building)
        {
            if (!CanPlace(building.Origin, building.Size))
            {
                return false;
            }

            int endX = building.Origin.x + building.Size.x;
            int endY = building.Origin.y + building.Size.y;

            for (int y = building.Origin.y; y < endY; y++)
            {
                int row = y * width;
                for (int x = building.Origin.x; x < endX; x++)
                {
                    occupants[row + x] = building;
                }
            }

            building.Destroyed += OnBuildingDestroyed;
            Changed?.Invoke();
            return true;
        }

        public void Remove(IBuilding building)
        {
            int endX = building.Origin.x + building.Size.x;
            int endY = building.Origin.y + building.Size.y;

            for (int y = building.Origin.y; y < endY; y++)
            {
                int row = y * width;
                for (int x = building.Origin.x; x < endX; x++)
                {
                    int idx = row + x;
                    if (occupants[idx] == building)
                    {
                        occupants[idx] = null;
                    }
                }
            }

            building.Destroyed -= OnBuildingDestroyed;
            Changed?.Invoke();
        }

        private void OnBuildingDestroyed(IBuilding building)
        {
            Remove(building);
        }

        public IBuilding GetOccupant(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return occupants[y * width + x];
        }

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return tilemap.WorldToCell(worldPosition);
        }

        public Vector3 CellToWorld(int x, int y)
        {
            return tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }

        private void OnDrawGizmosSelected()
        {
            if (tilemap == null) return;
            Gizmos.color = Color.green;
            Bounds bounds = Bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
