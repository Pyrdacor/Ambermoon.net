using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;
using Color = Ambermoon.Render.Color;

namespace Ambermoon.UI;


#region Triggers

file enum  TriggerType
{
    Activation,
    Completion
}

public interface IQuestTrigger
{
    void CheckEvent(Event @event, SubQuest subQuest) { }
    void CheckExploration(uint mapIndex, uint x, uint y, SubQuest subQuest) { }
    void CheckItem(Item item, uint itemCount, SubQuest subQuest) { }
    void CheckChestUnlock(uint chestIndex, SubQuest subQuest) { }
    void CheckDoorUnlock(uint chestIndex, SubQuest subQuest) { }
    QuestState OldState { get; }
    QuestState NewState { get; }
}

file class PreviousSubQuestCompletedTrigger : IQuestTrigger
{
    public QuestState OldState => QuestState.Inactive;
    public QuestState NewState => QuestState.Active;
}

file class NextSubQuestActivatedTrigger : IQuestTrigger
{
    public QuestState OldState => QuestState.Active;
    public QuestState NewState => QuestState.Completed;
}

file class GlobalVariableTrigger(Game game, TriggerType triggerType, uint index, bool expectedValue = true) : IQuestTrigger
{
    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetGlobalVariable && e.ObjectIndex == index && e.Value == (expectedValue ? 1 : 0))
            subQuest.State = NewState;
        else if (game.CurrentSavegame.GetGlobalVariable(index) == expectedValue)
            subQuest.State = NewState;
    }
}

file class TileChangeTrigger(Game game, TriggerType triggerType, uint mapIndex, uint x, uint y, uint expectedTile) : IQuestTrigger
{
    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (@event is ChangeTileEvent e && e.MapIndex == mapIndex && e.X == x && e.Y == y && e.FrontTileIndex == expectedTile)
            subQuest.State = NewState;
        else if (game.CurrentSavegame.TileChangeEvents.TryGetValue(mapIndex, out var changes) &&
            changes.Any(c => c.X == x && c.Y == y && c.FrontTileIndex == expectedTile))
            subQuest.State = NewState;
    }
}

file class EventDisabledTrigger(Game game, TriggerType triggerType, uint mapIndex, uint eventIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        uint index = (mapIndex - 1) * 64 + eventIndex - 1;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetEventBit && e.ObjectIndex == index && e.Value == 1)
            subQuest.State = NewState;
        else if (game.CurrentSavegame.GetEventBit(mapIndex, eventIndex - 1))
            subQuest.State = NewState;
    }
}

file class CharacterVisibilityTrigger(Game game, TriggerType triggerType, uint mapIndex, uint characterIndex, bool visible) : IQuestTrigger
{
    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        uint index = (mapIndex - 1) * 32 + characterIndex - 1;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetCharacterBit && e.ObjectIndex == index && e.Value == (visible ? 0 : 1))
            subQuest.State = NewState;
        else if (game.CurrentSavegame.GetCharacterBit(mapIndex, characterIndex - 1) != visible) // if the bit is true, the character is invisible and vice versa
            subQuest.State = NewState; ;
    }
}

file class ItemObtainedTrigger(TriggerType triggerType, uint itemIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckItem(Item item, uint itemCount, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (item != null && item.Index == itemIndex)
        {
            while (subQuest.CompletionCount < subQuest.MaxCompletionCount)
            {
                if (subQuest.MinAmount == 0 || subQuest.MinAmount < subQuest.CurrentAmount)
                {
                    subQuest.State = NewState;
                    return;
                }

                uint countToAdd = Math.Min(itemCount, (uint)(subQuest.MinAmount - subQuest.CurrentAmount));

                if (countToAdd == 0)
                    break;

                subQuest.CurrentAmount += countToAdd;

                if (subQuest.CurrentAmount == subQuest.MinAmount)
                {
                    subQuest.State = NewState;
                }
            }
        }
    }
}

