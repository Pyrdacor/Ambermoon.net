using Ambermoon.Data;

namespace Ambermoon.Mod.AmbermoonDC;

/// <summary>
/// Ambermoon DC - a dungeon crawler with endless procedurally generated floors.
///
/// - The party starts on floor 1 and has to find the hole leading down on each floor.
/// - Monsters roam the floors. Some of them only move when they see the party.
/// - Every 5th floor is guarded by a boss, every 10th floor by a boss with minions.
///   Boss floors have additional treasure chests and the boss drops a strong item.
/// - Every 12th floor a possible companion wanders around who can be asked to join.
/// - Secret rooms are hidden behind illusory walls or walls which open on touch.
/// </summary>
public class AmbermoonDCMod : ModBase
{
    const string StateKey = "state";

    List<DungeonTheme> themes = null!;
    FloorBuilder floorBuilder = null!;
    DCState? state;
    FloorLayout? currentLayout;
    readonly Queue<string> pendingMessages = new();

    DCTexts Texts => DCTexts.Get(Api.Language);

    internal DCState? State => state;

    internal FloorLayout? CurrentLayout => currentLayout;

    internal IReadOnlyList<DungeonTheme> Themes => themes;

    internal DungeonTheme? CurrentTheme => state == null ? null : floorBuilder.ThemeForFloor(state.Seed, state.Floor);

    public override void OnInitialize()
    {
        // Note: The themes create new monsters. This has to be done before the game is created.
        themes = DungeonTheme.LoadAll(Api);
        floorBuilder = new FloorBuilder(Api, themes);
    }

    public override void OnLoading(ModSaveData data)
    {
        state = data.GetObject<DCState>(StateKey);
    }

    public override void OnSaving(ModSaveData data)
    {
        if (state != null)
            data.SetObject(StateKey, state);
    }

    public override void OnGameStarting(Savegame savegame, bool newGame)
    {
        pendingMessages.Clear();

        if (newGame || state == null)
        {
            StartNewRun(savegame);
            return;
        }

        // Loaded game: restore the texts of the recruits and recreate the current floor exactly like it was.
        foreach (var recruitTexts in state.RecruitTexts)
        {
            if (savegame.PartyMembers.TryGetValue(recruitTexts.Key, out var recruit))
                recruit.Texts = recruitTexts.Value;
        }

        currentLayout = FloorGenerator.Generate(state.Seed, state.Floor);
        floorBuilder.Build(currentLayout, state);

        if (!FloorBuilder.IsDungeonMap(savegame.CurrentMapIndex))
            PlacePartyAtStart(savegame, currentLayout);
    }

    /// <summary>
    /// Starts a new dungeon run on floor 1.
    /// </summary>
    internal void StartNewRun(Savegame savegame, int? seed = null)
    {
        // For testing (debug builds only): The environment variables AMBERMOON_DC_SEED
        // and AMBERMOON_DC_START_FLOOR set the seed and the first floor.
#if DEBUG
        static int? FromEnvironment(string name) => int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : null;
#else
        static int? FromEnvironment(string name) => null;
#endif

        state = new DCState
        {
            Seed = seed ?? FromEnvironment("AMBERMOON_DC_SEED") ?? System.Random.Shared.Next(1, int.MaxValue),
            Floor = 1
        };

        EquipHero(savegame);
        EnterFloor(savegame, Math.Max(1, FromEnvironment("AMBERMOON_DC_START_FLOOR") ?? 1));
        PlacePartyAtStart(savegame, currentLayout!);
        pendingMessages.Enqueue(Texts.Welcome);
    }

    void EquipHero(Savegame savegame)
    {
        var hero = savegame.GetPartyMember(0);

        if (hero == null)
            return;

        var itemManager = Api.GameData.ItemManager;

        foreach (var (item, amount) in DCContent.StartEquipment)
        {
            if (CharacterHelper.CanEquip(hero, itemManager.GetItem(item)))
                CharacterHelper.Equip(hero, item, itemManager, amount);
            else
                CharacterHelper.AddToInventory(hero, item, amount, itemManager);
        }

        foreach (var (item, amount) in DCContent.StartItems)
            CharacterHelper.AddToInventory(hero, item, amount, itemManager);

        hero.AddFood(20);
        hero.AddGold(50);
    }

    /// <summary>
    /// Creates a new floor and prepares the savegame for it.
    /// </summary>
    internal void EnterFloor(Savegame savegame, int floor)
    {
        state!.Floor = floor;
        currentLayout = FloorGenerator.Generate(state.Seed, floor);
        floorBuilder.InitializeNewFloor(savegame, currentLayout, state);
        floorBuilder.Build(currentLayout, state);

        var texts = Texts;
        var message = string.Format(texts.FloorEntered, floor);

        if (currentLayout.IsBossWithMinionsFloor)
            message += " " + texts.BossWithMinionsFloor;
        else if (currentLayout.IsBossFloor)
            message += " " + texts.BossFloor;

        if (currentLayout.IsRecruitFloor && state.Recruit != 0)
            message += " " + texts.RecruitFloor;

        if (floor % 5 == 1)
            message = string.Format(texts.ThemeEntered, CurrentTheme!.Name(Api.Language)) + " " + message;

        if (floor > 1)
            pendingMessages.Enqueue(message);
    }

    static void PlacePartyAtStart(Savegame savegame, FloorLayout layout)
    {
        savegame.CurrentMapIndex = FloorBuilder.MapIndexForFloor(layout.Floor);
        savegame.CurrentMapX = (uint)layout.Start.X + 1;
        savegame.CurrentMapY = (uint)layout.Start.Y + 1;
        savegame.CharacterDirection = layout.StartDirection;
        savegame.TravelType = Data.Enumerations.TravelType.Walk;
    }

    public override bool OnEvent(Map map, Event @event, EventTrigger trigger, uint x, uint y)
    {
        // The hole on each floor leads to the next floor which is created right before the teleport.
        if (state != null && @event is TeleportEvent teleport && FloorBuilder.IsDungeonMap(map.Index) &&
            teleport.MapIndex == FloorBuilder.MapIndexForFloor(state.Floor + 1) && Api.Savegame != null)
        {
            EnterFloor(Api.Savegame, state.Floor + 1);
            teleport.X = (uint)currentLayout!.Start.X + 1;
            teleport.Y = (uint)currentLayout.Start.Y + 1;
            teleport.Direction = currentLayout.StartDirection;
        }

        return true;
    }

    public override void OnBattleEnded(uint monsterGroupIndex, bool monstersDefeated, bool partyDefeated)
    {
        if (monstersDefeated && currentLayout?.Boss != null && monsterGroupIndex == FloorBuilder.FirstMonsterGroupIndex)
            pendingMessages.Enqueue(Texts.BossDefeated);
    }

    public override void OnUpdate(double deltaTime)
    {
        if (pendingMessages.Count == 0)
            return;

        // Only show messages when the player can see the map.
        if (!Api.CanShowMessage || !FloorBuilder.IsDungeonMap(Api.CurrentMap!.Index))
            return;

        Api.ShowMessage(pendingMessages.Dequeue());
    }
}
