using System.Reflection;
using Ambermoon.Data;

namespace Ambermoon.Mod.Assets;

/// <summary>
/// Creates map events with all their unknown/unused byte arrays initialized,
/// so they can be cloned, saved and executed like events loaded from the game data.
/// </summary>
public static class EventFactory
{
    /// <summary>
    /// Value for branch event indices which means "no event".
    /// </summary>
    public const uint NoEvent = 0xffff;

    static readonly Dictionary<EventType, Type> EventClasses = new()
    {
        { EventType.Teleport, typeof(TeleportEvent) },
        { EventType.Door, typeof(DoorEvent) },
        { EventType.Chest, typeof(ChestEvent) },
        { EventType.MapText, typeof(PopupTextEvent) },
        { EventType.Spinner, typeof(SpinnerEvent) },
        { EventType.Trap, typeof(TrapEvent) },
        { EventType.ChangeBuffs, typeof(ChangeBuffsEvent) },
        { EventType.Riddlemouth, typeof(RiddlemouthEvent) },
        { EventType.Reward, typeof(RewardEvent) },
        { EventType.ChangeTile, typeof(ChangeTileEvent) },
        { EventType.StartBattle, typeof(StartBattleEvent) },
        { EventType.EnterPlace, typeof(EnterPlaceEvent) },
        { EventType.Condition, typeof(ConditionEvent) },
        { EventType.Action, typeof(ActionEvent) },
        { EventType.Dice100Roll, typeof(Dice100RollEvent) },
        { EventType.Conversation, typeof(ConversationEvent) },
        { EventType.PrintText, typeof(PrintTextEvent) },
        { EventType.Create, typeof(CreateEvent) },
        { EventType.Decision, typeof(DecisionEvent) },
        { EventType.ChangeMusic, typeof(ChangeMusicEvent) },
        { EventType.Exit, typeof(ExitEvent) },
        { EventType.Spawn, typeof(SpawnEvent) },
        { EventType.Interact, typeof(InteractEvent) },
        { EventType.RemovePartyMember, typeof(RemovePartyMemberEvent) },
        { EventType.Delay, typeof(DelayEvent) },
        { EventType.PartyMemberCondition, typeof(PartyMemberConditionEvent) },
        { EventType.Shake, typeof(ShakeEvent) },
        { EventType.ShowMap, typeof(ShowMapEvent) },
        { EventType.ToggleSwitch, typeof(ToggleSwitchEvent) },
        { EventType.DynamicChangeTile, typeof(DynamicChangeTileEvent) },
        { EventType.RectangularExploration, typeof(RectangularExplorationEvent) },
        { EventType.VerticalLineReveal, typeof(VerticalLineRevealEvent) },
    };

    // Sizes of the raw byte arrays as they are stored in the original data.
    static readonly Dictionary<(Type, string), int> ByteArraySizes = new()
    {
        { (typeof(TeleportEvent), nameof(TeleportEvent.Unknown2)), 2 },
        { (typeof(PopupTextEvent), nameof(PopupTextEvent.Unknown)), 4 },
        { (typeof(SpinnerEvent), nameof(SpinnerEvent.Unused)), 8 },
        { (typeof(TrapEvent), nameof(TrapEvent.Unused)), 5 },
        { (typeof(ChangeBuffsEvent), nameof(ChangeBuffsEvent.Unused2)), 2 },
        { (typeof(RiddlemouthEvent), nameof(RiddlemouthEvent.Unused)), 3 },
        { (typeof(ChangeTileEvent), nameof(ChangeTileEvent.Unknown)), 3 },
        { (typeof(StartBattleEvent), nameof(StartBattleEvent.Unknown1)), 6 },
        { (typeof(StartBattleEvent), nameof(StartBattleEvent.Unknown2)), 2 },
        { (typeof(ActionEvent), nameof(ActionEvent.Unknown1)), 2 },
        { (typeof(ActionEvent), nameof(ActionEvent.Unknown2)), 2 },
        { (typeof(Dice100RollEvent), nameof(Dice100RollEvent.Unused)), 6 },
        { (typeof(ConversationEvent), nameof(ConversationEvent.Unused1)), 4 },
        { (typeof(ConversationEvent), nameof(ConversationEvent.Unused2)), 2 },
        { (typeof(PrintTextEvent), nameof(PrintTextEvent.Unused)), 8 },
        { (typeof(CreateEvent), nameof(CreateEvent.Unused)), 4 },
        { (typeof(DecisionEvent), nameof(DecisionEvent.Unknown1)), 6 },
        { (typeof(ChangeMusicEvent), nameof(ChangeMusicEvent.Unknown1)), 6 },
        { (typeof(ExitEvent), nameof(ExitEvent.Unused)), 9 },
        { (typeof(SpawnEvent), nameof(SpawnEvent.Unknown1)), 2 },
        { (typeof(SpawnEvent), nameof(SpawnEvent.Unknown2)), 2 },
        { (typeof(InteractEvent), nameof(InteractEvent.Unused)), 9 },
        { (typeof(RemovePartyMemberEvent), nameof(RemovePartyMemberEvent.Unused)), 6 },
        { (typeof(DelayEvent), nameof(DelayEvent.Unused1)), 5 },
        { (typeof(ShakeEvent), nameof(ShakeEvent.Unused1)), 5 },
        { (typeof(ShowMapEvent), nameof(ShowMapEvent.Unused)), 8 },
        { (typeof(ToggleSwitchEvent), nameof(ToggleSwitchEvent.GlobalVariableBytes)), 5 },
        { (typeof(DebugEvent), nameof(DebugEvent.Data)), 9 },
    };

