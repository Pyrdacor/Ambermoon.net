using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IItemManager
    {
        IReadOnlyList<Item> Items { get; }
        Item GetItem(uint index);
        string GetText(uint index, uint subIndex);
    }
}
