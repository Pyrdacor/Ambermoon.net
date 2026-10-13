using Ambermoon.Data;
using Ambermoon.Mod.Packaging;

namespace Ambermoon.Mod;

public class ModEventArgs(Map map, Event @event, EventTrigger trigger, uint x, uint y)
{
    public Map Map { get; } = map;
    public Event Event { get; } = @event;
    public EventTrigger Trigger { get; } = trigger;
    public uint X { get; } = x;
    public uint Y { get; } = y;
    /// <summary>
    /// Set to true to skip the event (and the rest of its chain).
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// The interface between a code mod and the game.
///
/// Data access (maps, monsters, items) is available all the time.
/// Game actions (teleport, battles, messages) are only available while
/// ingame (<see cref="IsIngame"/>).
/// </summary>
public class ModApi
{
    GameCore? game;

    internal ModApi(ModManifest manifest, IModSource source, IGameData gameData)
    {
        Manifest = manifest;
        Source = source;
        GameData = gameData;
    }

    internal void AttachGame(GameCore game) => this.game = game;

    #region Events

    public event Action<Savegame, bool>? GameStarting;
    public event Action<bool>? GameStarted;
    public event Action<double>? Update;
    public event Action<Map>? MapChanged;
    public event Action<Map, uint, uint, bool>? PlayerMoved;
    public event Action<ModEventArgs>? EventExecuting;
    public event Action<uint>? BattleStarting;
    public event Action<uint, bool, bool>? BattleEnded;
    public event Action<PartyMember>? LevelUp;
    public event Action<PartyMember>? PartyMemberJoined;
    public event Action<PartyMember>? PartyMemberLeft;
    public event Action<ModSaveData>? Saving;
    public event Action<ModSaveData>? Loading;

    internal void RaiseGameStarting(Savegame savegame, bool newGame) => GameStarting?.Invoke(savegame, newGame);
    internal void RaiseGameStarted(bool newGame) => GameStarted?.Invoke(newGame);
    internal void RaiseUpdate(double deltaTime) => Update?.Invoke(deltaTime);
    internal void RaiseMapChanged(Map map) => MapChanged?.Invoke(map);
    internal void RaisePlayerMoved(Map map, uint x, uint y, bool mapChange) => PlayerMoved?.Invoke(map, x, y, mapChange);
    internal void RaiseEventExecuting(ModEventArgs args) => EventExecuting?.Invoke(args);
    internal void RaiseBattleStarting(uint monsterGroupIndex) => BattleStarting?.Invoke(monsterGroupIndex);
    internal void RaiseBattleEnded(uint monsterGroupIndex, bool monstersDefeated, bool partyDefeated) => BattleEnded?.Invoke(monsterGroupIndex, monstersDefeated, partyDefeated);
    internal void RaiseLevelUp(PartyMember partyMember) => LevelUp?.Invoke(partyMember);
    internal void RaisePartyMemberJoined(PartyMember partyMember) => PartyMemberJoined?.Invoke(partyMember);
    internal void RaisePartyMemberLeft(PartyMember partyMember) => PartyMemberLeft?.Invoke(partyMember);
    internal void RaiseSaving(ModSaveData data) => Saving?.Invoke(data);
    internal void RaiseLoading(ModSaveData data) => Loading?.Invoke(data);

    #endregion

    #region General

    public ModManifest Manifest { get; }

    /// <summary>
    /// Access to the files of the mod (e.g. to load own data files).
    /// </summary>
    public IModSource Source { get; }

    /// <summary>
    /// The base game data (with the asset changes of the mod applied).
    /// </summary>
    public IGameData GameData { get; }

    public GameLanguage Language => GameData.Language;

    public bool IsIngame => game?.Ingame == true;

    /// <summary>
    /// The game instance. Only use it if the API does not provide what you need.
    /// </summary>
    public GameCore? Game => game;

    /// <summary>
    /// The current savegame (null if not ingame).
    /// </summary>
    public Savegame? Savegame => game?.CurrentSavegame;

    /// <summary>
    /// The current map (null if not ingame).
    /// </summary>
    public Map? CurrentMap => game?.Map;

    /// <summary>
    /// Writes a line to the log (console).
    /// </summary>
    public void Log(string message) => Console.WriteLine($"[{Manifest.Id}] {message}");

    #endregion

    #region Data

