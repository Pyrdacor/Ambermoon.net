using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;
    using Util;

    public class MapEventData : IMutableIndex, IIndexedData, IEquatable<MapEventData>, INotifyPropertyChanged
    {

        #region Fields

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public EventType Type => (EventType)EventData[0];

        [Range(0, ushort.MaxValue)]
        public uint? NextEventIndex
        {
            get
            {
                ushort index = EventData[^2];
                index <<= 8;
                index |= EventData[^1];

                return index == ushort.MaxValue ? null : index;
            }
            set
            {
                uint index = value ?? ushort.MaxValue;
                ValueChecker.Check(index, 0, ushort.MaxValue);
                EventData[^2] = (byte)(index >> 8);
                EventData[^1] = (byte)(index & 0xff);
            }
        }

        public byte[] EventData { get; private init; } = new byte[12];

        #endregion


        #region Serialization

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            return new MapEventData
            {
                EventData = dataReader.ReadBytes(12)
            };
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write(EventData);
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var mapEventData = (MapEventData)Deserialize(dataReader, advanced);
            (mapEventData as IMutableIndex).Index = index;
            return mapEventData;
        }

        #endregion


        #region Equality

        public bool Equals(MapEventData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EventData.Equals(other.EventData);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapEventData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(MapEventData? left, MapEventData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapEventData? left, MapEventData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapEventData Copy()
        {
            return new MapEventData
            {
                EventData = (byte[])EventData.Clone()
            };
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}
