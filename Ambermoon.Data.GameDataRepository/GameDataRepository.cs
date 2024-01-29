using Ambermoon.Data.FileSystems;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using SonicArranger;

namespace Ambermoon.Data.GameDataRepository
{
    using Collections;
    using Data;

    public class GameDataRepository : IDisposable
    {

        #region Constants

        private const ushort DefaultJHKey = 0xd2e7;
        // Map
        public const uint MaxMaps = 1023;
        public const uint MaxMapWidth = 100;
        public const uint MaxMapHeight = 100;
        public const int MaxMapCharacters = 32;
        public const int MinObject3DIndex = 1;
        public const int MaxObject3DIndex = 100;
        public const int MinWall3DIndex = 1;
        public const int MaxWall3DIndex = 154;
        public const int MaxSubObjects3D = 8;
        // Events
        public const uint MaxEvents = (uint)short.MaxValue;
        public const uint MaxDoors = 256;
        public const uint MaxChests = 256;
        public const uint MaxExtendedChest = 128;
        // Items
        public const uint MaxItems = 1023;
        // Fixed Sizes
        public const int EventDataSize = 12;
        public const int GotoPointDataSize = 20;
        public const int ItemDataSize = 60;
        public const int Object3DDataSize = 66;
        public const int SubObject3DDataSize = 8;
        public const int ObjectInfo3DDataSize = 14;
        public const int Overlay3DDataSize = 6;
        public const int Wall3DHeaderDataSize = 8;
        // Palettes and Images
        internal const int PaletteSize = 32;
        internal const uint PortraitMaskColorIndex = 25;
        internal const uint SkyBackgroundMaskColorIndex = 9;
        public const uint CombatBackgroundCount = 16;

        #endregion


        #region Fields

        private readonly TextContainer _textContainer;
        private bool disposed;
        private static readonly List<GameDataRepository> OpenRepositories = new();

        #endregion


        #region Properties


        #region Misc

        public bool Advanced { get; private set; } = false;
        
        #endregion


        #region Palettes
        
        public DictionaryList<Palette> Palettes { get; }
        /// <summary>
        /// Used for UI elements, items, portraits, etc.
        /// </summary>
        public Palette UserInterfacePalette { get; }
        public Palette DungeonMapPalette { get; }
        public Palette SecondaryUserInterfacePalette { get; }
        /// <summary>
        /// Technically it is the same as the <see cref="UserInterfacePalette"/>
        /// but portraits use color index 25 (purple) as the transparency indicator.
        /// Most likely as they need a black color which is normally color index 0.
        /// The repository loads the portraits in a way that it swaps the color
        /// indices 0 and 25 so that the transparent areas are represented by color
        /// index 0 (as for all other images). But it also relies on the fact that
        /// color index 25 is then black. For that reason the portrait palette is
        /// provided which just is the normal UI palette with color 25 set to black.
        /// </summary>
        public Palette PortraitPalette { get; }
        /// <summary>
        /// Palette for item graphics. It is the same as <see cref="UserInterfacePalette"/>.
        /// </summary>
        public Palette ItemPalette => UserInterfacePalette;

        #endregion


        #region Maps

        public DictionaryList<MapData> Maps { get; }
        public DictionaryList<TextList<MapData>> MapTexts { get; }
        public DictionaryList<LabyrinthData> Labyrinths { get; }
        public DictionaryList<ImageList> Tile2DImages { get; }
        public DictionaryList<Image> Wall3DImages { get; }
        public DictionaryList<Image> Object3DImages { get; }
        public DictionaryList<Image> Overlay3DImages { get; }
        public DictionaryList<Image> Floor3DImages { get; }

        #endregion


        #region NPCs & Party Members

        public DictionaryList<NpcData> Npcs { get; } = new();
        public DictionaryList<TextList<NpcData>> NpcTexts { get; } = new();
        public DictionaryList<PartyMemberData> PartyMembers { get; } = new();
        public DictionaryList<TextList<PartyMemberData>> PartyMemberTexts { get; } = new();
        public DictionaryList<ImageWithPaletteIndex> Portraits { get; }
        /// <summary>
        /// Small battlefield icons for the party.
        /// The key is the class of the party member.
        /// </summary>
        public DictionaryList<Image> PartyCombatIcons { get; }