file class ExplorationTrigger(Game game, TriggerType triggerType, uint mapIndex, uint x, uint y) : IQuestTrigger
{
    private readonly Game game = game;
    private readonly uint mapIndex = mapIndex;
    private readonly uint x = x;
    private readonly uint y = y;

    public QuestState OldState { get; } = triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;

    public QuestState NewState { get; } = triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckExploration(uint mapIndex, uint x, uint y, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (this.mapIndex == mapIndex && this.x == x && this.y == y)
            subQuest.State = NewState;
        else if (game.CurrentSavegame.Automaps.TryGetValue(this.mapIndex, out var exploration) && exploration.IsBlockExplored(game.MapManager.GetMap(this.mapIndex), this.x, this.y))
            subQuest.State = NewState;
    }
}

// TODO: Careful with this, as you can also add keywords by typing them in conversation which might activate the quest.
// Always add a required quest when using this.
file class KeywordLearnedTrigger(Game game, TriggerType triggerType, uint keywordIndex) : IQuestTrigger
{
    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckEvent(Event @event, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.AddKeyword && e.ObjectIndex == keywordIndex && e.Value == 1)
            subQuest.State = NewState;
        else if (game.CurrentSavegame.IsDictionaryWordKnown(keywordIndex))
        {
            if (triggerType == TriggerType.Completion)
            {
                subQuest.State = NewState;
            }
            else
            {
                var quests = subQuest.Quest.SubQuests;
                var requiredQuests = subQuest.RequiredCompletedQuests.Select(type => quests.FirstOrDefault(q => q.Type == type));

                if (requiredQuests.All(q => q.State == QuestState.Completed))
                    subQuest.State = NewState;
            }
        }
    }
}

file class ChestUnlockedTrigger(Game game, TriggerType triggerType, uint chestIndex) : IQuestTrigger
{
    private readonly uint chestIndex = chestIndex;

    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckChestUnlock(uint chestIndex, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (this.chestIndex == chestIndex)
            subQuest.State = NewState;
        else if (!game.CurrentSavegame.IsChestLocked(this.chestIndex - 1))
            subQuest.State = NewState;
    }
}

file class DoorUnlockedTrigger(Game game, TriggerType triggerType, uint doorIndex) : IQuestTrigger
{
    private readonly uint doorIndex = doorIndex;

    public QuestState OldState => triggerType == TriggerType.Activation ? QuestState.Inactive : QuestState.Active;
    public QuestState NewState => triggerType == TriggerType.Activation ? QuestState.Active : QuestState.Completed;

    void IQuestTrigger.CheckDoorUnlock(uint doorIndex, SubQuest subQuest)
    {
        if (subQuest.State != OldState)
            return;

        if (this.doorIndex == doorIndex)
            subQuest.State = NewState;
        else if (!game.CurrentSavegame.IsDoorLocked(this.doorIndex))
            subQuest.State = NewState;
    }
}


#endregion


#region Quest State

public enum QuestState
{
    Inactive,
    Active,
    Blocked,
    Completed
}

file static class QuestStateExtensions
{
    public static TextColor ToColor(this QuestState state)
    {
        return state switch
        {
            QuestState.Active => TextColor.BrightGray,
            QuestState.Blocked => TextColor.DarkBrown,
            _ => TextColor.DarkerGray,
        };
    }

    public static QuestState ToCompoundState(this MainQuest quest)
    {
        var states = quest.SubQuests.Select(q => q.State).Distinct().ToArray();

        if (states.Length == 1 && states[0] == QuestState.Completed)
            return QuestState.Completed;

        if (states.Contains(QuestState.Active))
            return QuestState.Active;

        if (states.Contains(QuestState.Blocked))
            return QuestState.Blocked;

        return states.Contains(QuestState.Completed) ? QuestState.Active : QuestState.Inactive;
    }
}

#endregion


#region Quests

public enum MainQuestType
{
    Grandfather,
    SwampFever,
    AlkemsRing,
    ThiefPlague,
    OrcPlague,
    Sylphs,
    WineTrophies,
    GoldenHorseshoes,
    ChainOfOffice,
    Graveyard,
    Monstereye,
    ThiefGuild,
    SandrasDaughter,
    TowerOfBlackMagic,
    ValdynsReturn,
    AmbermoonPicture,
}

