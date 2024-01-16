using Ambermoon.Data.Serialization;
using System;

namespace Ambermoon.Data.Legacy.Repository.Entities
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

    public class MapTile3DEntity : IEntity<Map.Block>, IBackConversionEntity<Map.Block>
    {
        private uint _mapEventId = 0;

        public uint? ObjectIndex { get; private set; }
        public uint? WallIndex { get; private set; }
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

        public bool HasMapEvent => MapEventId != 0;

        public static MapTile3DEntity Empty => new() { MapBlockType = MapBlockType.Free };

        /// <summary>
        /// Sets this map block to become a wall.
        /// </summary>
        /// <param name="wallIndex">The wall index (1 to 154).</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetWall(uint wallIndex)
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
        public void SetObject(uint objectIndex)
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

        public static IEntity Deserialize(IDataReader dataReader, IGameData gameData)
        {
            uint index = dataReader.ReadByte();

            var mapBlock = new MapTile3DEntity() { MapEventId = dataReader.ReadByte() };

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

        public void Serialize(IDataWriter dataWriter, IGameData gameData)
        {
            byte index = MapBlockType switch
            {
                MapBlockType.Free => 0,
                MapBlockType.Object => (byte)ObjectIndex,
                MapBlockType.Wall => (byte)(100 + WallIndex),
                _ => 255
            };
            dataWriter.Write(index);
            dataWriter.Write((byte)MapEventId);
        }

        public static IEntity<Map.Block> FromGameObject(Map.Block gameObject, IGameData gameData)
        {
            var mapBlock = new MapTile3DEntity() { MapEventId = gameObject.MapEventId };

            if (gameObject.ObjectIndex != 0)
                mapBlock.SetObject(gameObject.ObjectIndex);
            else if (gameObject.WallIndex != 0)
                mapBlock.SetWall(gameObject.WallIndex);
            else if (gameObject.MapBorder)
                mapBlock.SetInvalid();
            else
                mapBlock.SetFree();

            return mapBlock;
        }

        public Map.Block ToGameObject(IGameData gameData)
        {
            return new Map.Block
            {
                ObjectIndex = ObjectIndex ?? 0,
                WallIndex = WallIndex ?? 0,
                MapBorder = MapBlockType == MapBlockType.Invalid,
                MapEventId = MapEventId
            };
        }
    }
}
