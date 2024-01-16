using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Entities
{
    public class MapTile2DEntity : IEntity<Map.Tile>, IBackConversionEntity<Map.Tile>
    {
        private uint _frontTileIndex = 0;
        private uint _backTileIndex = 0;
        private uint _mapEventId = 0;

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

        public bool HasMapEvent => MapEventId != 0;

        public static MapTile2DEntity Empty => new();

        public static IEntity Deserialize(IDataReader dataReader, IGameData gameData)
        {
            var backTileIndex = dataReader.ReadByte();
            var mapEventId = dataReader.ReadByte();
            var frontTileIndex = dataReader.ReadWord();

            return new MapTile2DEntity
            {
                BackTileIndex = backTileIndex,
                FrontTileIndex = frontTileIndex,
                MapEventId = mapEventId
            };
        }

        public void Serialize(IDataWriter dataWriter, IGameData gameData)
        {
            dataWriter.Write((byte)BackTileIndex);
            dataWriter.Write((byte)MapEventId);
            dataWriter.Write((ushort)FrontTileIndex);
        }

        public static IEntity<Map.Tile> FromGameObject(Map.Tile gameObject, IGameData gameData)
        {
            return new MapTile2DEntity
            {
                BackTileIndex = gameObject.BackTileIndex,
                FrontTileIndex = gameObject.FrontTileIndex,
                MapEventId = gameObject.MapEventId
            };
        }

        public Map.Tile ToGameObject(IGameData gameData)
        {
            return new Map.Tile
            {
                BackTileIndex = BackTileIndex,
                FrontTileIndex = FrontTileIndex,
                MapEventId = MapEventId
            };
        }
    }
}
