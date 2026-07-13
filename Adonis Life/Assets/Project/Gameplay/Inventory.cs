using System;
using System.Collections.Generic;

namespace AdonisLife.Gameplay
{
    /// <summary>
    /// Pure item inventory: stackable item counts with a slot capacity. Raises
    /// <see cref="OnChanged"/> whenever contents change so UI and gameplay can hook in.
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();
        private readonly int _maxSlots;

        /// <summary>Raised after any successful add or remove. Payload: item id, new count.</summary>
        public event Action<string, int> OnChanged;

        public int MaxSlots => _maxSlots;
        public int UsedSlots => _items.Count;

        public Inventory(int maxSlots)
        {
            if (maxSlots <= 0)
            {
                throw new ArgumentException("Inventory must have at least one slot.", nameof(maxSlots));
            }

            _maxSlots = maxSlots;
        }

        public int GetCount(string itemId)
        {
            return _items.TryGetValue(itemId, out int count) ? count : 0;
        }

        /// <summary>Adds items; fails (returns false) if a new slot would exceed capacity.</summary>
        public bool Add(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
            {
                return false;
            }

            if (!_items.ContainsKey(itemId) && _items.Count >= _maxSlots)
            {
                return false;
            }

            _items[itemId] = GetCount(itemId) + amount;
            OnChanged?.Invoke(itemId, _items[itemId]);
            return true;
        }

        /// <summary>Removes items; fails (returns false) if fewer than requested are held.</summary>
        public bool Remove(string itemId, int amount = 1)
        {
            if (amount <= 0 || GetCount(itemId) < amount)
            {
                return false;
            }

            int remaining = _items[itemId] - amount;
            if (remaining == 0)
            {
                _items.Remove(itemId);
            }
            else
            {
                _items[itemId] = remaining;
            }

            OnChanged?.Invoke(itemId, remaining);
            return true;
        }

        /// <summary>Snapshot of all item counts, for saving.</summary>
        public List<(string itemId, int count)> GetAll()
        {
            var all = new List<(string, int)>();
            foreach (KeyValuePair<string, int> entry in _items)
            {
                all.Add((entry.Key, entry.Value));
            }

            all.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
            return all;
        }
    }
}
