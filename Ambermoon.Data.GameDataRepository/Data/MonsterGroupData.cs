using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MonsterGroupData : IIndexedData
    {
        public uint Index { get; private set; }

        public TwoDimensionalData<uint> MonsterIndices { get; } = new(6, 3);

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var monsterGroupData = (MonsterGroupData)Deserialize(dataReader, advanced);
            monsterGroupData.Index = index;
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
