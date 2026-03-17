using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;
using TextReader = Ambermoon.Data.Legacy.Serialization.TextReader;

namespace Ambermoon.Data.Pyrdacor;

partial class GameData
{
    private static void WritePalettes(IDataWriter dataWriter, Dictionary<int, IDataReader> palettes, IGraphicInfoProvider graphicInfoProvider)
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

        var palette = new Palette(graphic)
        {
            PrimaryUIPaletteIndex = graphicInfoProvider.PrimaryUIPaletteIndex,
            AutomapPaletteIndex = graphicInfoProvider.AutomapPaletteIndex,
            SecondaryUIPaletteIndex = graphicInfoProvider.SecondaryUIPaletteIndex,
            FirstIntroPaletteIndex = graphicInfoProvider.FirstIntroPaletteIndex,
            FirstOutroPaletteIndex = graphicInfoProvider.FirstOutroPaletteIndex,
            FirstFantasyIntroPaletteIndex = graphicInfoProvider.FirstFantasyIntroPaletteIndex,
        };

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

    private static void WriteGraphics(IDataWriter dataWriter, List<Graphic> graphics, bool tiles, bool alpha, bool usePalette, int? fixedPaletteIndex = null, int colorIndexOffset = 0)
    {
        GraphicAtlasData graphicAtlas;

        if (tiles)
        {
            if (usePalette)
            {
                graphicAtlas = GraphicAtlasData.FromTiles(fixedPaletteIndex ?? GraphicAtlasData.MultiplePalettes, graphics, alpha, colorIndexOffset);
            }
            else
            {
                graphicAtlas = GraphicAtlasData.FromTiles(graphics, alpha, colorIndexOffset);
            }
        }
        else
        {
            if (usePalette)
            {
                graphicAtlas = GraphicAtlasData.FromGraphics(fixedPaletteIndex ?? GraphicAtlasData.MultiplePalettes, graphics, alpha, colorIndexOffset);
            }
            else
            {
                graphicAtlas = GraphicAtlasData.FromGraphics(graphics, alpha, colorIndexOffset);
            }
        }

        PADF.Write(dataWriter, graphicAtlas);
    }

    private static void WriteIndexedGraphics(IDataWriter dataWriter, Dictionary<int, Graphic> graphics, bool alpha, bool usePalette, int? fixedPaletteIndex = null, int colorIndexOffset = 0)
    {
        var graphicAtlas = GraphicAtlasData.FromIndexedGraphics(usePalette ? (fixedPaletteIndex ?? GraphicAtlasData.MultiplePalettes) : null, graphics, alpha, colorIndexOffset);

        PADF.Write(dataWriter, graphicAtlas);
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

        var graphicProvider = (gameData.GraphicInfoProvider as IGraphicProvider)!;
        var info = new GameDataInfo()
        {
            Advanced = gameData.Advanced,
            Version = gameData.Version,
            Language = gameData.Language,
        };

        WriteSection(MagicInfo, () => PADF.Write(dataWriter, info));

        WriteSection(MagicPalette, () => WritePalettes(dataWriter, gameData.Files["Palettes.amb"].Files, gameData.GraphicInfoProvider));

        WriteSection(MagicSavegame, () =>
        {
            var savegame = new SavegameData();
            var savegameReader = (gameData.Files.TryGetValue("Initial/Party_data.sav", out var sgReader) ? sgReader : gameData.Files["Save.00/Party_data.sav"]).Files[1];
            SavegameSerializer.ReadSaveData(savegame, savegameReader);
            PADF.Write(dataWriter, new FileSpecs.SavegameData(savegame));
        });

        // TODO: fonts

        WriteSection(MagicInitialParty, () =>
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

        WriteSection(MagicMonsterGraphics, () =>
        {
            var monsterGraphics = gameData.CharacterManager.Monsters.ToDictionary(monster => (int)monster.Index, monster => monster.CombatGraphic);

            static string CreateHash(Graphic graphic)
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hashBytes = md5.ComputeHash(graphic.Data);
                return $"{graphic.Width},{graphic.Height}," + Convert.ToBase64String(hashBytes);
            }

            var monsterGraphicsByHash = monsterGraphics.GroupBy(mg => CreateHash(mg.Value)).ToDictionary(g => g.Key, g => new { Graphic = g.First().Value, Indices = g.Select(g => g.Key) });
            var graphics = monsterGraphicsByHash.Values.ToDictionary(g => g.Indices.Min(), g => g.Graphic);
            var atlas = GraphicAtlasData.FromIndexedGraphics(GraphicAtlasData.MultiplePalettes, graphics, alpha: true);

            foreach (var hashGroup in monsterGraphicsByHash)
            {
                var firstIndex = hashGroup.Value.Indices.Min();

                foreach (var index in hashGroup.Value.Indices)
                {
                    if (index != firstIndex)
                    {
                        atlas.ReuseTextureArea(index, firstIndex);
                    }
                }
            }
        });

        WriteSection(MagicNPCs, () =>
        {
            var npcReader = new NPCReader();
            PADP.Write(dataWriter, gameData.Files["NPC_char.amb"].Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new CharacterData(NPC.Load((uint)file.Key, npcReader, file.Value, null))));
        });

