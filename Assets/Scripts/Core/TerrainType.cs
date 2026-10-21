using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "NewTerrainType", menuName = "Game/Terrain Type", order = 1)]
    public class TerrainType : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _walkable = true;
        [SerializeField, Min(1f)] private float _moveCost = 1f;
        [SerializeField] private TileBase _tile;

        public string Id => _id;
        public bool Walkable => _walkable;
        public float MoveCost => _moveCost;
        public TileBase Tile => _tile;

        private void OnValidate()
        {
            if (_moveCost < 1f)
            {
                _moveCost = 1f;
            }
        }
    }
}
