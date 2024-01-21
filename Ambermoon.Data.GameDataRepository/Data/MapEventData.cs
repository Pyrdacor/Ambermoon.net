using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MapEventData : IMutableIndex, IIndexedData, IEquatable<MapEventData>
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
                ushort index = EventData[^2];
                index <<= 8;
                index |= EventData[^1];

                return index == ushort.MaxValue ? null : index;
            }
        }

        public byte[] EventData { get; private init; } = new byte[12];

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

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapEventData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public MapEventData Copy()
        {
            return new MapEventData
            {
                EventData = (byte[])EventData.Clone()
            };
        }

        public object Clone() => Copy();

        public bool Equals(MapEventData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EventData.Equals(other.EventData);
        }
    }
}
