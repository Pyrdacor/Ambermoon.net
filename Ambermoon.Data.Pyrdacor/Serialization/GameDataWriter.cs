using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Characters;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;
using TextReader = Ambermoon.Data.Legacy.Serialization.TextReader;

namespace Ambermoon.Data.Pyrdacor;

partial class GameData
{
    private static void WritePalettes(IDataWriter dataWriter, Dictionary<int, Graphic> palettes,
        IReadOnlyList<Graphic> introPalettes, IReadOnlyList<Graphic> outroPalettes,
        IReadOnlyList<Graphic> fantasyIntroPalettes, IGraphicInfoProvider graphicInfoProvider)
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
            Buffer.BlockCopy(pal.Value.Data, 0, graphic.Data, dataIndex, 32 * 4);
            dataIndex += 32 * 4;
        }

        var gamePalette = new Palette(graphic)
        {
            DefaultTextPaletteIndex = graphicInfoProvider.DefaultTextPaletteIndex,
            PrimaryUIPaletteIndex = graphicInfoProvider.PrimaryUIPaletteIndex,
            AutomapPaletteIndex = graphicInfoProvider.AutomapPaletteIndex,
            SecondaryUIPaletteIndex = graphicInfoProvider.SecondaryUIPaletteIndex,
            FirstIntroPaletteIndex = graphicInfoProvider.FirstIntroPaletteIndex,
            FirstOutroPaletteIndex = graphicInfoProvider.FirstOutroPaletteIndex,
            FirstFantasyIntroPaletteIndex = graphicInfoProvider.FirstFantasyIntroPaletteIndex,
        };

        var outroPaletteGraphic = new Graphic
        {
            Width = 32,
            Height = outroPalettes.Count,
            Data = new byte[32 * outroPalettes.Count * 4],
            IndexedGraphic = false
        };

        for (int i = 0; i < outroPalettes.Count; i++)
            Buffer.BlockCopy(outroPalettes[i].Data, 0, outroPaletteGraphic.Data, i * 128, 128);

        var introPaletteGraphic = new Graphic
        {
            Width = 32,
            Height = introPalettes.Count,
            Data = new byte[32 * introPalettes.Count * 4],
            IndexedGraphic = false
        };

        for (int i = 0; i < introPalettes.Count; i++)
            Buffer.BlockCopy(introPalettes[i].Data, 0, introPaletteGraphic.Data, i * 128, 128);

        var fantasyIntroPaletteGraphic = new Graphic
        {
            Width = 32,
            Height = fantasyIntroPalettes.Count,
            Data = new byte[32 * fantasyIntroPalettes.Count * 4],
            IndexedGraphic = false
        };

        for (int i = 0; i < fantasyIntroPalettes.Count; i++)
            Buffer.BlockCopy(fantasyIntroPalettes[i].Data, 0, fantasyIntroPaletteGraphic.Data, i * 128, 128);

        var outroPalette = new Palette(outroPaletteGraphic);
        var introPalette = new Palette(introPaletteGraphic);
        var fantasyIntroPalette = new Palette(fantasyIntroPaletteGraphic);

        var paletteEntries = new Dictionary<ushort, Palette>
        {
            { Palette.GamePalettesIndex, gamePalette },
            { Palette.OutroPalettesIndex, outroPalette },
            { Palette.IntroPalettesIndex, introPalette },
            { Palette.FantasyIntroPalettesIndex, fantasyIntroPalette },
        };

        PADP.Write(dataWriter, paletteEntries);
    }

    private static void WriteTexts(IDataWriter dataWriter, Dictionary<int, IDataReader> textFiles)
    {
        PADP.Write(dataWriter, textFiles.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key, file =>
        {
            var texts = TextReader.ReadTexts(file.Value);

            return new Texts(new TextList(texts));
        }));
    }

    private static void WriteTexts(IDataWriter dataWriter, IReadOnlyList<string> texts)
    {
        PADF.Write(dataWriter, new Texts(new TextList(texts)));
    }

    private static IReadOnlyList<Graphic> TransparentToBlack(IReadOnlyList<Graphic> graphics)
    {
        return graphics.Select(graphic =>
        {
            for (int i = 0; i < graphic.Data.Length; i++)
            {
                if (graphic.Data[i] == 0)
                    graphic.Data[i] = 32;
            }

            return graphic;
        }).ToList();
    }

    private static void WriteGraphics(IDataWriter dataWriter, IReadOnlyList<Graphic> graphics, bool tiles, bool alpha, bool usePalette, int? fixedPaletteIndex = null, int colorIndexOffset = 0)
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

    private static void WriteIndexedGraphics(IDataWriter dataWriter, IDictionary<int, Graphic> graphics, bool alpha, bool usePalette, int? fixedPaletteIndex = null, int colorIndexOffset = 0)
    {
        var graphicAtlas = GraphicAtlasData.FromIndexedGraphics(usePalette ? (fixedPaletteIndex ?? GraphicAtlasData.MultiplePalettes) : null, graphics, alpha, colorIndexOffset);

        PADF.Write(dataWriter, graphicAtlas);
    }

    public static void WriteGameDataInfo(IDataWriter dataWriter, string name, bool advanced, string version, GameLanguage language)
    {
        var info = new GameDataInfo()
        {
            Name = name,
            Advanced = advanced,
            Version = version,
            Language = language,
        };

        PADF.Write(dataWriter, info);
    }

    public static void WriteFile(IDataWriter dataWriter, byte[] data, bool compress)
    {
        PADF.Write(dataWriter, new RawData(data), compress ? new DeflateCompression() : new NullCompression());
    }

    public static void WriteContainer(IDataWriter dataWriter, IReadOnlyList<byte[]> files, bool compress)
    {
        WriteContainer(dataWriter, files.Select((f, i) => new { f, i }).ToDictionary(f => (ushort)(1 + f.i), f => f.f), compress);
    }

    public static void WriteContainer(IDataWriter dataWriter, IDictionary<ushort, byte[]> files, bool compress)
    {
        PADP.Write(dataWriter, files.ToDictionary(f => f.Key, f => new RawData(f.Value)), compress ? new DeflateCompression() : new NullCompression());
    }

    public static void WriteLegacyGameData(IDataWriter dataWriter, Legacy.GameData gameData,
        params (string Magic, Action<IDataWriter> DataWriter)[] customDataWriters)
    {
        dataWriter.WriteWithoutLength("PYGD");

        int fileCount = 0;
        int fileCountPosition = dataWriter.Position;
        dataWriter.Write((ushort)0); // Will be replaced by file count later

        var customDataWriterMagic = customDataWriters?.Select(w => w.Magic).ToHashSet() ?? [];

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

        void WriteDefaultSection(string magic, Action write)
        {
            if (customDataWriterMagic.Contains(magic))
                return;

            WriteSection(magic, write);
        }

        var graphicProvider = (gameData.GraphicInfoProvider as IGraphicProvider)!;
        var info = new GameDataInfo()
        {
            Name = "Ambermoon",
            Advanced = gameData.Advanced,
            Version = gameData.Version,
            Language = gameData.Language,
        };

        WriteDefaultSection(MagicInfo, () => PADF.Write(dataWriter, info));

        if (customDataWriters != null)
        {
            foreach (var customDataWriter in customDataWriters)
            {
                WriteSection(customDataWriter.Magic, () => customDataWriter.DataWriter(dataWriter));
            }
        }

        WriteDefaultSection(MagicPalette, () => WritePalettes(dataWriter,
            gameData.GraphicInfoProvider.Palettes
                .OrderBy(p => p.Key)
                .TakeWhile(p => p.Key < gameData.GraphicInfoProvider.FirstIntroPaletteIndex)
                .ToDictionary(p => p.Key, p => p.Value),
            gameData.IntroData.IntroPalettes, gameData.OutroData.OutroPalettes,
            gameData.FantasyIntroData.FantasyIntroPalettes, gameData.GraphicInfoProvider));

        WriteDefaultSection(MagicSavegame, () =>
        {
            var savegame = new Savegame();
            var savegameReader = (gameData.Files.TryGetValue("Initial/Party_data.sav", out var sgReader) ? sgReader : gameData.Files["Save.00/Party_data.sav"]).Files[1];
            SavegameSerializer.ReadSaveData(savegame, savegameReader);
            PADF.Write(dataWriter, new FileSpecs.SavegameData(savegame));
        });

        WriteDefaultSection(MagicFonts, () =>
        {
            var ingameFont = new FontData(new(gameData.ExecutableData.Glyphs.Entries.Select(g => new Glyph { Graphic = g, Advance = 6 }).ToList()));
            var ingameDigitFont = new FontData(new(gameData.ExecutableData.DigitGlyphs.Entries.Select(g => new Glyph { Graphic = g, Advance = 5 }).ToList()));
            var outroSmallFont = new FontData(new(gameData.OutroData.Glyphs.OrderBy(g => g.Key).Select(g => g.Value).ToList()));
            var outroLargeFont = new FontData(new(gameData.OutroData.LargeGlyphs.OrderBy(g => g.Key).Select(g => g.Value).ToList()));
            var introSmallFont = new FontData(new(gameData.IntroData.Glyphs.OrderBy(g => g.Key).Select(g => g.Value).ToList()));
            var introLargeFont = new FontData(new(gameData.IntroData.LargeGlyphs.OrderBy(g => g.Key).Select(g => g.Value).ToList()));

            var fonts = new Dictionary<ushort, FontData>
            {
                { FontData.IngameFontIndex, ingameFont },
                { FontData.IngameDigitFontIndex, ingameDigitFont },
                { FontData.OutroSmallFontIndex, outroSmallFont },
                { FontData.OutroLargeFontIndex, outroLargeFont },
                { FontData.IntroSmallFontIndex, introSmallFont },
                { FontData.IntroLargeFontIndex, introLargeFont },
            };

            PADP.Write(dataWriter, fonts);
        });

        WriteDefaultSection(MagicGlyphMappings, () =>
        {
            var outroSmallGlyphMapping = new GlyphMappingData(new(gameData.OutroData.Glyphs.OrderBy(g => g.Key).Select((g, index) => new { g, index }).ToDictionary(e => e.g.Key, e => e.index)));
            var outroLargeGlyphMapping = new GlyphMappingData(new(gameData.OutroData.LargeGlyphs.OrderBy(g => g.Key).Select((g, index) => new { g, index }).ToDictionary(e => e.g.Key, e => e.index)));
            var introSmallGlyphMapping = new GlyphMappingData(new(gameData.IntroData.Glyphs.OrderBy(g => g.Key).Select((g, index) => new { g, index }).ToDictionary(e => e.g.Key, e => e.index)));
            var introLargeGlyphMapping = new GlyphMappingData(new(gameData.IntroData.LargeGlyphs.OrderBy(g => g.Key).Select((g, index) => new { g, index }).ToDictionary(e => e.g.Key, e => e.index)));

            var glyphMappings = new Dictionary<ushort, GlyphMappingData>
            {
                { GlyphMappingData.OutroSmallGlyphMappingIndex, outroSmallGlyphMapping },
                { GlyphMappingData.OutroLargeGlyphMappingIndex, outroLargeGlyphMapping },
                { GlyphMappingData.IntroSmallGlyphMappingIndex, introSmallGlyphMapping },
                { GlyphMappingData.IntroLargeGlyphMappingIndex, introLargeGlyphMapping },
            };

            PADP.Write(dataWriter, glyphMappings);
        });

        WriteDefaultSection(MagicInitialParty, () =>
        {
            var partyMemberReader = new PartyMemberReader();
            var partyFiles = (gameData.Files.TryGetValue("Initial/Party_char.amb", out var pcReader) ? pcReader : gameData.Files["Save.00/Party_char.amb"]).Files;
            PADP.Write(dataWriter, partyFiles.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key, file =>
                new CharacterData(PartyMember.Load((uint)file.Key, partyMemberReader, file.Value, null))));
        });

        WriteDefaultSection(MagicMonsters, () =>
        {
            var monsterReader = new MonsterReader();
            var monsterFiles = (gameData.Files.TryGetValue("Monster_char.amb", out var mcReader) ? mcReader : gameData.Files["Monster_char_data.amb"]).Files;
            PADP.Write(dataWriter, monsterFiles.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key, file =>
                new CharacterData(Monster.Load((uint)file.Key, monsterReader, file.Value))));
        });

        WriteDefaultSection(MagicMonsterGraphics, () =>
        {
            var monsterGraphics = gameData.CharacterManager.Monsters.ToDictionary(monster => (int)monster.Index, monster => monster.CombatGraphic);

            static string CreateHash(Graphic graphic)
            {
                var hashBytes = System.Security.Cryptography.MD5.HashData(graphic.Data);
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

            PADF.Write(dataWriter, atlas);
        });

        WriteDefaultSection(MagicNPCs, () =>
        {
            var npcReader = new NPCReader();
            PADP.Write(dataWriter, gameData.Files["NPC_char.amb"].Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new CharacterData(NPC.Load((uint)file.Key, npcReader, file.Value, null))));
        });

        WriteDefaultSection(MagicNPCTexts, () => WriteTexts(dataWriter, gameData.Files["NPC_texts.amb"].Files));
        WriteDefaultSection(MagicNPCGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.NPC), tiles: false, alpha: false, usePalette: true));

        IEnumerable<TravelGraphicInfo> CreateTravelGraphicInfos(TravelType travelType)
        {
            for (int d = 0; d < 4; d++)
            {
                yield return gameData.GetTravelGraphicInfo(travelType, (CharacterDirection)d);
            }
        }

        var travelGraphicInfos = EnumHelper.GetValues<TravelType>().ToDictionary(t => t, t => CreateTravelGraphicInfos(t).ToArray());
        var graphicInfos = new GraphicsInfoData(graphicProvider.NPCGraphicOffsets, graphicProvider.NPCGraphicFrameCounts,
            CombatGraphics.Info, gameData.StationaryImageInfos, travelGraphicInfos, gameData.PlayerAnimationInfo,
            gameData.CursorHotspots);
        WriteDefaultSection(MagicGraphicsInfo, () => PADF.Write(dataWriter, graphicInfos));

        WriteDefaultSection(MagicPartyTexts, () => WriteTexts(dataWriter, gameData.Files["Party_texts.amb"].Files));
        WriteDefaultSection(MagicPartyGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Player), tiles: false, alpha: false, usePalette: true));
        WriteDefaultSection(MagicTravelGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.TravelGfx), tiles: false, alpha: false, usePalette: true));
        WriteDefaultSection(MagicTransportGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Transports), tiles: false, alpha: false, usePalette: true));

        WriteDefaultSection(MagicMonsterGroups, () =>
        {
            var monsterGroupReader = new MonsterGroupReader();
            PADP.Write(dataWriter, gameData.Files["Monster_groups.amb"].Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new MonsterGroups(MonsterGroup.Load(gameData.CharacterManager, monsterGroupReader, file.Value))));
        });

        WriteDefaultSection(MagicItems, () => PADP.Write(dataWriter, gameData.ItemManager.Items.Select(item => new ItemData(item))));
        WriteDefaultSection(MagicItemTexts, () => WriteTexts(dataWriter, gameData.Files["Object_texts.amb"].Files));
        WriteDefaultSection(MagicItemNames, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.ItemManager.Items.Select(item => item.Name)]))));
        WriteDefaultSection(MagicItemGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Item), tiles: true, alpha: true, usePalette: true, fixedPaletteIndex: graphicProvider.PrimaryUIPaletteIndex));
        
        WriteDefaultSection(MagicLocations, () => PADP.Write(dataWriter, gameData.Places.Entries.Select(place => new LocationData(place))));
        WriteDefaultSection(MagicLocationNames, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.Places.Entries.Select(place => place.Name)]))));

        WriteDefaultSection(MagicOutro, () => PADF.Write(dataWriter, new OutroSequenceData(gameData.OutroData.OutroActions)));

        WriteDefaultSection(MagicTexts, () =>
        {
            var texts = new Dictionary<ushort, Texts>();

            void AddTextDict<TKey>(MessageTextType textType, IReadOnlyDictionary<TKey, string> textList)
            {
                AddTextList(textType, textList.OrderBy(e => e.Key).Select(e => e.Value).ToList());
            }

            void AddTextList(MessageTextType textType, IReadOnlyList<string> textList)
            {
                texts.Add((ushort)textType, new Texts(new(textList)));
            }

            AddTextDict(MessageTextType.UI, gameData.ExecutableData.UITexts.Entries);
            AddTextDict(MessageTextType.Condition, gameData.ExecutableData.ConditionNames.Entries);
            AddTextDict(MessageTextType.Class, gameData.ExecutableData.ClassNames.Entries);
            GenderFlag[] genders = [GenderFlag.Male, GenderFlag.Female, GenderFlag.Both];
            AddTextList(MessageTextType.Gender, genders.Select(gameData.DataNameProvider.GetGenderName).ToList());
            AddTextDict(MessageTextType.Language, gameData.ExecutableData.LanguageNames.Entries);
            AddTextDict(MessageTextType.Race, gameData.ExecutableData.RaceNames.Entries);
            AddTextDict(MessageTextType.Spell, gameData.ExecutableData.SpellNames.Entries);
            AddTextDict(MessageTextType.SpellType, gameData.ExecutableData.SpellTypeNames.Entries);
            AddTextDict(MessageTextType.World, gameData.ExecutableData.WorldNames.Entries);
            AddTextDict(MessageTextType.ItemType, gameData.ExecutableData.ItemTypeNames.Entries);
            AddTextDict(MessageTextType.Song, gameData.ExecutableData.SongNames.Entries);
            AddTextDict(MessageTextType.Attribute, gameData.ExecutableData.AttributeNames.Entries);
            AddTextDict(MessageTextType.Skill, gameData.ExecutableData.SkillNames.Entries);
            AddTextDict(MessageTextType.AttributeShortcut, gameData.ExecutableData.AttributeNames.ShortNames);
            AddTextDict(MessageTextType.SkillShortcut, gameData.ExecutableData.SkillNames.ShortNames);
            AddTextDict(MessageTextType.Automap, gameData.ExecutableData.AutomapNames.Entries);
            AddTextList(MessageTextType.Message, gameData.ExecutableData.Messages.Entries);

            PADP.Write(dataWriter, texts);
        });

        WriteDefaultSection(MagicTilesets, () => PADP.Write(dataWriter, gameData.MapManager.Tilesets.Select(tileset => new TilesetData(tileset))));
        WriteDefaultSection(MagicLabyrinthData, () => PADP.Write(dataWriter, gameData.MapManager.Labdata.Select(labdata => new LabyrinthData(labdata))));
        WriteDefaultSection(MagicMaps, () => PADP.Write(dataWriter, gameData.MapManager.Maps.ToDictionary(map => (ushort)map.Index, map => new MapData(map))));
        WriteDefaultSection(MagicMapTexts, () => PADP.Write(dataWriter, gameData.MapManager.Maps.ToDictionary(map => (ushort)map.Index, map => new Texts(new Objects.TextList(map.Texts)))));

        // TODO: fonts

        WriteDefaultSection(MagicGotoPointNames, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.MapManager.Maps.SelectMany(map => map.GotoPoints ?? []).OrderBy(gotoPoint => gotoPoint.Index).Select(gotoPoint => gotoPoint.Name)])))); // Note: This assumes there are no gaps!
        
        WriteDefaultSection(MagicLayouts, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Layout), tiles: true, alpha: false, usePalette: true));

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
        WriteDefaultSection(MagicTextures, () => PADF.Write(dataWriter, textures));

        WriteDefaultSection(MagicEventGraphics, () => WriteGraphics(dataWriter, TransparentToBlack(graphicProvider.GetGraphics(GraphicType.EventPictures)), tiles: true, alpha: false, usePalette: true));
        WriteDefaultSection(MagicTileGraphics, () => PADP.Write(dataWriter,
            Enumerable.Range(1, 10)
                .Select(i => new { Index = (ushort)i, Graphics = graphicProvider.GetGraphics(GraphicType.Tileset1 + i - 1) })
                .Where(tileset => tileset.Graphics.Count != 0)
                .ToDictionary(tileset => tileset.Index, tileset => GraphicAtlasData.FromTiles(GraphicAtlasData.MultiplePalettes, tileset.Graphics, alpha: true))));
        WriteDefaultSection(MagicCombatBackgrounds, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.CombatBackground), tiles: true, alpha: false, usePalette: true));
        Graphic swordAndMaceUIGraphic = null!;
        WriteDefaultSection(MagicCombatGraphics, () =>
        {
            var combatGraphics = graphicProvider.GetGraphics(GraphicType.CombatGraphics);

            // Exclude sword and mace
            swordAndMaceUIGraphic = combatGraphics[^1];
            combatGraphics.RemoveAt(combatGraphics.Count - 1);

            WriteGraphics(dataWriter, combatGraphics, tiles: false, alpha: true, usePalette: true);
        });
        WriteDefaultSection(MagicBattleFieldSprites, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.BattleFieldIcons), tiles: true, alpha: true, usePalette: true));
        WriteDefaultSection(MagicPortraits, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Portrait), tiles: true, alpha: true, usePalette: true));

        WriteSection(MagicUIGraphics, () => WriteGraphics(dataWriter, [..graphicProvider.GetGraphics(GraphicType.UIElements), swordAndMaceUIGraphic], tiles: false, alpha: true, usePalette: true));

        WriteDefaultSection(MagicRiddlemouthGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.RiddlemouthGraphics), tiles: false, alpha: false, usePalette: true));
        WriteDefaultSection(MagicCursors, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Cursor), tiles: true, alpha: true, usePalette: true));
        WriteDefaultSection(MagicPictures80x80, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.Pics80x80), tiles: true, alpha: false, usePalette: true));
        WriteDefaultSection(MagicAutomapGraphics, () => WriteGraphics(dataWriter, graphicProvider.GetGraphics(GraphicType.AutomapGraphics), tiles: false, alpha: true, usePalette: true));

        WriteDefaultSection(MagicInitialChests, () =>
        {
            var chestReader = new ChestReader();
            var container = gameData.Files.TryGetValue("Initial/Chest_data.amb", out var c) ? c : gameData.Files["Save.00/Chest_data.amb"];

            PADP.Write(dataWriter, container.Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new ChestData(Chest.Load(chestReader, file.Value))));
        });
        WriteDefaultSection(MagicInitialMerchants, () =>
        {
            var merchantReader = new MerchantReader();
            var container = gameData.Files.TryGetValue("Initial/Merchant_data.amb", out var m) ? m : gameData.Files["Save.00/Merchant_data.amb"];

            PADP.Write(dataWriter, container.Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new MerchantData(Merchant.Load(merchantReader, file.Value))));
        });
        WriteDefaultSection(MagicInitialAutomaps, () =>
        {
            var automapReader = new AutomapReader();
            var container = gameData.Files.TryGetValue("Initial/Automap.amb", out var a) ? a : gameData.Files["Save.00/Automap.amb"];

            PADP.Write(dataWriter, container.Files.Where(file => file.Value.Size != 0).ToDictionary(file => (ushort)file.Key,
                file => new ExplorationData(Automap.Load(automapReader, file.Value))));
        });

        WriteDefaultSection(MagicDictionary, () => PADF.Write(dataWriter, new Texts(new TextList([.. gameData.Dictionary.Entries]))));

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

        WriteDefaultSection(MagicMusic, () => PADP.Write(dataWriter, songData.ToDictionary(song => (ushort)song.Key, song => new MusicData(song.Value))));

        WriteDefaultSection(MagicOutroGraphics, () => WriteGraphics(dataWriter, gameData.OutroData.Graphics, false, false, true, gameData.GraphicInfoProvider.FirstOutroPaletteIndex));

        WriteDefaultSection(MagicIntroGraphics, () => WriteIndexedGraphics(dataWriter, gameData.IntroData.Graphics.ToDictionary(g => (int)g.Key, g => g.Value), true, true));

        WriteDefaultSection(MagicFantasyIntroGraphics, () => WriteIndexedGraphics(dataWriter, gameData.FantasyIntroData.Graphics.ToDictionary(g => (int)g.Key, g => g.Value), true, true));

        WriteDefaultSection(MagicOutroGraphicInfos, () => PADP.Write(dataWriter, gameData.OutroData.GraphicInfos.ToDictionary(info => (ushort)(1 + info.Value.GraphicIndex), info => new OutroGraphicInfoData(info.Value, info.Key))));

        WriteDefaultSection(MagicIntroAssets, () =>
        {
            var introData = gameData.IntroData;
            var introAssets = new IntroAssets(introData.Graphics.ToDictionary(g => g.Key, g => new Size(g.Value.Width, g.Value.Height)),
                introData.TwinlakeImageParts.Cast<IntroTwinlakeImagePart>().ToList(),
                introData.TextCommands.Cast<TextCommand>().ToList(),
                introData.TextCommandTexts.ToList());

            PADF.Write(dataWriter, new IntroAssetData(introAssets));
        });

        WriteDefaultSection(MagicFantasyIntroAssets, () =>
        {
            var fantasyIntroData = gameData.FantasyIntroData;
            var fantasyIntroAssets = new FantasyIntroAssets(fantasyIntroData.Graphics.ToDictionary(g => g.Key, g => new Size(g.Value.Width, g.Value.Height)),
                fantasyIntroData.Actions.ToList());

            PADF.Write(dataWriter, new FantasyIntroAssetData(fantasyIntroAssets));
        });

        WriteDefaultSection(MagicIntroTexts, () => WriteTexts(dataWriter, gameData.IntroData.Texts.OrderBy(e => e.Key).Select(e => e.Value).ToList()));

        WriteDefaultSection(MagicOutroTexts, () => WriteTexts(dataWriter, gameData.OutroData.Texts));

        WriteDefaultSection(MagicLightEffectData, () => PADF.Write(dataWriter, new LightEffectData(gameData.ExecutableData)));

        dataWriter.Replace(fileCountPosition, (ushort)fileCount);
    }
}
