using Ambermoon.Data;

namespace Ambermoon;

/// <summary>
/// Notifications from the game core to the mod system.
/// Set <see cref="GameCore.ModHooks"/> to receive them.
/// </summary>
public interface IGameModHooks
{
    /// <summary>
    /// Called before a savegame is started (new game or loaded game).
    /// The savegame can still be modified, e.g. to change the start position.
    /// The map given by <see cref="SavegameData.CurrentMapIndex"/> must exist afterwards.
    /// </summary>
    void GameStarting(Savegame savegame, bool newGame);
    /// <summary>
    /// Called after a savegame was started and the first map is shown.
    /// </summary>
    void GameStarted(bool newGame);
    /// <summary>
    /// Called every frame while the game is running.
    /// </summary>
    void Update(double deltaTime);
    /// <summary>
    /// Called when a new map was entered.
    /// </summary>
    void MapChanged(Map map);
    /// <summary>
    /// Called after the player moved (or a map change happened).
    /// The position is 1-based like in the savegame.
    /// </summary>
    void PlayerMoved(Map map, uint x, uint y, bool mapChange);
    /// <summary>
    /// Called before a map event is executed.
    /// Return false to skip the event (and the rest of its chain).
    /// </summary>
    bool EventExecuting(Map map, Event @event, EventTrigger trigger, uint x, uint y);
    /// <summary>
    /// Called before a battle is started. The monster group can still be replaced
    /// through the character manager.
    /// </summary>
    void BattleStarting(uint monsterGroupIndex);
    /// <summary>
    /// Called when a battle ended.
    /// </summary>
    void BattleEnded(uint monsterGroupIndex, bool monstersDefeated, bool partyDefeated);
    void LevelUp(PartyMember partyMember);
    void PartyMemberJoined(PartyMember partyMember);
    void PartyMemberLeft(PartyMember partyMember);
    /// <summary>
    /// The quests of the mod which are shown in the quest log instead of the quests of the
    /// original game. If null (the mod has no quests), the quest log is disabled.
    /// </summary>
    UI.IQuestLogSource? QuestLogSource { get; }
}