public enum SubQuestType
{
    // Grandfather's Quest
    Grandfather_TalkToGrandfather,
    Grandfather_GoToWineCellar,
    Grandfather_FindHisEquipment,
    Grandfather_RemoveCaveIn,
    Grandfather_ReturnToGrandfather,
    Grandfather_VisitGrave,
    // Swamp Fever
    SwampFever_TalkToFatherAnthony,
    SwampFever_ObtainEmptyBottle,
    SwampFever_ObtainSwampLilly,
    SwampFever_ObtainWaterOfLife,
    // Alkem's Ring
    AlkemsRing_EnterTheCrypt,
    AlkemsRing_FindTheRing,
    AlkemsRing_ReturnTheRing,
    // ...
}

public enum QuestSourceType
{
    NPC,
    PartyMember,
    Item, // Books etc
    MapEvent,
}

public record SubQuest(QuestLog questLog, MainQuestType quest, QuestState initialState = QuestState.Inactive)
{
    private QuestState state = initialState;

    public MainQuest Quest => questLog.Quests.FirstOrDefault(q => q.Type == quest);

    public required IQuestTrigger[] Triggers { get; init; } = [];

    public required SubQuestType Type { get; init; }

    /// <summary>
    /// Note: This is only used if there are other quests which have to be
    /// completed first to proceed with this quest. So it is used to
    /// determine if this quest is blocked or not.
    /// 
    /// In general you won't use this inside the same quest to create some
    /// order as normally you would only add/activate further steps when you
    /// complete the previous step anyway.
    /// </summary>
    public SubQuestType[] RequiredCompletedQuests { get; private set; } = [];

    /// <summary>
    /// This is similar to <see cref="RequiredCompletedQuests"/> but
    /// here if any of the given quests is completed, it is enough to
    /// unblock the quest. <see cref="RequiredCompletedQuests"/> is still
    /// checked beforehand and all of those have to be completed of course.
    /// </summary>
    public SubQuestType[] AnyCompletedQuests { get; init; } = [];

    /// <summary>
    /// If this quest is added to the quest log it will be added after
    /// the given quest (which must be part of the same main quest!).
    /// If null, it will be appended to the end of the parent main quest.
    /// </summary>
    public SubQuestType? AddAfter { get; init; } = null;

    /// <summary>
    /// Type of the source.
    /// 
    /// The source specifies where you got this quest/task.
    /// </summary>
    public required QuestSourceType SourceType { get; init; }

    /// <summary>
    /// NPC: NPC index
    /// PartyMember: Party member index
    /// Item: Item index
    /// MapEvent: Low word = Map index, highest byte = X, second highest byte = Y
    /// </summary>
    public required uint SourceIndex { get; init; }

    /// <summary>
    /// If not null, the quest will only be completed if the given amount (of items)
    /// is reached. It also will display $"{<see cref="CurrentAmount"/>}/{<see cref="MinAmount"/>}"
    /// behind the quest description.
    /// </summary>
    public uint? MinAmount { get; init; } = null;

    /// <summary>
    /// See <see cref="MinAmount"/>. Tracks the current amount of items you obtained.
    /// </summary>
    public uint CurrentAmount { get; set; } = 0;

    /// <summary>
    /// Specifies how often you already completed the quest.
    /// </summary>
    public int CompletionCount { get; private set; } = 0;

    public Action<SubQuest> PostActivationAction { get; init; } = null;

    public Action<SubQuest> PostCompletionAction { get; init; } = null;

    /// <summary>
    /// Most quests can only be completed once which is the default.
    /// But some might be completed multiple times or even unlimited.
    /// Use a value of <see cref="int.MaxValue"/> for unlimited completions.
    /// Not that the quest is only completed after you completed it the
    /// given amount of times. Unlimited quests may never be completed.
    /// </summary>
    public int MaxCompletionCount { get; set; } = 1;