        #endregion


        #region Monsters & Combat

        public DictionaryList<MonsterData> Monsters { get; }
        public DictionaryList<MonsterGroupData> MonsterGroups { get; }
        public DictionaryList<Image> MonsterImages { get; }
        public DictionaryList<Image> MonsterCombatIcons { get; }
        public CombatBackgroundImage[] CombatBackgroundImages2D { get; }
        public CombatBackgroundImage[] CombatBackgroundImages3D { get; }
        public List<CombatBackgroundImage> DistinctCombatBackgroundImages { get; }

        #endregion


        #region Items & Places

        //public Dictionary<uint, Place> Places { get; } = new();
        public DictionaryList<ItemData> Items { get; }
        public DictionaryList<Image> ItemImages { get; }

        #endregion


        #region Songs

        //public Dictionary<uint, KeyValuePair<string, Song>> Songs { get; } = new();

        #endregion


        #region Texts

        //public TextList Dictionary { get; } = new();
        public List<string> WorldNames => _textContainer.WorldNames;
        public List<string> FormatMessages => _textContainer.FormatMessages;
        public List<string> Messages => _textContainer.Messages;
        public List<string> AutomapTypeNames => _textContainer.AutomapTypeNames;
        public List<string> OptionNames => _textContainer.OptionNames;
        public List<string> SongNames => _textContainer.MusicNames;
        public List<string> SpellClassNames => _textContainer.SpellClassNames;
        public List<string> SpellNames => _textContainer.SpellNames;
        public List<string> LanguageNames => _textContainer.LanguageNames;
        public List<string> ClassNames => _textContainer.ClassNames;
        public List<string> RaceNames => _textContainer.RaceNames;
        public List<string> SkillNames => _textContainer.SkillNames;
        public List<string> AttributeNames => _textContainer.AttributeNames;
        public List<string> SkillShortNames => _textContainer.SkillShortNames;
        public List<string> AttributeShortNames => _textContainer.AttributeShortNames;
        public List<string> ItemTypeNames => _textContainer.ItemTypeNames;
        public List<string> ConditionNames => _textContainer.ConditionNames;
        public List<string> UITexts => _textContainer.UITexts;
        public List<int> UITextWithPlaceholderIndices => _textContainer.UITextWithPlaceholderIndices;
        public string VersionString
        {
            get => _textContainer.VersionString;
            set => _textContainer.VersionString = value;
        }
        public string DateAndLanguageString
        {
            get => _textContainer.DateAndLanguageString;
            set => _textContainer.DateAndLanguageString = value;
        }

        #endregion


        #endregion


        #region Constructors

        public GameDataRepository(string path)
            : this(FileContainerFromPath(path))
        {
        }

        public GameDataRepository(IReadOnlyFileSystem fileSystem)
            : this(FileContainerFromFileSystem(fileSystem))
        {

        }

