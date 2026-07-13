using AdonisLife.World.Environment;
using UnityEngine;

namespace AdonisLife.Gameplay
{
    /// <summary>
    /// Scene-level gameplay service hub: owns the player's inventory and quest log and wires
    /// them to the save system and world clock. Other systems access it via
    /// <see cref="Instance"/>.
    /// </summary>
    public class GameplayServices : MonoBehaviour
    {
        [SerializeField] private Transform _player;
        [SerializeField] private DayNightCycle _dayNight;
        [SerializeField] private WeatherSystem _weather;
        [SerializeField] private int _inventorySlots = 24;

        public static GameplayServices Instance { get; private set; }

        public Inventory Inventory { get; private set; }
        public QuestLog QuestLog { get; private set; }

        private void Awake()
        {
            Instance = this;
            Inventory = new Inventory(_inventorySlots);
            QuestLog = new QuestLog();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Saves the current game state to the default save path.</summary>
        public void SaveGame()
        {
            SaveData data = SaveSystem.CreateSnapshot(
                _player != null ? _player.position : Vector3.zero,
                _player != null ? _player.eulerAngles.y : 0f,
                _dayNight != null ? _dayNight.TimeOfDay : 12f,
                _weather != null ? _weather.CurrentDay : 0,
                Inventory,
                QuestLog);
            SaveSystem.Save(data, SaveSystem.GetDefaultPath());
        }

        /// <summary>Loads the saved state and applies the player transform and clock.</summary>
        public bool LoadGame()
        {
            SaveData data = SaveSystem.Load(SaveSystem.GetDefaultPath());
            if (data == null)
            {
                return false;
            }

            if (_player != null)
            {
                _player.SetPositionAndRotation(
                    data.playerPosition, Quaternion.Euler(0f, data.playerYawDegrees, 0f));
            }

            if (_dayNight != null)
            {
                _dayNight.SetTimeOfDay(data.timeOfDay);
            }

            foreach (SavedItem item in data.items)
            {
                Inventory.Add(item.itemId, item.count);
            }

            foreach (Quest quest in data.quests)
            {
                QuestLog.Register(quest);
            }

            return true;
        }
    }
}
