using Ambermoon.Data;
using Ambermoon.Mod.Packaging;
using Ambermoon.UI;
using Newtonsoft.Json;

namespace Ambermoon.Mod;

#region Definitions (quests.json)

/// <summary>
/// The content of the optional "quests.json" at the root of a mod.
/// </summary>
public class QuestFile
{
    public List<QuestDefinition> Quests { get; set; } = [];
}

/// <summary>
/// A main quest. It consists of steps (sub quests).
/// </summary>
public class QuestDefinition
{
    public string Id { get; set; } = "";
    /// <summary>
    /// Name per language ("en", "de", ...).
    /// </summary>
    public Dictionary<string, string> Name { get; set; } = [];
    public List<QuestStepDefinition> Steps { get; set; } = [];
}

public class QuestStepDefinition
{
    /// <summary>
    /// Unique id (in all quests of the mod).
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Text per language.
    /// </summary>
    public Dictionary<string, string> Text { get; set; } = [];
    /// <summary>
    /// Optional text per language which is shown when hovering the step.
    /// </summary>
    public Dictionary<string, string>? Hint { get; set; }
    /// <summary>
    /// If true, the step is active from the start of a new game.
    /// </summary>
    public bool Active { get; set; }
    /// <summary>
    /// If given, the step shows its progress like "(2/5)" and is completed when the amount is reached.
    /// Each fired completion trigger adds 1 (item triggers set the amount to the number of carried items).
    /// </summary>
    public int? Amount { get; set; }
    /// <summary>
    /// Steps which have to be completed first. Until then an active step is shown as blocked.
    /// </summary>
    public List<string> Requires { get; set; } = [];
    /// <summary>
    /// The step is activated when one of these triggers fires.
    /// </summary>
    public List<QuestTrigger> ActivateOn { get; set; } = [];
    /// <summary>
    /// The (active) step is completed when one of these triggers fires.
    /// </summary>
    public List<QuestTrigger> CompleteOn { get; set; } = [];
}

public enum QuestTriggerType
{
    /// <summary>
    /// Another step is completed (<see cref="QuestTrigger.Step"/>).
    /// </summary>
    StepCompleted,
    /// <summary>
    /// The party enters a map (<see cref="QuestTrigger.Map"/>, optionally at <see cref="QuestTrigger.X"/>/<see cref="QuestTrigger.Y"/>).
    /// </summary>
    Map,
    /// <summary>
    /// A map event is executed (<see cref="QuestTrigger.Map"/>, optionally at a position and of an <see cref="QuestTrigger.EventType"/>).
    /// </summary>
    Event,
    /// <summary>
    /// The party carries at least <see cref="QuestTrigger.Amount"/> items with the index <see cref="QuestTrigger.Item"/>.
    /// </summary>
    Item,
    /// <summary>
    /// A battle is won (optionally against the monster group <see cref="QuestTrigger.MonsterGroup"/>).
    /// </summary>
    Battle,
    /// <summary>
    /// A party member joins (optionally the one with the index <see cref="QuestTrigger.PartyMember"/>).
    /// </summary>
    PartyMemberJoined,
    /// <summary>
    /// A party member reaches the level <see cref="QuestTrigger.Level"/>.
    /// </summary>
    Level
}

public class QuestTrigger
{
    public QuestTriggerType Type { get; set; }
    public string? Step { get; set; }
    public uint? Map { get; set; }
    public uint? X { get; set; }
    public uint? Y { get; set; }
    public EventType? EventType { get; set; }
    public uint? Item { get; set; }
    public int Amount { get; set; } = 1;
    public uint? MonsterGroup { get; set; }
    public uint? PartyMember { get; set; }
    public uint? Level { get; set; }
}

#endregion

/// <summary>
/// The quests of a mod. They are defined in "quests.json" and replace the quests of the
/// original game in the quest log. If a mod has no quests, the quest log is disabled.
///
/// Steps are activated and completed by the triggers of the definition or by code
/// (<see cref="Activate"/>, <see cref="Complete"/>, <see cref="SetAmount"/>).
/// The progress is stored with each savegame.
/// </summary>
public class ModQuests : IQuestLogSource
{
    public const string FileName = "quests.json";
    const string SaveKey = "$quests";

