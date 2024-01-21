using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class ItemData : IIndexed, IMutableIndex, IIndexedData, IEquatable<ItemData>
    {
        // TODO

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        // TODO
        public uint Damage { get; set; }
        public uint Defense { get; set; }
        public uint MagicAttackLevel { get; set; }
        public uint MagicDefenseLevel { get; set; }
        public uint Weight { get; set; }

        public ItemData Copy()
        {
            return new(); // TODO
        }

        public object Clone() => Copy();

        public bool Equals(ItemData? other)
        {
            if (other is null)
                return false;

            // TODO
            return false;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var itemData = (ItemData)Deserialize(dataReader, advanced);
            (itemData as IMutableIndex).Index = index;
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
