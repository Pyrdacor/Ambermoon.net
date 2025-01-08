using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;
using Color = Ambermoon.Render.Color;

namespace Ambermoon.UI;

public interface IQuestTrigger
{
    void Check(Event @event, Item item, uint itemCount, SubQuest subQuest);
    QuestState NewState { get; }
}

public class GlobalVariableTrigger(Game game, QuestState newState, uint index, bool expectedState = true) : IQuestTrigger
{
    public QuestState NewState => newState;

    public void Check(Event @event, Item item, uint itemCount, SubQuest subQuest)
    {
        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetGlobalVariable && e.ObjectIndex == index && e.Value == (expectedState ? 1 : 0))
            subQuest.Complete();
        else if (game.CurrentSavegame.GetGlobalVariable(index) == expectedState)
            subQuest.Complete();
    }
}

public class TileChangeTrigger(Game game, QuestState newState, uint mapIndex, uint x, uint y, uint expectedTile) : IQuestTrigger
{
    public QuestState NewState => newState;

    public void Check(Event @event, Item item, uint itemCount, SubQuest subQuest)
    {
        if (@event is ChangeTileEvent e && e.MapIndex == mapIndex && e.X == x && e.Y == y && e.FrontTileIndex == expectedTile)
            subQuest.Complete();
        else if (game.CurrentSavegame.TileChangeEvents.TryGetValue(mapIndex, out var changes) &&
            changes.Any(c => c.X == x && c.Y == y && c.FrontTileIndex == expectedTile))
            subQuest.Complete();
    }
}

public class EventDisabledTrigger(Game game, QuestState newState, uint mapIndex, uint eventIndex) : IQuestTrigger
{
    public QuestState NewState => newState;

    public void Check(Event @event, Item item, uint itemCount, SubQuest subQuest)
    {
        uint index = (mapIndex - 1) * 64 + eventIndex;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetEventBit && e.ObjectIndex == index && e.Value == 1)
            subQuest.Complete();
        else if (game.CurrentSavegame.GetEventBit(mapIndex, eventIndex))
            subQuest.Complete();
    }
}

public class CharacterVisibilityTrigger(Game game, QuestState newState, uint mapIndex, uint characterIndex, bool visible) : IQuestTrigger
{
    public QuestState NewState => newState;

    public void Check(Event @event, Item item, uint itemCount, SubQuest subQuest)
    {
        uint index = (mapIndex - 1) * 32 + characterIndex;

        if (@event is ActionEvent e && e.TypeOfAction == ActionEvent.ActionType.SetCharacterBit && e.ObjectIndex == index && e.Value == (visible ? 0 : 1))
            subQuest.Complete();
        else if (game.CurrentSavegame.GetCharacterBit(mapIndex, characterIndex) != visible) // if the bit is true, the character is invisible and vice versa
            subQuest.Complete();
    }
}

public class ItemObtainedTrigger(QuestState newState, uint itemIndex) : IQuestTrigger
{
    public QuestState NewState => newState;

    public void Check(Event @event, Item item, uint itemCount, SubQuest subQuest)
    {
        if (item != null && item.Index == itemIndex)
        {
            while (subQuest.CompletionCount < subQuest.MaxCompletionCount)
            {
                if (subQuest.MinAmount == 0 || subQuest.MinAmount < subQuest.CurrentAmount)
                {
                    subQuest.Complete();
                    return;
                }

                uint countToAdd = Math.Min(itemCount, (uint)(subQuest.MinAmount - subQuest.CurrentAmount));

                if (countToAdd == 0)
                    break;

                subQuest.CurrentAmount += countToAdd;

                if (subQuest.CurrentAmount == subQuest.MinAmount)
                {
                    subQuest.Complete();
                }
            }
        }
    }
}

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
            QuestState.Active => TextColor.Yellow,
            QuestState.Blocked => TextColor.DarkBrown,
            _ => TextColor.DarkerGray,
        };
    }

    public static QuestState ToCompoundState(this MainQuest quest)
    {
        var states = quest.SubQuests.Select(q => q.State).Distinct().ToArray();

        if (states.Length == 1 && states[0] == QuestState.Completed)
            return QuestState.Completed;

        if (states.Contains(QuestState.Blocked))
            return QuestState.Blocked;

        if (states.Contains(QuestState.Inactive))
            return QuestState.Inactive;

        return QuestState.Active;
    }
}

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
    Grandfather_GoToWineCellar,
    Grandfather_FindHisEquipment,
    Grandfather_RemoveCaveIn,
    Grandfather_VisitAntonius,
    // Swamp Fever
    SwampFever_ObtainEmptyBottle,
    SwampFever_ObtainSwampLilly,
    SwampFever_ObtainWaterOfLife,
    // ...
}

public record SubQuest(MainQuest Quest, QuestState initialState = QuestState.Inactive)
{
    public required IQuestTrigger Trigger { get; init; }

    public required SubQuestType Type { get; init; }

    public SubQuestType[] RequiredCompletedQuests { get; init; } = [];

    public SubQuestType[] AnyCompletedQuests { get; init; } = [];

    public SubQuestType? AddAfter { get; init; } = null; // null means append to the end

    public CharacterType? SourceType { get; init; } = null;

    public uint? SourceIndex { get; init; } = null;

    public uint? MinAmount { get; init; } = null;

    public uint CurrentAmount { get; set; } = 0;

    public int CompletionCount { get; private set; } = 0;

    public int MaxCompletionCount { get; set; } = 1; // use int.MaxValue for unlimited completions

    public QuestState State { get; private set; } = initialState;