    class StepProgress
    {
        public QuestState State { get; set; }
        public int Amount { get; set; }
    }

    readonly ModApi? api;
    readonly List<QuestDefinition> quests;
    readonly Dictionary<string, QuestStepDefinition> steps = [];
    Dictionary<string, StepProgress> progress = [];
    double itemCheckTime = 0.0;

    /// <summary>
    /// Raised when a step is completed (step id).
    /// </summary>
    public event Action<string>? StepCompleted;

    internal ModQuests(ModApi? api, QuestFile file)
    {
        this.api = api;
        quests = file.Quests;

        foreach (var step in quests.SelectMany(q => q.Steps))
            steps.Add(step.Id, step);

        Reset();
    }

    public bool HasQuests => quests.Count != 0;

    public IReadOnlyList<QuestDefinition> Definitions => quests;

    /// <summary>
    /// Parses and checks a quest file.
    /// </summary>
    public static QuestFile Parse(string json)
    {
        var file = JsonConvert.DeserializeObject<QuestFile>(json, ModJson.Settings) ?? new QuestFile();
        Validate(file);
        return file;
    }

    internal static ModQuests Load(ModApi? api, IModSource source)
        => new(api, source.Exists(FileName) ? Parse(source.ReadAllText(FileName)) : new QuestFile());

    static void Validate(QuestFile file)
    {
        var questIds = new HashSet<string>();
        var stepIds = new HashSet<string>();

        void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidDataException($"{FileName}: {message}");
        }

        foreach (var quest in file.Quests)
        {
            Require(!string.IsNullOrWhiteSpace(quest.Id) && questIds.Add(quest.Id), "Quest ids must be unique and not empty.");
            Require(quest.Name.Count != 0, $"Quest '{quest.Id}' needs a name.");
            Require(quest.Steps.Count != 0, $"Quest '{quest.Id}' needs at least one step.");

            foreach (var step in quest.Steps)
            {
                Require(!string.IsNullOrWhiteSpace(step.Id) && stepIds.Add(step.Id), "Step ids must be unique (in all quests) and not empty.");
                Require(step.Text.Count != 0, $"Step '{step.Id}' needs a text.");
                Require(step.Amount is null or > 0, $"The amount of step '{step.Id}' must be positive.");
            }
        }

