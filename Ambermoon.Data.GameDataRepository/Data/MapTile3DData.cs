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
        private uint _mapEventId = 0;

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

        /// <summary>
        /// Sets this map block to become a wall.
        /// </summary>
        /// <param name="wallIndex">The wall index (1 to 154).</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetWall([Range(1, 154)] uint wallIndex)
        {
            // Valid wall indices range from 1 to 154.
            // Technically they are stored as index 101 to 254.
            if (wallIndex == 0 || wallIndex > 154)
                throw new ArgumentOutOfRangeException(nameof(wallIndex), "Wall indices must be in the range 1 to 154.");

            MapBlockType = MapBlockType.Wall;
            WallIndex = wallIndex;
            ObjectIndex = null;
        }

        /// <summary>
        /// Sets this map block to become a 3D object.
        /// </summary>
        /// <param name="objectIndex">The object index (1 to 100).</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetObject([Range(1, 100)] uint objectIndex)
        {
            // Valid object indices range from 1 to 100.
            // Technically they are stored as index 1 to 100 as well.
            if (objectIndex == 0 || objectIndex > 100)
                throw new ArgumentOutOfRangeException(nameof(objectIndex), "Object indices must be in the range 1 to 100.");

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

        /// <inheritdoc/>
        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            uint index = dataReader.ReadByte();

            var mapBlock = new MapTile3DData() { MapEventId = dataReader.ReadByte() };

            if (index == 0)
                mapBlock.SetFree();
            else if (index <= 100)
                mapBlock.SetObject(index);
            else if (index == 255)
                mapBlock.SetInvalid();
            else
                mapBlock.SetWall(index - 100);

            return mapBlock;
        }

        /// <inheritdoc/>
        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            byte index = MapBlockType switch
            {
                MapBlockType.Free => 0,
                MapBlockType.Object => (byte)ObjectIndex!,
                MapBlockType.Wall => (byte)(100 + WallIndex!),
                _ => 255
            };
            dataWriter.Write(index);
            dataWriter.Write((byte)MapEventId);
        }
    }
}
