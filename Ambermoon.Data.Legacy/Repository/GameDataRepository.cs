using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.FileSystem;
using SonicArranger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Repository
{
    using MonsterGroup = List<KeyValuePair<uint, Position>>;
    using TextWriter = TextWriter;

    public class GameDataRepository
    {
        public GameDataRepository()
        {
        }

        public GameDataRepository(GameData gameData)
        {
            Maps = gameData.MapManager.Maps.ToDictionary(map => map.Index, map => map);
            MapTexts = Maps.ToDictionary(map => map.Key, map => new TextList<Map>(map.Value, map.Value.Texts));
            LabyrinthData = gameData.MapManager.Labdata.Select((labdata, index) => new { Labdata = labdata, Index = (uint)index + 1 }).ToDictionary(l => l.Index, l => l.Labdata);
            Npcs = gameData.CharacterManager.NPCs.ToDictionary(npc => npc.Index, npc => npc);
            NpcTexts = Npcs.ToDictionary(npc => npc.Key, npc => new TextList<NPC>(npc.Value, npc.Value.Texts));
            PartyMembers = LoadPartyMembers(gameData);
            PartyMemberTexts = PartyMembers.ToDictionary(partyMember => partyMember.Key, partyMember => new TextList<PartyMember>(partyMember.Value, partyMember.Value.Texts));
            Monsters = gameData.CharacterManager.Monsters.ToDictionary(monster => monster.Index, monster => monster);
            MonsterGroups = gameData.CharacterManager.MonsterGroups.ToDictionary(group => group.Key, group => ConvertMonsterGroup(group.Value));
            Items = gameData.ItemManager.Items.Select((item, index) => new { Item = item, Index = (uint)index + 1 }).ToDictionary(item => item.Index, item => item.Item);
            Places = gameData.Places.Entries.Select((place, index) => new { Place = place, Index = (uint)index + 1 }).ToDictionary(item => item.Index, item => item.Place);
            Songs = LoadSongs(gameData);
            Portraits = gameData.GraphicProvider.GetGraphics(GraphicType.Portrait)
                .Select((gfx, index) => new { Graphic = gfx, Index = (uint)index + 1 })
                .ToDictionary(g => g.Index, g => new ImageWithPaletteIndex(gameData.GraphicProvider.PrimaryUIPaletteIndex, g.Graphic));
            Dictionary = new TextList(gameData.Dictionary.Entries);
            // to be continued ...
        }

        private static Dictionary<uint, PartyMember> LoadPartyMembers(GameData gameData)
        {
            var partyMemberContainer = gameData.Files.TryGetValue("Initial/Party_char.amb", out var container) ? container : gameData.Files["Save.00/Party_char.amb"];
            var partyTextFiles = gameData.Files["Party_texts.amb"].Files;
            var partyMemberReader = new PartyMemberReader();

            PartyMember ReadPartyMember(int index, IDataReader dataReader)
            {
                var partyTextReader = partyTextFiles.TryGetValue(index, out var textReader) && textReader.Size != 0 ? textReader : null;
                return PartyMember.Load((uint)index, partyMemberReader, dataReader, partyTextReader);
            }

            return partyMemberContainer.Files.ToDictionary(file => (uint)file.Key, file => ReadPartyMember(file.Key, file.Value));
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

        private static Dictionary<uint, KeyValuePair<string, SonicArranger.Song>> LoadSongs(GameData gameData)
        {
            var introContainer = gameData.Files["Intro_music"];
            var extroContainer = gameData.Files["Extro_music"];
            var musicContainer = gameData.Files["Music.amb"];
            var songs = new Dictionary<uint, KeyValuePair<string, SonicArranger.Song>>(3 + musicContainer.Files.Count);

            void AddSong(Enumerations.Song song, SonicArranger.Song songData)
            {
                songs.Add((uint)song, KeyValuePair.Create(gameData.DataNameProvider.GetSongName(song), songData));
            }

            foreach (var file in musicContainer.Files)
            {
                AddSong((Enumerations.Song)file.Key, new SonicArrangerFile(file.Value as DataReader).Songs[0]);
            }

            var introSongFile = new SonicArrangerFile(introContainer.Files[1] as DataReader);
            AddSong(Enumerations.Song.Intro, introSongFile.Songs[0]);
            AddSong(Enumerations.Song.Menu, introSongFile.Songs[1]);
            AddSong(Enumerations.Song.Outro, new SonicArrangerFile(extroContainer.Files[1] as DataReader).Songs[0]);

            return songs;
        }

        public Dictionary<uint, Map> Maps { get; }
        public Dictionary<uint, TextList<Map>> MapTexts { get; }
        public Dictionary<uint, Labdata> LabyrinthData { get; }
        public Dictionary<uint, NPC> Npcs { get; }
        public Dictionary<uint, TextList<NPC>> NpcTexts { get; }        
        public Dictionary<uint, PartyMember> PartyMembers { get; }
        public Dictionary<uint, TextList<PartyMember>> PartyMemberTexts { get; }
        public Dictionary<uint, Monster> Monsters { get; }
        public Dictionary<uint, MonsterGroup> MonsterGroups { get; }
        public Dictionary<uint, ImageList> MonsterImages { get; }
        public IReadOnlyDictionary<uint, ImageList<Monster>> ColoredMonsterImages { get; }
        public Dictionary<uint, Place> Places { get; }
        public Dictionary<uint, Item> Items { get; }
        public Dictionary<uint, KeyValuePair<string, SonicArranger.Song>> Songs { get; }
        public TextList Dictionary { get; }
        public Dictionary<uint, ImageWithPaletteIndex> Portraits { get; }

        private static IEnumerable<IGrouping<int, KeyValuePair<uint, T>>> GroupMapRelatedEntities<T>(Dictionary<uint, T> mapRelatedEntities)
        {
            return mapRelatedEntities.GroupBy(mapRelatedEntity=> mapRelatedEntity.Key switch
            {
                <= 256 => 1,
                >= 300 and < 400 => 3,
                _ => 2
            });
        }

        private static byte[] SerializeMap(Map map)
        {
            var writer = new DataWriter();
            MapWriter.WriteMap(map, writer);
            return writer.ToArray();
        }

        private static byte[] SerializeTextList(TextList textList)
        {
            var writer = new DataWriter();
            TextWriter.WriteTexts(writer, textList);
            return writer.ToArray();
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
            var mapGroups = GroupMapRelatedEntities(Maps);
            foreach (var mapGroup in mapGroups)
            {
                var containerName = $"{mapGroup.Key}Map_data.amb";
                WriteContainer(containerName, FileType.AMPC, mapGroup.ToDictionary(g => g.Key, g => SerializeMap(g.Value)));                
            }
            var mapTextGroups = GroupMapRelatedEntities(MapTexts);
            foreach (var mapTextGroup in mapTextGroups)
            {
                var containerName = $"{mapTextGroup.Key}Map_text.amb";
                WriteContainer(containerName, FileType.AMNP, mapTextGroup.ToDictionary(g => g.Key, g => SerializeTextList(g.Value)));
            }
            // ...
            #endregion
        }
    }
}
