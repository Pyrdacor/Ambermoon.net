using Ambermoon.Data;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.UI;

/// <summary>
/// A sub quest (step) of an external quest log source.
/// </summary>
/// <param name="Text">Text of the step (incl. an optional amount like "(2/5)").</param>
/// <param name="Tooltip">Optional text which is shown when hovering the step.</param>
public record QuestLogSubEntry(string Text, QuestState State, string? Tooltip = null);

/// <summary>
/// A main quest of an external quest log source.
/// </summary>
/// <param name="Key">Unique key (used to remember if the quest is expanded).</param>
public record QuestLogEntry(string Key, string Text, IReadOnlyList<QuestLogSubEntry> SubEntries)
{
    /// <summary>
    /// State of the quest which is derived from its steps.
    /// </summary>
    public QuestState State
    {
        get
        {
            var states = SubEntries.Select(s => s.State).Distinct().ToArray();

            if (states.Length != 0 && states.All(s => s == QuestState.Completed))
                return QuestState.Completed;
            if (states.Contains(QuestState.Active))
                return QuestState.Active;
            if (states.Contains(QuestState.Blocked))
                return QuestState.Blocked;

            return states.Contains(QuestState.Completed) ? QuestState.Active : QuestState.Inactive;
        }
    }
}

/// <summary>
/// Provides the quests of the quest log instead of the quests of the original game (used by mods).
/// </summary>
public interface IQuestLogSource
{
    /// <summary>
    /// All main quests in display order. Inactive quests and steps are not shown.
    /// </summary>
    IReadOnlyList<QuestLogEntry> GetEntries(GameLanguage language);
}
