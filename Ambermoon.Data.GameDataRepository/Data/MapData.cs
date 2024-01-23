using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Collections;
    using Serialization;
    using Util;

    public enum MapEnvironment
    {
        Indoor,
        Outdoor,
        Dungeon
    }

    public sealed class MapData : IMutableIndex, IIndexedData, IEquatable<MapData>, INotifyPropertyChanged
    {
        private uint _paletteIndex;
        private uint _songIndex;
        private uint? _labdataIndex;
        private uint? _tilesetIndex1;
        private uint? _skyBackgroundIndex1;
        private uint? _npcGraphicFileIndex1;
        private World _world;
        private MapType _type;

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public MapType Type
        {
            get => _type;
            private set => SetField(ref _type, value);
        }

        [Range(0, byte.MaxValue)]
        public uint PaletteIndex
        {
            get => _paletteIndex;
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(ref _paletteIndex, value);
            }
        }

        [Range(0, byte.MaxValue)]
        public uint SongIndex
        {
            get => _songIndex;
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(ref _songIndex, value);
            }
        }

        public MapFlags Flags { get; private set; }

        public World World
        {
            get => _world;
            private set => SetField(ref _world, value);
        }

        [Range(0, byte.MaxValue)]
        public uint? LabdataIndex
        {
            get => _labdataIndex;
            private set => SetField(ref _labdataIndex, value);
        }

        [Range(0, byte.MaxValue)]
        public uint? TilesetIndex
        {
            get => _tilesetIndex1;
            private set => SetField(ref _tilesetIndex1, value);
        }

        [Range(0, byte.MaxValue)]
        public uint? SkyBackgroundIndex
        {
            get => _skyBackgroundIndex1;
            private set => SetField(ref _skyBackgroundIndex1, value);
        }

        [Range(0, byte.MaxValue)]
        public uint? NpcGraphicFileIndex
        {
            get => _npcGraphicFileIndex1;
            private set => SetField(ref _npcGraphicFileIndex1, value);
        }

        public MapEnvironment Environment
        {
            get
            {
                if (Flags.HasFlag(MapFlags.Indoor))
                    return MapEnvironment.Indoor;
                if (Flags.HasFlag(MapFlags.Outdoor))
                    return MapEnvironment.Outdoor;
                return MapEnvironment.Dungeon;
            }
            set
            {
                Flags &= (MapFlags)0xf8; // mask out environment first

                switch (value)
                {
                    case MapEnvironment.Indoor:
                        Flags |= MapFlags.Indoor;
                        break;
                    case MapEnvironment.Outdoor:
                        Flags |= MapFlags.Outdoor;
                        break;
                    default:
                        Flags |= MapFlags.Dungeon;
                        break;
                }

                OnPropertyChanged();
            }
        }

        /*Indoor = 1 << 0, // Always at full light.
           Outdoor = 1 << 1, // Light level is given by the daytime.
           Dungeon = 1 << 2, // Only own light sources will grant light.
           Automapper = 1 << 3, // If set the map is available and the map has to be explored. It also allows map-related spells. All Morag temples omit this.
           CanRest = 1 << 4,
           Unknown1 = 1 << 5, // Unknown. All world maps use that in Ambermoon.
           Sky = 1 << 6, // All towns have this and the ruin tower. Only considered for 3D maps.
           NoSleepUntilDawn = 1 << 7, // If active sleep time is always 8 hours.
           StationaryGraphics = 1 << 8, // Allow stationary graphics (travel type images) and therefore transports. Is set for all world maps. This also controls if the music is taken from the map file or dependent on the travel type.
           Unknown2 = 1 << 9, // Unknown. Never used in Ambermoon.
           WorldSurface = 1 << 10, // If set the map doesn't use map text 0 as the title but uses the world name instead. Moreover based on world adjacent maps are shown with a size of 50x50.
           CanUseMagic = 1 << 11, // Only 0 in map 269 which is the house of the baron of Spannenberg (also in map 148 but this is a bug). It just disables the spell book if not set but you still can use scrolls or items.
           NoTravelMusic = 1 << 12, // Won't use travel music if StationaryGraphics is set
           NoMarkOrReturn = 1 << 13, // Forbids the use of "Word of marking" and "Word of returning"
           NoEagleOrBroom = 1 << 14, // Forbids the use of eagle and broom
           SharedMapData = 1 << 15, // Only used internal by the new game data, do not use in original!
           SmallPlayer = StationaryGraphics // Display player smaller. Only all world maps have this set. Only considered for 2D maps.
               */

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

        #endregion


        #region Methods

        public void Resize(int width, int height)
        {
            Tiles2D?.Resize(width, height, () => MapTile2DData.Empty);
            Tiles3D?.Resize(width, height, () => MapTile3DData.Empty);
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // Header
            dataWriter.Write((ushort)Flags);
            dataWriter.Write((byte)Type);
            dataWriter.Write((byte)SongIndex);
            dataWriter.Write((byte)Width);
            dataWriter.Write((byte)Height);
            dataWriter.Write((byte)(Type == MapType.Map2D ? Util.EnsureValue(TilesetIndex) : Util.EnsureValue(LabdataIndex)));
            dataWriter.Write((byte)(Type == MapType.Map2D ? Util.EnsureValue(NpcGraphicFileIndex) : 0));
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
                mapData.NpcGraphicFileIndex = npcGraphicFileIndex;
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

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var mapEntity = (MapData)Deserialize(dataReader, advanced);
            (mapEntity as IMutableIndex).Index = index;
            return mapEntity;
        }

        #endregion


        #region Equality

        public bool Equals(MapData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   Type == other.Type &&
                   PaletteIndex == other.PaletteIndex &&
                   SongIndex == other.SongIndex &&
                   Flags == other.Flags &&
                   World == other.World &&
                   LabdataIndex == other.LabdataIndex &&
                   TilesetIndex == other.TilesetIndex &&
                   SkyBackgroundIndex == other.SkyBackgroundIndex &&
                   NpcGraphicFileIndex == other.NpcGraphicFileIndex &&
                   Equals(Tiles2D, other.Tiles2D) &&
                   Equals(Tiles3D, other.Tiles3D) &&
                   MapCharacters.Equals(other.MapCharacters) &&
                   EventEntryList.Equals(other.EventEntryList) &&
                   Events.Equals(other.Events);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(MapData? left, MapData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapData? left, MapData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MapData Copy()
        {
            var copy = new MapData();

            copy.Type = Type;
            copy.PaletteIndex = PaletteIndex;
            copy.SongIndex = SongIndex;
            copy.Flags = Flags;
            copy.World = World;
            copy.LabdataIndex = LabdataIndex;
            copy.TilesetIndex = TilesetIndex;
            copy.SkyBackgroundIndex = SkyBackgroundIndex;
            copy.NpcGraphicFileIndex = NpcGraphicFileIndex;
            copy.Tiles2D = Tiles2D?.Copy();
            copy.Tiles3D = Tiles3D?.Copy();
            copy.MapCharacters = MapCharacters.Copy();
            copy.EventEntryList = new(EventEntryList);
            copy.Events = Events.Copy();

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
