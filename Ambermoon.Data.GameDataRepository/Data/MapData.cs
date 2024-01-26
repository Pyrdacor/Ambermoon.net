using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Ambermoon.Data.Enumerations;
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
        private uint? _tilesetIndex;
        private uint? _skyBackgroundIndex;
        private uint? _npcGraphicFileIndex;
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
            get => _tilesetIndex;
            private set => SetField(ref _tilesetIndex, value);
        }

        [Range(1, 3)]
        public uint? SkyBackgroundIndex
        {
            get => _skyBackgroundIndex;
            private set => SetField(ref _skyBackgroundIndex, value);
        }

        [Range(0, byte.MaxValue)]
        public uint? NpcGraphicFileIndex
        {
            get => _npcGraphicFileIndex;
            private set => SetField(ref _npcGraphicFileIndex, value);
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

                Flags |= value switch
                {
                    MapEnvironment.Indoor => MapFlags.Indoor,
                    MapEnvironment.Outdoor => MapFlags.Outdoor,
                    _ => MapFlags.Dungeon
                };

                HandleEnvironmentChanges();
                OnPropertyChanged();
            }
        }

        public bool AllowResting
        {
            get => Flags.HasFlag(MapFlags.CanRest);
            set
            {
                if (value)
                    Flags |= MapFlags.CanRest;
                else
                    Flags &= ~MapFlags.CanRest;

                OnPropertyChanged();
            }
        }

        public bool AllowUsingMagic
        {
            get => Flags.HasFlag(MapFlags.CanUseMagic);
            set
            {
                if (value)
                    Flags |= MapFlags.CanUseMagic;
                else
                    Flags &= ~MapFlags.CanUseMagic;

                OnPropertyChanged();
            }
        }

        public bool AlwaysSleepEightHours
        {
            get => Flags.HasFlag(MapFlags.NoSleepUntilDawn);
            set
            {
                if (value)
                    Flags |= MapFlags.NoSleepUntilDawn;
                else
                    Flags &= ~MapFlags.NoSleepUntilDawn;

                OnPropertyChanged();
            }
        }

        public bool WorldMap
        {
            get => Type == MapType.Map2D && Flags.HasFlag(MapFlags.WorldSurface);
            set
            {
                if (value)
                {
                    if (Type != MapType.Map2D)
                        throw new InvalidOperationException("Only 2D maps can be world maps.");
                    Flags |= MapFlags.NoSleepUntilDawn;
                }
                else
                    Flags &= ~MapFlags.NoSleepUntilDawn;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// This disables the travel music on world maps
        /// and is ignored on all other maps.
        ///
        /// Normally on world maps the music depends on the travel type.
        /// If this is active, the map will instead play the song which
        /// is specified in the map data.
        ///
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool DisableTravelMusic
        {
            get => Flags.HasFlag(MapFlags.NoTravelMusic);
            set
            {
                if (value)
                    Flags |= MapFlags.NoTravelMusic;
                else
                    Flags &= ~MapFlags.NoTravelMusic;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Disables the usage of the spells "Word of marking" and "Word of returning".
        ///
        /// This is always true on the Forest Moon and Morag but
        /// with this you can also disable it on Lyramion maps.
        ///
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool DisableMarkAndReturn
        {
            get => Flags.HasFlag(MapFlags.NoMarkOrReturn);
            set
            {
                if (value)
                    Flags |= MapFlags.NoMarkOrReturn;
                else
                    Flags &= ~MapFlags.NoMarkOrReturn;

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Disables the usage of the eagle and the broom.
        ///
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public bool DisableEagleAndBroom
        {
            get => Flags.HasFlag(MapFlags.NoEagleOrBroom);
            set
            {
                if (value)
                    Flags |= MapFlags.NoEagleOrBroom;
                else
                    Flags &= ~MapFlags.NoEagleOrBroom;

                OnPropertyChanged();
            }
        }

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
        public DictionaryList<MapEventEntryData> EventEntryList { get; private set; } = new();

        /// <summary>
        /// List of all existing map events.
        /// </summary>
        public DictionaryList<EventData> Events { get; private set; } = new();

        /// <summary>
        /// List of all goto (fast travel) points.
        /// </summary>
        public DictionaryList<MapGotoPointData>? GotoPoints { get; private set; }

        #endregion


        #region Methods

        public void Resize(int width, int height)
        {
            Tiles2D?.Resize(width, height, () => MapTile2DData.Empty);
            Tiles3D?.Resize(width, height, () => MapTile3DData.Empty);
        }

        private void HandleEnvironmentChanges()
        {
            if (Type == MapType.Map3D)
            {
                if (SkyBackgroundIndex is not null && Environment != MapEnvironment.Outdoor)
                    SkyBackgroundIndex = null;
                else if (SkyBackgroundIndex is null && Environment == MapEnvironment.Outdoor)
                    SkyBackgroundIndex = (uint)World + 1;
            }
        }

        public void SetupWorldMap(World world)
        {
            Type = MapType.Map2D;
            Flags = MapFlags.Outdoor |
                    MapFlags.WorldSurface |
                    MapFlags.CanRest |
                    MapFlags.CanUseMagic |
                    MapFlags.StationaryGraphics;
            World = world;
            SongIndex = (uint)Song.PloddingAlong;
            // The tileset and palette indices match the world + 1 for world maps (1, 2, 3)
            TilesetIndex = (uint)world + 1;
            PaletteIndex = (uint)world + 1;
            
            Tiles2D = new(50, 50);

            // Inputs are X and Y
            Func<uint, uint, MapTile2DData> defaultTileCreator = world switch
            {
                World.ForestMoon => (x, y) => new MapTile2DData() { BackTileIndex = 7 + (x + y) % 8 },
                World.Morag => (x, y) => new MapTile2DData() { BackTileIndex = 1 + (y % 8) * 6 + x % 6 },
                _ => (x, _) => new MapTile2DData() { BackTileIndex = 215 + x % 4 }
            };

            for (int y = 0; y < 50; y++)
            {
                for (int x = 0; x < 50; x++)
                {
                    Tiles2D.Set(x, y, defaultTileCreator((uint)x, (uint)y));
                }
            }

            foreach (var mapChar in MapCharacters)
                mapChar.SetEmpty();

            NpcGraphicFileIndex = null;
            LabdataIndex = null;
            Tiles3D = null;
            SkyBackgroundIndex = null;
            GotoPoints = null;
        }

        public void Setup2DMap(MapEnvironment environment, World world, int width, int height)
        {
            Type = MapType.Map2D;
            Flags = MapFlags.CanUseMagic;
            if (environment == MapEnvironment.Outdoor)
                Flags |= MapFlags.StationaryGraphics; // 2D outdoor maps always use this
            Environment = environment; // Important to set it after Flags is assigned!
            World = world;
            SongIndex = (uint)(environment == MapEnvironment.Dungeon
                ? Song.SapphireFireballsOfPureLove
                : Song.OwnerOfALonelySword);
            TilesetIndex = world switch
            {
                World.ForestMoon => 8,
                World.Morag => 7,
                _ => 4
            };
            PaletteIndex = world switch
            {
                World.ForestMoon => 10,
                World.Morag => 9,
                _ => 7
            };
            NpcGraphicFileIndex = world == World.ForestMoon ? 2u : 1u;

            Tiles2D = new(width, height);

            MapTile2DData DefaultTileCreator() => new() { BackTileIndex = 0 };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Tiles2D.Set(x, y, DefaultTileCreator());
                }
            }

            foreach (var mapChar in MapCharacters)
                mapChar.SetEmpty();

            LabdataIndex = null;
            Tiles3D = null;
            SkyBackgroundIndex = null;
            GotoPoints = null;
        }

        public void Setup3DMap(MapEnvironment environment, World world, int width, int height,
            uint labdataIndex, uint paletteIndex)
        {
            Type = MapType.Map3D;
            Flags = MapFlags.CanUseMagic | MapFlags.Automapper;
            Environment = environment; // Important to set it after Flags is assigned!
            if (environment == MapEnvironment.Dungeon)
                AlwaysSleepEightHours = true;
            else if (environment == MapEnvironment.Outdoor)
                Flags |= MapFlags.Sky;
            World = world;
            SongIndex = (uint)(environment switch {
                MapEnvironment.Dungeon => Song.MistyDungeonHop,
                MapEnvironment.Outdoor => Song.Capital,
                _ => Song.TheAumRemainsTheSame
            });
            LabdataIndex = labdataIndex;
            SkyBackgroundIndex = environment != MapEnvironment.Outdoor
                ? null
                : (uint)world + 1;
            PaletteIndex = paletteIndex;
            GotoPoints = new DictionaryList<MapGotoPointData>();
            // TODO: change detection

            Tiles3D = new(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Tiles3D.Set(x, y, MapTile3DData.Empty);
                }
            }

            foreach (var mapChar in MapCharacters)
                mapChar.SetEmpty();

            TilesetIndex = null;
            Tiles2D = null;
            NpcGraphicFileIndex = null;
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
            dataWriter.Write((byte)(Type == MapType.Map3D ? (SkyBackgroundIndex ?? 0) : 0));
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
            foreach (var entry in EventEntryList)
                entry.Serialize(dataWriter, advanced);

            // Events
            foreach (var mapEvent in Events)
                mapEvent.Serialize(dataWriter, advanced);

            // Map Character Positions
            foreach (var mapChar in MapCharacters)
            {
                if (mapChar.CharacterType is null)
                    continue;

                if (mapChar.CharacterType == CharacterType.Monster ||
                    mapChar.MovementType != MapCharacterMovementType.Path)
                {
                    mapChar.Position.Serialize(dataWriter, advanced);
                }
                else
                {
                    mapChar.Path!.Serialize(dataWriter, advanced);
                }
            }

            // Goto Points
            foreach (var gotoPoint in GotoPoints ?? Enumerable.Empty<MapGotoPointData>())
                gotoPoint.Serialize(dataWriter, advanced);

            // Event Entry Automap Types
            if (Type == MapType.Map3D)
            {
                foreach (var entry in EventEntryList)
                {
                    dataWriter.Write((byte)entry.AutomapType);
                }
            }

            if (dataWriter.Position % 2 == 1)
                dataWriter.Write(0);
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
            mapData.MapCharacters.ItemChanged += mapData.MapCharactersChanged;

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

                mapData.LabdataIndex = null;
                mapData.Tiles3D = null;
                mapData.SkyBackgroundIndex = null;
            }
            else
            {
                mapData.LabdataIndex = tilesetIndex;
                mapData.SkyBackgroundIndex = skyBackgroundIndex == 0 ? null : skyBackgroundIndex;
                mapData.Tiles3D = new(width, height);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        mapData.Tiles3D.Set(x, y, (MapTile3DData)MapTile3DData.Deserialize(dataReader, advanced));
                    }
                }

                mapData.TilesetIndex = null;
                mapData.Tiles2D = null;
                mapData.NpcGraphicFileIndex = null;
            }

            // Event entry list
            int eventEntryListSize = dataReader.ReadWord();
            var eventEntryList = DataCollection<MapEventEntryData>.Deserialize(dataReader, eventEntryListSize, advanced);
            mapData.EventEntryList = new DictionaryList<MapEventEntryData>(eventEntryList);
            // TODO: change detection

            // Events
            int numberOfEvents = dataReader.ReadWord();
            var events = DataCollection<EventData>.Deserialize(dataReader, numberOfEvents, advanced);
            mapData.Events = new DictionaryList<EventData>(events);
            // TODO: change detection

            // Map Character Positions
            foreach (var mapChar in mapData.MapCharacters)
            {
                if (mapChar.CharacterType == CharacterType.Monster)
                    mapChar.Position = (MapPositionData)MapPositionData.Deserialize(dataReader, advanced);
                else if (mapChar.CharacterType is not null)
                {
                    if (mapChar.MovementType == MapCharacterMovementType.Path)
                    {
                        mapChar.InitPath(DataCollection<MapPositionData>.Deserialize(dataReader, 288, advanced));
                        mapChar.Position = mapChar.Path![0];
                    }
                    else
                    {
                        mapChar.Position = (MapPositionData)MapPositionData.Deserialize(dataReader, advanced);
                    }
                }
            }

            // Goto Points (Fast Travel)
            int numGotoPoints = dataReader.ReadWord(); // Note: This is always present, even for 2D maps.

            if (mapData.Type == MapType.Map2D)
            {
                dataReader.Position += numGotoPoints * GameDataRepository.GotoPointDataSize;
                mapData.GotoPoints = null;
            }
            else
            {
                var gotoPoints = DataCollection<MapGotoPointData>.Deserialize(dataReader, numGotoPoints, advanced);
                mapData.GotoPoints = new DictionaryList<MapGotoPointData>(gotoPoints);
                // TODO: change detection

                // Event Entry Automap Types
                foreach (var entry in mapData.EventEntryList)
                    entry.AutomapType = (AutomapType)dataReader.ReadByte();
            }

            if (dataReader.Position < dataReader.Size && dataReader.Position % 2 == 1)
                dataReader.Position++;

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
            copy.EventEntryList = new(EventEntryList.Select(e => e.Copy()));
            copy.Events = new(Events.Select(e => e.Copy()));
            copy.GotoPoints = GotoPoints is null ? null : new(GotoPoints.Select(e => e.Copy()));

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

        private void MapCharactersChanged(int index)
        {
            OnPropertyChanged(nameof(MapCharacters));
        }

        #endregion

    }
}
