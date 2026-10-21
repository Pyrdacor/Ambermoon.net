using Ambermoon.Data;

namespace Ambermoon.Mod;

/// <summary>
/// Base class of every code mod. The mod assembly must contain exactly
/// one public non-abstract class deriving from it.
///
/// All hooks are optional. Use <see cref="Api"/> to access the game data
/// and to perform game actions. Besides the virtual methods, the same
/// notifications are also available as C# events on <see cref="ModApi"/>.
/// </summary>
public abstract class ModBase
{
    /// <summary>
    /// The mod API. It is available in all hooks (not in the constructor).
    /// </summary>
    public ModApi Api { get; internal set; } = null!;

    /// <summary>
    /// Called once after the base game data was loaded and the asset files of the
    /// mod were applied. The game itself does not exist yet but all data
    /// (maps, monsters, items, ...) can be modified here.
    /// </summary>
    public virtual void OnInitialize() { }

    /// <summary>
    /// Called before a savegame is started. For new games this is the initial
    /// savegame after character creation. Modify it to change the start
    /// position, party, items etc. The map at <see cref="SavegameData.CurrentMapIndex"/>
    /// must exist when this method returns.
    /// </summary>
    public virtual void OnGameStarting(Savegame savegame, bool newGame) { }

    /// <summary>
    /// Called after a savegame was started and the first map is visible.
    /// </summary>
    public virtual void OnGameStarted(bool newGame) { }

    /// <summary>
    /// Called every frame while ingame and not paused.
    /// </summary>
    public virtual void OnUpdate(double deltaTime) { }

    public virtual void OnMapChanged(Map map) { }

    /// <summary>
    /// The position is 1-based.
    /// </summary>
    public virtual void OnPlayerMoved(Map map, uint x, uint y, bool mapChange) { }

    /// <summary>
    /// Called before a map event is executed. Return false to skip it (and the rest of its chain).
    /// </summary>
    public virtual bool OnEvent(Map map, Event @event, EventTrigger trigger, uint x, uint y) => true;

    /// <summary>
    /// Called before a battle starts. The monster group can still be replaced
    /// with <see cref="ModApi.SetMonsterGroup"/>.
    /// </summary>
    public virtual void OnBattleStarting(uint monsterGroupIndex) { }

    public virtual void OnBattleEnded(uint monsterGroupIndex, bool monstersDefeated, bool partyDefeated) { }

    public virtual void OnLevelUp(PartyMember partyMember) { }

    public virtual void OnPartyMemberJoined(PartyMember partyMember) { }

    public virtual void OnPartyMemberLeft(PartyMember partyMember) { }

    /// <summary>
    /// Called when the game is saved. Store the state of the mod in the given data.
    /// </summary>
    public virtual void OnSaving(ModSaveData data) { }

    /// <summary>
    /// Called when a savegame is loaded (before <see cref="OnGameStarting"/>).
    /// It is not called for new games. Reset your state in <see cref="OnGameStarting"/>
    /// if its parameter newGame is true.
    /// </summary>
    public virtual void OnLoading(ModSaveData data) { }
}