        WriteSection(MagicNPCTexts, () => WriteTexts(dataWriter, gameData.Files["NPC_texts.amb"].Files));
        WriteSection(MagicNPCGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.NPC), tiles: false, alpha: false, usePalette: true));

        IEnumerable<TravelGraphicInfo> CreateTravelGraphicInfos(TravelType travelType)
        {
            for (int d = 0; d < 4; d++)
            {
                yield return gameData.GetTravelGraphicInfo(travelType, (CharacterDirection)d);
            }
        }

        var travelGraphicInfos = EnumHelper.GetValues<TravelType>().ToDictionary(t => t, t => CreateTravelGraphicInfos(t).ToArray());
        var graphicInfos = new GraphicsInfoData(graphicProvider.NPCGraphicOffsets, graphicProvider.NPCGraphicFrameCounts,
            CombatGraphics.Info, gameData.StationaryImageInfos, travelGraphicInfos, gameData.PlayerAnimationInfo);
        WriteSection(MagicGraphicsInfo, () => PADF.Write(dataWriter, graphicInfos));

        WriteSection(MagicPartyTexts, () => WriteTexts(dataWriter, gameData.Files["Party_texts.amb"].Files));
        WriteSection(MagicPartyGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Player), tiles: false, alpha: false, usePalette: true));
        WriteSection(MagicTravelGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.TravelGfx), tiles: false, alpha: false, usePalette: true));
        WriteSection(MagicTransportGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Transports), tiles: false, alpha: false, usePalette: true));

        WriteSection(MagicMonsterGroups, () =>
        {
            var monsterGroupReader = new MonsterGroupReader();
            PADP.Write(dataWriter, gameData.Files["Monster_groups.amb"].Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new MonsterGroups(MonsterGroup.Load(gameData.CharacterManager, monsterGroupReader, file.Value))));
        });

        WriteSection(MagicItems, () => PADP.Write(dataWriter, gameData.ItemManager.Items.Select(item => new ItemData(item))));
        WriteSection(MagicItemTexts, () => WriteTexts(dataWriter, gameData.Files["Object_texts.amb"].Files));
        WriteSection(MagicItemNames, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.ItemManager.Items.Select(item => item.Name)]))));
        WriteSection(MagicItemGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Item), tiles: true, alpha: true, usePalette: true, fixedPaletteIndex: graphicProvider.PrimaryUIPaletteIndex));
        
        WriteSection(MagicLocations, () => PADP.Write(dataWriter, gameData.Places.Entries.Select(place => new LocationData(place))));
        WriteSection(MagicLocationNames, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.Places.Entries.Select(place => place.Name)]))));

        // TODO: outro
        // TODO: texts (messages, etc)

        WriteSection(MagicTilesets, () => PADP.Write(dataWriter, gameData.MapManager.Tilesets.Select(tileset => new TilesetData(tileset))));
        WriteSection(MagicLabyrinthData, () => PADP.Write(dataWriter, gameData.MapManager.Labdata.Select(labdata => new LabyrinthData(labdata))));
        WriteSection(MagicMaps, () => PADP.Write(dataWriter, gameData.MapManager.Maps.ToDictionary(map => (ushort)map.Index, map => new MapData(map))));
        WriteSection(MagicMapTexts, () => PADP.Write(dataWriter, gameData.MapManager.Maps.ToDictionary(map => (ushort)map.Index, map => new Texts(new Objects.TextList(map.Texts)))));

        // TODO: fonts

        WriteSection(MagicGotoPointNames, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.MapManager.Maps.SelectMany(map => map.GotoPoints ?? []).OrderBy(gotoPoint => gotoPoint.Index).Select(gotoPoint => gotoPoint.Name)])))); // Note: This assumes there are no gaps!
        
        WriteSection(MagicLayouts, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Layout), tiles: true, alpha: false, usePalette: true));

        var wallGraphics = new Dictionary<int, Graphic>();
        var objectGraphics = new Dictionary<int, Graphic>();
        var overlayGraphics = new Dictionary<int, Graphic>();
        var floorGraphics = new Dictionary<int, Graphic>();
        var graphicReader = new GraphicReader();

        void MergeGraphics(Dictionary<int, Graphic> target, Func<int, IDataReader, Graphic> loader, params IFileContainer[] fileContainers)
        {
            foreach (var container in fileContainers)
            {
                foreach (var entry in container.Files)
                {
                    if (entry.Value.Size != 0 && !target.ContainsKey(entry.Key))
                    {
                        entry.Value.Position = 0;
                        var graphic = loader(entry.Key, entry.Value);
                        target.Add(entry.Key, graphic);
                    }
                }
            }
        }

        MergeGraphics(wallGraphics, (_, dataReader) =>
        {
            var graphic = new Graphic() { IndexedGraphic = true };

            graphicReader.ReadGraphic(graphic, dataReader, new GraphicInfo()
            {
                Width = 128,
                Height = 80,
                GraphicFormat = GraphicFormat.Texture4Bit
            });

            return graphic;

        }, gameData.Files["2Wall3D.amb"], gameData.Files["3Wall3D.amb"]);
        MergeGraphics(objectGraphics, (index, dataReader) =>
        {
            var graphic = new Graphic() { IndexedGraphic = true };
            var info = TextureGraphicInfos.ObjectGraphicInfos[index - 1];

            graphicReader.ReadGraphic(graphic, dataReader, info);

            return graphic;

        }, gameData.Files["2Object3D.amb"], gameData.Files["3Object3D.amb"]);
        MergeGraphics(overlayGraphics, (index, dataReader) =>
        {
            var graphic = new Graphic() { IndexedGraphic = true };
            var info = TextureGraphicInfos.OverlayGraphicInfos[index - 1];

            graphicReader.ReadGraphic(graphic, dataReader, info);

            return graphic;

        }, gameData.Files["2Overlay3D.amb"], gameData.Files["3Overlay3D.amb"]);
        foreach (var entry in gameData.Files["Floors.amb"].Files)
        {
            if (entry.Value.Size != 0)
            {
                entry.Value.Position = 0;

                var graphic = new Graphic() { IndexedGraphic = true };

                graphicReader.ReadGraphic(graphic, entry.Value, new GraphicInfo()
                {
                    Width = 64,
                    Height = 64,
                    GraphicFormat = GraphicFormat.Palette4Bit
                });

                floorGraphics.Add(entry.Key, graphic);
            }
        }

        var textures = new Textures(wallGraphics, objectGraphics, overlayGraphics, floorGraphics, graphicProvider.GetGraphics(GraphicType.LabBackground));
        WriteSection(MagicTextures, () => PADF.Write(dataWriter, textures));

        WriteSection(MagicEventGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.EventPictures), tiles: true, alpha: false, usePalette: true));
        WriteSection(MagicTileGraphics, () => PADP.Write(dataWriter,
            Enumerable.Range(1, 10)
                .Select(i => new { Index = (ushort)i, Graphics = graphicProvider.GetGraphics(GraphicType.Tileset1 + i - 1) })
                .Where(tileset => tileset.Graphics.Count != 0)
                .ToDictionary(tileset => tileset.Index, tileset => GraphicAtlasData.FromTiles(GraphicAtlasData.MultiplePalettes, tileset.Graphics, alpha: true))));
        WriteSection(MagicCombatBackgrounds, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.CombatBackground), tiles: true, alpha: false, usePalette: true));
        WriteSection(MagicCombatGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.CombatGraphics), tiles: false, alpha: true, usePalette: true));
        WriteSection(MagicBattleFieldSprites, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.BattleFieldIcons), tiles: true, alpha: true, usePalette: true));
        WriteSection(MagicPortraits, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Portrait), tiles: true, alpha: true, usePalette: true));

        // TODO:
        // WriteSection(MagicUIGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Portrait), tiles: true, alpha: true, usePalette: true));

        WriteSection(MagicRiddlemouthGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.RiddlemouthGraphics), tiles: false, alpha: false, usePalette: true));
        WriteSection(MagicCursors, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Cursor), tiles: true, alpha: true, usePalette: true));
        WriteSection(MagicPictures80x80, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Pics80x80), tiles: true, alpha: false, usePalette: true));
        WriteSection(MagicAutomapGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.AutomapGraphics), tiles: false, alpha: true, usePalette: true));

        WriteSection(MagicInitialChests, () =>
        {
            var chestReader = new ChestReader();
            var container = gameData.Files.TryGetValue("Initial/Chest_data.amb", out var c) ? c : gameData.Files["Save.00/Chest_data.amb"];

            PADP.Write(dataWriter, container.Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new ChestData(Chest.Load(chestReader, file.Value))));
        });

        WriteSection(MagicDictionary, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.Dictionary.Entries]))));

        var songData = new Dictionary<Song, byte[]>();
        var introMusicContainer = gameData.Files["Intro_music"];
        var outroMusicContainer = gameData.Files["Extro_music"];
        var musicContainer = gameData.Files["Music.amb"];

        // Note: We don't store data for the menu song as it uses the same data as the intro song.
        introMusicContainer.Files[1].Position = 0;
        songData.Add(Song.Intro, introMusicContainer.Files[1].ReadToEnd());
        outroMusicContainer.Files[1].Position = 0;
        songData.Add(Song.Outro, outroMusicContainer.Files[1].ReadToEnd());

        foreach (var file in musicContainer.Files)
        {
            var song = (Song)file.Key;
            file.Value.Position = 0;
            songData.Add(song, file.Value.ReadToEnd());
        }

        WriteSection(MagicMusic, () => PADP.Write(dataWriter, songData.ToDictionary(song => (ushort)song.Key, song => new MusicData(song.Value))));

        dataWriter.Replace(fileCountPosition, (ushort)fileCount);
    }
}
