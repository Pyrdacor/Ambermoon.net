using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class ItemManager : IItemManager
    {
        readonly Dictionary<uint, Item> items;
        readonly Dictionary<uint, List<string>> itemTexts = new Dictionary<uint, List<string>>();

        internal ItemManager(Dictionary<uint, Item> items)
        {
            this.items = items;
        }

        public void AddTexts(uint index, List<string> texts)
        {
            itemTexts[index] = texts;
        }

        public Item GetItem(uint index) => items[index];

        public string GetText(uint index, uint subIndex) =>
            itemTexts.TryGetValue(index, out var texts) ? subIndex < texts.Count ? texts[(int)subIndex] : null : null;

        public IReadOnlyList<Item> Items => items.Values.ToList();
    }
}
