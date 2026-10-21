using System;
using UnityEngine;
using Alchemy.Inspector;
using Game.Core;
using Game.Buildings;

namespace Game.Pathfinding
{
    public class GridFlowfield : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] private GridMap _gridMap;
        [SerializeField] private Castle _castle;

        [Title("Settings")]
        [SerializeField] private bool _allowDiagonal = false;

        private float[] _costField;
        private Vector2[] _flowDirections;
        private int _mapWidth;
        private int _mapHeight;

        private FastMinHeap _frontier;

        private static readonly Vector2Int[] s_orthogonalNeighbors =
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0)
        };

        private static readonly Vector2Int[] s_allNeighbors =
        {
            new(0, 1),
            new(0, -1),
            new(1, 0),
            new(-1, 0),
            new(1, 1),
            new(1, -1),
            new(-1, 1),
            new(-1, -1)
        };

        public bool IsInitialized => _flowDirections != null;

        private void Start()
        {
            InitializeBuffers();
            Recalculate();
            if (_gridMap != null)
            {
                _gridMap.Changed += Recalculate;
            }
        }

        private void OnDestroy()
        {
            if (_gridMap != null)
            {
                _gridMap.Changed -= Recalculate;
            }
        }

        public void InitializeBuffers()
        {
            if (_gridMap == null) return;

            _mapWidth = _gridMap.Width;
            _mapHeight = _gridMap.Height;
            int totalCells = _mapWidth * _mapHeight;

            if (_costField == null || _costField.Length != totalCells)
            {
                _costField = new float[totalCells];
                _flowDirections = new Vector2[totalCells];
                _frontier = new FastMinHeap(totalCells);
            }
        }

        [Button]
        public void Recalculate()
        {
            if (_gridMap == null || _castle == null) return;

            if (_flowDirections == null || _mapWidth != _gridMap.Width || _mapHeight != _gridMap.Height)
            {
                InitializeBuffers();
            }

            if (_flowDirections == null) return;

            int totalCells = _mapWidth * _mapHeight;
            for (int i = 0; i < totalCells; i++)
            {
                _costField[i] = float.PositiveInfinity;
                _flowDirections[i] = Vector2.zero;
            }

            _frontier.Clear();

            Vector2Int castleOrigin = _castle.Origin;
            Vector2Int castleSize = _castle.Size;

            for (int y = castleOrigin.y; y < castleOrigin.y + castleSize.y; y++)
            {
                for (int x = castleOrigin.x; x < castleOrigin.x + castleSize.x; x++)
                {
                    if (_gridMap.InBounds(x, y))
                    {
                        int targetIdx = y * _mapWidth + x;
                        _costField[targetIdx] = 0f;
                        _frontier.Push(targetIdx, 0f);
                    }
                }
            }

            Vector2Int[] neighbors = _allowDiagonal ? s_allNeighbors : s_orthogonalNeighbors;

            while (_frontier.Count > 0)
            {
                _frontier.Pop(out int currentIdx, out float currentCost);

                if (currentCost > _costField[currentIdx])
                {
                    continue;
                }

                int cx = currentIdx % _mapWidth;
                int cy = currentIdx / _mapWidth;

                for (int i = 0; i < neighbors.Length; i++)
                {
                    int nx = cx + neighbors[i].x;
                    int ny = cy + neighbors[i].y;

                    if (!_gridMap.InBounds(nx, ny)) continue;

                    int nIdx = ny * _mapWidth + nx;
                    float cellCost = _gridMap.Cost(nx, ny);

                    if (float.IsPositiveInfinity(cellCost)) continue;

                    float stepDistance = (neighbors[i].x != 0 && neighbors[i].y != 0) ? 1.4142f : 1f;
                    float newCost = currentCost + cellCost * stepDistance;

                    if (newCost < _costField[nIdx])
                    {
                        _costField[nIdx] = newCost;
                        _frontier.Push(nIdx, newCost);
                    }
                }
            }

            for (int y = 0; y < _mapHeight; y++)
            {
                int row = y * _mapWidth;
                for (int x = 0; x < _mapWidth; x++)
                {
                    int idx = row + x;
                    float bestCost = _costField[idx];

                    if (bestCost == 0f || float.IsPositiveInfinity(bestCost))
                    {
                        _flowDirections[idx] = Vector2.zero;
                        continue;
                    }

                    Vector2 bestDir = Vector2.zero;

                    for (int i = 0; i < neighbors.Length; i++)
                    {
                        int nx = x + neighbors[i].x;
                        int ny = y + neighbors[i].y;

                        if (!_gridMap.InBounds(nx, ny)) continue;

                        int nIdx = ny * _mapWidth + nx;
                        float nCost = _costField[nIdx];

                        if (nCost < bestCost)
                        {
                            bestCost = nCost;
                            bestDir = new Vector2(neighbors[i].x, neighbors[i].y);
                        }
                    }

                    _flowDirections[idx] = bestDir.normalized;
                }
            }
        }

        public Vector2 GetDirection(Vector3 worldPosition)
        {
            if (_gridMap == null) return Vector2.zero;

            if (_flowDirections == null)
            {
                InitializeBuffers();
                Recalculate();
                if (_flowDirections == null)
                {
                    Vector3 center = _gridMap.Bounds.center;
                    return (new Vector2(center.x, center.y) - new Vector2(worldPosition.x, worldPosition.y)).normalized;
                }
            }

            Vector3Int cell = _gridMap.WorldToCell(worldPosition);

            if (!_gridMap.InBounds(cell.x, cell.y))
            {
                Vector3 center = _gridMap.Bounds.center;
                return (new Vector2(center.x, center.y) - new Vector2(worldPosition.x, worldPosition.y)).normalized;
            }

            return _flowDirections[cell.y * _mapWidth + cell.x];
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            if (_gridMap == null) return Vector2Int.zero;
            Vector3Int c = _gridMap.WorldToCell(worldPosition);
            return new Vector2Int(c.x, c.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (_flowDirections == null || _gridMap == null) return;

            Gizmos.color = Color.cyan;
            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    Vector2 dir = _flowDirections[y * _mapWidth + x];
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Vector3 worldPos = _gridMap.CellToWorld(x, y);
                        Gizmos.DrawRay(worldPos, new Vector3(dir.x, dir.y, 0f) * 0.4f);
                    }
                }
            }
        }

        private struct HeapNode
        {
            public int Index;
            public float Priority;
        }

        private class FastMinHeap
        {
            private HeapNode[] _elements;
            private int _count;

            public int Count => _count;

            public FastMinHeap(int capacity)
            {
                _elements = new HeapNode[Mathf.Max(32, capacity)];
                _count = 0;
            }

            public void Clear()
            {
                _count = 0;
            }

            public void Push(int index, float priority)
            {
                if (_count == _elements.Length)
                {
                    Array.Resize(ref _elements, _elements.Length * 2);
                }

                int i = _count++;
                _elements[i] = new HeapNode { Index = index, Priority = priority };

                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_elements[i].Priority >= _elements[parent].Priority) break;

                    (_elements[i], _elements[parent]) = (_elements[parent], _elements[i]);
                    i = parent;
                }
            }

            public void Pop(out int index, out float priority)
            {
                index = _elements[0].Index;
                priority = _elements[0].Priority;

                _count--;
                _elements[0] = _elements[_count];

                int i = 0;
                while (true)
                {
                    int left = 2 * i + 1;
                    int right = 2 * i + 2;
                    int smallest = i;

                    if (left < _count && _elements[left].Priority < _elements[smallest].Priority)
                    {
                        smallest = left;
                    }

                    if (right < _count && _elements[right].Priority < _elements[smallest].Priority)
                    {
                        smallest = right;
                    }

                    if (smallest == i) break;

                    (_elements[i], _elements[smallest]) = (_elements[smallest], _elements[i]);
                    i = smallest;
                }
            }
        }
    }
}
