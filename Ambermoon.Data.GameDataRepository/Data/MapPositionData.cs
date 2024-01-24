using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;
    using Util;

    public sealed class MapPositionData : IMutableIndex, IIndexedData, IEquatable<MapPositionData>, INotifyPropertyChanged
    {

        #region Fields

        private uint _x;
        private uint _y;

        #endregion


        #region Properties

        public static MapPositionData Invalid => new() { X = 0, Y = 0 };

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        [Range(0, GameDataRepository.MaxMapWidth)]
        public uint X
        {
            get => _x;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMapWidth);
                SetField(ref _x, value);
            }
        }

        [Range(0, GameDataRepository.MaxMapHeight)]
        public uint Y
        {
            get => _y;
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxMapHeight);
                SetField(ref _y, value);
            }
        }

        public bool IsInvalid => X == 0 || Y == 0;

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)X);
            dataWriter.Write((byte)Y);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var positionData = new MapPositionData();

            positionData.X = dataReader.ReadByte();
            positionData.Y = dataReader.ReadByte();

            return positionData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var positionData = (MapPositionData)Deserialize(dataReader, advanced);
            (positionData as IMutableIndex).Index = index;
            return positionData;
        }

        #endregion


        #region Equality

        public bool Equals(MapPositionData? other)
        {
            if (other is null)
                return false;

            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapPositionData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(MapPositionData? left, MapPositionData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapPositionData? left, MapPositionData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapPositionData Copy()
        {
            MapPositionData copy = new()
            {
                X = X,
                Y = Y
            };

            (copy as IMutableIndex).Index = Index;

            return copy;
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }

}
