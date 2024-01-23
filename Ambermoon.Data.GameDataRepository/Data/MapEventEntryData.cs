using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MapEventEntryData : IMutableIndex, IIndexedData, IEquatable<MapEventEntryData>, INotifyPropertyChanged
    {

        #region Fields

        private AutomapType _automapType = AutomapType.None;
        private uint _eventIndex;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        [Range(0, ushort.MaxValue)]
        public uint EventIndex
        {
            get => _eventIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _eventIndex, value);
            }
        }

        public AutomapType AutomapType
        {
            get => _automapType;
            set => SetField(ref _automapType, value);
        }

        #endregion


        #region Serialization

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            return new MapEventEntryData
            {
                EventIndex = dataReader.ReadWord()
            };
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((ushort)EventIndex);
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var mapEventData = (MapEventEntryData)Deserialize(dataReader, advanced);
            (mapEventData as IMutableIndex).Index = index;
            return mapEventData;
        }

        #endregion


        #region Equality

        public bool Equals(MapEventEntryData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EventIndex == other.EventIndex && AutomapType == other.AutomapType;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapEventEntryData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(MapEventEntryData? left, MapEventEntryData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapEventEntryData? left, MapEventEntryData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapEventEntryData Copy()
        {
            return new MapEventEntryData
            {
                EventIndex = EventIndex,
                AutomapType = AutomapType
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
