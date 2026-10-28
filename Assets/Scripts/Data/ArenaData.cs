using UnityEngine;
using Alchemy.Inspector;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewArenaData", menuName = "Game/Meta/Arena Data", order = 41)]
    public class ArenaData : ScriptableObject
    {
        [Title("Identity")]
        [SerializeField] private string _id = "arena_plains";
        [SerializeField] private string _arenaName = "Зелёные равнины";
        [TextArea(2, 3)]
        [SerializeField] private string _description = "Базовая арена без экстремальных эффектов местности. Идеально для начала.";
        [SerializeField] private Sprite _preview;
        [SerializeField] private string _sceneName = "SampleScene";

        [Title("Meta Unlock")]
        [SerializeField] private bool _unlockedByDefault = true;
        [SerializeField, Min(0)] private int _diamondCost = 0;

        public string Id => _id;
        public string ArenaName => _arenaName;
        public string Description => _description;
        public Sprite Preview => _preview;
        public string SceneName => _sceneName;
        public bool UnlockedByDefault => _unlockedByDefault;
        public int DiamondCost => _diamondCost;

        public void InitializeRuntime(string id, string name, string description, string sceneName, bool unlocked, int cost)
        {
            _id = id;
            _arenaName = name;
            _description = description;
            _sceneName = sceneName;
            _unlockedByDefault = unlocked;
            _diamondCost = cost;
        }
    }
}
