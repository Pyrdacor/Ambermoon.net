using Ambermoon.Data.FileSystems;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using SonicArranger;

namespace Ambermoon.Data.GameDataRepository
{
    using Data;
    using Util;
    using MonsterGroup = List<KeyValuePair<uint, Position>>;

    public class GameDataRepository
    {
        private const ushort DefaultJHKey = 0xd2e7;
        private readonly TextContainer _textContainer = new();

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
            Dictionary<int, IDataReader> ReadFileContainer(string name)
                => fileContainerProvider(name).Files.Where(f => f.Value.Size != 0)
                    .ToDictionary(f => f.Key, f => f.Value);
            Dictionary<int, IDataReader> ReadFileContainers(params string[] names)
                => names.SelectMany(name => fileContainerProvider(name).Files).Where(f => f.Value.Size != 0)
                    .ToDictionary(f => f.Key, f => f.Value);

            #region General Texts
            _textContainer = TextContainer.Load(new TextContainerReader(), ReadFileContainer("Text.amb")[1], false);
            Advanced = _textContainer.VersionString.ToLower().Contains("adv");
            #endregion

            // Maps
            var mapFiles = ReadFileContainers("1Map_data.amb", "2Map_data.amb", "3Map_data.amb");
            Maps = mapFiles.Select(mapFile => (MapData)MapData.Deserialize(mapFile.Value, (uint)mapFile.Key, Advanced)).ToDictionaryList();
            var mapTextFiles = ReadFileContainers("1Map_texts.amb", "2Map_texts.amb", "3Map_texts.amb");
            MapTexts = mapTextFiles.Select(mapTextFile => (TextList<MapData>)TextList<MapData>.Deserialize(mapTextFile.Value, (uint)mapTextFile.Key, Maps[(uint)mapTextFile.Key], Advanced)).ToDictionaryList();

            // Monsters
            var monsterFiles = ReadFileContainer("Monster_char.amb");
            Monsters = monsterFiles.Select(monsterFile => (MonsterData)MonsterData.Deserialize(monsterFile.Value, (uint)monsterFile.Key, Advanced)).ToDictionaryList();
            var monsterGroupFiles = ReadFileContainer("Monster_groups.amb");
            MonsterGroups = monsterGroupFiles.Select(monsterGroupFile => (MonsterGroupData)MonsterGroupData.Deserialize(monsterGroupFile.Value, (uint)monsterGroupFile.Key, Advanced)).ToDictionaryList();

            // TODO ...
        }

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
            var fileSystem = FileSystem.FromOperatingSystemPath(path);
            Save(fileSystem);
        }

        public void SaveAsContainer(string path)
        {
            var fileSystem = FileSystem.CreateVirtual();
            Save(fileSystem);
            using var file = File.Create(path);
            FileSystem.SaveVirtual(file, fileSystem);
        }

        public void Save(IFileSystem fileSystem)
        {
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
                    throw new InvalidOperationException($"Format {Enum.GetName(fileType)} is not supported for writing single files.");
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

        public bool Advanced { get; private set; } = false;
        public DictionaryList<MapData> Maps { get; } = new();
        public DictionaryList<TextList<MapData>> MapTexts { get; } = new();
        //public Dictionary<uint, Labdata> LabyrinthData { get; } = new();
        public DictionaryList<NpcData> Npcs { get; } = new();
        public DictionaryList<TextList<NpcData>> NpcTexts { get; } = new();
        public DictionaryList<PartyMemberData> PartyMembers { get; } = new();
        public DictionaryList<TextList<PartyMemberData>> PartyMemberTexts { get; } = new();
        public DictionaryList<MonsterData> Monsters { get; } = new();
        public DictionaryList<MonsterGroupData> MonsterGroups { get; } = new();
        /*public Dictionary<uint, ImageList> MonsterImages { get; } = new();
        public IReadOnlyDictionary<uint, ImageList<Monster>> ColoredMonsterImages => throw new NotImplementedException();
        public Dictionary<uint, Place> Places { get; } = new();
        public Dictionary<uint, Item> Items { get; } = new();
        public Dictionary<uint, KeyValuePair<string, Song>> Songs { get; } = new();
        public TextList Dictionary { get; } = new();
        public Dictionary<uint, ImageWithPaletteIndex> Portraits { get; } = new();*/
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

        private static IEnumerable<IGrouping<int, T>> GroupMapRelatedEntities<T>(DictionaryList<T> mapRelatedEntities)
            where T : IIndexed
        {
            return mapRelatedEntities.GroupBy(mapRelatedEntity=> mapRelatedEntity.Index switch
            {
                <= 256 => 1,
                >= 300 and < 400 => 3,
                _ => 2
            });
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
    }
}
