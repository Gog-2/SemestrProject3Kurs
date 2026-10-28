using UnityEngine;
using Alchemy.Inspector;
using Game.Data;

namespace Game.Core
{
    public class RunConfig : MonoBehaviour
    {
        public static RunConfig Instance { get; private set; }

        [Title("Selected Configuration")]
        [SerializeField] private KnightData _selectedKnight;
        [SerializeField] private ArenaData _selectedArena;
        [SerializeField] private WeaponSelectionData _selectedWeapon;

        public KnightData SelectedKnight
        {
            get => _selectedKnight;
            set => _selectedKnight = value;
        }

        public ArenaData SelectedArena
        {
            get => _selectedArena;
            set => _selectedArena = value;
        }

        public WeaponSelectionData SelectedWeapon
        {
            get => _selectedWeapon;
            set => _selectedWeapon = value;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