        private GameDataRepository(Func<string, IFileContainer> fileContainerProvider)
        {

            #region File Container Helpers

            Dictionary<int, IDataReader> ReadFileContainer(string name)
                => fileContainerProvider(name).Files.Where(f => f.Value.Size != 0)
                    .ToDictionary(f => f.Key, f => f.Value);
            Dictionary<int, IDataReader> ReadFileContainers(params string[] names)
                => names.SelectMany(name => fileContainerProvider(name).Files).Where(f => f.Value.Size != 0)
                    .DistinctBy(f => f.Key)
                    .ToDictionary(f => f.Key, f => f.Value);

            #endregion


            #region Misc & Shared Data

            _textContainer = TextContainer.Load(new TextContainerReader(), ReadFileContainer("Text.amb")[1], false);
            Advanced = _textContainer.VersionString.ToLower().Contains("adv");
            var combatGraphicFiles = ReadFileContainer("Combat_graphics");
            var combatGraphics = CombatGraphicData.Deserialize(combatGraphicFiles[1]);

            #endregion


            #region Palettes

            var paletteFiles = ReadFileContainer("Palettes.amb");
            Palettes = paletteFiles.Select(paletteFile => Palette.Deserialize((uint)paletteFile.Key, paletteFile.Value)).ToDictionaryList();
            var builtinPalettesData = new DataReader(StaticData.BuiltinPalettes);
            UserInterfacePalette = Palette.Deserialize(1000, builtinPalettesData);
            DungeonMapPalette = Palette.Deserialize(1001, builtinPalettesData);
            SecondaryUserInterfacePalette = Palette.Deserialize(1002, builtinPalettesData);
            PortraitPalette = UserInterfacePalette.WithColorReplacement(PortraitMaskColorIndex, 0, 0, 0);

            #endregion


            #region Maps

            var mapFiles = ReadFileContainers("1Map_data.amb", "2Map_data.amb", "3Map_data.amb");
            Maps = mapFiles.Select(mapFile => (MapData)MapData.Deserialize(mapFile.Value, (uint)mapFile.Key, Advanced)).ToDictionaryList();
            var mapTextFiles = ReadFileContainers("1Map_texts.amb", "2Map_texts.amb", "3Map_texts.amb");
            MapTexts = mapTextFiles.Select(mapTextFile => (TextList<MapData>)TextList<MapData>.Deserialize(mapTextFile.Value, (uint)mapTextFile.Key, Maps[(uint)mapTextFile.Key], Advanced)).ToDictionaryList();
            var tile2DImageFiles = ReadFileContainers("1Icon_gfx.amb", "2Icon_gfx.amb", "3Icon_gfx.amb");
            Tile2DImages = tile2DImageFiles.Select(tile2DImageFile => ImageList.Deserialize((uint)tile2DImageFile.Key, tile2DImageFile.Value, 16, 16, GraphicFormat.Palette5Bit)).ToDictionaryList();
            var labdataFiles = ReadFileContainers("2Lab_data.amb", "3Lab_data.amb");
            Labyrinths = labdataFiles.Select(labdataFile => (LabyrinthData)LabyrinthData.Deserialize(labdataFile.Value, (uint)labdataFile.Key, Advanced)).ToDictionaryList();
            var wall3DImageFiles = ReadFileContainers("2Wall3D.amb", "3Wall3D.amb");
            Wall3DImages = wall3DImageFiles.Select(wall3DImageFile => Image.Deserialize((uint)wall3DImageFile.Key, wall3DImageFile.Value, 1, 128, 80, GraphicFormat.Texture4Bit)).ToDictionaryList();
            var object3DImageFiles = ReadFileContainers("2Object3D.amb", "3Object3D.amb");
            static Image Load3DObjectImage(uint index, IDataReader dataReader)
            {
                var info = //index < TextureGraphicInfos.ObjectGraphicFrameCountsAndSizes.Length
                    /*? */TextureGraphicInfos.ObjectGraphicFrameCountsAndSizes[index - 1]
                    /*: 0*/; // TODO
                return Image.Deserialize(index, dataReader, info.Key, info.Value.Width, info.Value.Height, GraphicFormat.Texture4Bit);
            }
            Object3DImages = object3DImageFiles.Select(object3DImageFile => Load3DObjectImage((uint)object3DImageFile.Key, object3DImageFile.Value)).ToDictionaryList();
            var overlay3DImageFiles = ReadFileContainers("2Overlay3D.amb", "3Overlay3D.amb");
            static Image Load3DOverlayImage(uint index, IDataReader dataReader)
            {
                var size = TextureGraphicInfos.OverlayGraphicSizes[index - 1];
                return Image.Deserialize(index, dataReader, 1, size.Width, size.Height, GraphicFormat.Texture4Bit);
            }
            Overlay3DImages = overlay3DImageFiles.Select(overlay3DImageFile => Load3DOverlayImage((uint)overlay3DImageFile.Key, overlay3DImageFile.Value)).ToDictionaryList();
            var floor3DImageFiles = ReadFileContainer("Floors.amb");
            Floor3DImages = floor3DImageFiles.Select(floor3DImageFile => Image.Deserialize((uint)floor3DImageFile.Key, floor3DImageFile.Value, 1, 64, 64, GraphicFormat.Palette4Bit)).ToDictionaryList();

            #endregion


            #region NPCs & Party Members

            /*var npcFiles = ReadFileContainer("NPC_char.amb");
            Npcs = npcFiles.Select(npcFile => (NpcData)NpcData.Deserialize(npcFile.Value, (uint)npcFile.Key, Advanced)).ToDictionaryList();
            var npcTextFiles = ReadFileContainer("NPC_texts.amb");
            NpcTexts = npcTextFiles.Select(npcTextFile => (TextList<NpcData>)TextList<NpcData>.Deserialize(npcTextFile.Value, (uint)npcTextFile.Key, Npcs[(uint)npcTextFile.Key], Advanced)).ToDictionaryList();
            var partyMemberFiles = ReadFileContainer("Save.00/Party_char.amb"); // TODO: Fallback to Initial/Party_char.amb
            PartyMembers = partyMemberFiles.Select(partyMemberFile => (PartyMemberData)PartyMemberData.Deserialize(partyMemberFile.Value, (uint)partyMemberFile.Key, Advanced)).ToDictionaryList();
            var partyMemberTextFiles = ReadFileContainer("Party_texts.amb");
            PartyMemberTexts = partyMemberTextFiles.Select(partyMemberTextFile => (TextList<PartyMemberData>)TextList<PartyMemberData>.Deserialize(partyMemberTextFile.Value, (uint)partyMemberTextFile.Key, PartyMembers[(uint)partyMemberTextFile.Key], Advanced)).ToDictionaryList();
            */
            PartyCombatIcons = combatGraphics.BattleFieldIcons.Take((int)Class.Monster).ToDictionaryList((_, i) => (uint)i);
            var portraitFiles = ReadFileContainer("Portraits.amb");
            Portraits = portraitFiles.Select(portraitFile =>
                ImageWithPaletteIndex.Deserialize((uint)portraitFile.Key, UserInterfacePalette.Index, portraitFile.Value, 1, 32, 34, GraphicFormat.Palette5Bit)
                    .WithColorReplacements(new ImageColorReplacement(0, PortraitMaskColorIndex), new ImageColorReplacement(PortraitMaskColorIndex, 0))).ToDictionaryList();

            #endregion


            #region Monsters & Combat

            var monsterFiles = ReadFileContainer("Monster_char.amb");
            Monsters = monsterFiles.Select(monsterFile => (MonsterData)MonsterData.Deserialize(monsterFile.Value, (uint)monsterFile.Key, Advanced)).ToDictionaryList();
            var monsterGroupFiles = ReadFileContainer("Monster_groups.amb");
            MonsterGroups = monsterGroupFiles.Select(monsterGroupFile => (MonsterGroupData)MonsterGroupData.Deserialize(monsterGroupFile.Value, (uint)monsterGroupFile.Key, Advanced)).ToDictionaryList();
            var monsterGraphicFiles = ReadFileContainer("Monster_gfx.amb");
            var monsterGraphicInfos = Monsters.Select(monster =>
                Tuple.Create(monster.OriginalFrameWidth, monster.OriginalFrameHeight, monster.CombatGraphicIndex)).Distinct();
            MonsterImages = monsterGraphicInfos.Select(info =>
                Image.DeserializeFullData(info.Item3, monsterGraphicFiles[(int)info.Item3], (int)info.Item1, (int)info.Item2, GraphicFormat.Palette5Bit, true)).ToDictionaryList();
            MonsterCombatIcons = combatGraphics.BattleFieldIcons.Skip((int)Class.Monster).ToDictionaryList((_, i) => (uint)i + 1);
            var combatBackgroundFiles = ReadFileContainers("Combat_background.amb");
            var combatBackgrounds = combatBackgroundFiles.Select(combatBackgroundFile =>
                CombatBackgroundImage.DeserializeImage((uint)combatBackgroundFile.Key, combatBackgroundFile.Value)).ToDictionaryList();
            CombatBackgroundImages2D = CombatBackgrounds.Info2D.Select((info, index) => new CombatBackgroundImage((uint)index, info.Palettes, combatBackgrounds[info.GraphicIndex].Frames[0])).ToArray();
            CombatBackgroundImages3D = CombatBackgrounds.Info3D.Select((info, index) => new CombatBackgroundImage((uint)index, info.Palettes, combatBackgrounds[info.GraphicIndex].Frames[0])).ToArray();
            DistinctCombatBackgroundImages = CombatBackgrounds.Info2D.Concat(CombatBackgrounds.Info3D)
                .DistinctBy(info => info.GraphicIndex)
                .Select((info, index) => new CombatBackgroundImage((uint)index, info.Palettes, combatBackgrounds[info.GraphicIndex].Frames[0]))
                .ToList();
            #endregion


            #region Items

            var itemFile = ReadFileContainer("Objects.amb")[1];
            int itemCount = itemFile.ReadWord();
            Items = DataCollection<ItemData>.Deserialize(itemFile, itemCount, Advanced).ToDictionaryList();
            var itemGraphicFiles = ReadFileContainer("Object_icons");
            ItemImages = ImageList.Deserialize(0, itemGraphicFiles[1], 16, 16, GraphicFormat.Palette5Bit)
                .ToDictionaryList();

            #endregion

            // TODO ...

            OpenRepositories.Add(this);
        }

