using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;

namespace Ambermoon.Data.Pyrdacor;

using Font = Objects.Font;
using SavegameData = FileSpecs.SavegameData;

public partial class GameData : IGameData, IGraphicAtlasProvider
{
    ISavegameManager? savegameManager;
    readonly Lazy<Savegame> initialSavegame;
    LazyFileLoader<GameDataInfo, GameDataInfo> gameDataInfoLoader = null!;
    LazyContainerLoader<Palette, Palette> paletteLoader = null!;
    LazyFileLoader<SavegameData, Savegame> savegameLoader = null!;
    LazyContainerLoader<FontData, Font> fontLoader = null!;
    LazyContainerLoader<GlyphMappingData, GlyphMapping> glyphMappingLoader = null!;
    LazyContainerLoader<MonsterGroups, MonsterGroup> monsterGroupLoader = null!;
    LazyContainerLoader<CharacterData, PartyMember> partyLoader = null!;
    LazyContainerLoader<CharacterData, Monster> monsterLoader = null!;
    LazyContainerLoader<CharacterData, NPC> npcLoader  = null!;
    LazyContainerLoader<Texts, TextList> npcTextLoader = null!;
    LazyContainerLoader<Texts, TextList> partyTextLoader = null!;
    LazyContainerLoader<ItemData, Item> itemLoader = null!;
    LazyFileLoader<Texts, TextList> itemNameLoader = null!;
    LazyContainerLoader<Texts, TextList> itemTextLoader = null!;
    LazyContainerLoader<MapData, Map> mapLoader = null!;
    LazyContainerLoader<Texts, TextList> mapTextLoader = null!;
    LazyContainerLoader<LabyrinthData, Labdata> labdataLoader = null!;
    LazyContainerLoader<TilesetData, Tileset> tilesetLoader = null!;
    LazyContainerLoader<LocationData, Place> locationLoader = null!;
    LazyFileLoader<Texts, TextList> locationNameLoader = null!;
    LazyFileLoader<OutroSequenceData, IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>>> outroSequenceLoader = null!;
    LazyFileLoader<Texts, TextList> gotoPointNameLoader = null!;
    LazyContainerLoader<ChestData, Chest> initialChestLoader = null!;
    LazyContainerLoader<MerchantData, Merchant> initialMerchantLoader = null!;
    LazyContainerLoader<ExplorationData, Automap> initialAutomapLoader = null!;
    LazyFileLoader<GraphicsInfoData, GraphicsInfoData> graphicsInfoLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> layoutGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> npcGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> playerGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> monsterGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> itemGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> portraitGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> eventGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> cursorGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> pics80x80GraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> uiGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> riddlemouthGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> travelGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> transportGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> combatBackgroudLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> combatGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> battleFieldSpriteLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> automapGraphicLoader = null!;
    LazyContainerLoader<GraphicAtlasData, GraphicAtlas> tileGraphicLoader = null!; // one entry per tileset
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> outroGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> introGraphicLoader = null!;
    LazyFileLoader<GraphicAtlasData, GraphicAtlas> fantasyIntroGraphicLoader = null!;
    LazyFileLoader<Texts, TextList> outroTextLoader = null!;
    LazyFileLoader<Texts, TextList> introTextLoader = null!;
    LazyContainerLoader<OutroGraphicInfoData, Objects.OutroGraphicInfo> outroGraphicsInfoLoader = null!;
    LazyFileLoader<IntroAssetData, IntroAssets> introAssetLoader = null!;
    LazyFileLoader<FantasyIntroAssetData, FantasyIntroAssets> fantasyIntroAssetLoader = null!;
    LazyFileLoader<LightEffectData, LightEffectDataProvider> lightEffectDataLoader = null!;
    LazyFileLoader<Texts, TextList> dictionaryLoader = null!;
    LazyFileLoader<Textures, Textures> texturesLoader = null!;
    LazyContainerLoader<MusicData, byte[]> musicLoader = null!;
    LazyContainerLoader<Texts, TextList> textLoader = null!; // all kind of texts, names and messages, key is the MessageTextType
    readonly Dictionary<string, Action<IDataReader>> fileHandlers = [];
    readonly Lazy<SongManager> songManager = null!;
    readonly Lazy<ICharacterManager> characterManager = null!;
    readonly Lazy<IItemManager> itemManager = null!;
    readonly Lazy<IMapManager> mapManager = null!;
    readonly Lazy<Palette> gamePalette = null!;
    readonly Lazy<IngameFont> ingameFont = null!;
    readonly Lazy<Font> outroSmallFont = null!;
    readonly Lazy<Font> outroLargeFont = null!;
    readonly Lazy<Font> introSmallFont = null!;
    readonly Lazy<Font> introLargeFont = null!;
    readonly Lazy<IOutroData> outroData = null!;
    readonly Lazy<IIntroData> introData = null!;
    readonly Lazy<IFantasyIntroData> fantasyIntroData = null!;
    readonly Lazy<Places> places = null!;
    readonly Lazy<Dictionary<int, Graphic>> palettes = null!;
    readonly Lazy<IngameFontProvider> ingameFontProvider = null!;
    readonly Lazy<TextDictionary> dictionary = null!;
    readonly Lazy<IDataNameProvider> dataNameProvider = null!;
    readonly Lazy<ILightEffectProvider> lightEffectProvider = null!;

