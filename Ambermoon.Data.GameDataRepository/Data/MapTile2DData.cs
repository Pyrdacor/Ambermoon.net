using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Util;
    using Serialization;

    public class MapTile2DData : IData, IEquatable<MapTile2DData>, INotifyPropertyChanged
    {

        #region Fields

        private uint _frontTileIndex;
        private uint _backTileIndex;
        private uint? _mapEventId;

        #endregion


        #region Properties

        [Range(1, byte.MaxValue)]
        public uint BackTileIndex
        {
            get => _backTileIndex;
            set
            {
                ValueChecker.Check(value, 1, byte.MaxValue);
                SetField(ref _backTileIndex, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint FrontTileIndex
        {
            get => _frontTileIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _frontTileIndex, value);
            }
        }

        // TODO: The original states that 64 is max. But I guess we already have working maps with more? Needs testing/verification.
        /// <summary>
        /// Index of the map event entry associated with this map tile.
        /// </summary>
        [Range(1, byte.MaxValue)]
        public uint? MapEventId
        {
            get => _mapEventId;
            set
            {
                if (value is not null)
                    ValueChecker.Check(value.Value, 1, byte.MaxValue);
                SetField(ref _mapEventId, value);
            }
        }

        /// <summary>
        /// Determines if the map tile contains a map event.
        /// </summary>
        public bool HasMapEvent => MapEventId != null;

        /// <summary>
        /// Default empty 2D map tile.
        /// </summary>
        public static MapTile2DData Empty => new();

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)BackTileIndex);
            dataWriter.Write((byte)(MapEventId ?? 0));
            dataWriter.Write((ushort)FrontTileIndex);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            uint backTileIndex = dataReader.ReadByte();
            uint mapEventId = dataReader.ReadByte();
            uint frontTileIndex = dataReader.ReadWord();

            return new MapTile2DData
            {
                BackTileIndex = backTileIndex,
                FrontTileIndex = frontTileIndex,
                MapEventId = mapEventId == 0 ? null : mapEventId
            };
        }

        #endregion


        #region Equality

        public bool Equals(MapTile2DData? other)
        {
            if (other is null)
                return false;

            return
                BackTileIndex == other.BackTileIndex &&
                MapEventId == other.MapEventId &&
                FrontTileIndex == other.FrontTileIndex;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapTile2DData)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MapEventId, BackTileIndex, FrontTileIndex);
        }

        public static bool operator ==(MapTile2DData? left, MapTile2DData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapTile2DData? left, MapTile2DData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapTile2DData Copy()
        {
            return new()
            {
                BackTileIndex = BackTileIndex,
                MapEventId = MapEventId,
                FrontTileIndex = FrontTileIndex
            };
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