    public IModifiableMapManager Maps => GameData.MapManager as IModifiableMapManager
        ?? throw new NotSupportedException("The base game data does not support modifying maps.");

    public IModifiableCharacterManager Characters => GameData.CharacterManager as IModifiableCharacterManager
        ?? throw new NotSupportedException("The base game data does not support modifying characters.");

    public IModifiableItemManager Items => GameData.ItemManager as IModifiableItemManager
        ?? throw new NotSupportedException("The base game data does not support modifying items.");

    /// <summary>
    /// Adds or replaces a map. Valid indices for new maps are 529 to 767.
    /// </summary>
    public void SetMap(Map map)
    {
        if (map.Index < 1 || map.Index > 767)
            throw new ArgumentOutOfRangeException(nameof(map), "Map indices must be between 1 and 767.");

        Maps.SetMap(map);
    }

    public Map? GetMap(uint index) => GameData.MapManager.GetMap(index);

    public Labdata GetLabdata(uint index) => GameData.MapManager.Labdata.Count >= index && index > 0
        ? GameData.MapManager.GetLabdataForMap(new Map { Type = MapType.Map3D, TilesetOrLabdataIndex = index })
        : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Adds or replaces a monster group. The monsters of the group are used as
    /// templates for the battle so you can give them individual loot etc.
    /// Note that new monsters (with new indices) must be added before the game
    /// is created (in <see cref="ModBase.OnInitialize"/>) because their graphics
    /// are put into the battle texture atlas at that point.
    /// </summary>
    public void SetMonsterGroup(uint index, MonsterGroup monsterGroup) => Characters.SetMonsterGroup(index, monsterGroup);

    public MonsterGroup? GetMonsterGroup(uint index) => GameData.CharacterManager.GetMonsterGroup(index);

    public Monster? GetMonster(uint index) => GameData.CharacterManager.GetMonster(index);

    /// <summary>
    /// Creates an independent copy of a monster (keeps the index).
    /// </summary>
    public Monster CloneMonster(Monster monster) => GameData.CharacterManager.CloneMonster(monster);

    public Item GetItem(uint index) => GameData.ItemManager.GetItem(index);

    #endregion

    #region Game actions

    GameCore Game_ => game is { Ingame: true } ? game : throw new InvalidOperationException("This action is only available while ingame.");

    /// <summary>
    /// Returns a random number in the range [min, max].
    /// </summary>
    public int RandomInt(int min, int max) => game?.RandomInt(min, max) ?? System.Random.Shared.Next(min, max + 1);

    /// <summary>
    /// Teleports the party. The position is 1-based.
    /// </summary>
    public bool Teleport(uint mapIndex, uint x, uint y, CharacterDirection direction, bool fade = true)
    {
        var game = Game_;
        return game.Teleport(mapIndex, x - 1, y - 1, direction, out _, true, fade);
    }

    /// <summary>
    /// Starts a battle against the given monster group at the current position.
    /// Returns false if a battle can't be started right now (e.g. a window is open).
    /// </summary>
    public bool StartBattle(uint monsterGroupIndex) => Game_.StartBattle(monsterGroupIndex);

    /// <summary>
    /// Shows a message popup. Text formatting like ~INK 22~ and placeholders like ~LEAD~ are supported.
    /// </summary>
    public void ShowMessage(string text, Action? closeAction = null) => Game_.ShowMessagePopup(text, closeAction);

    /// <summary>
    /// Shows a yes/no popup.
    /// </summary>
    public void AskQuestion(string text, Action<bool> answerHandler)
        => Game_.ShowDecisionPopup(text, response => answerHandler(response == PopupTextEvent.Response.Yes));

    public IEnumerable<PartyMember> PartyMembers => game?.PartyMembers ?? [];

    /// <summary>
    /// Clears all saved state of a map (opened chests are not affected): event bits,
    /// defeated monsters, joined characters, tile changes and the automap.
    /// Use this before you replace a map with completely new content.
    /// </summary>
    public void ResetMapState(Savegame savegame, uint mapIndex)
    {
        if (mapIndex < 1 || mapIndex > 1024)
            throw new ArgumentOutOfRangeException(nameof(mapIndex));

        savegame.MapEventBits[mapIndex - 1] = 0;
        savegame.CharacterBits[mapIndex - 1] = 0;
        savegame.TileChangeEvents.Remove(mapIndex);
        savegame.Automaps.Remove(mapIndex);
        MapExtensions.Reset();
    }

    #endregion
}
