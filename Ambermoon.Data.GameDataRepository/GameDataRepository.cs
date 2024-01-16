using Ambermoon.Data.FileSystems;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using SonicArranger;

namespace Ambermoon.Data.GameDataRepository
{
    using Entities;
    using Util;
    using MonsterGroup = List<KeyValuePair<uint, Position>>;
    using TextWriter = Serialization.TextWriter;

    public class GameDataRepository
    {
        private readonly GameData _gameData;

        public GameDataRepository(string path, bool allowPartialData = false)
            : this(LoadGameData(path, allowPartialData))
        {
        }

        public GameDataRepository(IReadOnlyFileSystem fileSystem, bool allowPartialData = false)
            : this(LoadGameData(fileSystem, allowPartialData))
        {
        }

        public GameDataRepository(GameData gameData)
        {
            _gameData = gameData;

            // Maps
            LoadEntitiesWithTexts<Map, MapEntity>(gameData.MapManager.Maps, out var maps, out var mapTexts, map => map.Texts, gameData);
            Maps = maps;
            MapTexts = mapTexts;
            LabyrinthData = gameData.MapManager.Labdata.Select((labdata, index) => new { Labdata = labdata, Index = (uint)index + 1 }).ToDictionary(l => l.Index, l => l.Labdata);

            // Characters
            LoadEntitiesWithTexts<NPC, NpcEntity>(gameData.CharacterManager.NPCs, out var npcs, out var npcTexts, npc => npc.Texts, gameData);
            Npcs = npcs;
            NpcTexts = npcTexts;
            var partyMemberObjects = LoadPartyMembers(gameData);
            LoadEntitiesWithTexts<PartyMember, PartyMemberEntity>(partyMemberObjects, out var partyMembers, out var partyMemberTexts, partyMember => partyMember.Texts, gameData);
            PartyMembers = partyMembers;
            PartyMemberTexts = partyMemberTexts;
            Monsters = gameData.CharacterManager.Monsters.ToDictionary(monster => monster.Index, monster => monster);
            MonsterGroups = gameData.CharacterManager.MonsterGroups.ToDictionary(group => group.Key, group => ConvertMonsterGroup(group.Value));
            
            // ...
            Items = gameData.ItemManager.Items.Select((item, index) => new { Item = item, Index = (uint)index + 1 }).ToDictionary(item => item.Index, item => item.Item);
            Places = gameData.Places.Entries.Select((place, index) => new { Place = place, Index = (uint)index + 1 }).ToDictionary(item => item.Index, item => item.Place);
            Songs = LoadSongs(gameData);
            Portraits = gameData.GraphicProvider.GetGraphics(GraphicType.Portrait)
                .Select((gfx, index) => new { Graphic = gfx, Index = (uint)index + 1 })
                .ToDictionary(g => g.Index, g => new ImageWithPaletteIndex(gameData.GraphicProvider.PrimaryUIPaletteIndex, g.Graphic));
            Dictionary = new TextList(gameData.Dictionary.Entries);
            // to be continued ...
        }

        public static GameDataRepository FromContainer(string containerPath, bool allowPartialData = false)
        {
            using var file = File.OpenRead(containerPath);
            var container = FileSystem.LoadVirtual(file, false);

            return new GameDataRepository(container.AsReadOnly(), allowPartialData);
        }

        private static GameData LoadGameData(string path, bool allowPartialData)
        {
            var gameData = new GameData(GameData.LoadPreference.PreferExtracted, null, !allowPartialData);
            gameData.Load(path);
            return gameData;
        }

        private static GameData LoadGameData(IReadOnlyFileSystem fileSystem, bool allowPartialData)
        {
            var gameData = new GameData(GameData.LoadPreference.PreferExtracted, null, !allowPartialData);
            gameData.LoadFromFileSystem(fileSystem);
            return gameData;
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
            void WriteContainer(string containerName, FileType fileType, Dictionary<uint, byte[]> files)
            {
                var writer = new DataWriter();
                FileWriter.WriteContainer(writer, files, fileType);
                fileSystem.CreateFile(containerName, writer.ToArray());
            }

            #region Maps
            /*var mapGroups = GroupMapRelatedEntities(Maps);
            foreach (var mapGroup in mapGroups)
            {
                var containerName = $"{mapGroup.Key}Map_data.amb";
                WriteContainer(containerName, FileType.AMPC, mapGroup.ToDictionary(m => m.Index, m => SerializeEntity(m, _gameData)));
            }*/
            var mapTextGroups = GroupMapRelatedEntities(MapTexts);
            foreach (var mapTextGroup in mapTextGroups)
            {
                var containerName = $"{mapTextGroup.Key}Map_text.amb";
                WriteContainer(containerName, FileType.AMNP, mapTextGroup.ToDictionary(t => t.Index, SerializeTextList));
            }
            // ...
            #endregion

            #region Characters
            //WriteContainer("NPC_char.amb", FileType.AMPC, Npcs.ToDictionary(m => m.Index, m => SerializeEntity(m, _gameData)));
            WriteContainer("NPC_texts.amb", FileType.AMNP, NpcTexts.ToDictionary(t => t.Index, SerializeTextList));
            //var partyMemberFiles = PartyMembers.ToDictionary(m => m.Index, m => SerializeEntity(m, _gameData));
            //WriteContainer("Save.00/Party_char.amb", FileType.AMBR, partyMemberFiles);
            //WriteContainer("Initial/Party_char.amb", FileType.AMBR, partyMemberFiles);
            WriteContainer("Party_texts.amb", FileType.AMNP, PartyMemberTexts.ToDictionary(t => t.Index, SerializeTextList));
            #endregion
        }