        #endregion


        #region Serialization

        private static Func<string, IFileContainer> FileContainerFromPath(string rootPath)
        {
            return containerPath => new FileReader().ReadRawFile(containerPath, File.ReadAllBytes(Path.Combine(rootPath, containerPath)));
        }

        private static Func<string, IFileContainer> FileContainerFromFileSystem(IReadOnlyFileSystem fileSystem)
        {
            return containerPath => new FileReader().ReadFile(containerPath, fileSystem.GetFileReader(containerPath).GetReader());
        }

        public static GameDataRepository FromContainer(string containerPath)
        {
            using var file = File.OpenRead(containerPath);
            var container = FileSystem.LoadVirtual(file, false);

            return new GameDataRepository(container.AsReadOnly());
        }

        public void Save(string path)
        {
            EnsureNotDisposed();

            var fileSystem = FileSystem.FromOperatingSystemPath(path);
            Save(fileSystem);
        }

        public void SaveAsContainer(string path)
        {
            EnsureNotDisposed();

            var fileSystem = FileSystem.CreateVirtual();
            Save(fileSystem);
            using var file = File.Create(path);
            FileSystem.SaveVirtual(file, fileSystem);
        }

        public void Save(IFileSystem fileSystem)
        {
            EnsureNotDisposed();

            void WriteSingleFileContainer(string containerName, FileType fileType, IDataWriter dataWriter)
            {
                var writer = new DataWriter();
                if (fileType == FileType.LOB)
                    FileWriter.WriteLob(writer, dataWriter.ToArray(), Legacy.Compression.LobCompression.LobType.Ambermoon);
                else if (fileType == FileType.JH)
                    FileWriter.WriteJH(writer, dataWriter.ToArray(), DefaultJHKey, false);
                else if (fileType == FileType.JHPlusLOB)
                    FileWriter.WriteJH(writer, dataWriter.ToArray(), DefaultJHKey, true);
                else
                    throw new InvalidOperationException($"Format {EnumHelper.GetName(fileType)} is not supported for writing single files.");
            }
            void WriteContainer(string containerName, FileType fileType, Dictionary<uint, byte[]> files)
            {
                var writer = new DataWriter();
                FileWriter.WriteContainer(writer, files, fileType);
                fileSystem.CreateFile(containerName, writer.ToArray());
            }

            #region General Texts
            var textContainerWriter = new DataWriter();
            new TextContainerWriter().WriteTextContainer(_textContainer, textContainerWriter, false);
            WriteSingleFileContainer("Text.amb", FileType.JHPlusLOB, textContainerWriter);
            #endregion

            #region Maps
            var mapGroups = GroupMapRelatedEntities(Maps);
            foreach (var mapGroup in mapGroups)
            {
                var containerName = $"{mapGroup.Key}Map_data.amb";
                WriteContainer(containerName, FileType.AMPC, mapGroup.ToDictionary(m => m.Index, m => SerializeEntity(m, Advanced)));
            }
            var mapTextGroups = GroupMapRelatedEntities(MapTexts);
            foreach (var mapTextGroup in mapTextGroups)
            {
                var containerName = $"{mapTextGroup.Key}Map_text.amb";
                WriteContainer(containerName, FileType.AMNP, mapTextGroup.ToDictionary(t => t.Index, SerializeTextList));
            }
            // ...
            #endregion

            #region Monsters
            WriteContainer("Monster_char.amb", FileType.AMPC, Monsters.ToDictionary(m => m.Index, m => SerializeEntity(m, Advanced)));
            WriteContainer("Monster_groups.amb", FileType.AMPC, MonsterGroups.ToDictionary(m => m.Index, m => SerializeEntity(m, Advanced)));
            #endregion
        }