    public QuestState State
    {
        get => state;
        set
        {
            if (state != value)
            {
                var oldState = state;

                state = value;

                if (state == QuestState.Completed)
                {
                    CurrentAmount = 0;

                    if (++CompletionCount >= MaxCompletionCount)
                    {
                        PostCompletionAction?.Invoke(this);
                        Quest.SubQuestCompleted(this);
                    }
                    else
                    {
                        state = QuestState.Active;
                    }
                }
                else if (oldState == QuestState.Inactive && state == QuestState.Active)
                {
                    PostActivationAction?.Invoke(this);
                    Quest.SubQuestActivated(this);
                }
            }
        }
    }

    public void Update(MainQuest[] allQuests)
    {
        if (State == QuestState.Active)
        {
            var blocked = allQuests.SelectMany(q => q.SubQuests).Where(sq => RequiredCompletedQuests.Contains(Type)).Any(sq => sq.State != QuestState.Completed);

            if (!blocked && AnyCompletedQuests.Length > 0 && allQuests.SelectMany(q => q.SubQuests).Where(sq => AnyCompletedQuests.Contains(sq.Type) && sq.State == QuestState.Completed).Count() == 0)
                blocked = true;

            if (blocked)
                State = QuestState.Blocked;
        }
        else if (State == QuestState.Blocked)
        {
            var unblocked = allQuests.SelectMany(q => q.SubQuests).Where(sq => RequiredCompletedQuests.Contains(Type)).All(sq => sq.State == QuestState.Completed);

            if (unblocked && AnyCompletedQuests.Length > 0 && allQuests.SelectMany(q => q.SubQuests).Where(sq => AnyCompletedQuests.Contains(sq.Type) && sq.State == QuestState.Completed).Count() == 0)
                unblocked = false;

            if (unblocked)
                State = QuestState.Active;
        }
    }

    public string GetSourceInfo(Game game)
    {
        switch (SourceType)
        {
            case QuestSourceType.NPC:
                return game.CharacterManager.GetNPC(SourceIndex).Name;
            case QuestSourceType.PartyMember:
                return game.CurrentSavegame.PartyMembers[SourceIndex].Name;
            case QuestSourceType.Item:
                return game.ItemManager.GetItem(SourceIndex).Name;
            case QuestSourceType.MapEvent:
            {
                var map = game.MapManager.GetMap(SourceIndex & 0xffff);
                string mapName = map.IsWorldMap
                    ? game.DataNameProvider.GetWorldName(map.World)
                    : map.Name;
                uint y = SourceIndex >> 16;
                uint x = y >> 8;
                y &= 0xff;

                if (map.IsWorldMap)
                {
                    var offset = map.MapOffset;
                    x += (uint)offset.X;
                    y += (uint)offset.Y;
                }

                return $"{mapName} ({x}, {y})";
            }
            default:
                return "";
        }
    }

    public void AddRequiredQuest(SubQuestType subQuestType)
    {
        RequiredCompletedQuests = [.. RequiredCompletedQuests, subQuestType];
        State = QuestState.Blocked;
    }
}

public record MainQuest(QuestLog QuestLog)
{
    public MainQuestType Type { get; init; }
    public SubQuest[] SubQuests { get; init; }

    public void SubQuestActivated(SubQuest subQuest)
    {
        int index = SubQuests.ToList().IndexOf(subQuest);

        if (index > 0)
        {
            var nextActivatedTrigger = SubQuests[index - 1].Triggers.FirstOrDefault(trigger => trigger is NextSubQuestActivatedTrigger);

            if (nextActivatedTrigger != null)
                SubQuests[index - 1].State = QuestState.Completed;
        }
    }

    public void SubQuestCompleted(SubQuest subQuest)
    {
        foreach (var quest in QuestLog.Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed && q.State != QuestState.Inactive))
        {
            if (quest != subQuest)
                quest.Update(QuestLog.Quests);
        }

        if (this.ToCompoundState() == QuestState.Completed) // all completed?
            QuestLog.CompleteMainQuest(Type);

        int index = SubQuests.ToList().IndexOf(subQuest);

