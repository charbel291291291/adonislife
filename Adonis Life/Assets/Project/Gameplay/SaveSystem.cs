using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AdonisLife.Gameplay
{
    /// <summary>One saved inventory stack.</summary>
    [Serializable]
    public class SavedItem
    {
        public string itemId;
        public int count;
    }

    /// <summary>
    /// The full serializable game state: player transform, inventory, quests, and world clock.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public Vector3 playerPosition;
        public float playerYawDegrees;
        public float timeOfDay;
        public int day;
        public List<SavedItem> items = new List<SavedItem>();
        public List<Quest> quests = new List<Quest>();
    }

    /// <summary>
    /// JSON save system. Pure file round-trip — building the <see cref="SaveData"/> snapshot is
    /// the caller's responsibility so this stays testable and engine-agnostic.
    /// </summary>
    public static class SaveSystem
    {
        public const string DefaultFileName = "adonis_save.json";

        public static string GetDefaultPath()
        {
            return Path.Combine(Application.persistentDataPath, DefaultFileName);
        }

        public static void Save(SaveData data, string path)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true));
        }

        public static SaveData Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }

        /// <summary>Builds a snapshot from live gameplay objects.</summary>
        public static SaveData CreateSnapshot(
            Vector3 playerPosition, float playerYawDegrees, float timeOfDay, int day,
            Inventory inventory, QuestLog questLog)
        {
            var data = new SaveData
            {
                playerPosition = playerPosition,
                playerYawDegrees = playerYawDegrees,
                timeOfDay = timeOfDay,
                day = day
            };

            if (inventory != null)
            {
                foreach ((string itemId, int count) in inventory.GetAll())
                {
                    data.items.Add(new SavedItem { itemId = itemId, count = count });
                }
            }

            if (questLog != null)
            {
                foreach (KeyValuePair<string, Quest> entry in questLog.Quests)
                {
                    data.quests.Add(entry.Value);
                }
            }

            return data;
        }
    }
}