    public bool Loaded { get; } = false;

    public GameDataSource GameDataSource => GameDataSource.Memory;

    public bool Advanced => gameDataInfoLoader.Load().Advanced;

    public GameLanguage Language => gameDataInfoLoader.Load().Language;

    public string Version => gameDataInfoLoader.Load().Version;

    public ICharacterManager CharacterManager => characterManager.Value;

    public ISavegameManager? SavegameManager
    {
        get => savegameManager;
        set
        {
            if (Loaded)
                throw new InvalidOperationException("You can't change the savegame manager after game data is fully loaded.");

            savegameManager = value;
        }
    }

    public ISongManager SongManager => songManager.Value;

    public IReadOnlyDictionary<TravelType, GraphicInfo> StationaryImageInfos
        => graphicsInfoLoader.Load().StationaryImageInfos;

    public Character2DAnimationInfo PlayerAnimationInfo => graphicsInfoLoader.Load().PlayerAnimationInfo;

    public IReadOnlyList<Position> CursorHotspots => graphicsInfoLoader.Load().CursorHotspots;

    public Places Places => places.Value;

    public IItemManager ItemManager => itemManager.Value;

    public IFontProvider FontProvider => ingameFontProvider.Value;

    public IDataNameProvider DataNameProvider => dataNameProvider.Value;

    public ILightEffectProvider LightEffectProvider => lightEffectProvider.Value;

    public IMapManager MapManager => mapManager.Value;

    public IGraphicInfoProvider GraphicInfoProvider => this;

    public IIntroData IntroData => introData.Value;

    public IFantasyIntroData FantasyIntroData => fantasyIntroData.Value;

    public IOutroData OutroData => outroData.Value;

    public Savegame InitialSavegame => initialSavegame.Value;

    public TextDictionary Dictionary => dictionary.Value;

    public Dictionary<int, Graphic> Palettes => palettes.Value;

    public byte DefaultTextPaletteIndex => gamePalette.Value.DefaultTextPaletteIndex;

    public byte PrimaryUIPaletteIndex => gamePalette.Value.PrimaryUIPaletteIndex;

    public byte SecondaryUIPaletteIndex => gamePalette.Value.SecondaryUIPaletteIndex;

    public byte AutomapPaletteIndex => gamePalette.Value.AutomapPaletteIndex;

    public byte FirstIntroPaletteIndex => gamePalette.Value.FirstIntroPaletteIndex;

    public byte FirstOutroPaletteIndex => gamePalette.Value.FirstOutroPaletteIndex;

    public byte FirstFantasyIntroPaletteIndex => gamePalette.Value.FirstFantasyIntroPaletteIndex;

    public IReadOnlyDictionary<int, int> NPCGraphicOffsets
        => graphicsInfoLoader.Load().NPCGraphicOffsets;

    public IReadOnlyDictionary<int, List<int>> NPCGraphicFrameCounts
        => graphicsInfoLoader.Load().NPCGraphicFrameCounts;

    internal Dictionary<uint, TextList> NPCTexts => npcTextLoader.LoadAll();

    internal Dictionary<uint, TextList> PartyTexts => partyTextLoader.LoadAll();