        if (index < SubQuests.Length - 1)
        {
            var previousCompletedTrigger = SubQuests[index + 1].Triggers.FirstOrDefault(trigger => trigger is PreviousSubQuestCompletedTrigger);

            if (previousCompletedTrigger != null && SubQuests[index + 1].State != QuestState.Completed)
                SubQuests[index + 1].State = QuestState.Active;
        }
    }
}

#endregion


file static class QuestFactory
{
    public static MainQuest CreateMainQuest(QuestLog questLog, MainQuestType mainQuestType, params Func<MainQuestType, SubQuest>[] subQuestFactory)
    {
        var mainQuest = new MainQuest(questLog)
        {
            Type = mainQuestType,
            SubQuests = []
        };

        var subQuests = subQuestFactory.Select(factory => factory(mainQuest.Type)).ToArray();

        return mainQuest with { SubQuests = subQuests };
    }
}

public class QuestLog
{
    public const uint IconGraphicIndex = 1000u;
    const int Columns = 20;
    const int Rows = 10;
    const int X = 0;
    const int Y = 38;
    const int TextLineCount = 16;

    private static readonly Rect Area = new(X, Y, Columns * 16, Rows * 16);

    private readonly Game game;
    private readonly IRenderView renderView;
    private readonly UIText[] texts = new UIText[TextLineCount];
    private readonly Dictionary<MainQuestType, bool> questGroups = []; // boolean = expanded
    private readonly List<CollapseIndicator> collapseIndicators = [];
    internal MainQuest[] Quests { get; }
    internal bool Open { get; private set; } = false;
    private int scrollOffset = 0;
    private Popup popup;
    private Scrollbar scrollbar;

    private class CollapseIndicator
    {
        private const byte DisplayLayer = 10;
        private readonly Position position;
        private readonly IColoredRect[] rects = new IColoredRect[6];

        public int QuestIndex { get; }

        public CollapseIndicator(Position position, Popup popup, bool expanded, int questIndex)
        {
            this.position = position;
            QuestIndex = questIndex;

            if (expanded)
            {
                // Shape
                rects[0] = popup.FillArea(new Rect(position, new Size(5, 1)), Color.White, DisplayLayer);
                rects[1] = popup.FillArea(new Rect(position + new Position(1, 1), new Size(3, 1)), Color.White, DisplayLayer);
                rects[2] = popup.FillArea(new Rect(position + new Position(2, 2), new Size(1, 1)), Color.White, DisplayLayer);
                // Shadow
                rects[3] = popup.FillArea(new Rect(position + new Position(4, 1), new Size(2, 1)), Color.Black, DisplayLayer);
                rects[4] = popup.FillArea(new Rect(position + new Position(3, 2), new Size(2, 1)), Color.Black, DisplayLayer);
                rects[5] = popup.FillArea(new Rect(position + new Position(3, 3), new Size(1, 1)), Color.Black, DisplayLayer);
            }
            else // Collapsed
            {
                // Shape
                rects[0] = popup.FillArea(new Rect(position + new Position(1, -1), new Size(1, 5)), Color.White, DisplayLayer);
                rects[1] = popup.FillArea(new Rect(position + new Position(2, 0), new Size(1, 3)), Color.White, DisplayLayer);
                rects[2] = popup.FillArea(new Rect(position + new Position(3, 1), new Size(1, 1)), Color.White, DisplayLayer);
                // Shadow
                rects[3] = popup.FillArea(new Rect(position + new Position(3, 2), new Size(2, 1)), Color.Black, DisplayLayer);
                rects[4] = popup.FillArea(new Rect(position + new Position(2, 3), new Size(2, 1)), Color.Black, DisplayLayer);
                rects[5] = popup.FillArea(new Rect(position + new Position(2, 4), new Size(1, 1)), Color.Black, DisplayLayer);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < 6; i++)
                rects[i]?.Delete();
        }

        public bool TestClick(Position position)
        {
            return new Rect(this.position.X - 2, this.position.Y - 1, Columns * 16 - 32 + 4, 7).Contains(position);
        }
    }