        private static void LoadEntitiesWithTexts<TGameObject, TEntity>(IEnumerable<TGameObject> gameObjects,
            out DictionaryList<TEntity> entities, out DictionaryList<TextList<TEntity>> textEntities,
            Func<TGameObject, IEnumerable<string>> textProvider, IGameData gameData)
            where TEntity : class, IIndexedEntity<TGameObject>, new()
        {
            var entitiesWithTexts = gameObjects.Select(gameObject =>
            {
                TEntity entity = new();
                entity.Index = (uint)gameObject.GetType().GetProperty("Index").GetValue(gameObject)!;
                // TODO
                //var entity = gameObject.ToEntity<TGameObject, TEntity>(gameData);

                return new
                {
                    Entity = entity,
                    Texts = new TextList<TEntity>(entity, textProvider(gameObject))
                };
            });

            entities = entitiesWithTexts.Select(e => e.Entity).ToDictionaryList();
            textEntities = entitiesWithTexts.Select(e => e.Texts).ToDictionaryList();
        }

        private static IEnumerable<PartyMember> LoadPartyMembers(GameData gameData)
        {
            var partyMemberContainer = gameData.Files.TryGetValue("Initial/Party_char.amb", out var container) ? container : gameData.Files["Save.00/Party_char.amb"];
            var partyTextFiles = gameData.Files["Party_texts.amb"].Files;
            var partyMemberReader = new PartyMemberReader();

            PartyMember ReadPartyMember(int index, IDataReader dataReader)
            {
                var partyTextReader = partyTextFiles.TryGetValue(index, out var textReader) && textReader.Size != 0 ? textReader : null;
                return PartyMember.Load((uint)index, partyMemberReader, dataReader, partyTextReader);
            }

            return partyMemberContainer.Files.Select(file => ReadPartyMember(file.Key, file.Value));
        }

        private static MonsterGroup ConvertMonsterGroup(Data.MonsterGroup monsterGroup)
        {
            var group = new MonsterGroup(18); // max 18

            // 6x3
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    var monster = monsterGroup.Monsters[x, y];

                    if (monster != null)
                        group.Add(KeyValuePair.Create(monster.Index, new Position(x, y)));
                }
            }

            return group;
        }

        private static Dictionary<uint, KeyValuePair<string, Song>> LoadSongs(GameData gameData)
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
        }

        public DictionaryList<MapEntity> Maps { get; } = new();
        public DictionaryList<TextList<MapEntity>> MapTexts { get; } = new();
        public Dictionary<uint, Labdata> LabyrinthData { get; } = new();
        public DictionaryList<NpcEntity> Npcs { get; } = new();
        public DictionaryList<TextList<NpcEntity>> NpcTexts { get; } = new();
        public DictionaryList<PartyMemberEntity> PartyMembers { get; } = new();
        public DictionaryList<TextList<PartyMemberEntity>> PartyMemberTexts { get; } = new();
        public Dictionary<uint, Monster> Monsters { get; } = new();
        public Dictionary<uint, MonsterGroup> MonsterGroups { get; } = new();
        public Dictionary<uint, ImageList> MonsterImages { get; } = new();
        public IReadOnlyDictionary<uint, ImageList<Monster>> ColoredMonsterImages => throw new NotImplementedException();
        public Dictionary<uint, Place> Places { get; } = new();
        public Dictionary<uint, Item> Items { get; } = new();
        public Dictionary<uint, KeyValuePair<string, Song>> Songs { get; } = new();
        public TextList Dictionary { get; } = new();
        public Dictionary<uint, ImageWithPaletteIndex> Portraits { get; } = new();

        private static IEnumerable<IGrouping<int, T>> GroupMapRelatedEntities<T>(DictionaryList<T> mapRelatedEntities)
            where T : IIndexedEntity
        {
            return mapRelatedEntities.GroupBy(mapRelatedEntity=> mapRelatedEntity.Index switch
            {
                <= 256 => 1,
                >= 300 and < 400 => 3,
                _ => 2
            });
        }

        private static byte[] SerializeEntity<T>(T entity, IGameData gameData) where T : IEntity
        {
            var writer = new DataWriter();
            entity.Serialize(writer, gameData);
            return writer.ToArray();
        }

        private static byte[] SerializeTextList(TextList textList)
        {
            var writer = new DataWriter();
            TextWriter.WriteTexts(writer, textList);
            return writer.ToArray();
        }
    }
}