        private static byte[] SerializeEntity<T>(T entity, bool advanced) where T : IData
        {
            var writer = new DataWriter();
            entity.Serialize(writer, advanced);
            return writer.ToArray();
        }

        private static byte[] SerializeDependentEntity<T, D>(T entity, bool advanced)
            where T : IDependentData<D>
            where D : IData
        {
            var writer = new DataWriter();
            entity.Serialize(writer, advanced);
            return writer.ToArray();
        }

        private static byte[] SerializeTextList(TextList textList)
        {
            var writer = new DataWriter();
            Legacy.Serialization.TextWriter.WriteTexts(writer, textList);
            return writer.ToArray();
        }

        #endregion


        /*private static Dictionary<uint, KeyValuePair<string, Song>> LoadSongs(GameData gameData)
        {
            var introContainer = gameData.Files["Intro_music"];
            var extroContainer = gameData.Files["Extro_music"];
            var musicContainer = gameData.Files["Music.amb"];
            var songs = new Dictionary<uint, KeyValuePair<string, Song>>(3 + musicContainer.Files.Count);

            static SonicArrangerFile LoadSong(IDataReader dataReader)
            {
                dataReader.Position = 0;
                return new SonicArrangerFile(dataReader as DataReader);
            }

            void AddSong(Enumerations.Song song, Song songData)
            {
                songs.Add((uint)song, KeyValuePair.Create(gameData.DataNameProvider.GetSongName(song), songData));
            }

            foreach (var file in musicContainer.Files)
            {
                AddSong((Enumerations.Song)file.Key, LoadSong(file.Value).Songs[0]);
            }

            var introSongFile = LoadSong(introContainer.Files[1]);
            AddSong(Enumerations.Song.Intro, introSongFile.Songs[0]);
            AddSong(Enumerations.Song.Menu, introSongFile.Songs[1]);
            AddSong(Enumerations.Song.Outro, LoadSong(extroContainer.Files[1]).Songs[0]);

            return songs;
        }*/


        #region Methods

        private static IEnumerable<IGrouping<int, T>> GroupMapRelatedEntities<T>(DictionaryList<T> mapRelatedEntities)
            where T : IIndexed, new()
        {
            return mapRelatedEntities.GroupBy(mapRelatedEntity=> mapRelatedEntity.Index switch
            {
                <= 256 => 1,
                >= 300 and < 400 => 3,
                _ => 2
            });
        }

        internal static IEnumerable<GameDataRepository> GetOpenRepositories() => OpenRepositories;

        #endregion


        #region Disposing

        private void EnsureNotDisposed()
        {
            if (disposed)
                throw new InvalidOperationException("Game data repository was already disposed.");
        }

        public void Close() => Dispose();

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            OpenRepositories.Remove(this);
            GC.SuppressFinalize(this);
        }

        internal static void CloseAll()
        {
            foreach (var repository in OpenRepositories)
                repository.Close();
        }

        #endregion

    }
}
