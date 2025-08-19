using Ambermoon.Data;
using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using TextColor = Ambermoon.Data.Enumerations.Color;
using Color = Ambermoon.Render.Color;

namespace Ambermoon.UI;


#region Triggers

public interface IQuestTrigger
{
    bool CheckEvent(Event @event, SubQuest subQuest) { return false; }
    bool CheckExploration(uint mapIndex, uint x, uint y, SubQuest subQuest) { return false; }
    bool CheckItem(Item item, uint itemCount, SubQuest subQuest) { return false; }
    bool CheckChestUnlock(uint chestIndex, SubQuest subQuest) { return false; }
    bool CheckDoorUnlock(uint doorIndex, SubQuest subQuest) { return false; }
    QuestState OldState { get; }
    QuestState NewState { get; }
}

#endregion


#region Quest State

public enum QuestState
{
    Any = -1,
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

    private SubQuestType[] requiredCompletedQuests = [];

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
    public SubQuestType[] RequiredCompletedQuests
    {
        get => requiredCompletedQuests;
        init => requiredCompletedQuests = value;
    }

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

    public Action<SubQuest> PostActivationAction { get; init; } = null;

    public Action<SubQuest> PostCompletionAction { get; init; } = null;

    /// <summary>
    /// Most quests can only be completed once which is the default.
    /// But some might be completed multiple times.
    /// </summary>
    public bool CanBeRepeated { get; set; } = false;

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
                    PostCompletionAction?.Invoke(this);
                    Quest.SubQuestCompleted(this);

                    if (CanBeRepeated)
                        state = QuestState.Active;
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
        requiredCompletedQuests = [.. RequiredCompletedQuests, subQuestType];
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


public partial class QuestLog
{
    public const uint IconGraphicIndex = 1000u;
    const int Columns = 20;
    const int Rows = 10;
    const int X = 0;
    const int Y = 38;
    const int TextLineCount = 16;

    private static readonly Rect Area = new(X, Y, Columns * 16, Rows * 16);

    private readonly Game game;
    private readonly IGameRenderView renderView;
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

    public QuestLog(Game game, IGameRenderView renderView, bool advanced, int episode)
    {
        this.game = game;
        this.renderView = renderView;

        InitCustomGlobalVariableEvents();

        Quests = CreateQuests(advanced, episode);

        foreach (var quest in Quests)
        {
            questGroups.Add(quest.Type, true);
        }

        // Directly check for quest states
        CheckEvent(null, null);
        CheckItem(null, 0);
        CheckExploration(0, 0, 0);
        CheckChestUnlock(uint.MaxValue);
        CheckDoorUnlock(uint.MaxValue);
    }

    internal void CompleteMainQuest(MainQuestType questType)
    {
        questGroups[questType] = false; // collapse completed quests
    }

    public void CheckEvent(IEventProvider eventProvider, Event @event)
    {
        if (eventProvider != null)
        {
            foreach (var customGlobalVariableEvent in customGlobalVariableEvents)
                customGlobalVariableEvent.Check(eventProvider, @event);
        }

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

    public void Hover(Position position) => popup?.Hover(position);

    public void ScrollTo(int y)
    {
        if (scrollOffset != y)
        {
            scrollOffset = y;
            Redraw();
        }
    }

    private void ToggleCompletedQuests()
    {
        game.Configuration.ShowCompletedQuests = !game.Configuration.ShowCompletedQuests;
        Redraw();
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

        var eyeButton = popup.AddButton(new Position(legendAreaX + 2, area.Bottom - 16 - Button.Height + 2));
        eyeButton.ButtonType = Data.Enumerations.ButtonType.Eye;
        eyeButton.ToggleButton = true;
        eyeButton.Pressed = game.Configuration.ShowCompletedQuests;
        eyeButton.LeftClickAction = ToggleCompletedQuests;
        eyeButton.Tooltip = game.Configuration.ShowButtonTooltips ? QuestTexts.ShowCompletedQuestsTooltip[game.GameLanguage] : null;
        eyeButton.Visible = true;

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

    private string GetSubQuestText(SubQuestType subQuestType, uint currentAmount, uint? minAmount)
    {
        string text = QuestTexts.SubQuests[game.GameLanguage][subQuestType];

        if (minAmount != null)
        {
            text += $" ({Math.Min(currentAmount, minAmount.Value)}/{minAmount.Value})";
        }

        return TruncateText(text, false);
    }

    class SubQuestSorter(MainQuest mainQuest) : IComparer<SubQuest>
    {
        public int Compare(SubQuest x, SubQuest y)
        {
            // If state differs, sort by it. Higher ones come later (completed last).
            // Inactive ones would be first but they are not in the list anyways.
            if (x.State != y.State)
                return x.State.CompareTo(y.State);

            // If states match, we check for dependencies.
            if (IsDependentOn(x, y))
                return -1; // if x depends on y, move it up as it has to be done later
            else if (IsDependentOn(y, x))
                return 1; // other way around this time

            return GetIndex(y).CompareTo(GetIndex(x)); // default, reverse order (newest first)
        }

        private SubQuest GetPredecessor(SubQuest subQuest)
        {
            int index = GetIndex(subQuest);

            if (index == 0)
                return null;

            return mainQuest.SubQuests[index - 1];
        }

        private int GetIndex(SubQuest subQuest) => mainQuest.SubQuests.ToList().IndexOf(subQuest);

        private bool IsDependentOn(SubQuest check, SubQuest target)
        {
            if (check.RequiredCompletedQuests.Length == 0 && check.AnyCompletedQuests.Length == 0)
                return false;

            if (check.RequiredCompletedQuests.Contains(target.Type) || check.AnyCompletedQuests.Contains(target.Type))
                return true;

            target = GetPredecessor(target);

            if (target == null || check == target)
                return false;

            return IsDependentOn(check, target);
        }
    }

    private void Redraw()
    {
        foreach (var collapseIndicator in collapseIndicators)
            collapseIndicator.Destroy();

        collapseIndicators.Clear();

        Func<QuestState, bool> filter = game.Configuration.ShowCompletedQuests
            ? state => state != QuestState.Inactive
            : state => state != QuestState.Inactive && state != QuestState.Completed;

        var quests = Quests.Where(quest => filter(quest.ToCompoundState())).ToArray();

        int visibleLines = 0;

        for (int i = 0; i < quests.Length; i++)
        {
            if (questGroups[quests[i].Type])
            {
                visibleLines += 1 + quests[i].SubQuests.Where(s => filter(s.State)).Count();
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
            var subQuests = quest.SubQuests.Where(s => filter(s.State)).OrderBy(q => q, new SubQuestSorter(quest)).ToArray();

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
                    text.SetText(renderView.TextProcessor.CreateText("  " + GetSubQuestText(subQuest.Type, subQuest.CurrentAmount, subQuest.MinAmount)));
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
