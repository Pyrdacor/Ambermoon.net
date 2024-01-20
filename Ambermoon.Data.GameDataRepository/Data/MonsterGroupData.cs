using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;
using static Ambermoon.Data.Map.CharacterReference;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MonsterGroupData : IIndexed, IMutableIndex, IIndexedData, IEquatable<MonsterGroupData>
    {
        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public TwoDimensionalData<uint> MonsterIndices { get; } = new(6, 3);

        public MonsterGroupData Copy()
        {
            MonsterGroupData copy = new();

            (copy as IMutableIndex).Index = Index;

            for (int i = 0; i < MonsterIndices.Count; i++)
                copy.MonsterIndices[i] = MonsterIndices[i];

            return copy;
        }

        public object Clone() => Copy();

        public bool Equals(MonsterGroupData? other)
        {
            if (other is null)
                return false;

            return !MonsterIndices.Select((item, index) => new { Item = item, Index = index })
                .Any(entry => other.MonsterIndices[entry.Index] != entry.Item);
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var monsterGroupData = (MonsterGroupData)Deserialize(dataReader, advanced);
            (monsterGroupData as IMutableIndex).Index = index;
            return monsterGroupData;
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var monsterGroupData = new MonsterGroupData();

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    monsterGroupData.MonsterIndices.Set(x, y, dataReader.ReadWord());
                }
            }

            return monsterGroupData;
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    dataWriter.Write((ushort)MonsterIndices.Get(x, y));
                }
            }
        }
    }
}
