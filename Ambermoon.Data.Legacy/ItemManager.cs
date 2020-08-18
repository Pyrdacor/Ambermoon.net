using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public class ItemManager : IItemManager
    {
        readonly Dictionary<uint, Item> items = new Dictionary<uint, Item>();

        public ItemManager(IGameData gameData, IItemReader itemReader)
        {
            var file = gameData.Files[$"AM2_CPU"].Files[1];

            file.Position = 0;
            var data = AmigaExecutable.Read(file).Last(h => h.Type == AmigaExecutable.HunkType.Data).Data;
            file = new DataReader(data);

            // First find the item offset (the lamed ailment is the first item)
            long offset = file.FindString("LAMED", 0);

            if (offset == -1)
                offset = file.FindString("GELÄHMT", 0);

            if (offset == -1)
                throw new AmbermoonException(ExceptionScope.Data, "Could not find item data in AM2_CPU.");

            offset -= 40; // item name has a 40 byte offset to the item data offset

            file.Position = (int)offset;

            for (uint i = 1; i <= 402; ++i) // there are 402 items
                items.Add(i, Item.Load(itemReader, file));
        }

        public Item GetItem(uint index) => items[index];
    }
}