    public TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction)
    {
        return graphicsInfoLoader.Load().TravelGraphicInfos[type][(int)direction];
    }

    public const string MagicInfo = "INFO";
    public const string MagicPalette = "PALS";
    public const string MagicSavegame = "SAVE";
    public const string MagicInitialParty = "PLAY";
    public const string MagicMonsters = "MONS";
    public const string MagicMonsterGraphics = "MONG";
    public const string MagicNPCs = "NPCS";
    public const string MagicNPCTexts = "NTXT";
    public const string MagicNPCGraphics = "NGFX";
    public const string MagicGraphicsInfo = "GFXI"; // NPC graphic frame counts, player offsets, etc
    public const string MagicPartyTexts = "PTXT";
    public const string MagicPartyGraphics = "PGFX";
    public const string MagicTravelGraphics = "TRAV";
    public const string MagicTransportGraphics = "TRAN";
    public const string MagicMonsterGroups = "MOGS";
    public const string MagicItems = "ITEM";
    public const string MagicItemNames = "INAM";
    public const string MagicItemTexts = "ITXT";
    public const string MagicItemGraphics = "IGFX";
    public const string MagicLocations = "LOCS";
    public const string MagicLocationNames = "LNAM";
    public const string MagicOutro = "OUTR";
    public const string MagicTexts = "TEXT";
    public const string MagicTilesets = "TILE";
    public const string MagicLabyrinthData = "LABY";
    public const string MagicMaps = "MAPS";
    public const string MagicMapTexts = "MTXT";
    public const string MagicFonts = "FONT";
    public const string MagicGlyphMappings = "GMAP";
    public const string MagicGotoPointNames = "GOTO";
    public const string MagicLayouts = "LAYO";
    public const string MagicTextures = "TX3D";
    public const string MagicEventGraphics = "EVEG";
    public const string MagicTileGraphics = "TILG";
    public const string MagicCombatBackgrounds = "COMB";
    public const string MagicCombatGraphics = "COMG";
    public const string MagicBattleFieldSprites = "BFSP";
    public const string MagicPortraits = "PORT";
    public const string MagicUIGraphics = "UGFX";
    public const string MagicRiddlemouthGraphics = "RIDG";
    public const string MagicCursors = "CURS";
    public const string MagicPictures80x80 = "8080";
    public const string MagicAutomapGraphics = "AUMG";
    public const string MagicInitialChests = "CHES";
    public const string MagicInitialMerchants = "MERC";
    public const string MagicInitialAutomaps = "EXPL";
    public const string MagicDictionary = "DICT";
    public const string MagicMusic = "MUSI";
    public const string MagicOutroGraphics = "OUTG";
    public const string MagicIntroGraphics = "INTG";
    public const string MagicFantasyIntroGraphics = "FING";
    public const string MagicOutroTexts = "OUTT";
    public const string MagicIntroTexts = "INTT";
    public const string MagicOutroGraphicInfos = "OUGI";
    public const string MagicIntroAssets = "INAS";
    public const string MagicFantasyIntroAssets = "FINA";
    public const string MagicLightEffectData = "LEDT";

    public GameData(Stream stream, params (string Magic, Action<IDataReader, GameData> Action)[] customFileHandlers)
        : this(new DataReader(stream), customFileHandlers)
    {

    }

    // This is only used to read the info entry.
    private GameData(IDataReader reader)
    {
        int position = reader.Position;

        if (!FileHeader.CheckHeader(reader, "PYGD", true))
            throw new AmbermoonException(ExceptionScope.Data, "The given file is no Pyrdacor game data file.");

        fileHandlers.Add(MagicInfo, LoadInfo);

        // Read all files
        int fileCount = reader.ReadWord();

        for (int i = 0; i < fileCount; ++i)
        {
            var file = reader.ReadString(4);
            int dataLength = (int)(reader.ReadDword() & int.MaxValue);

            if (file == MagicInfo)
            {
                LoadInfo(new DataReader(reader.ReadBytes(dataLength)));
                reader.Position = position;
                Loaded = true;
                return;
            }

            reader.Position += dataLength;
        }

        Loaded = false;
    }

    public static GameDataInfo ReadGameDataInfo(IDataReader reader)
    {
        try
        {
            var gameData = new GameData(reader);

            if (!gameData.Loaded)
                throw new InvalidDataException("No game data info found");

            return gameData.gameDataInfoLoader.Load();
        }
        catch
        {
            throw new InvalidDataException("No valid Pyrdacor game data file.");
        }
    }

    public GameData(IDataReader reader, params (string Magic, Action<IDataReader, GameData> Action)[] customFileHandlers)
    {
        if (!FileHeader.CheckHeader(reader, "PYGD", true))
            throw new AmbermoonException(ExceptionScope.Data, "The given file is no Pyrdacor game data file.");

        // Note: The loaders are all lazy loaded as well as the managers. This allows any order of the loaded
        // file specs as the data is only used when some object is requested by the game. At that point in time
        // all file specs have been loaded from the game data.

        fileHandlers.Add(MagicInfo, LoadInfo);
        fileHandlers.Add(MagicPalette, LoadPalettes);
        fileHandlers.Add(MagicSavegame, LoadSavegame); // Only the Party_data.sav
        fileHandlers.Add(MagicInitialParty, LoadInitialParty);
        fileHandlers.Add(MagicMonsters, LoadMonsters);
        fileHandlers.Add(MagicMonsterGraphics, LoadMonsterGraphics);
        fileHandlers.Add(MagicNPCs, LoadNPCs);
        fileHandlers.Add(MagicNPCTexts, LoadNPCTexts);
        fileHandlers.Add(MagicNPCGraphics, LoadNPCGraphics);
        fileHandlers.Add(MagicGraphicsInfo, LoadGraphicsInfo);
        fileHandlers.Add(MagicPartyTexts, LoadPartyTexts);
        fileHandlers.Add(MagicPartyGraphics, LoadPartyGraphics);
        fileHandlers.Add(MagicTravelGraphics, LoadTravelGraphics);
        fileHandlers.Add(MagicTransportGraphics, LoadTransportGraphics);
        fileHandlers.Add(MagicMonsterGroups, LoadMonsterGroups);
        fileHandlers.Add(MagicItems, LoadItems);
        fileHandlers.Add(MagicItemNames, LoadItemNames);
        fileHandlers.Add(MagicItemTexts, LoadItemTexts);
        fileHandlers.Add(MagicItemGraphics, LoadItemGraphics);
        fileHandlers.Add(MagicLocations, LoadLocations);
        fileHandlers.Add(MagicLocationNames, LoadLocationNames);
        fileHandlers.Add(MagicOutro, LoadOutroSequences);
        fileHandlers.Add(MagicTexts, LoadTexts);
        fileHandlers.Add(MagicTilesets, LoadTilesets);
        fileHandlers.Add(MagicLabyrinthData, LoadLabyrinthData);
        fileHandlers.Add(MagicMaps, LoadMaps);
        fileHandlers.Add(MagicMapTexts, LoadMapTexts);
        fileHandlers.Add(MagicFonts, LoadFonts);
        fileHandlers.Add(MagicGlyphMappings, LoadGlyphMappings);
        fileHandlers.Add(MagicGotoPointNames, LoadGotoPointNames);
        fileHandlers.Add(MagicLayouts, LoadLayoutGraphics);
        fileHandlers.Add(MagicTextures, LoadTextures);
        fileHandlers.Add(MagicEventGraphics, LoadEventGraphics);
        fileHandlers.Add(MagicTileGraphics, LoadTileGraphics);
        fileHandlers.Add(MagicCombatBackgrounds, LoadCombatBackgroundGraphics);
        fileHandlers.Add(MagicCombatGraphics, LoadCombatGraphics);
        fileHandlers.Add(MagicBattleFieldSprites, LoadBattleFieldSprites);
        fileHandlers.Add(MagicPortraits, LoadPortraitGraphics);
        fileHandlers.Add(MagicUIGraphics, LoadUIGraphics);
        fileHandlers.Add(MagicRiddlemouthGraphics, LoadRiddlemouthGraphics);
        fileHandlers.Add(MagicCursors, LoadCursors);
        fileHandlers.Add(MagicPictures80x80, Load80x80Graphics);
        fileHandlers.Add(MagicAutomapGraphics, LoadAutomapGraphics);
        fileHandlers.Add(MagicInitialChests, LoadInitialChests);
        fileHandlers.Add(MagicInitialMerchants, LoadInitialMerchants);
        fileHandlers.Add(MagicInitialAutomaps, LoadInitialAutomaps);        
        fileHandlers.Add(MagicDictionary, LoadDictionary);
        fileHandlers.Add(MagicMusic, LoadMusic);
        fileHandlers.Add(MagicOutroGraphics, LoadOutroGraphics);
        fileHandlers.Add(MagicIntroGraphics, LoadIntroGraphics);
        fileHandlers.Add(MagicFantasyIntroGraphics, LoadFantasyIntroGraphics);
        fileHandlers.Add(MagicOutroTexts, LoadOutroTexts);
        fileHandlers.Add(MagicIntroTexts, LoadIntroTexts);
        fileHandlers.Add(MagicOutroGraphicInfos, LoadOutroGraphicInfos);
        fileHandlers.Add(MagicIntroAssets, LoadIntroAssets);
        fileHandlers.Add(MagicFantasyIntroAssets, LoadFantasyIntroAssets);
        fileHandlers.Add(MagicLightEffectData, LoadLightEffectData);

        foreach (var customFileHandler in customFileHandlers)
        {
            if (fileHandlers.ContainsKey(customFileHandler.Magic))
                throw new ArgumentException($"Custom file handler magic {customFileHandler.Magic} is already used by another file type.");

            void ProvideData(IDataReader dataReader)
            {
                var fileSpec = PADF.Read(dataReader, this);

                if (fileSpec is RawData rawData)
                    customFileHandler.Action?.Invoke(new DataReader(rawData.Data), this);
                else
                    throw new InvalidDataException("Invalid custom file data.");
            }

            fileHandlers.Add(customFileHandler.Magic, ProvideData);
        }

        dictionary = new Lazy<TextDictionary>(() => TextDictionary.Load(gameDataInfoLoader.Load().Language, dictionaryLoader.Load().ToList()));

        songManager = new Lazy<SongManager>(() => new SongManager(musicLoader.LoadAll()));

        initialSavegame = new Lazy<Savegame>(() => LoadInitial());

        characterManager = new Lazy<ICharacterManager>(() => new CharacterManager
        (
            () => partyLoader!.LoadAll(),
            () => npcLoader!.LoadAll(),
            () => monsterLoader!.LoadAll(),
            () => monsterGroupLoader!.LoadAll(),
            () => monsterGraphicLoader!.Load()
        ));

        itemManager = new Lazy<IItemManager>(() => new ItemManager
        (
            () => itemLoader!.LoadAll(),
            () => itemTextLoader!.LoadAll()
        ));

        mapManager = new Lazy<IMapManager>(() => new MapManager
        (
            () => mapLoader!.LoadAll(),
            () => mapTextLoader!.LoadAll(),
            () => labdataLoader!.LoadAll(),
            () => texturesLoader!.Load(),
            () => tilesetLoader!.LoadAll()
        ));

        gamePalette = new Lazy<Palette>(() => paletteLoader!.Load(Palette.GamePalettesIndex));

        ingameFont = new Lazy<IngameFont>(() => new IngameFont
        (
            () => fontLoader!.Load(FontData.IngameFontIndex),
            () => fontLoader!.Load(FontData.IngameDigitFontIndex)
        ));

        outroSmallFont = new Lazy<Font>(() => fontLoader!.Load(FontData.OutroSmallFontIndex));
        outroLargeFont = new Lazy<Font>(() => fontLoader!.Load(FontData.OutroLargeFontIndex));
        introSmallFont = new Lazy<Font>(() => fontLoader!.Load(FontData.IntroSmallFontIndex));
        introLargeFont = new Lazy<Font>(() => fontLoader!.Load(FontData.IntroLargeFontIndex));
        ingameFontProvider = new Lazy<IngameFontProvider>(() => new(ingameFont.Value));

        palettes = new Lazy<Dictionary<int, Graphic>>(() =>
        {
            var result = new Dictionary<int, Graphic>();
            var gamePaletteInfo = paletteLoader!.Load(Palette.GamePalettesIndex);
            var paletteGraphics = gamePaletteInfo.Graphic;

            for (int y = 0; y < paletteGraphics.Height; y++)
            {
                result.Add(1 + y, new Graphic
                {
                    Width = 32,
                    Height = 1,
                    Data = paletteGraphics.Data.Skip(y * 32 * 4).Take(32 * 4).ToArray(),
                    IndexedGraphic = false
                });
            }

            var introPaletteGraphics = paletteLoader!.Load(Palette.IntroPalettesIndex).Graphic;

            for (int y = 0; y < introPaletteGraphics.Height; y++)
            {
                result.Add(gamePaletteInfo.FirstIntroPaletteIndex + y, new Graphic
                {
                    Width = 32,
                    Height = 1,
                    Data = introPaletteGraphics.Data.Skip(y * 32 * 4).Take(32 * 4).ToArray(),
                    IndexedGraphic = false
                });
            }

            var outroPaletteGraphics = paletteLoader!.Load(Palette.OutroPalettesIndex).Graphic;

            for (int y = 0; y < outroPaletteGraphics.Height; y++)
            {
                result.Add(gamePaletteInfo.FirstOutroPaletteIndex + y, new Graphic
                {
                    Width = 32,
                    Height = 1,
                    Data = outroPaletteGraphics.Data.Skip(y * 32 * 4).Take(32 * 4).ToArray(),
                    IndexedGraphic = false
                });
            }

            var fantasyIntroPaletteGraphics = paletteLoader!.Load(Palette.FantasyIntroPalettesIndex).Graphic;

            for (int y = 0; y < fantasyIntroPaletteGraphics.Height; y++)
            {
                result.Add(gamePaletteInfo.FirstFantasyIntroPaletteIndex + y, new Graphic
                {
                    Width = 32,
                    Height = 1,
                    Data = fantasyIntroPaletteGraphics.Data.Skip(y * 32 * 4).Take(32 * 4).ToArray(),
                    IndexedGraphic = false
                });
            }

            return result;
        });

        outroData = new Lazy<IOutroData>(() =>
        {
            var smallGlyphs = outroSmallFont.Value;
            var largeGlyphs = outroSmallFont.Value;
            var smallGlyphMapping = glyphMappingLoader!.Load(GlyphMappingData.OutroSmallGlyphMappingIndex);
            var largeGlyphMapping = glyphMappingLoader!.Load(GlyphMappingData.OutroLargeGlyphMappingIndex);

            return new OutroData
            {
                OutroActions = outroSequenceLoader!.Load(),
                OutroPalettes = paletteLoader.Load(Palette.OutroPalettesIndex).Slice(),
                Graphics = null,
                GraphicAtlas = outroGraphicLoader.Load(),
                Texts = outroTextLoader.Load().ToList(),
                GraphicInfos = outroGraphicsInfoLoader.LoadAll().ToDictionary(info => info.Value.ImageDataOffset, info => new OutroGraphicInfo()
                {
                    GraphicIndex = info.Key - 1,
                    Width = info.Value.Width,
                    Height = info.Value.Height,
                    PaletteIndex = info.Value.PaletteIndex
                }),
                Glyphs = smallGlyphMapping.Mapping.ToDictionary(kv => kv.Key, kv => smallGlyphs.GetGlyph((uint)kv.Value)),
                LargeGlyphs = largeGlyphMapping.Mapping.ToDictionary(kv => kv.Key, kv => largeGlyphs.GetGlyph((uint)kv.Value)),
            };
        });

        introData = new Lazy<IIntroData>(() =>
        {
            var smallGlyphs = introSmallFont.Value;
            var largeGlyphs = introSmallFont.Value;
            var smallGlyphMapping = glyphMappingLoader!.Load(GlyphMappingData.IntroSmallGlyphMappingIndex);
            var largeGlyphMapping = glyphMappingLoader!.Load(GlyphMappingData.IntroLargeGlyphMappingIndex);
            var introAssets = introAssetLoader.Load();
            var graphicSizes = introAssets.GraphicSizes;

            return new IntroData
            {
                IntroPalettes = paletteLoader.Load(Palette.IntroPalettesIndex).Slice(),
                Graphics = introGraphicLoader.Load().ToDictionary<IntroGraphic>(graphicSizes.ToDictionary(e => (uint)e.Key, e => e.Value)),
                Texts = introTextLoader.Load().ToDictionary<IntroText>(),
                Glyphs = smallGlyphMapping.Mapping.ToDictionary(kv => kv.Key, kv => smallGlyphs.GetGlyph((uint)kv.Value)),
                LargeGlyphs = largeGlyphMapping.Mapping.ToDictionary(kv => kv.Key, kv => largeGlyphs.GetGlyph((uint)kv.Value)),
                TwinlakeImageParts = introAssets.TwinlakeImageParts,
                TextCommands = introAssets.TextCommands,
                TextCommandTexts = introAssets.TextCommandTexts,
            };
        });

        fantasyIntroData = new Lazy<IFantasyIntroData>(() =>
        {
            var fantasyIntroAssets = fantasyIntroAssetLoader.Load();
            var graphicSizes = fantasyIntroAssets.GraphicSizes;

            return new FantasyIntroData
            {
                Actions = new(fantasyIntroAssets.Actions),
                FantasyIntroPalettes = paletteLoader.Load(Palette.FantasyIntroPalettesIndex).Slice(),
                Graphics = fantasyIntroGraphicLoader.Load().ToDictionary<FantasyIntroGraphic>(graphicSizes.ToDictionary(e => (uint)e.Key, e => e.Value)),
            };
        });

        places = new Lazy<Places>(() =>
        {
            var places = new Places();
            var locationData = locationLoader!.LoadAll();
            var locationNames = locationNameLoader!.Load().ToDictionary(1);

            if (locationData.Count != locationNames.Count)
                throw new AmbermoonException(ExceptionScope.Data, "Mismatch between number of location data and location name entries.");

            if (locationData.Keys.Min() != 1 || locationData.Keys.Max() != locationData.Count)
                throw new AmbermoonException(ExceptionScope.Data, "Location data must not contain any data gaps and first index must be 1.");

            foreach (var location in locationData.OrderBy(location => location.Key))
            {
                if (!locationNames.TryGetValue((int)location.Key, out var name))
                    throw new AmbermoonException(ExceptionScope.Data, $"Missing location name for location data {location.Key}.");

                location.Value.Name = name;

                places.Entries.Add(location.Value);
            }
            return places;
        });

        dataNameProvider = new Lazy<IDataNameProvider>(() =>
        {
            var info = gameDataInfoLoader.Load();
            var date = info.ReleaseDate;
            var name = info.Name;
            
            if (info.Advanced)
                name += " Advanced";

            var texts = textLoader.LoadAll().ToDictionary(kv => (MessageTextType)(int)kv.Key, kv => (IReadOnlyDictionary<int, string>)kv.Value.ToDictionary().AsReadOnly());

            return new DataNameProvider($"{name} {info.Version}", $"{date.Day:00}-{date.Month:00}-{date.Year:0000} / {info.Language}", texts);
        });

        lightEffectProvider = new Lazy<ILightEffectProvider>(() => new LightEffectProvider(lightEffectDataLoader.Load()));

        // Read all files
        int fileCount = reader.ReadWord();

        for (int i = 0; i < fileCount; ++i)
        {
            var file = reader.ReadString(4);

            if (!fileHandlers.TryGetValue(file, out var loader))
                throw new AmbermoonException(ExceptionScope.Data, $"No loader found for file '{file}' inside game data.");

            int dataLength = (int)(reader.ReadDword() & int.MaxValue);

            loader?.Invoke(new DataReader(reader.ReadBytes(dataLength)));
        }

        Loaded = true;
    }

    internal Tileset GetTileset(uint index) => tilesetLoader.Load((ushort)index);
    internal string? GetGotoPointName(int index) => gotoPointNameLoader.Load().GetText(index);
    internal string? GetItemName(int index) => itemNameLoader.Load().GetText(index);


    #region Loaders

    void LoadInfo(IDataReader dataReader)
    {
        gameDataInfoLoader = new(dataReader, this, p => p);
    }

    void LoadPalettes(IDataReader dataReader)
    {
        paletteLoader = new(dataReader, this, p => p);
    }

    void LoadCursors(IDataReader dataReader)
    {
        cursorGraphicLoader = new(dataReader, this, p => p.Atlas!);
    }

    void LoadSavegame(IDataReader dataReader)
    {
        savegameLoader = new(dataReader, this, p => p.Savegame);
    }

    void LoadInitialParty(IDataReader dataReader)
    {
        partyLoader = new(dataReader, this, n => (n.Character as PartyMember)!);
    }

    void LoadMonsters(IDataReader dataReader)
    {
        monsterLoader = new(dataReader, this, m => (m.Character as Monster)!);
    }

    void LoadNPCs(IDataReader dataReader)
    {
        npcLoader = new(dataReader, this, n => (n.Character as NPC)!);
    }

    void LoadNPCTexts(IDataReader dataReader)
    {
        npcTextLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadPartyTexts(IDataReader dataReader)
    {
        partyTextLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadMonsterGroups(IDataReader dataReader)
    {
        monsterGroupLoader = new(dataReader, this, g => g.MonsterGroup);
    }

    void LoadItems(IDataReader dataReader)
    {
        itemLoader = new(dataReader, this, i => i.Item);
    }

    void LoadItemNames(IDataReader dataReader)
    {
        itemNameLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadItemTexts(IDataReader dataReader)
    {
        itemTextLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadLocations(IDataReader dataReader)
    {
        locationLoader = new(dataReader, this, l => l.Place);
    }

    void LoadLocationNames(IDataReader dataReader)
    {
        locationNameLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadOutroSequences(IDataReader dataReader)
    {
        outroSequenceLoader = new(dataReader, this, o => o.Sequences);
    }

    void LoadTexts(IDataReader dataReader)
    {
        textLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadTilesets(IDataReader dataReader)
    {
        tilesetLoader = new(dataReader, this, t => t.Tileset);
    }

    void LoadLabyrinthData(IDataReader dataReader)
    {
        labdataLoader = new(dataReader, this, l => l.Labdata);
    }

    void LoadMaps(IDataReader dataReader)
    {
        mapLoader = new(dataReader, this, m => m.Map);
    }

    void LoadMapTexts(IDataReader dataReader)
    {
        mapTextLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadFonts(IDataReader dataReader)
    {
        fontLoader = new(dataReader, this, f => f.Font);
    }

    void LoadGlyphMappings(IDataReader dataReader)
    {
        glyphMappingLoader = new(dataReader, this, g => g.GlyphMapping);
    }

    void LoadGotoPointNames(IDataReader dataReader)
    {
        gotoPointNameLoader = new(dataReader, this, n => n.TextList);
    }

    void LoadGraphicsInfo(IDataReader dataReader)
    {
        graphicsInfoLoader = new(dataReader, this, g => g);
    }

    void LoadTileGraphics(IDataReader dataReader)
    {
        tileGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadTextures(IDataReader dataReader)
    {
        texturesLoader = new(dataReader, this, g => g);
    }

    void LoadAutomapGraphics(IDataReader dataReader)
    {
        automapGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadLayoutGraphics(IDataReader dataReader)
    {
        layoutGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadUIGraphics(IDataReader dataReader)
    {
        uiGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadRiddlemouthGraphics(IDataReader dataReader)
    {
        riddlemouthGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadBattleFieldSprites(IDataReader dataReader)
    {
        battleFieldSpriteLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadCombatBackgroundGraphics(IDataReader dataReader)
    {
        combatBackgroudLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadCombatGraphics(IDataReader dataReader)
    {
        combatGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }
        
    void LoadDictionary(IDataReader dataReader)
    {
        dictionaryLoader = new(dataReader, this, d => d.TextList);
    }

    void LoadEventGraphics(IDataReader dataReader)
    {
        eventGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void Load80x80Graphics(IDataReader dataReader)
    {
        pics80x80GraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadMonsterGraphics(IDataReader dataReader)
    {
        monsterGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadNPCGraphics(IDataReader dataReader)
    {
        npcGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadPartyGraphics(IDataReader dataReader)
    {
        playerGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadTravelGraphics(IDataReader dataReader)
    {
        travelGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadTransportGraphics(IDataReader dataReader)
    {
        transportGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadItemGraphics(IDataReader dataReader)
    {
        itemGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadPortraitGraphics(IDataReader dataReader)
    {
        portraitGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadMusic(IDataReader dataReader)
    {
        musicLoader = new(dataReader, this, m => m.SongData);
    }

    void LoadInitialChests(IDataReader dataReader)
    {
        initialChestLoader = new(dataReader, this, c => c.Chest);
    }

    void LoadInitialMerchants(IDataReader dataReader)
    {
        initialMerchantLoader = new(dataReader, this, m => m.Merchant);
    }

    void LoadInitialAutomaps(IDataReader dataReader)
    {
        initialAutomapLoader = new(dataReader, this, m => m.Automap);
    }

    void LoadOutroGraphics(IDataReader dataReader)
    {
        outroGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadIntroGraphics(IDataReader dataReader)
    {
        introGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadFantasyIntroGraphics(IDataReader dataReader)
    {
        fantasyIntroGraphicLoader = new(dataReader, this, g => g.Atlas!);
    }

    void LoadOutroTexts(IDataReader dataReader)
    {
        outroTextLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadIntroTexts(IDataReader dataReader)
    {
        introTextLoader = new(dataReader, this, t => t.TextList);
    }

    void LoadOutroGraphicInfos(IDataReader dataReader)
    {
        outroGraphicsInfoLoader = new(dataReader, this, t => t.OutroGraphicInfo);
    }

    void LoadIntroAssets(IDataReader dataReader)
    {
        introAssetLoader = new(dataReader, this, t => t.Assets);
    }

    void LoadFantasyIntroAssets(IDataReader dataReader)
    {
        fantasyIntroAssetLoader = new(dataReader, this, t => t.Assets);
    }

    void LoadLightEffectData(IDataReader dataReader)
    {
        lightEffectDataLoader = new(dataReader, this, t => t.LightEffectDataProvider);
    }    

    public CombatBackgroundInfo Get2DCombatBackground(uint index, bool advanced)
    {
        if (advanced && CombatBackgrounds.AdvancedReplacements2D.TryGetValue(index, out var info))
            return info;

        return CombatBackgrounds.Info2D[index];
    }

    public CombatBackgroundInfo Get3DCombatBackground(uint index, bool advanced)
    {
        if (advanced && CombatBackgrounds.AdvancedReplacements3D.TryGetValue(index, out var info))
            return info;

        return CombatBackgrounds.Info3D[index];
    }

    public CombatGraphicInfo GetCombatGraphicInfo(CombatGraphicIndex index)
    {
        return graphicsInfoLoader.Load().CombatGraphicInfos[index];
    }

    public float GetMonsterRowImageScaleFactor(MonsterRow row) => row switch
    {
        MonsterRow.Farthest => 0.7f,
        MonsterRow.Far => 0.8f,
        MonsterRow.Near => 1.25f,
        _ => 1.0f,
    };

    static readonly byte[] ColorIndexMapping =
    [
        0x00, 0x1F, 0x1E, 0x1D, 0x1C, 0x1B, 0x1A, 0x12, 0x13, 0x14, 0x11, 0x10, 0x09, 0x0A, 0x18, 0x17,
        0x00, 0x01, 0x1F, 0x12, 0x1C, 0x14, 0x15, 0x06, 0x08, 0x0A, 0x04, 0x02, 0x0E, 0x0C, 0x13, 0x10,
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F
    ];

    public byte PaletteIndexFromColorIndex(Map map, byte colorIndex)
    {
        int offset = map.Type == MapType.Map3D ? 32 : map.IsWorldMap ? 16 : 0;

        return ColorIndexMapping[offset + colorIndex % 16];
    }

    public IGraphicAtlas GetGraphicAtlas(GraphicType type)
    {
        return type switch
        {
            >= GraphicType.Tileset1 => tileGraphicLoader.LoadOrDefault((ushort)(1 + type - GraphicType.Tileset1), new GraphicAtlas()),
            GraphicType.Player => playerGraphicLoader.Load(),
            GraphicType.Portrait => portraitGraphicLoader.Load(),
            GraphicType.Item => itemGraphicLoader.Load(),
            GraphicType.Layout => layoutGraphicLoader.Load(),
            GraphicType.LabBackground => throw new NotImplementedException("Use GetLabBackgroundGraphics instead"), // Should not be used when using current GameData
            GraphicType.Cursor => cursorGraphicLoader.Load(),
            GraphicType.Pics80x80 => pics80x80GraphicLoader.Load(),
            GraphicType.UIElements => uiGraphicLoader.Load(),
            GraphicType.EventPictures => eventGraphicLoader.Load(),
            GraphicType.TravelGfx => travelGraphicLoader.Load(),
            GraphicType.Transports => transportGraphicLoader.Load(),
            GraphicType.NPC => npcGraphicLoader.Load(),
            GraphicType.CombatBackground => combatBackgroudLoader.Load(),
            GraphicType.CombatGraphics => combatGraphicLoader.Load(),
            GraphicType.BattleFieldIcons => battleFieldSpriteLoader.Load(),
            GraphicType.AutomapGraphics => automapGraphicLoader.Load(),
            GraphicType.RiddlemouthGraphics => riddlemouthGraphicLoader.Load(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public IReadOnlyList<Graphic> GetLabBackgroundGraphics() => texturesLoader.Load().BackgroundGraphics;

    #endregion
}
