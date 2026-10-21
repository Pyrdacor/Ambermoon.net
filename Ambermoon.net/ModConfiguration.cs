using Ambermoon.Data;
using Ambermoon.Game;
using Ambermoon.Mod;
using Ambermoon.Render;
using System;
using System.Runtime.CompilerServices;

namespace Ambermoon;

/// <summary>
/// The configuration the game uses while a mod with option defaults is active.
/// The options a mod can provide defaults for (see <see cref="ModOptions.AllowedOptions"/>)
/// are read from the mod options first and from the normal configuration otherwise.
/// Changes are always written to the normal configuration and remove the mod default.
/// All other options are passed through unchanged.
/// </summary>
internal class ModConfiguration(IConfiguration configuration, ModOptions modOptions) : IConfiguration
{
    readonly IConfiguration configuration = configuration;
    readonly ModOptions modOptions = modOptions;

    T Get<T>(T value, [CallerMemberName] string name = "") => modOptions.TryGet<T>(name, out var modValue) ? modValue : value;

    void Changed([CallerMemberName] string name = "") => modOptions.Remove(name);

    // Options which a mod can provide defaults for
    public int BattleSpeed { get => Get(configuration.BattleSpeed); set { configuration.BattleSpeed = value; Changed(); } }
    public int AutoBattleRounds { get => Get(configuration.AutoBattleRounds); set { configuration.AutoBattleRounds = value; Changed(); } }
    public bool AutoDerune { get => Get(configuration.AutoDerune); set { configuration.AutoDerune = value; Changed(); } }
    public bool ShowButtonTooltips { get => Get(configuration.ShowButtonTooltips); set { configuration.ShowButtonTooltips = value; Changed(); } }
    public bool ShowPlayerStatsTooltips { get => Get(configuration.ShowPlayerStatsTooltips); set { configuration.ShowPlayerStatsTooltips = value; Changed(); } }
    public bool ShowFloor { get => Get(configuration.ShowFloor); set { configuration.ShowFloor = value; Changed(); } }
    public bool ShowCeiling { get => Get(configuration.ShowCeiling); set { configuration.ShowCeiling = value; Changed(); } }
    public bool ShowFog { get => Get(configuration.ShowFog); set { configuration.ShowFog = value; Changed(); } }
    public bool ShowSaveLoadMessage { get => Get(configuration.ShowSaveLoadMessage); set { configuration.ShowSaveLoadMessage = value; Changed(); } }
    public Movement3D Movement3D { get => Get(configuration.Movement3D); set { configuration.Movement3D = value; Changed(); } }
    public bool TurnWithArrowKeys { get => Get(configuration.TurnWithArrowKeys); set { configuration.TurnWithArrowKeys = value; Changed(); } }

    // All other options
    public bool FirstStart { get => configuration.FirstStart; set => configuration.FirstStart = value; }
    public bool IsMobile => configuration.IsMobile;
    public int? Width { get => configuration.Width; set => configuration.Width = value; }
    public int? Height { get => configuration.Height; set => configuration.Height = value; }
    public int? FullscreenWidth { get => configuration.FullscreenWidth; set => configuration.FullscreenWidth = value; }
    public int? FullscreenHeight { get => configuration.FullscreenHeight; set => configuration.FullscreenHeight = value; }
#pragma warning disable CS0618, CS0672
    public bool? Fullscreen { get => configuration.Fullscreen; set => configuration.Fullscreen = value; }
    public bool? FastBattleMode { get => configuration.FastBattleMode; set => configuration.FastBattleMode = value; }
    public bool? CacheMusic { get => configuration.CacheMusic; set => configuration.CacheMusic = value; }
    public bool? UseGraphicFilter { get => configuration.UseGraphicFilter; set => configuration.UseGraphicFilter = value; }
    public bool? ShowThalionLogo { get => configuration.ShowThalionLogo; set => configuration.ShowThalionLogo = value; }
    public string[] AdditionalSavegameNames { get => configuration.AdditionalSavegameNames; set => configuration.AdditionalSavegameNames = value; }
    public int? ContinueSavegameSlot { get => configuration.ContinueSavegameSlot; set => configuration.ContinueSavegameSlot = value; }
#pragma warning restore CS0618, CS0672
    public WindowMode WindowMode { get => configuration.WindowMode; set => configuration.WindowMode = value; }
    public int GameVersionIndex { get => configuration.GameVersionIndex; set => configuration.GameVersionIndex = value; }
    public bool Music { get => configuration.Music; set => configuration.Music = value; }
    public int Volume { get => configuration.Volume; set => configuration.Volume = value; }
    public bool EnableCheats { get => configuration.EnableCheats; set => configuration.EnableCheats = value; }
    public GraphicFilter GraphicFilter { get => configuration.GraphicFilter; set => configuration.GraphicFilter = value; }
    public GraphicFilterOverlay GraphicFilterOverlay { get => configuration.GraphicFilterOverlay; set => configuration.GraphicFilterOverlay = value; }
    public Effects Effects { get => configuration.Effects; set => configuration.Effects = value; }
    public bool ShowPyrdacorLogo { get => configuration.ShowPyrdacorLogo; set => configuration.ShowPyrdacorLogo = value; }
    public bool ShowCompletedQuests { get => configuration.ShowCompletedQuests; set => configuration.ShowCompletedQuests = value; }
    public GameLanguage Language { get => configuration.Language; set => configuration.Language = value; }
    public bool LegacyMode { get => configuration.LegacyMode; set => configuration.LegacyMode = value; }
    public bool ExtendedSavegameSlots { get => configuration.ExtendedSavegameSlots; set => configuration.ExtendedSavegameSlots = value; }
    public bool ShowAdvancedLogo { get => configuration.ShowAdvancedLogo; set => configuration.ShowAdvancedLogo = value; }
    public bool ShowFantasyIntro { get => configuration.ShowFantasyIntro; set => configuration.ShowFantasyIntro = value; }
    public bool ShowIntro { get => configuration.ShowIntro; set => configuration.ShowIntro = value; }
    public bool ExternalMusic { get => configuration.ExternalMusic; set => configuration.ExternalMusic = value; }
    public string ExternalMusicPath { get => configuration.ExternalMusicPath; set => configuration.ExternalMusicPath = value; }
    public bool UseDataPath { get => configuration.UseDataPath; set => configuration.UseDataPath = value; }
    public string DataPath { get => configuration.DataPath; set => configuration.DataPath = value; }
    public SaveOption SaveOption { get => configuration.SaveOption; set => configuration.SaveOption = value; }
    public AdditionalSavegameSlots[] AdditionalSavegameSlots { get => configuration.AdditionalSavegameSlots; set => configuration.AdditionalSavegameSlots = value; }

    public AdditionalSavegameSlots GetOrCreateCurrentAdditionalSavegameSlots(string gameVersionName)
        => configuration.GetOrCreateCurrentAdditionalSavegameSlots(gameVersionName);

    public event Action SaveRequested
    {
        add => configuration.SaveRequested += value;
        remove => configuration.SaveRequested -= value;
    }

    public void RequestSave() => configuration.RequestSave();
}
