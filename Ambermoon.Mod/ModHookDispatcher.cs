using Ambermoon.Data;

namespace Ambermoon.Mod;

/// <summary>
/// Forwards the game core hooks to the mod (virtual methods first, then the API events).
/// Exceptions inside a mod are logged and do not crash the game.
/// </summary>
internal class ModHookDispatcher(ModBase? mod, ModApi api) : IGameModHooks
{
    readonly ModBase? mod = mod;
    readonly ModApi api = api;

    void Safe(string hook, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            api.Log($"Error in hook {hook}: {ex}");
        }
    }

    public void GameStarting(Savegame savegame, bool newGame)
    {
        // Note: Not wrapped as the game can't start properly if the mod failed to prepare the savegame.
        mod?.OnGameStarting(savegame, newGame);
        api.RaiseGameStarting(savegame, newGame);
    }

    public void GameStarted(bool newGame) => Safe(nameof(GameStarted), () =>
    {
        mod?.OnGameStarted(newGame);
        api.RaiseGameStarted(newGame);
    });

    public void Update(double deltaTime) => Safe(nameof(Update), () =>
    {
        mod?.OnUpdate(deltaTime);
        api.RaiseUpdate(deltaTime);
    });

    public void MapChanged(Map map) => Safe(nameof(MapChanged), () =>
    {
        mod?.OnMapChanged(map);
        api.RaiseMapChanged(map);
    });

    public void PlayerMoved(Map map, uint x, uint y, bool mapChange) => Safe(nameof(PlayerMoved), () =>
    {
        mod?.OnPlayerMoved(map, x, y, mapChange);
        api.RaisePlayerMoved(map, x, y, mapChange);
    });

    public bool EventExecuting(Map map, Event @event, EventTrigger trigger, uint x, uint y)
    {
        bool proceed = true;

        Safe(nameof(EventExecuting), () =>
        {
            if (mod != null && !mod.OnEvent(map, @event, trigger, x, y))
            {
                proceed = false;
                return;
            }

            var args = new ModEventArgs(map, @event, trigger, x, y);
            api.RaiseEventExecuting(args);
            proceed = !args.Cancel;
        });

        return proceed;
    }

    public void BattleStarting(uint monsterGroupIndex) => Safe(nameof(BattleStarting), () =>
    {
        mod?.OnBattleStarting(monsterGroupIndex);
        api.RaiseBattleStarting(monsterGroupIndex);
    });

    public void BattleEnded(uint monsterGroupIndex, bool monstersDefeated, bool partyDefeated) => Safe(nameof(BattleEnded), () =>
    {
        mod?.OnBattleEnded(monsterGroupIndex, monstersDefeated, partyDefeated);
        api.RaiseBattleEnded(monsterGroupIndex, monstersDefeated, partyDefeated);
    });

    public void LevelUp(PartyMember partyMember) => Safe(nameof(LevelUp), () =>
    {
        mod?.OnLevelUp(partyMember);
        api.RaiseLevelUp(partyMember);
    });

    public void PartyMemberJoined(PartyMember partyMember) => Safe(nameof(PartyMemberJoined), () =>
    {
        mod?.OnPartyMemberJoined(partyMember);
        api.RaisePartyMemberJoined(partyMember);
    });

    public void PartyMemberLeft(PartyMember partyMember) => Safe(nameof(PartyMemberLeft), () =>
    {
        mod?.OnPartyMemberLeft(partyMember);
        api.RaisePartyMemberLeft(partyMember);
    });

    public void Saving(ModSaveData data) => Safe(nameof(Saving), () =>
    {
        mod?.OnSaving(data);
        api.RaiseSaving(data);
    });

    public void Loading(ModSaveData data)
    {
        mod?.OnLoading(data);
        api.RaiseLoading(data);
    }
}
