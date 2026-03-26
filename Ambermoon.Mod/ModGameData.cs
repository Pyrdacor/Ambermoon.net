using System.Reflection;
using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Pyrdacor;
using Ambermoon.Data.Serialization;
using Ambermoon.Render;

namespace Ambermoon.Mod;

public class ModGameData : IModGameData
{
    const string ModSpecMagic = "MODS";
    private readonly GameData gameData;

    public ICoreConfiguration CoreConfiguration { get; }

    public string ModDirectory { get; }

    public ISavegameManager SavegameManager => gameData.SavegameManager!;

    public ModGameData(IDataReader dataReader, string modDirectory, ICoreConfiguration coreConfiguration)
    {
        CoreConfiguration = coreConfiguration;
        ModDirectory = modDirectory;

        gameData = new GameData(dataReader, (ModSpecMagic, LoadMod));

        if (Mod == null)
            throw new IOException("No mod file found inside game data.");
    }

    private void LoadMod(IDataReader dataReader, GameData gameData)
    {
        var modAssembly = Assembly.Load(dataReader.ReadToEnd());
        var modInterfaceName = typeof(IMod).FullName!;
        var modType = modAssembly.GetExportedTypes().FirstOrDefault(mod => mod.GetInterface(modInterfaceName) != null)
            ?? throw new IOException("No mod class exported from mod assembly.");

        Mod = (IMod)Activator.CreateInstance(modType)!;

        gameData.SavegameManager = Mod.CustomSavegameManager ?? new DefaultModSavegameManager(this);
    }

    public static void Write(IDataWriter dataWriter, Data.Legacy.GameData baseGameData, string modAssemblyPath,
        GameLanguage language, bool advanced = false)
    {
        var modAssembly = Assembly.LoadFrom(modAssemblyPath);
        var modInterfaceName = typeof(IMod).FullName!;
        var modType = modAssembly.GetExportedTypes().FirstOrDefault(mod => mod.GetInterface(modInterfaceName) != null)
            ?? throw new IOException("No mod class exported from mod assembly.");
        var mod = (IMod)Activator.CreateInstance(modType)!;
        var modInfo = mod.Info;

        GameData.WriteLegacyGameData(dataWriter, baseGameData,
            (GameData.MagicInfo, dataWriter => GameData.WriteGameDataInfo(dataWriter, modInfo.Name, advanced, modInfo.Version.ToString(), language, modInfo.ReleaseDate)),
            (ModSpecMagic, dataWriter => GameData.WriteFile(dataWriter, File.ReadAllBytes(modAssemblyPath), true))
        );
    }

    public IMod Mod { get; private set; } = null!;
    public bool Loaded => gameData.Loaded;
    public GameDataSource GameDataSource { get; } = GameDataSource.Memory;
    public GameLanguage Language => gameData.Language;
    public bool Advanced { get; } = false;
    public IReadOnlyDictionary<TravelType, GraphicInfo> StationaryImageInfos => gameData.StationaryImageInfos;
    public Character2DAnimationInfo PlayerAnimationInfo => gameData.PlayerAnimationInfo;
    public IReadOnlyList<Position> CursorHotspots => gameData.CursorHotspots;
    public Places Places => gameData.Places;
    public IItemManager ItemManager => gameData.ItemManager;
    public IGraphicInfoProvider GraphicInfoProvider => gameData.GraphicInfoProvider;
    public IFontProvider FontProvider => gameData.FontProvider;
    public IDataNameProvider DataNameProvider => gameData.DataNameProvider;
    public ILightEffectProvider LightEffectProvider => gameData.LightEffectProvider;
    public IMapManager MapManager => gameData.MapManager;
    public ISongManager SongManager => gameData.SongManager;
    public ICharacterManager CharacterManager => gameData.CharacterManager;
    public IIntroData IntroData => gameData.IntroData;
    public IFantasyIntroData FantasyIntroData => gameData.FantasyIntroData;
    public IOutroData OutroData => gameData.OutroData;
    public TextDictionary Dictionary => gameData.Dictionary;

    public TravelGraphicInfo GetTravelGraphicInfo(TravelType type, CharacterDirection direction)
        => gameData.GetTravelGraphicInfo(type, direction);

    public Savegame LoadInitial() => gameData.InitialSavegame;
}