    public QuestLog(Game game, IRenderView renderView, bool advanced, int episode)
    {
        this.game = game;
        this.renderView = renderView;

        static uint CreateMapEventSource(uint mapIndex, uint x, uint y)
        {
            uint sourceIndex = x & 0xff;
            sourceIndex <<= 8;
            sourceIndex |= y & 0xff;
            sourceIndex <<= 8;
            sourceIndex |= mapIndex & 0xffff;

            return sourceIndex;
        }

        Action<SubQuest> AddAsRequiredTo(MainQuestType mainQuest, SubQuestType targetQuest)
        {
            return (subQuest) => Quests.FirstOrDefault(q => q.Type == mainQuest).SubQuests.FirstOrDefault(q => q.Type == targetQuest).AddRequiredQuest(subQuest.Type);
        }

        Action<SubQuest> CompleteOtherQuest(MainQuestType mainQuest, SubQuestType otherQuest)
        {
            return (_) => Quests.FirstOrDefault(q => q.Type == mainQuest).SubQuests.FirstOrDefault(q => q.Type == otherQuest).State = QuestState.Completed;
        }

        // TODO ...
        Quests =
        [
            #region Original quests
            QuestFactory.CreateMainQuest(this, MainQuestType.Grandfather,
                mainQuest => new SubQuest(this, mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_TalkToGrandfather,
                    Triggers =
                    [
                        // Completion
                        new KeywordLearnedTrigger(game, TriggerType.Completion, 1), // "Wine"
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new TileChangeTrigger(game, TriggerType.Completion, 259, 10, 17, 0), // remove Riddlemouth in cellar
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_FindHisEquipment,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new ChestUnlockedTrigger(game, TriggerType.Completion, 23), // Grandfather's old chest
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_ReturnToGrandfather,
                    Triggers =
                    [
                        // Activate
                        new PreviousSubQuestCompletedTrigger(),
                        // Complete
                        new NextSubQuestActivatedTrigger(),
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_VisitGrave,
                    Triggers =
                    [
                        // Activate
                        new CharacterVisibilityTrigger(game, TriggerType.Activation, 266, 1, true), // spawned Anthony in house of healers
                        // Complete
                        new EventDisabledTrigger(game, TriggerType.Completion, 263, 9), // visit grave
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(this, mainQuest)
                {
                    Type = SubQuestType.Grandfather_RemoveCaveIn,
                    PostActivationAction = AddAsRequiredTo(mainQuest, SubQuestType.Grandfather_FindHisEquipment),
                    Triggers =
                    [
                        // Activate
                        new EventDisabledTrigger(game, TriggerType.Activation, 260, 13), // encounter cave-in (text popup)
                        // Complete
                        new TileChangeTrigger(game, TriggerType.Completion, 260, 26, 11, 12), // remove cave-in
                    ],
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 1, // Grandfather
                })/*,
            QuestFactory.CreateMainQuest(this, MainQuestType.SwampFever,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.SwampFever_TalkToFatherAnthony,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 12, // Wat the fisherman
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.SwampFever_ObtainSwampLilly,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Blocked)
                {
                    Type = SubQuestType.SwampFever_ObtainEmptyBottle,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Blocked)
                {
                    Type = SubQuestType.SwampFever_ObtainWaterOfLife,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 2, // Father Anthony
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.AlkemsRing,
                mainQuest => new SubQuest(mainQuest, QuestState.Inactive)
                {
                    Type = SubQuestType.AlkemsRing_EnterTheCrypt,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.AlkemsRing_FindTheRing,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.AlkemsRing_ReturnTheRing,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    SourceType = QuestSourceType.NPC,
                    SourceIndex = 18, // Alkem
                })*/,
            #endregion
            #region Advanced quests
            #endregion
        ];

        foreach (var quest in Quests)
        {
            questGroups.Add(quest.Type, true);
        }

        // Directly check for quest states
        CheckEvent(null);
        CheckItem(null, 0);
        CheckExploration(0, 0, 0);
        CheckChestUnlock(uint.MaxValue);
        CheckDoorUnlock(uint.MaxValue);
    }

    internal void CompleteMainQuest(MainQuestType questType)
    {
        questGroups[questType] = false; // collapse completed quests
    }

    public void CheckEvent(Event @event)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            foreach (var trigger in quest.Triggers)
                trigger.CheckEvent(@event, quest);
        }
    }

    public void CheckItem(Item item, uint itemCount)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            foreach (var trigger in quest.Triggers)
                trigger.CheckItem(item, itemCount, quest);
        }
    }

