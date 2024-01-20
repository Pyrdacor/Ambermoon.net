using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Ambermoon.Data.Legacy.Serialization;
    using Util;

    public class MapData : IIndexed, IMutableIndex, IIndexedData, IEquatable<MapData>
    {
        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public MapType Type { get; private set; }

        public uint PaletteIndex { get; set; }

        public uint SongIndex { get; set; }

        public MapFlags Flags { get; private set; }

        public World World { get; private set; }

        public uint? LabdataIndex { get; private set; }

        public uint? TilesetIndex { get; private set; }

        public uint? SkyBackgroundIndex { get; private set; }

        public uint? NPCGraphicFileIndex { get; private set; }        

        public TwoDimensionalData<MapTile2DData>? Tiles2D { get; set; }

        public TwoDimensionalData<MapTile3DData>? Tiles3D { get; set; }

        public DependentDataCollection<MapCharacterData, MapData> MapCharacters { get; private set; } = new();

        public int Width => Tiles3D?.Width ?? Tiles2D?.Width ?? 0;

        public int Height => Tiles3D?.Height ?? Tiles2D?.Height ?? 0;

        /// <summary>
        /// List of all event entries.
        /// 
        /// Each element represents the index of the first event in the event entry.
        /// </summary>
        public List<uint> EventEntryList { get; private set; } = new();

        /// <summary>
        /// List of all existing map events.
        /// </summary>
        public DataCollection<MapEventData> Events { get; private set; } = new();

        public void Resize(int width, int height)
        {
            Tiles2D?.Resize(width, height, () => MapTile2DData.Empty);
            Tiles3D?.Resize(width, height, () => MapTile3DData.Empty);
        }

        public MapData Copy()
        {
            return new(); // TODO
        }

        public object Clone() => Copy();

        public bool Equals(MapData? other)
        {
            if (other is null)
                return false;

            // TODO
            return false;
        }

        /// <inheritdoc/>
        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var mapData = new MapData();

            mapData.Flags = (MapFlags)dataReader.ReadWord();
            mapData.Type = (MapType)dataReader.ReadByte();
            mapData.SongIndex = dataReader.ReadByte();
            int width = dataReader.ReadByte();
            int height = dataReader.ReadByte();
            uint tilesetIndex = dataReader.ReadByte();
            uint npcGraphicFileIndex = dataReader.ReadByte();
            uint skyBackgroundIndex = dataReader.ReadByte();
            mapData.PaletteIndex = dataReader.ReadByte();
            mapData.World = (World)dataReader.ReadByte();

            if (dataReader.ReadByte() != 0) // end of map header (this is an unused byte)
                throw new InvalidDataException("Invalid map data");

            // Map characters
            mapData.MapCharacters = DependentDataCollection<MapCharacterData, MapData>.Deserialize(dataReader, 32, mapData, advanced);

            if (mapData.Type == MapType.Map2D)
            {
                mapData.TilesetIndex = tilesetIndex;
                mapData.NPCGraphicFileIndex = npcGraphicFileIndex;
                mapData.Tiles2D = new(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        mapData.Tiles2D.Set(x, y, (MapTile2DData)MapTile2DData.Deserialize(dataReader, advanced));
                    }
                }
            }
            else
            {
                mapData.LabdataIndex = tilesetIndex;
                mapData.SkyBackgroundIndex = skyBackgroundIndex;
                mapData.Tiles3D = new(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        mapData.Tiles3D.Set(x, y, (MapTile3DData)MapTile3DData.Deserialize(dataReader, advanced));
                    }
                }
            }

            // Event entry list
            int eventEntryListSize = dataReader.ReadWord();
            mapData.EventEntryList = new(Util.ReadWordArray(dataReader, eventEntryListSize));
            
            // Events
            int numberOfEvents = dataReader.ReadWord();
            mapData.Events = DataCollection<MapEventData>.Deserialize(dataReader, numberOfEvents, advanced);

            // TODO: events, automap icons, goto points, map char positions

            return mapData;
        }

        /// <inheritdoc/>
        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // Header
            dataWriter.Write((ushort)Flags);
            dataWriter.Write((byte)Type);
            dataWriter.Write((byte)SongIndex);
            dataWriter.Write((byte)Width);
            dataWriter.Write((byte)Height);
            dataWriter.Write((byte)(Type == MapType.Map2D ? Util.EnsureValue(TilesetIndex) : Util.EnsureValue(LabdataIndex)));
            dataWriter.Write((byte)(Type == MapType.Map2D ? Util.EnsureValue(NPCGraphicFileIndex) : 0));
            dataWriter.Write((byte)(Type == MapType.Map3D ? Util.EnsureValue(SkyBackgroundIndex) : 0));
            dataWriter.Write((byte)PaletteIndex);
            dataWriter.Write((byte)World);
            dataWriter.Write((byte)0);

            // Map characters
            MapCharacters.Serialize(dataWriter, advanced);

            // Tile data
            IEnumerable<object>? tiles = Type == MapType.Map2D ? Tiles2D : Tiles3D;

            if (tiles is null)
                throw new NullReferenceException("Map tiles are missing.");

            foreach (var tile in tiles)
                (tile as IData)!.Serialize(dataWriter, advanced);

            // Event entry list
            Util.WriteWordCollection(dataWriter, EventEntryList);

            // Events
            Events.Serialize(dataWriter, advanced);

            // TODO
        }

        /// <inheritdoc/>
        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var mapEntity = (MapData)Deserialize(dataReader, advanced);
            (mapEntity as IMutableIndex).Index = index;
            return mapEntity;
        }
    }
}
