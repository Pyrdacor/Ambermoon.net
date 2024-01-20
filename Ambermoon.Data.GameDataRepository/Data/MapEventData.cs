using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MapEventData : IIndexed, IMutableIndex, IIndexedData, IEquatable<MapEventData>
    {
        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public EventType Type => (EventType)EventData[0];

        public uint? NextEventIndex
        {
            get
            {
                ushort index = (ushort)EventData[^2];
                index <<= 8;
                index |= (ushort)EventData[^1];

                return index == ushort.MaxValue ? null : index;
            }
        }

        public byte[] EventData { get; private set; } = new byte[12];

        public MapEventData Copy()
        {
            return new(); // TODO
        }

        public object Clone() => Copy();

        public bool Equals(MapEventData? other)
        {
            if (other is null)
                return false;

            // TODO
            return false;
        }

        /// <inheritdoc/>
        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            return new MapEventData
            {
                EventData = dataReader.ReadBytes(12)
            };
        }

        /// <inheritdoc/>
        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write(EventData);
        }

        /// <inheritdoc/>
        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var mapEventData = (MapEventData)Deserialize(dataReader, advanced);
            (mapEventData as IMutableIndex).Index = index;
            return mapEventData;
        }
    }
}
