using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MapTile2DData : IData, IEquatable<MapTile2DData>
    {

        #region Fields

        private uint _frontTileIndex;
        private uint _backTileIndex;
        private uint _mapEventId;

        #endregion


        #region Properties

        [Range(0, byte.MaxValue)]
        public uint BackTileIndex
        {
            get => _backTileIndex;
            set
            {
                if (value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(BackTileIndex), $"Back tile indices are limited to the range 0 to {byte.MaxValue}.");

                _backTileIndex = value;
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint FrontTileIndex
        {
            get => _frontTileIndex;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(FrontTileIndex), $"Front tile indices are limited to the range 0 to {ushort.MaxValue}.");

                _frontTileIndex = value;
            }
        }

        // TODO: The original states that 64 is max. But I guess we already have working maps with more? Needs testing/verification.
        [Range(0, byte.MaxValue)]
        public uint MapEventId
        {
            get => _mapEventId;
            set
            {
                if (value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(MapEventId), $"Map event indices are limited to the range 0 to {byte.MaxValue}.");

                _mapEventId = value;
            }
        }

        /// <summary>
        /// Determines if the map tile contains a map event.
        /// </summary>
        public bool HasMapEvent => MapEventId != 0;

        /// <summary>
        /// Default empty 2D map tile.
        /// </summary>
        public static MapTile2DData Empty => new();

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)BackTileIndex);
            dataWriter.Write((byte)MapEventId);
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
                MapEventId = mapEventId
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
        
    }
}
