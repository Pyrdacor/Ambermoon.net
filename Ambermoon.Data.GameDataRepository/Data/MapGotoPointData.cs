using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;
    using Util;

    public sealed class MapGotoPointData : IMutableIndex, IIndexedData, IEquatable<MapGotoPointData>, INotifyPropertyChanged
    {

        #region Fields

        private uint _x;
        private uint _y;
        private uint _saveIndex;
        private string _name = string.Empty;
        private CharacterDirection _direction;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        [Range(1, GameDataRepository.MaxMapWidth)]
        public uint X
        {
            get => _x;
            set
            {
                ValueChecker.Check(value, 1, GameDataRepository.MaxMapWidth);
                SetField(ref _x, value);
            }
        }

        [Range(1, GameDataRepository.MaxMapHeight)]
        public uint Y
        {
            get => _y;
            set
            {
                ValueChecker.Check(value, 1, GameDataRepository.MaxMapHeight);
                SetField(ref _y, value);
            }
        }

        public CharacterDirection Direction
        {
            get => _direction;
            set => SetField(ref _direction, value);
        }

        [Range(0, byte.MaxValue)]
        public uint SaveIndex
        {
            get => _saveIndex;
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(ref _saveIndex, value);
            }
        }

        [StringLength(16)]
        public string Name
        {
            get => _name;
            set
            {
                ValueChecker.Check(value, 16);
                _name = value;
            }
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)X);
            dataWriter.Write((byte)Y);
            dataWriter.Write((byte)Direction);
            dataWriter.Write((byte)SaveIndex);
            string name = Name; // Note: It allows 16 chars and does not require a terminating 0
            if (name.Length > 16)
                name = name[..16];
            dataWriter.WriteWithoutLength(name.PadRight(16, '\0'));
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var gotoPointData = new MapGotoPointData();

            gotoPointData.X = dataReader.ReadByte();
            gotoPointData.Y = dataReader.ReadByte();
            gotoPointData.Direction = (CharacterDirection)dataReader.ReadByte();
            gotoPointData.SaveIndex = dataReader.ReadByte();
            gotoPointData.Name = dataReader.ReadString(16).TrimEnd('\0', ' ');

            return gotoPointData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var gotoPointData = (MapGotoPointData)Deserialize(dataReader, advanced);
            (gotoPointData as IMutableIndex).Index = index;
            return gotoPointData;
        }

        #endregion


        #region Equality

        public bool Equals(MapGotoPointData? other)
        {
            if (other is null)
                return false;

            return X == other.X && Y == other.Y && Direction == other.Direction &&
                   SaveIndex == other.SaveIndex && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapGotoPointData)obj);
        }

        public override int GetHashCode() => (int)SaveIndex;

        public static bool operator ==(MapGotoPointData? left, MapGotoPointData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapGotoPointData? left, MapGotoPointData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapGotoPointData Copy()
        {
            MapGotoPointData copy = new()
            {
                X = X,
                Y = Y,
                Direction = Direction,
                SaveIndex = SaveIndex,
                Name = Name
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
