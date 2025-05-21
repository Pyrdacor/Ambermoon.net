using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.FileSpecs;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;

namespace Ambermoon.Data.Pyrdacor;

public class GameData : IGameData, IGraphicProvider
{
    LazyFileLoader<Palette, Graphic> paletteLoader;
    LazyContainerLoader<FontData, Font> fontLoader;
    LazyContainerLoader<MonsterGroups, MonsterGroup> monsterGroupLoader;
    LazyContainerLoader<CharacterData, Monster> monsterLoader;
    LazyContainerLoader<CharacterData, NPC> npcLoader;
    LazyContainerLoader<Texts, TextList> npcTextLoader;
    LazyContainerLoader<Texts, TextList> partyTextLoader;
    LazyContainerLoader<ItemData, Item> itemLoader;
    LazyContainerLoader<Texts, string> itemNameLoader;
    LazyContainerLoader<Texts, TextList> itemTextLoader;
    LazyContainerLoader<LocationData, Place> locationLoader;
    LazyContainerLoader<Texts, string> locationNameLoader;
    LazyContainerLoader<TilesetData, Tileset> tilesetLoader;
    LazyFileLoader<Texts, TextList> gotoPointNameLoader;
    readonly Dictionary<string, Action<IDataReader>> fileHandlers = [];
    readonly Lazy<SongManager> songManager;
    readonly Lazy<ICharacterManager> characterManager;
    readonly Lazy<IItemManager> itemManager;
    readonly Lazy<ISavegameManager> savegameManager;
    readonly Lazy<IngameFont> ingameFont;
    readonly Lazy<Font> outroSmallFont;
    readonly Lazy<Font> outroLargeFont;
    readonly Lazy<Font> introSmallFont;
    readonly Lazy<Font> introLargeFont;
    readonly Lazy<Places> places;
    readonly Lazy<IngameFontProvider> ingameFontProvider;

    public bool Loaded { get; } = false;

    public GameDataSource GameDataSource => GameDataSource.Memory;

    public bool Advanced { get; private set; }

    public ICharacterManager CharacterManager => characterManager!.Value;

    public ISavegameManager SavegameManager => savegameManager!.Value;

    public ISongManager SongManager => songManager!.Value;

    // TODO
    public Dictionary<TravelType, GraphicInfo> StationaryImageInfos => throw new NotImplementedException();

    // TODO
    public Character2DAnimationInfo PlayerAnimationInfo => throw new NotImplementedException();

    public IReadOnlyList<Position> CursorHotspots => throw new NotImplementedException();

    public Places Places => places!.Value;

    public IItemManager ItemManager => itemManager!.Value;

    public IFontProvider FontProvider => ingameFontProvider!.Value;

    public IDataNameProvider DataNameProvider => throw new NotImplementedException();

    public ILightEffectProvider LightEffectProvider => throw new NotImplementedException();

    public IMapManager MapManager => throw new NotImplementedException();

    public IGraphicProvider GraphicProvider => this;

    public IIntroData IntroData => throw new NotImplementedException();

    public IFantasyIntroData FantasyIntroData => throw new NotImplementedException();

    public IOutroData OutroData => throw new NotImplementedException();

    public TextDictionary Dictionary => throw new NotImplementedException();

    public Dictionary<int, Graphic> Palettes => throw new NotImplementedException();

    public Dictionary<int, int> NPCGraphicOffsets => throw new NotImplementedException();

    public byte DefaultTextPaletteIndex => throw new NotImplementedException();

    public byte PrimaryUIPaletteIndex => throw new NotImplementedException();

    public byte SecondaryUIPaletteIndex => throw new NotImplementedException();

    public byte AutomapPaletteIndex => throw new NotImplementedException();

    public byte FirstIntroPaletteIndex => throw new NotImplementedException();

    public byte FirstOutroPaletteIndex => throw new NotImplementedException();

    public byte FirstFantasyIntroPaletteIndex => throw new NotImplementedException();

    public Dictionary<int, List<int>> NPCGraphicFrameCounts => throw new NotImplementedException();

