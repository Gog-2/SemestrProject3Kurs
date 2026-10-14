using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "NewTerrainType", menuName = "Game/Terrain Type", order = 1)]
    public class TerrainType : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private bool walkable = true;
        [SerializeField, Min(1f)] private float moveCost = 1f;
        [SerializeField] private TileBase tile;

        public string Id => id;
        public bool Walkable => walkable;
        public float MoveCost => moveCost;
        public TileBase Tile => tile;

        private void OnValidate()
        {
            if (moveCost < 1f)
            {
                moveCost = 1f;
            }
        }
    }
}