    public static Type GetEventClass(EventType type)
        => EventClasses.TryGetValue(type, out var eventClass) ? eventClass : throw new NotSupportedException($"Event type {type} is not supported.");

    /// <summary>
    /// Creates an event of the given type with initialized raw data.
    /// </summary>
    public static Event Create(EventType type)
    {
        var @event = (Event)Activator.CreateInstance(GetEventClass(type))!;
        @event.Type = type;
        InitializeRawData(@event);

        // Branch targets are 0-based indices into Map.Events where 0xffff means "none".
        if (@event is IBranchEvent branchEvent)
            branchEvent.AlternativeBranchEventIndex = NoEvent;

        return @event;
    }

    public static T Create<T>(Action<T>? setup = null) where T : Event
    {
        var type = EventClasses.First(e => e.Value == typeof(T)).Key;
        var @event = (T)Create(type);
        setup?.Invoke(@event);

        return @event;
    }

    /// <summary>
    /// Initializes all raw byte arrays which are still null.
    /// </summary>
    public static void InitializeRawData(Event @event)
    {
        var type = @event.GetType();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType != typeof(byte[]) || !property.CanWrite || property.GetValue(@event) != null)
                continue;

            int size = ByteArraySizes.TryGetValue((type, property.Name), out var s) ? s : 0;
            property.SetValue(@event, new byte[size]);
        }
    }

    public static TeleportEvent Teleport(uint mapIndex, uint x, uint y, CharacterDirection direction,
        TeleportEvent.TransitionType transition = TeleportEvent.TransitionType.Teleporter)
        => Create<TeleportEvent>(e =>
        {
            e.MapIndex = mapIndex;
            e.X = x;
            e.Y = y;
            e.Direction = direction;
            e.Transition = transition;
            e.Unknown2[1] = 0xff; // original data uses 00 FF here
        });

    /// <summary>
    /// Creates a chest event.
    /// </summary>
    /// <param name="chestIndex">1-based chest index (the key of Savegame.Chests).</param>
    public static ChestEvent Chest(uint chestIndex, uint lockpickingChanceReduction = 0, uint keyIndex = 0, uint textIndex = 255,
        ChestEvent.ChestFlags flags = ChestEvent.ChestFlags.None)
        => Create<ChestEvent>(e =>
        {
            e.ChestIndex = chestIndex - 1;
            e.LockpickingChanceReduction = lockpickingChanceReduction;
            e.KeyIndex = keyIndex;
            e.TextIndex = textIndex;
            e.Flags = flags;
        });

    /// <summary>
    /// Creates a text popup event which shows a map text.
    /// </summary>
    public static PopupTextEvent MapText(uint textIndex, Data.EventTrigger trigger = Data.EventTrigger.Always, uint eventImageIndex = 0xff)
        => Create<PopupTextEvent>(e =>
        {
            e.TextIndex = textIndex;
            e.PopupTrigger = trigger;
            e.EventImageIndex = eventImageIndex;
            e.TriggerIfBlind = true;
        });

    /// <summary>
    /// Changes a 3D block. Use a wall index for walls or 0 to remove the wall/object.
    /// </summary>
    public static ChangeTileEvent ChangeBlock(uint x, uint y, uint wallIndex = 0, uint objectIndex = 0, uint mapIndex = 0)
        => Create<ChangeTileEvent>(e =>
        {
            e.X = x;
            e.Y = y;
            e.MapIndex = mapIndex;
            e.FrontTileIndex = wallIndex != 0 ? 100 + wallIndex : objectIndex;
        });

    public static StartBattleEvent StartBattle(uint monsterGroupIndex)
        => Create<StartBattleEvent>(e => e.MonsterGroupIndex = monsterGroupIndex);

    public static DoorEvent Door(byte doorIndex, uint lockpickingChanceReduction = 0, uint keyIndex = 0)
        => Create<DoorEvent>(e =>
        {
            e.DoorIndex = doorIndex;
            e.LockpickingChanceReduction = lockpickingChanceReduction;
            e.KeyIndex = keyIndex;
            e.TextIndex = 255;
            e.UnlockTextIndex = 255;
        });
}