    public void CheckExploration(uint mapIndex, uint x, uint y)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            foreach (var trigger in quest.Triggers)
                trigger.CheckExploration(mapIndex, x, y, quest);
        }
    }

    public void CheckChestUnlock(uint chestIndex)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            foreach (var trigger in quest.Triggers)
                trigger.CheckChestUnlock(chestIndex, quest);
        }
    }

    public void CheckDoorUnlock(uint chestIndex)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            foreach (var trigger in quest.Triggers)
                trigger.CheckDoorUnlock(chestIndex, quest);
        }
    }

    public void Click(Position position)
    {
        foreach (var collapseIndicator in collapseIndicators)
        {
            if (collapseIndicator.TestClick(position))
            {
                questGroups[Quests[collapseIndicator.QuestIndex].Type] = !questGroups[Quests[collapseIndicator.QuestIndex].Type];
                Redraw();
                break;
            }
        }

        popup.Click(position, MouseButtons.Left, out _);
    }

    public void ScrollTo(int y)
    {
        if (scrollOffset != y)
        {
            scrollOffset = y;
            Redraw();
        }
    }

    public void Show()
    {
        var area = Area;

        popup = game.Layout.OpenPopup(area.Position, Columns, Rows, true, false, 8);

        var boxArea = new Rect(area.Left + 16 + 3 - 2, area.Top + 16 - 2, area.Width - 32 - 16 + 3, area.Height - 32 - 16 + 2);
        popup.AddSunkenBox(boxArea);

        var closeButton = popup.AddButton(new Position(area.Right - 16 - Button.Width - 4, area.Bottom - 16 - Button.Height + 2));
        closeButton.ButtonType = Data.Enumerations.ButtonType.Ok;
        closeButton.LeftClickAction = () => game.Layout.ClosePopup();
        closeButton.Visible = true;

        popup.Closed += () => Open = false;

        var textArea = new Rect(area.Left + 16 + 3, area.Top + 16 - 2 + 1, area.Width - 32 - 16, area.Height - 32 - 16);

        for (int i = 0; i < TextLineCount; i++)
        {
            var text = texts[i] = popup.AddText(new Rect(textArea.X + 2, textArea.Y + i * Global.GlyphLineHeight, textArea.Width - 2, Global.GlyphLineHeight),
                "", TextColor.LightGray, TextAlign.Left);
            text.PaletteIndex = game.PrimaryUIPaletteIndex;
            text.Visible = false;
        }

        scrollbar = null;

        // Legend
        var legendAreaX = boxArea.Left + 1;
        var legendAreaY = boxArea.Bottom + 4;
        void AddColorRect(TextColor color)
        {
            popup.FillArea(new Rect(legendAreaX - 1, legendAreaY - 1, 8, 8), Color.DarkShadow, 1);
            popup.FillArea(new Rect(legendAreaX, legendAreaY, 6, 6), game.GetPrimaryUIColor((int)color), 2);
            legendAreaX += 9;
        }
        void AddLegendText(string text)
        {
            popup.AddText(new Position(legendAreaX, legendAreaY), text, TextColor.LightGray);
            legendAreaX += text.Length * Global.GlyphWidth + 4;
        }
        AddColorRect(QuestState.Active.ToColor());
        AddLegendText(QuestTexts.LegendActive[game.GameLanguage]);
        AddColorRect(QuestState.Blocked.ToColor());
        AddLegendText(QuestTexts.LegendBlocked[game.GameLanguage]);
        AddColorRect(QuestState.Completed.ToColor());
        AddLegendText(QuestTexts.LegendCompleted[game.GameLanguage]);

        Redraw();

        Open = true;
    }

    private static string TruncateText(string text, bool header)
    {
        int maxLength = header ? 44 : 43;
        return text.Length <= maxLength ? text : text[0..(maxLength - 2)] + "..";
    }

    private string GetMainQuestText(MainQuestType mainQuestType)
    {
        return TruncateText(QuestTexts.MainQuests[game.GameLanguage][mainQuestType], true);
    }

    private string GetSubQuestText(SubQuestType subQuestType)
    {
        return TruncateText(QuestTexts.SubQuests[game.GameLanguage][subQuestType], false);
    }

    private void Redraw()
    {
        foreach (var collapseIndicator in collapseIndicators)
            collapseIndicator.Destroy();

        collapseIndicators.Clear();

        var quests = Quests.Where(quest => quest.ToCompoundState() != QuestState.Inactive).ToArray();

        int visibleLines = 0;

        for (int i = 0; i < quests.Length; i++)
        {
            if (questGroups[quests[i].Type])
            {
                visibleLines += 1 + quests[i].SubQuests.Where(s => s.State != QuestState.Inactive).Count();
            }
            else
            {
                visibleLines += 1;
            }
        }

        int scrollRange = Math.Max(0, visibleLines - TextLineCount);

        if (scrollbar == null)
        {
            scrollbar = popup.AddScrollbar(game.Layout, scrollRange, 1, -1);
            scrollbar.Scrolled += ScrollTo;
            scrollOffset = Math.Min(scrollRange, scrollOffset);

            if (scrollOffset != 0)
            {
                scrollbar.SetScrollPosition(scrollOffset, false, true);
                Redraw();
            }
        }
        else
        {
            scrollbar.SetScrollRange(scrollRange);
            scrollbar.Disabled = scrollRange == 0;
            scrollOffset = Math.Min(scrollRange, scrollOffset);
        }

        var area = Area;
        var textArea = new Rect(area.Left + 16 + 3, area.Top + 16 - 2 + 1, area.Width - 32 - 16, area.Height - 32 - 16);

        int questIndex = 0;
        int subIndex = 0;
        int y = textArea.Y;
        bool header = true;
        int textIndex = 0;
        int iterationIndex = 0;

        while (true)
        {
            if (questIndex == quests.Length)
            {
                for (int j = textIndex; j < TextLineCount; j++)
                    texts[j].Visible = false;
                break;
            }

            var quest = quests[questIndex];
            var subQuests = quest.SubQuests.Reverse().OrderBy(q => q.State).ToArray();

            // TODO: hide completed subquests later

            if (!header && subIndex == subQuests.Length)
            {
                questIndex++;
                subIndex = 0;
                header = true;

                if (questIndex == quests.Length)
                {
                    for (int j = textIndex; j < TextLineCount; j++)
                        texts[j].Visible = false;
                    break;
                }

                quest = quests[questIndex];
            }

            if (header)
            {
                bool expanded = questGroups[quests[questIndex].Type];

                if (iterationIndex >= scrollOffset)
                {
                    collapseIndicators.Add(new(new Position(textArea.Left, y + 1), popup, expanded, questIndex));

                    var text = texts[textIndex++];
                    text.SetText(renderView.TextProcessor.CreateText(" " + GetMainQuestText(quest.Type)));
                    text.SetTextColor(quest.ToCompoundState().ToColor());
                    text.Visible = true;
                    y += 7;
                }

                if (expanded)
                    header = false; // if group is expanded, switch to no header
                else
                    questIndex++; // otherwise go to next main quest
            }
            else
            {
                var subQuest = subQuests[subIndex++];

                while (subQuest.State == QuestState.Inactive)
                {
                    if (subIndex == subQuests.Length)
                    {
                        header = true;
                        questIndex++;
                        break;
                    }

                    subQuest = subQuests[subIndex++];
                }

                if (header)
                    continue;

                if (iterationIndex >= scrollOffset)
                {
                    var text = texts[textIndex++];
                    text.SetText(renderView.TextProcessor.CreateText("  " + GetSubQuestText(subQuest.Type)));
                    text.SetTextColor(subQuest.State.ToColor());
                    text.Visible = true;
                    y += 7;
                }
            }

            iterationIndex++;

            if (textIndex == texts.Length)
                break;
        }
    }
}
