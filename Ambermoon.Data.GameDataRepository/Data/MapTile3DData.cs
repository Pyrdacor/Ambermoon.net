using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public enum MapBlockType
    {
        /// <summary>
        /// Free 3D tile.
        /// </summary>
        Free,
        /// <summary>
        /// 3D object.
        /// </summary>
        Object,
        /// <summary>
        /// 3D wall block.
        /// </summary>
        Wall,
        /// <summary>
        /// Invalid marker. Use for map borders or areas which are not accessible.
        /// </summary>
        Invalid
    }

    public class MapTile3DData : IData, IEquatable<MapTile3DData>
    {

        #region Fields

        private uint _mapEventId;

        #endregion


        #region Properties

        public uint? ObjectIndex { get; private set; }

        public uint? WallIndex { get; private set; }

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

        public MapBlockType MapBlockType { get; private set; } = MapBlockType.Free;

        /// <summary>
        /// Determines if the map tile contains a map event.
        /// </summary>
        public bool HasMapEvent => MapEventId != 0;

        /// <summary>
        /// Default empty 3D map tile.
        /// </summary>
        public static MapTile3DData Empty => new() { MapBlockType = MapBlockType.Free };

        #endregion


        #region Methods

        /// <summary>
        /// Sets this map block to become a wall.
        /// </summary>
        /// <param name="wallIndex">The wall index (1 to 154).</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetWall([Range(GameDataRepository.MinWall3DIndex, GameDataRepository.MaxWall3DIndex)] uint wallIndex)
        {
            // Valid wall indices range from 1 to 154.
            // Technically they are stored as index 101 to 254.
            if (wallIndex is < GameDataRepository.MinWall3DIndex or > GameDataRepository.MaxWall3DIndex)
                throw new ArgumentOutOfRangeException(nameof(wallIndex), $"Wall indices must be in the range {GameDataRepository.MinWall3DIndex} to {GameDataRepository.MaxWall3DIndex}.");

            MapBlockType = MapBlockType.Wall;
            WallIndex = wallIndex;
            ObjectIndex = null;
        }

        /// <summary>
        /// Sets this map block to become a 3D object.
        /// </summary>
        /// <param name="objectIndex">The object index (1 to 100).</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetObject([Range(GameDataRepository.MinObject3DIndex, GameDataRepository.MaxObject3DIndex)] uint objectIndex)
        {
            // Valid object indices range from 1 to 100.
            // Technically they are stored as index 1 to 100 as well.
            if (objectIndex is < GameDataRepository.MinObject3DIndex or > GameDataRepository.MaxObject3DIndex)
                throw new ArgumentOutOfRangeException(nameof(objectIndex), $"Object indices must be in the range {GameDataRepository.MinObject3DIndex} to {GameDataRepository.MaxObject3DIndex}.");

            MapBlockType = MapBlockType.Object;
            ObjectIndex = objectIndex;
            WallIndex = null;
        }

        /// <summary>
        /// Sets this map block to become a free 3D tile.
        /// </summary>
        public void SetFree()
        {
            MapBlockType = MapBlockType.Free;
            ObjectIndex = null;
            WallIndex = null;
        }

        /// <summary>
        /// Sets this map block to become an invalid 3D tile (filler tile).
        /// </summary>
        public void SetInvalid()
        {
            MapBlockType = MapBlockType.Invalid;
            ObjectIndex = null;
            WallIndex = null;
        }

        /// <summary>
        /// If the tile is a 3D object, this method returns true and provides the object index.
        /// Otherwise, it returns false and provides null.
        /// </summary>
        /// <param name="objectIndex"></param>
        /// <returns></returns>
        public bool TryGetObject(out uint? objectIndex)
        {
            if (MapBlockType == MapBlockType.Object)
            {
                objectIndex = ObjectIndex;
                return true;
            }
            else
            {
                objectIndex = null;
                return false;
            }
        }

        /// <summary>
        /// If the tile is a 3D wall, this method returns true and provides the wall index.
        /// Otherwise, it returns false and provides null.
        /// </summary>
        /// <param name="wallIndex"></param>
        /// <returns></returns>
        public bool TryGetWall(out uint? wallIndex)
        {
            if (MapBlockType == MapBlockType.Wall)
            {
                wallIndex = WallIndex;
                return true;
            }
            else
            {
                wallIndex = null;
                return false;
            }
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            byte index = MapBlockType switch
            {
                MapBlockType.Free => 0,
                MapBlockType.Object => (byte)ObjectIndex!,
                MapBlockType.Wall => (byte)(GameDataRepository.MaxObject3DIndex + WallIndex!),
                _ => 255
            };
            dataWriter.Write(index);
            dataWriter.Write((byte)MapEventId);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            uint index = dataReader.ReadByte();

            var mapBlock = new MapTile3DData() { MapEventId = dataReader.ReadByte() };

            switch (index)
            {
                case 0:
                    mapBlock.SetFree();
                    break;
                case <= GameDataRepository.MaxObject3DIndex:
                    mapBlock.SetObject(index);
                    break;
                case 255:
                    mapBlock.SetInvalid();
                    break;
                default:
                    mapBlock.SetWall(index - GameDataRepository.MaxObject3DIndex);
                    break;
            }

            return mapBlock;
        }

        #endregion


        #region Equality

        public bool Equals(MapTile3DData? other)
        {
            if (other is null)
                return false;

            return
                WallIndex == other.WallIndex &&
                ObjectIndex == other.ObjectIndex &&
                MapBlockType == other.MapBlockType &&
                MapEventId == other.MapEventId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapTile3DData)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MapEventId, ObjectIndex ?? WallIndex ?? 0, (int)MapBlockType);
        }

        public static bool operator ==(MapTile3DData? left, MapTile3DData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapTile3DData? left, MapTile3DData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapTile3DData Copy()
        {
            return new()
            {
                WallIndex = WallIndex,
                ObjectIndex = ObjectIndex,
                MapBlockType = MapBlockType,
                MapEventId = MapEventId
            };
        }

        public object Clone() => Copy();

        #endregion

    }
}