        foreach (var step in file.Quests.SelectMany(q => q.Steps))
        {
            foreach (var required in step.Requires)
                Require(stepIds.Contains(required), $"Step '{step.Id}' requires the unknown step '{required}'.");

            foreach (var trigger in step.ActivateOn.Concat(step.CompleteOn))
            {
                string prefix = $"Trigger {trigger.Type} of step '{step.Id}'";

                switch (trigger.Type)
                {
                    case QuestTriggerType.StepCompleted:
                        Require(trigger.Step != null && stepIds.Contains(trigger.Step), $"{prefix} needs a known 'Step'.");
                        break;
                    case QuestTriggerType.Map:
                    case QuestTriggerType.Event:
                        Require(trigger.Map != null, $"{prefix} needs a 'Map'.");
                        Require((trigger.X == null) == (trigger.Y == null), $"{prefix} needs both 'X' and 'Y' or none of them.");
                        break;
                    case QuestTriggerType.Item:
                        Require(trigger.Item != null && trigger.Amount > 0, $"{prefix} needs an 'Item' and a positive 'Amount'.");
                        break;
                    case QuestTriggerType.Level:
                        Require(trigger.Level != null, $"{prefix} needs a 'Level'.");
                        break;
                }
            }
        }
    }

    #region State

    StepProgress Progress(string stepId)
    {
        if (!steps.ContainsKey(stepId))
            throw new KeyNotFoundException($"There is no quest step '{stepId}'.");

        return progress[stepId];
    }

    /// <summary>
    /// The stored state of a step (an active step whose required steps are not completed is shown as blocked).
    /// </summary>
    public QuestState GetState(string stepId) => Progress(stepId).State;

    public int GetAmount(string stepId) => Progress(stepId).Amount;

    bool IsBlocked(QuestStepDefinition step) => step.Requires.Any(required => progress[required].State != QuestState.Completed);

    /// <summary>
    /// Activates a step (if it is inactive).
    /// </summary>
    public void Activate(string stepId)
    {
        var stepProgress = Progress(stepId);

        if (stepProgress.State == QuestState.Inactive)
        {
            stepProgress.State = QuestState.Active;
            var step = steps[stepId];

            // The amount might have been reached before the step was activated.
            if (step.Amount != null && stepProgress.Amount >= step.Amount && !IsBlocked(step))
                Complete(stepId);
            else
                CheckStateTriggers();
        }
    }

    /// <summary>
    /// Completes a step (also if it is inactive).
    /// </summary>
    public void Complete(string stepId)
    {
        var stepProgress = Progress(stepId);

        if (stepProgress.State == QuestState.Completed)
            return;

        var step = steps[stepId];
        stepProgress.State = QuestState.Completed;

        if (step.Amount != null)
            stepProgress.Amount = Math.Max(stepProgress.Amount, step.Amount.Value);

        StepCompleted?.Invoke(stepId);

        foreach (var other in steps.Values.Where(s => progress[s.Id].State == QuestState.Inactive))
        {
            if (other.ActivateOn.Any(t => t.Type == QuestTriggerType.StepCompleted && t.Step == stepId))
                Activate(other.Id);
        }

        foreach (var other in steps.Values.Where(s => progress[s.Id].State == QuestState.Active))
        {
            if (other.CompleteOn.Any(t => t.Type == QuestTriggerType.StepCompleted && t.Step == stepId))
                CompleteOrProgress(other, 1);
        }

        CheckStateTriggers();
    }

    /// <summary>
    /// Sets the progress of a step with an amount. If the amount is reached, an active step is completed.
    /// </summary>
    public void SetAmount(string stepId, int amount)
    {
        var stepProgress = Progress(stepId);

        if (stepProgress.State == QuestState.Completed)
            return;

        var step = steps[stepId];
        stepProgress.Amount = Math.Max(0, amount);

        if (step.Amount != null && stepProgress.Amount >= step.Amount && stepProgress.State == QuestState.Active && !IsBlocked(step))
            Complete(stepId);
    }

    public void AddAmount(string stepId, int amount = 1) => SetAmount(stepId, GetAmount(stepId) + amount);

    /// <summary>
    /// Resets all steps to their initial state (done for new games).
    /// </summary>
    public void Reset()
    {
        progress = steps.Values.ToDictionary(s => s.Id, s => new StepProgress { State = s.Active ? QuestState.Active : QuestState.Inactive });
    }

    internal void Save(ModSaveData data)
    {
        if (HasQuests)
            data.SetObject(SaveKey, progress);
    }

    internal void Load(ModSaveData data)
    {
        Reset();

        var loaded = data.GetObject<Dictionary<string, StepProgress>>(SaveKey);

        if (loaded == null)
            return;

        // Steps which were removed in a newer mod version are ignored, new ones keep their initial state.
        foreach (var step in loaded.Where(s => progress.ContainsKey(s.Key)))
            progress[step.Key] = step.Value;
    }

    #endregion

    #region Triggers

    void CompleteOrProgress(QuestStepDefinition step, int amount)
    {
        if (IsBlocked(step))
            return;

        if (step.Amount == null)
            Complete(step.Id);
        else
            AddAmount(step.Id, amount);
    }

    void Fire(Func<QuestTrigger, bool> matches)
    {
        foreach (var step in steps.Values.Where(s => progress[s.Id].State == QuestState.Inactive).ToList())
        {
            if (step.ActivateOn.Any(t => t.Type != QuestTriggerType.StepCompleted && matches(t)))
                Activate(step.Id);
        }

        foreach (var step in steps.Values.Where(s => progress[s.Id].State == QuestState.Active).ToList())
        {
            int count = step.CompleteOn.Count(t => t.Type != QuestTriggerType.StepCompleted && matches(t));

            if (count != 0)
                CompleteOrProgress(step, 1);
        }
    }

    static bool MatchesPosition(QuestTrigger trigger, uint mapIndex, uint x, uint y) =>
        trigger.Map == mapIndex && (trigger.X == null || (trigger.X == x && trigger.Y == y));

    internal void OnPlayerMoved(Map map, uint x, uint y) =>
        Fire(t => t.Type == QuestTriggerType.Map && MatchesPosition(t, map.Index, x, y));

    internal void OnEvent(Map map, Event @event, uint x, uint y) =>
        Fire(t => t.Type == QuestTriggerType.Event && MatchesPosition(t, map.Index, x, y) && (t.EventType == null || t.EventType == @event.Type));

    internal void OnBattleEnded(uint monsterGroupIndex, bool monstersDefeated)
    {
        if (monstersDefeated)
            Fire(t => t.Type == QuestTriggerType.Battle && (t.MonsterGroup == null || t.MonsterGroup == monsterGroupIndex));
    }

    internal void OnPartyMemberJoined(PartyMember partyMember) =>
        Fire(t => t.Type == QuestTriggerType.PartyMemberJoined && (t.PartyMember == null || t.PartyMember == partyMember.Index));

    internal void OnUpdate(double deltaTime)
    {
        // Items and levels are checked twice per second.
        itemCheckTime += deltaTime;

        if (itemCheckTime < 0.5)
            return;

        itemCheckTime = 0.0;
        CheckStateTriggers();
    }

    internal void OnGameStarted() => CheckStateTriggers();

    /// <summary>
    /// Checks the triggers which depend on the current state of the party (items and levels).
    /// </summary>
    void CheckStateTriggers()
    {
        var party = api?.PartyMembers.ToList() ?? [];

        if (party.Count == 0)
            return;

        int CountItems(uint itemIndex) => party.Sum(p =>
            p.Inventory.Slots.Where(s => s.ItemIndex == itemIndex).Sum(s => s.Amount) +
            p.Equipment.Slots.Values.Where(s => s.ItemIndex == itemIndex).Sum(s => s.Amount));

        bool Fulfilled(QuestTrigger t) => t.Type switch
        {
            QuestTriggerType.Item => CountItems(t.Item!.Value) >= t.Amount,
            QuestTriggerType.Level => party.Any(p => p.Level >= t.Level),
            _ => false
        };

        foreach (var step in steps.Values.Where(s => progress[s.Id].State == QuestState.Inactive).ToList())
        {
            if (step.ActivateOn.Any(Fulfilled))
                Activate(step.Id);
        }

        foreach (var step in steps.Values.Where(s => progress[s.Id].State == QuestState.Active).ToList())
        {
            if (IsBlocked(step))
                continue;

            foreach (var trigger in step.CompleteOn)
            {
                if (trigger.Type == QuestTriggerType.Item && step.Amount != null)
                {
                    // The progress shows the number of carried items.
                    SetAmount(step.Id, CountItems(trigger.Item!.Value));
                }
                else if (Fulfilled(trigger))
                {
                    Complete(step.Id);
                }

                if (progress[step.Id].State == QuestState.Completed)
                    break;
            }
        }
    }

    #endregion

    #region Quest log

    public IReadOnlyList<QuestLogEntry> GetEntries(GameLanguage language)
    {
        return [.. quests.Select(quest => new QuestLogEntry(quest.Id, ModText.Get(quest.Name, language), [.. quest.Steps.Select(step =>
        {
            var stepProgress = progress[step.Id];
            var state = stepProgress.State == QuestState.Active && IsBlocked(step) ? QuestState.Blocked : stepProgress.State;
            string text = ModText.Get(step.Text, language);

            if (step.Amount != null)
                text += $" ({Math.Min(stepProgress.Amount, step.Amount.Value)}/{step.Amount.Value})";

            return new QuestLogSubEntry(text, state, step.Hint == null ? null : ModText.Get(step.Hint, language));
        })]))];
    }

    #endregion
}

/// <summary>
/// Texts of mods per language ("en", "de", "fr", "pl", "cz"). English is the fallback.
/// </summary>
public static class ModText
{
    public static string LanguageKey(GameLanguage language) => language switch
    {
        GameLanguage.German => "de",
        GameLanguage.French => "fr",
        GameLanguage.Polish => "pl",
        GameLanguage.Czech => "cz",
        _ => "en"
    };

    public static string Get(IReadOnlyDictionary<string, string> texts, GameLanguage language)
        => texts.TryGetValue(LanguageKey(language), out var text) ? text
            : texts.TryGetValue("en", out text) ? text
            : texts.Values.FirstOrDefault() ?? "";
}