    public void Complete()
    {
        if (State != QuestState.Completed)
        {
            CurrentAmount = 0;

            if (++CompletionCount >= MaxCompletionCount)
            {
                State = QuestState.Completed;
                Quest.SubQuestCompleted(this);
            }
        }
    }

    public void Update(MainQuest[] allQuests)
    {
        if (State == QuestState.Active)
        {
            var blocked = allQuests.SelectMany(q => q.SubQuests).Where(sq => sq.RequiredCompletedQuests.Contains(Type) && sq.State != QuestState.Completed).Any();

            if (!blocked && AnyCompletedQuests.Length > 0 && allQuests.SelectMany(q => q.SubQuests).Where(sq => AnyCompletedQuests.Contains(sq.Type) && sq.State == QuestState.Completed).Count() == 0)
                blocked = true;

            if (blocked)
                State = QuestState.Blocked;
        }
        else if (State == QuestState.Blocked)
        {
            var unblocked = allQuests.SelectMany(q => q.SubQuests).Where(sq => sq.RequiredCompletedQuests.Contains(Type) && sq.State == QuestState.Completed).Any();

            if (unblocked && AnyCompletedQuests.Length > 0 && allQuests.SelectMany(q => q.SubQuests).Where(sq => AnyCompletedQuests.Contains(sq.Type) && sq.State == QuestState.Completed).Count() == 0)
                unblocked = false;

            if (unblocked)
                State = QuestState.Active;
        }
    }
}

public record MainQuest(QuestLog QuestLog)
{
    public MainQuestType Type { get; init; }
    public SubQuest[] SubQuests { get; init; }

    public void SubQuestCompleted(SubQuest subQuest)
    {
        foreach (var quest in QuestLog.Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed && q.State != QuestState.Inactive))
        {
            if (quest != subQuest)
                quest.Update(QuestLog.Quests);
        }

        if (this.ToCompoundState() == QuestState.Completed) // all completed?
            QuestLog.CompleteMainQuest(Type);
    }
}

file static class QuestFactory
{
    public static MainQuest CreateMainQuest(QuestLog questLog, MainQuestType mainQuestType, params Func<MainQuest, SubQuest>[] subQuestFactory)
    {
        var mainQuest = new MainQuest(questLog)
        {
            Type = mainQuestType,
            SubQuests = []
        };

        var subQuests = subQuestFactory.Select(factory => factory(mainQuest)).ToArray();

        return mainQuest with { SubQuests = subQuests };
    }
}

public class QuestLog
{
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

        // TODO ...
        Quests =
        [
            #region Original quests
            QuestFactory.CreateMainQuest(this, MainQuestType.Grandfather,
                mainQuest => new SubQuest(mainQuest, QuestState.Completed)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0), // TODO
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = CharacterType.NPC,
                    SourceIndex = 1,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Completed)
                {
                    Type = SubQuestType.Grandfather_FindHisEquipment,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0), // TODO
                    RequiredCompletedQuests = [SubQuestType.Grandfather_GoToWineCellar],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = CharacterType.NPC,
                    SourceIndex = 1,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Completed)
                {
                    Type = SubQuestType.Grandfather_RemoveCaveIn,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0), // TODO
                    RequiredCompletedQuests = [SubQuestType.Grandfather_FindHisEquipment],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = CharacterType.NPC,
                    SourceIndex = 1,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_VisitAntonius,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0), // TODO
                    RequiredCompletedQuests = [SubQuestType.Grandfather_RemoveCaveIn],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = CharacterType.NPC,
                    SourceIndex = 2,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.SwampFever,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.SwampFever_ObtainSwampLilly,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Blocked)
                {
                    Type = SubQuestType.SwampFever_ObtainEmptyBottle,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Blocked)
                {
                    Type = SubQuestType.SwampFever_ObtainWaterOfLife,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.AlkemsRing,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.ThiefPlague,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.OrcPlague,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.Sylphs,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.WineTrophies,
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Active)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                }),
            QuestFactory.CreateMainQuest(this, MainQuestType.GoldenHorseshoes,
                mainQuest => new SubQuest(mainQuest, QuestState.Inactive)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                },
                mainQuest => new SubQuest(mainQuest, QuestState.Inactive)
                {
                    Type = SubQuestType.Grandfather_GoToWineCellar,
                    Trigger = new GlobalVariableTrigger(game, QuestState.Active, 0),
                    RequiredCompletedQuests = [],
                    AnyCompletedQuests = [],
                    AddAfter = null,
                    SourceType = null,
                    SourceIndex = null,
                    MinAmount = null,
                    MaxCompletionCount = 1
                })
            #endregion
            #region Advanced quests
            #endregion
        ];

        foreach (var quest in Quests)
        {
            questGroups.Add(quest.Type, true);
        }
    }

    internal void CompleteMainQuest(MainQuestType questType)
    {
        questGroups[questType] = false; // collapse completed quests
    }

    public void Check(Event @event)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            quest.Trigger.Check(@event, null, 0, quest);
        }
    }

    public void Check(Item item, uint itemCount)
    {
        foreach (var quest in Quests.SelectMany(q => q.SubQuests).Where(q => q.State != QuestState.Completed))
        {
            quest.Trigger.Check(null, item, itemCount, quest);
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
        closeButton.LeftClickAction = () =>
        {
            Open = false;
            game.Layout.ClosePopup();
        };
        closeButton.Visible = true;

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

            if (!header && subIndex == quest.SubQuests.Length)
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
                var subQuest = quest.SubQuests[subIndex++];

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