    // TODO
    public TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction)
    {
        throw new NotImplementedException();
    }

    const string MagicPalette = "PALS";
    const string MagicSavegame = "SAVE";
    const string MagicMonsters = "MONS";
    const string MagicNPCs = "NPCS";
    const string MagicNPCTexts = "NTXT";
    const string MagicPartyTexts = "PTXT";
    const string MagicMonsterGroups = "MOGS";
    const string MagicItems = "ITEM";
    const string MagicItemNames = "INAM";
    const string MagicItemTexts = "ITXT";
    const string MagicLocations = "LOCS";
    const string MagicLocationNames = "LNAM";
    const string MagicOutro = "OUTR";
    const string MagicTexts = "TEXT";
    const string MagicTilesets = "TILE";
    const string MagicLabyrinthData = "LABY";
    const string MagicMaps = "MAPS";
    const string MagicMapTexts = "MTXT";
    const string MagicFonts = "FONT";
    const string MagicGotoPointNames = "GOTO";

    public GameData(Stream stream)
    {
        var reader = new DataReader(stream);

        if (!FileHeader.CheckHeader(reader, "PYGD", true))
            throw new AmbermoonException(ExceptionScope.Data, "The given file is no Pyrdacor game data file.");

        // Note: The loaders are all lazy loaded as well as the managers. This allows any order of the loaded
        // file specs as the data is only used when some object is requested by the game. At that point in time
        // all file specs have been loaded from the game data.

        fileHandlers.Add(MagicPalette, LoadPalettes);
        fileHandlers.Add(MagicSavegame, LoadSavegame);
        fileHandlers.Add(MagicMonsters, LoadMonsters);
        fileHandlers.Add(MagicNPCs, LoadNPCs);
        fileHandlers.Add(MagicNPCTexts, LoadNPCTexts);
        fileHandlers.Add(MagicPartyTexts, LoadPartyTexts);
        fileHandlers.Add(MagicMonsterGroups, LoadMonsterGroups);
        fileHandlers.Add(MagicItems, LoadItems);
        fileHandlers.Add(MagicItemNames, LoadItemNames);
        fileHandlers.Add(MagicItemTexts, LoadItemTexts);
        fileHandlers.Add(MagicLocations, LoadLocations);
        fileHandlers.Add(MagicLocationNames, LoadLocationNames);
        fileHandlers.Add(MagicOutro, LoadOutro);
        fileHandlers.Add(MagicTexts, LoadTexts);
        fileHandlers.Add(MagicTilesets, LoadTilesets);
        fileHandlers.Add(MagicLabyrinthData, LoadLabyrinthData);
        fileHandlers.Add(MagicMaps, LoadMaps);
        fileHandlers.Add(MagicMapTexts, LoadMapTexts);
        fileHandlers.Add(MagicFonts, LoadFonts);
        fileHandlers.Add(MagicGotoPointNames, LoadGotoPointNames);

        characterManager = new Lazy<ICharacterManager>(() => new CharacterManager
        (
            () => npcLoader!.LoadAll(),
            () => monsterLoader!.LoadAll(),
            () => monsterGroupLoader!.LoadAll()
        ));

        itemManager = new Lazy<IItemManager>(() => new ItemManager
        (
            () => itemLoader!.LoadAll(),
            () => itemTextLoader!.LoadAll()
        ));

        ingameFont = new Lazy<IngameFont>(() => new IngameFont
        (
            () => fontLoader!.Load(FontData.IngameFontIndex),
            () => fontLoader!.Load(FontData.IngameDigitFontIndex)
        ));

        outroSmallFont = new Lazy<Font>(() => fontLoader!.Load(FontData.OutroSmallFontIndex));
        outroLargeFont = new Lazy<Font>(() => fontLoader!.Load(FontData.OutroLargeFontIndex));
        introSmallFont = new Lazy<Font>(() => fontLoader!.Load(FontData.IntroSmallFontIndex));
        introLargeFont = new Lazy<Font>(() => fontLoader!.Load(FontData.IntroLargeFontIndex));
        ingameFontProvider = new Lazy<IngameFontProvider>(() => new(ingameFont!.Value));
        places = new Lazy<Places>(() =>
        {
            var places = new Places();
            var locationData = locationLoader!.LoadAll();
            var locationNames = locationNameLoader!.LoadAll();

            if (locationData.Count != locationNames.Count)
                throw new AmbermoonException(ExceptionScope.Data, "Mismatch between number of location data and location name entries.");

            if (locationData.Keys.Min() != 1 || locationData.Keys.Max() != locationData.Count)
                throw new AmbermoonException(ExceptionScope.Data, "Location data must not contain any data gaps and first index must be 1.");

            foreach (var location in locationData.OrderBy(location => location.Key))
            {
                if (!locationNames.TryGetValue(location.Key, out var name))
                    throw new AmbermoonException(ExceptionScope.Data, $"Missing location name for location data {location.Key}.");

                location.Value.Name = name;

                places.Entries.Add(location.Value);
            }
            return places;
        });

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


    #region Loaders

    void LoadPalettes(IDataReader dataReader)
    {
        paletteLoader = new LazyFileLoader<Palette, Graphic>(dataReader, this, p => p.Graphic);
    }

    void LoadSavegame(IDataReader dataReader)
    {
        // TODO
        throw new NotImplementedException();
    }

    void LoadMonsters(IDataReader dataReader)
    {
        monsterLoader = new LazyContainerLoader<CharacterData, Monster>(dataReader, this, m => (m.Character as Monster)!);
    }

    void LoadNPCs(IDataReader dataReader)
    {
        npcLoader = new LazyContainerLoader<CharacterData, NPC>(dataReader, this, n => (n.Character as NPC)!);
    }

    void LoadNPCTexts(IDataReader dataReader)
    {
        npcTextLoader = new LazyContainerLoader<Texts, TextList>(dataReader, this, t => t.TextList);
    }

    void LoadPartyTexts(IDataReader dataReader)
    {
        partyTextLoader = new LazyContainerLoader<Texts, TextList>(dataReader, this, t => t.TextList);
    }

    void LoadMonsterGroups(IDataReader dataReader)
    {
        monsterGroupLoader = new LazyContainerLoader<MonsterGroups, MonsterGroup>(dataReader, this, g => g.MonsterGroup);
    }

    void LoadItems(IDataReader dataReader)
    {
        itemLoader = new LazyContainerLoader<ItemData, Item>(dataReader, this, i => i.Item);
    }

    void LoadItemNames(IDataReader dataReader)
    {
        itemNameLoader = new LazyContainerLoader<Texts, string>(dataReader, this, t => t.TextList.First()!);
    }

    void LoadItemTexts(IDataReader dataReader)
    {
        itemTextLoader = new LazyContainerLoader<Texts, TextList>(dataReader, this, t => t.TextList);
    }

    void LoadLocations(IDataReader dataReader)
    {
        locationLoader = new LazyContainerLoader<LocationData, Place>(dataReader, this, l => l.Place);
    }

    void LoadLocationNames(IDataReader dataReader)
    {
        locationNameLoader = new LazyContainerLoader<Texts, string>(dataReader, this, t => t.TextList.First()!);
    }

    void LoadOutro(IDataReader dataReader)
    {

    }

    void LoadTexts(IDataReader dataReader)
    {

    }

    void LoadTilesets(IDataReader dataReader)
    {
        tilesetLoader = new LazyContainerLoader<TilesetData, Tileset>(dataReader, this, t => t.Tileset);
    }

    void LoadLabyrinthData(IDataReader dataReader)
    {

    }

    void LoadMaps(IDataReader dataReader)
    {

    }

    void LoadMapTexts(IDataReader dataReader)
    {

    }

    void LoadFonts(IDataReader dataReader)
    {
        fontLoader = new LazyContainerLoader<FontData, Font>(dataReader, this, f => f.Font);
    }

    void LoadGotoPointNames(IDataReader dataReader)
    {
        gotoPointNameLoader = new LazyFileLoader<Texts, TextList>(dataReader, this, n => n.TextList);
    }

    void LoadTileGraphics(IDataReader dataReader)
    {

    }

    void Load3DObjectGraphics(IDataReader dataReader)
    {

    }

    void Load3DWallGraphics(IDataReader dataReader)
    {

    }

    void Load3DOverlayGraphics(IDataReader dataReader)
    {

    }

    void Load3DFloorGraphics(IDataReader dataReader)
    {

    }

    void LoadAutomapGraphics(IDataReader dataReader)
    {

    }

    void LoadLayoutGraphics(IDataReader dataReader)
    {

    }

    // These include the riddlemouth, combat and button graphics but not the battle field player/monster sprites and not the layouts!
    void LoadUIGraphics(IDataReader dataReader)
    {

    }

    void LoadBattleFieldSprites(IDataReader dataReader)
    {

    }

    void LoadCombatBackgroundGraphics(IDataReader dataReader)
    {

    }

    void LoadDictionary(IDataReader dataReader)
    {

    }

    void LoadEventGraphics(IDataReader dataReader)
    {

    }

    void Load80x80Graphics(IDataReader dataReader)
    {

    }

    void LoadHorizonGraphics(IDataReader dataReader)
    {

    }

    void LoadMonsterGraphics(IDataReader dataReader)
    {

    }

    void LoadNPCGraphics(IDataReader dataReader)
    {

    }

    void LoadTravelGraphics(IDataReader dataReader)
    {

    }

    void LoadPartyGraphics(IDataReader dataReader)
    {

    }

    void LoadItemGraphics(IDataReader dataReader)
    {

    }

    void LoadPortraitGraphics(IDataReader dataReader)
    {

    }

    void LoadStationaryGraphics(IDataReader dataReader)
    {

    }

    void LoadMusic(IDataReader dataReader)
    {

    }

    public List<Graphic> GetGraphics(GraphicType type)
    {
        throw new NotImplementedException();
    }

    public CombatBackgroundInfo Get2DCombatBackground(uint index)
    {
        throw new NotImplementedException();
    }

    public CombatBackgroundInfo Get3DCombatBackground(uint index)
    {
        throw new NotImplementedException();
    }

    public CombatGraphicInfo GetCombatGraphicInfo(CombatGraphicIndex index)
    {
        throw new NotImplementedException();
    }

    public float GetMonsterRowImageScaleFactor(MonsterRow row)
    {
        throw new NotImplementedException();
    }

    public byte PaletteIndexFromColorIndex(Map map, byte colorIndex)
    {
        throw new NotImplementedException();
    }

    public CombatBackgroundInfo Get2DCombatBackground(uint index, bool advanced)
    {
        throw new NotImplementedException();
    }

    public CombatBackgroundInfo Get3DCombatBackground(uint index, bool advanced)
    {
        throw new NotImplementedException();
    }

    #endregion
}
