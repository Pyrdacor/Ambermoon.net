using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class ItemManager : IItemManager
    {
        readonly Dictionary<uint, Item> items = new Dictionary<uint, Item>();

        internal ItemManager(Dictionary<uint, Item> items)
        {
            this.items = items;
        }

        public Item GetItem(uint index) => items[index];
        public IReadOnlyList<Item> Items => items.Values.ToList();
    }
}
