using System.Collections.Generic;

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
    }
}
