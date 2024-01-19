using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class MapTile2DData : IData
    {
        private uint _frontTileIndex = 0;
        private uint _backTileIndex = 0;
        private uint _mapEventId = 0;

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

        /// <inheritdoc/>
        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var backTileIndex = dataReader.ReadByte();
            var mapEventId = dataReader.ReadByte();
            var frontTileIndex = dataReader.ReadWord();

            return new MapTile2DData
            {
                BackTileIndex = backTileIndex,
                FrontTileIndex = frontTileIndex,
                MapEventId = mapEventId
            };
        }

        /// <inheritdoc/>
        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)BackTileIndex);
            dataWriter.Write((byte)MapEventId);
            dataWriter.Write((ushort)FrontTileIndex);
        }
    }
}
