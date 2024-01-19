using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class ItemData : IIndexedData
    {
        public uint Index { get; private set; }

        public static ItemData Create(DictionaryList<ItemData> list, uint? index)
        {
            var itemData = new ItemData { Index = index ?? list.Keys.Max() + 1 };
            list.Add(itemData);
            return itemData;
        }

        // TODO

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var itemData = (ItemData)Deserialize(dataReader, advanced);
            itemData.Index = index;
            return itemData;
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
