using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Serialization;
using TextReader = Ambermoon.Data.Legacy.Serialization.TextReader;

namespace Ambermoon.Data.Pyrdacor;

partial class GameData
{
    private static void WritePalettes(IDataWriter dataWriter, Dictionary<int, IDataReader> palettes)
    {
        var graphic = new Graphic()
        {
            Width = 32,
            Height = palettes.Count,
            IndexedGraphic = false,
            Data = new byte[32 * palettes.Count * 4]
        };

        int dataIndex = 0;

        foreach (var pal in palettes.OrderBy(p => p.Key))
        {
            pal.Value.Position = 0;

            var data = pal.Value.ReadBytes(64);

            for (int i = 0; i < 32; i++)
            {
                var r = data[i * 2 + 0] & 0xf;
                var gb = data[i * 2 + 1];
                var g = gb >> 4;
                var b = gb & 0xf;

                r |= (r << 4);
                g |= (g << 4);
                b |= (b << 4);

                graphic.Data[dataIndex++] = (byte)r;
                graphic.Data[dataIndex++] = (byte)g;
                graphic.Data[dataIndex++] = (byte)b;
                graphic.Data[dataIndex++] = 0xff;
            }
        }

        var palette = new Palette(graphic);

        PADF.Write(dataWriter, palette);
    }

    private static void WriteTexts(IDataWriter dataWriter, Dictionary<int, IDataReader> textFiles)
    {
        PADP.Write(dataWriter, textFiles.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key, file =>
        {
            var texts = TextReader.ReadTexts(file.Value);

            return new Texts(new Objects.TextList(texts));
        }));
    }

    public static void WriteLegacyGameData(IDataWriter dataWriter, Legacy.GameData gameData)
    {
        dataWriter.WriteWithoutLength("PYGD");

        int fileCount = 0;
        int fileCountPosition = dataWriter.Position;
        dataWriter.Write((ushort)0); // Will be replaced by file count later

        void WriteSection(string magic, Action write)
        {
            dataWriter.WriteWithoutLength(magic);
            int position = dataWriter.Position;
            dataWriter.Write((uint)0); // size placeholder
            write();
            uint size = (uint)(dataWriter.Position - position - 4);
            dataWriter.Replace(position, size);
            fileCount++;
        }

        WriteSection(MagicPalette, () => WritePalettes(dataWriter, gameData.Files["Palettes.amb"].Files));

        WriteSection(MagicSavegame, () =>
        {
            var savegame = new SavegameData();
            var savegameReader = (gameData.Files.TryGetValue("Initial/Party_data.sav", out var sgReader) ? sgReader : gameData.Files["Save.00/Party_data.sav"]).Files[1];
            SavegameSerializer.ReadSaveData(savegame, savegameReader);
            PADF.Write(dataWriter, new FileSpecs.SavegameData(savegame));
        });

        // TODO: fonts

        WriteSection(MagicMonsterGroups, () =>
        {
            var monsterGroupReader = new MonsterGroupReader();
            PADP.Write(dataWriter, gameData.Files["Monster_groups.amb"].Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new MonsterGroups(MonsterGroup.Load(gameData.CharacterManager, monsterGroupReader, file.Value))));
        });

        WriteSection(MagicPlayers, () =>
        {
            var partyMemberReader = new PartyMemberReader();
            var partyFiles = (gameData.Files.TryGetValue("Initial/Party_char.amb", out var pcReader) ? pcReader : gameData.Files["Save.00/Party_char.amb"]).Files;
            PADP.Write(dataWriter, partyFiles.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key, file =>
                new CharacterData(PartyMember.Load((uint)file.Key, partyMemberReader, file.Value, null))));
        });

        WriteSection(MagicMonsters, () =>
        {
            var monsterReader = new MonsterReader();
            var monsterFiles = (gameData.Files.TryGetValue("Monster_char.amb", out var mcReader) ? mcReader : gameData.Files["Monster_char_data.amb"]).Files;
            PADP.Write(dataWriter, monsterFiles.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key, file =>
                new CharacterData(Monster.Load((uint)file.Key, monsterReader, file.Value))));
        });

        WriteSection(MagicNPCs, () =>
        {
            var npcReader = new NPCReader();
            PADP.Write(dataWriter, gameData.Files["NPC_char.amb"].Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new CharacterData(NPC.Load((uint)file.Key, npcReader, file.Value, null))));
        });

        WriteSection(MagicNPCTexts, () => WriteTexts(dataWriter, gameData.Files["NPC_texts.amb"].Files));
        WriteSection(MagicPartyTexts, () => WriteTexts(dataWriter, gameData.Files["Party_texts.amb"].Files));
        WriteSection(MagicItemTexts, () => WriteTexts(dataWriter, gameData.Files["Object_texts.amb"].Files));
        WriteSection(MagicItemNames, () => PADF.Write(dataWriter, new Texts(new Objects.TextList([.. gameData.ItemManager.Items.Select(item => item.Name)]))));
        WriteSection(MagicGotoPointNames, () => PADF.Write(dataWriter, new Texts(new Objects.TextList([.. gameData.MapManager.Maps.SelectMany(map => map.GotoPoints ?? []).OrderBy(gotoPoint => gotoPoint.Index).Select(gotoPoint => gotoPoint.Name)])))); // Note: This assumes there are no gaps!
        WriteSection(MagicItems, () => PADP.Write(dataWriter, gameData.ItemManager.Items.Select(item => new ItemData(item))));
        WriteSection(MagicMaps, () => PADP.Write(dataWriter, gameData.MapManager.Maps.ToDictionary(map => (ushort)map.Index, map => new MapData(map))));
        WriteSection(MagicMapTexts, () => PADP.Write(dataWriter, gameData.MapManager.Maps.ToDictionary(map => (ushort)map.Index, map => new Texts(new Objects.TextList(map.Texts)))));
        WriteSection(MagicLabyrinthData, () => PADP.Write(dataWriter, gameData.MapManager.Labdata.Select(labdata => new LabyrinthData(labdata))));
        WriteSection(MagicTilesets, () => PADP.Write(dataWriter, gameData.MapManager.Tilesets.Select(tileset => new TilesetData(tileset))));
        WriteSection(MagicLocationNames, () => PADF.Write(dataWriter, new Texts(new Objects.TextList([.. gameData.Places.Entries.Select(place => place.Name)]))));
        WriteSection(MagicLocations, () => PADP.Write(dataWriter, gameData.Places.Entries.Select(place => new LocationData(place))));
        // TODO: outro
        // TODO: texts (messages, etc)

        // TODO ...

        dataWriter.Replace(fileCountPosition, (ushort)fileCount);
    }
}
