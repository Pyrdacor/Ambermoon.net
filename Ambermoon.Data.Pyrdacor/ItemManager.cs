using Ambermoon.Data.Pyrdacor.Objects;

namespace Ambermoon.Data.Pyrdacor
{
    internal class ItemManager : IItemManager
    {
        readonly Lazy<Dictionary<uint, Item>> items;
        readonly Lazy<Dictionary<uint, TextList>> itemTexts;        

        public ItemManager(Func<Dictionary<uint, Item>> itemProvider,
            Func<Dictionary<uint, TextList>> itemTextProvider)
        {
            items = new Lazy<Dictionary<uint, Item>>(itemProvider);
            itemTexts = new Lazy<Dictionary<uint, TextList>>(itemTextProvider);
        }

        public Item? GetItem(uint index) => index == 0 || !items.Value.ContainsKey(index) ? null : items.Value[index];

        public string? GetText(uint index, uint subIndex) => index == 0 || !itemTexts.Value.ContainsKey(index) ? null : itemTexts.Value[index].GetText((int)subIndex);

        public IReadOnlyList<Item> Items => items.Value.Values.ToList().AsReadOnly();
    }
}
