using Ambermoon.Data.Enumerations;
using System;
using System.Globalization;
using System.Linq;

namespace Ambermoon.Data
{
    public enum EventType
    {
        /// <summary>
        /// This is not used
        /// </summary>
        Invalid,
        /// <summary>
        /// Map transitions, teleporters, windgates, etc.
        /// </summary>
        Teleport,
        /// <summary>
        /// Locked doors
        /// </summary>
        Door,
        /// <summary>
        /// Chests and lootable map objects.
        /// Also used for locked chests.
        /// </summary>
        Chest,
        /// <summary>
        /// Shows a text popup
        /// </summary>
        PopupText,
        /// <summary>
        /// Rotates the player to a random direction
        /// </summary>
        Spinner,
        /// <summary>
        /// Hurts the player (map traps, fire, chest/door traps, etc)
        /// </summary>
        Trap,
        /// <summary>
        /// Removes one or all buffs
        /// </summary>
        RemoveBuffs,
        /// <summary>
        /// Opens the riddlemouth window with some riddle
        /// </summary>
        Riddlemouth,
        /// <summary>
        /// Awards, rewards, punishments
        /// </summary>
        Award,
        /// <summary>
        /// Change map tiles.
        /// </summary>
        ChangeTile,
        /// <summary>
        /// Starts a battle with a monster group.
        /// </summary>
        StartBattle,
        /// <summary>
        /// Enters a place like merchants, healer, etc.
        /// </summary>
        EnterPlace,
        /// <summary>
        /// Tests some condition
        /// </summary>
        Condition,
        /// <summary>
        /// Executes some action
        /// </summary>
        Action,
        /// <summary>
        /// Dice roll against some percent value
        /// </summary>
        Dice100Roll,
        /// <summary>
        /// Trigger for a conversation action.
        /// Starts a conversation event chain by user input.
        /// </summary>
        Conversation,
        /// <summary>
        /// Prints conversation text (conversation only)
        /// </summary>
        PrintText,
        /// <summary>
        /// Create items (conversation only)
        /// </summary>
        Create,
        /// <summary>
        /// Yes/No popup with text
        /// </summary>
        Decision,
        /// <summary>
        /// Changes music to a new song or the default map song
        /// </summary>
        ChangeMusic,
        /// <summary>
        /// Exits conversations (conversation only)
        /// </summary>
        Exit,
        /// <summary>
        /// Spawns transports like ships or horses
        /// </summary>
        Spawn,
        /// <summary>
        /// Executes conversation actions like giving the item/gold/food or join/leave the party (conversation only)
        /// </summary>
        Interact,
        Unknown
    }

    public class Event
    {
        public uint Index { get; set; }
        public EventType Type { get; set; }
        public Event Next { get; set; }
    }

    public class TeleportEvent : Event
    {
        public enum TransitionType
        {
            MapChange, // with black fading
            Teleporter, // without black fading
            WindGate, // you need the wind chain to use it
            Climbing, // moving up (levitating or climbing up)
            Outro, // teleport to outro sequence
            Falling // moving down (falling or climbing down)
        }

        public uint MapIndex { get; set; }
        public uint X { get; set; }
        public uint Y { get; set; }
        public CharacterDirection Direction { get; set; }
        public TravelType? NewTravelType { get; set; }
        public TransitionType Transition { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            var position = X == 0 || Y == 0 ? "same" : $"{X},{Y}";
            return $"{Type}: Map {MapIndex} / Position {position} / Direction {Direction}, Transition {Transition}, New Travel Type {NewTravelType?.ToString() ?? "None"}, Unknown3 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class DoorEvent : Event
    {
        public uint LockpickingChanceReduction { get; set; }
        public byte DoorIndex { get; set; }
        public uint TextIndex { get; set; }
        public uint UnlockTextIndex { get; set; }
        public byte Unused { get; set; }
        public uint KeyIndex { get; set; }
        public uint UnlockFailedEventIndex { get; set; }

        public override string ToString()
        {
            string lockType = LockpickingChanceReduction == 0 ? "Open" : LockpickingChanceReduction >= 100 ? "No Lockpicking" : $"-{LockpickingChanceReduction}% Chance";
            return $"{Type}: Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Lock=[{lockType}], Event index if unlock failed {UnlockFailedEventIndex:x4}, Text {(TextIndex == 0xff ? "none" : TextIndex.ToString())}, UnlockText {(UnlockTextIndex == 0xff ? "none" : UnlockTextIndex.ToString())}, Door Index {DoorIndex}";
        }
    }

    public class ChestEvent : Event
    {
        [Flags]
        public enum ChestFlags : byte
        {
            None = 0,
            Unknown0 = 0x01,
            SearchSkillCheck = 0x02,
            Unknown2 = 0x04,
            Unknown3 = 0x08,
            Unknown4 = 0x10,
            Unknown5 = 0x20
        }

        [Flags]
        public enum ChestLootFlags : byte
        {
            None = 0,
            /// <summary>
            /// Close the chest window when looted.
            /// Also remove the event when <see cref="NoAutoRemove"/> is false.
            /// 
            /// This can also be interpreted as "temporary chest" which can't
            /// store any new items.
            /// </summary>
            CloseWhenEmpty = 0x01,
            /// <summary>
            /// Only considered if <see cref="CloseWhenEmpty"/> is set.
            /// If true the chest (event) is not removed automatically.
            /// </summary>
            NoAutoRemove = 0x02
        }

        public uint LockpickingChanceReduction { get; set; }
        public uint TextIndex { get; set; } // 255 = none
        /// <summary>
        /// Note: This is 0-based but the files might by 1-based.
        /// </summary>
        public uint ChestIndex { get; set; }
        public ChestLootFlags LootFlags { get; set; }
        public bool CloseWhenEmpty => LootFlags.HasFlag(ChestLootFlags.CloseWhenEmpty);
        public bool AutoRemove => CloseWhenEmpty && !LootFlags.HasFlag(ChestLootFlags.NoAutoRemove);
        public uint KeyIndex { get; set; }
        public uint UnlockFailedEventIndex { get; set; }
        /// <summary>
        /// Only 1 chest uses this and it has the following bits set:
        /// - SearchSkillCheck
        /// - Unknown4
        /// - Unknown5
        /// So at least the lowest 6 bits seem to have some meaning.
        /// 
        /// The Ambermoon code just checks the whole byte for != 0 and then performs the search skill check.
        /// Maybe it had some other meaning in Amberstar.
        /// </summary>
        public ChestFlags Flags { get; set; }
        public bool SearchSkillCheck => Flags != 0;

        public override string ToString()
        {
            string lockType = LockpickingChanceReduction == 0 ? "Open" : LockpickingChanceReduction >= 100 ? "No Lockpicking" : $"-{LockpickingChanceReduction}% Chance";
            return $"{Type}: Chest {ChestIndex}, Lock=[{lockType}], LootFlags={LootFlags}, Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Event index if unlock failed {UnlockFailedEventIndex:x4}, Text {(TextIndex == 0xff ? "none" : TextIndex.ToString())}, Flags: {Flags}";
        }
    }

    /// <summary>
    /// This is used for text popups and traps.
    /// </summary>
    [Flags]
    public enum EventTrigger
    {
        None = 0,
        Move = 0x01,
        EyeCursor = 0x02,
        Always = Move | EyeCursor
    }

    public class PopupTextEvent : Event
    {
        public enum Response
        {
            Close,
            Yes,
            No
        }

        public uint TextIndex { get; set; }
        /// <summary>
        /// From event_pix (0-based). 0xff -> no image.
        /// </summary>
        public uint EventImageIndex { get; set; }
        public bool HasImage => EventImageIndex != 0xff;
        public EventTrigger PopupTrigger { get; set; }
        public bool CanTriggerByMoving => PopupTrigger.HasFlag(EventTrigger.Move);
        public bool CanTriggerByCursor => PopupTrigger.HasFlag(EventTrigger.EyeCursor);
        public bool TriggerIfBlind { get; set; }
        public byte[] Unknown { get; set; }

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Image {(EventImageIndex == 0xff ? "None" : EventImageIndex.ToString())}, Trigger {PopupTrigger}, {(TriggerIfBlind ? "" : "Not ")}Trigger If Blind, Unknown {string.Join(" ", Unknown.Select(u => u.ToString("x2")))}";
        }
    }

    public class SpinnerEvent : Event
    {
        public CharacterDirection Direction { get; set; }
        public byte[] Unused { get; set; }

        public override string ToString()
        {
            return $"{Type}: Direction {Direction}";
        }
    }

    public class TrapEvent : Event
    {
        public enum TrapAilment
        {
            None,
            Crazy,
            Blind,
            Stoned,
            Paralyzed,
            Poisoned,
            Petrified,
            Diseased,
            Aging,
            Dead
        }

        public enum TrapTarget
        {
            ActivePlayer,
            All
        }

        public TrapAilment Ailment { get; set; }
        public TrapTarget Target { get; set; }
        /// <summary>
        /// Base damage. Sometimes direct value but maybe in percentage of max health for other TrapType than 0?
        /// </summary>
        public byte BaseDamage { get; set; }
        public GenderFlag AffectedGenders { get; set; }
        public byte[] Unused { get; set; } // 5 bytes
        public Condition GetAilment() => Ailment switch
        {
            TrapAilment.Crazy => Data.Condition.Crazy,
            TrapAilment.Blind => Data.Condition.Blind,
            TrapAilment.Stoned => Data.Condition.Drugged,
            TrapAilment.Paralyzed => Data.Condition.Lamed,
            TrapAilment.Poisoned => Data.Condition.Poisoned,
            TrapAilment.Petrified => Data.Condition.Petrified,
            TrapAilment.Diseased => Data.Condition.Diseased,
            TrapAilment.Aging => Data.Condition.Aging,
            TrapAilment.Dead => Data.Condition.DeadCorpse,
            _ => Data.Condition.None
        };

        public override string ToString()
        {
            return $"{Type}: {BaseDamage} damage with ailment {Ailment} on {Target}, Affected genders {AffectedGenders}";
        }
    }

    public class RemoveBuffsEvent : Event
    {
        /// <summary>
        /// null means all.
        /// </summary>
        public ActiveSpellType? AffectedBuff { get; set; }
        public byte[] Unused { get; set; } // TODO: Maybe some byte is used but was 0 in test case?

        public override string ToString()
        {
            return $"{Type}: Affected buff {(AffectedBuff == null ? "all" : AffectedBuff.ToString())}, Unused {string.Join(" ", Unused.Select(u => u.ToString("x2")))}";
        }
    }

    public class RiddlemouthEvent : Event
    {
        public uint RiddleTextIndex { get; set; }
        public uint SolutionTextIndex { get; set; }
        public uint CorrectAnswerDictionaryIndex1 { get; set; }
        public uint CorrectAnswerDictionaryIndex2 { get; set; }
        public byte[] Unused { get; set; }

        public override string ToString()
        {
            string answerIndices = CorrectAnswerDictionaryIndex1 == CorrectAnswerDictionaryIndex2
                ? $"AnswerIndex {CorrectAnswerDictionaryIndex1}"
                : $"AnswerIndices {CorrectAnswerDictionaryIndex1} or {CorrectAnswerDictionaryIndex2}";
            return $"{Type}: RiddleText {RiddleTextIndex}, SolvedText {SolutionTextIndex}, {answerIndices}";
        }
    }

    public class AwardEvent : Event
    {
        public enum AwardType
        {
            Attribute = 0x00,
            Ability = 0x01,
            HitPoints = 0x02,
            SpellPoints = 0x03,
            SpellLearningPoints = 0x04,
            Ailments = 0x05,
            UsableSpellTypes = 0x06,
            Languages = 0x07,
            Experience = 0x08,
            TrainingPoints = 0x09
        }

        public enum AwardOperation
        {
            Increase,
            Decrease,
            IncreasePercentage,
            DecreasePercentage,
            Fill,
            Remove, // Clear bit
            Add, // Set bit
            Toggle // Toggle bit
        }

        // Note: This is actually a bool so there are only those two targets.
        public enum AwardTarget
        {
            ActivePlayer,
            All
        }

        public AwardType TypeOfAward { get; set; }
        public AwardTarget Target { get; set; }
        public AwardOperation Operation { get; set; }
        /// <summary>
        /// If set the real value is random in the range 0 to Value.
        /// </summary>
        public bool Random { get; set; }
        public ushort AwardTypeValue { get; set; }
        public Attribute? Attribute => TypeOfAward == AwardType.Attribute ? (Attribute)AwardTypeValue : (Attribute?)null;
        public Skill? Ability => TypeOfAward == AwardType.Ability ? (Skill)AwardTypeValue : (Skill?)null;
        public Language? Languages => TypeOfAward == AwardType.Languages ? (Language)(1 << AwardTypeValue) : (Language?)null;
        public Condition? Ailments => TypeOfAward == AwardType.Ailments ? (Condition)(1 << AwardTypeValue) : (Condition?)null;
        public SpellTypeMastery? UsableSpellTypes => TypeOfAward == AwardType.UsableSpellTypes ? (SpellTypeMastery)(1 << AwardTypeValue) : (SpellTypeMastery?)null;
        public uint Value { get; set; }
        public byte Unknown { get; set; }

        public override string ToString()
        {
            string operationString = Operation switch
            {
                AwardOperation.Increase => Random ? $"+rand(0~{Value})" : $"+{Value}",
                AwardOperation.Fill => "max",
                _ => $"?op={(int)Operation}?"
            };

            return TypeOfAward switch
            {
                AwardType.Attribute => $"{Type}: {Attribute} on {Target} {operationString}, Unknown {Unknown:x2}",
                AwardType.Ability => $"{Type}: {Ability} on {Target} {operationString}, Unknown {Unknown:x2}",
                AwardType.HitPoints => $"{Type}: HP on {Target} {operationString}, Unknown {Unknown:x2}",
                AwardType.SpellPoints => $"{Type}: SP on {Target} {operationString}, Unknown {Unknown:x2}",
                AwardType.SpellLearningPoints => $"{Type}: SLP on {Target} {operationString}, Unknown {Unknown:x2}",
                AwardType.TrainingPoints => $"{Type}: TP on {Target} {operationString}, Unknown {Unknown:x2}",
                AwardType.Languages => $"{Type}: Add {Languages} on {Target}, Unknown {Unknown:x2}",
                AwardType.Experience => $"{Type}: Exp on {Target} {operationString}, Unknown {Unknown:x2}",
                _ => $"{Type}: Unknown ({(int)TypeOfAward}:{AwardTypeValue}) on {Target} {operationString}, Unknown {Unknown:x2}"
            };
        }
    }

    public class ChangeTileEvent : Event
    {
        public uint X { get; set; }
        public uint Y { get; set; }
        public byte[] Unknown { get; set; }
        public uint FrontTileIndex { get; set; }
        /// <summary>
        /// 0 means same map
        /// </summary>
        public uint MapIndex { get; set; }
        public uint WallIndex => FrontTileIndex > 100 && FrontTileIndex < 255 ? FrontTileIndex - 100 : 0;
        public uint ObjectIndex => FrontTileIndex <= 100 ? FrontTileIndex : 0;

        public override string ToString()
        {
            return $"{Type}: Map {(MapIndex == 0 ? "Self" : MapIndex.ToString())}, X {X}, Y {Y}, Front tile / Wall / Object {FrontTileIndex}, Unknown {(Unknown == null ? "null" : string.Join(" ", Unknown.Select(u => u.ToString("x2"))))}";
        }
    }

    public class StartBattleEvent : Event
    {
        public uint MonsterGroupIndex { get; set; }
        public byte[] Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Monster group {MonsterGroupIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class EnterPlaceEvent : Event
    {
        public byte OpeningHour { get; set; }
        public byte ClosingHour { get; set; }
        public uint PlaceIndex { get; set; }
        public byte ClosedTextIndex { get; set; }
        public PlaceType PlaceType { get; set; }
        /// <summary>
        /// Displayed when you bought a horse, ship, etc.
        /// The text is taken from the map texts.
        /// 0xff means to use some default text.
        /// </summary>
        public byte UsePlaceTextIndex { get; set; }
        public uint MerchantDataIndex { get; set; }

        public override string ToString()
        {
            string index = PlaceType == PlaceType.Merchant
                ? $"Merchant index {MerchantDataIndex}" : PlaceType == PlaceType.Library
                ? $"Libary merchant index {MerchantDataIndex}" : $"Place index {PlaceIndex}";

            return $"{PlaceType}: {index}, Open {OpeningHour:00}-{ClosingHour:00}, TextIndexWhenClosed {ClosedTextIndex}, UseTextIndex {UsePlaceTextIndex}";
        }
    }

    public class ConditionEvent : Event
    {
        public enum ConditionType
        {
            GlobalVariable = 0x00,
            EventBit = 0x01,
            DoorOpen = 0x02,
            ChestOpen = 0x03,
            CharacterBit = 0x04,
            PartyMember = 0x05,
            ItemOwned = 0x06,
            UseItem = 0x07,
            KnowsKeyword = 0x08,
            LastEventResult = 0x09, // treasure fully looted, battle won, etc
            GameOptionSet = 0x0a,
            CanSee = 0x0b,
            Direction = 0x0c,
            HasAilment = 0x0d,
            Hand = 0x0e,
            SayWord = 0x0f, // it also pops up the dictionary to say something
            EnterNumber = 0x10, // enter number popup with correct number
            Levitating = 0x11,
            HasGold = 0x12,
            HasFood = 0x13,
            Eye = 0x14
        }

        public ConditionType TypeOfCondition { get; set; }
        public byte[] Unknown1 { get; set; }
        /// <summary>
        /// This depends on condition type.
        /// It can be the item or variable index for example.
        /// </summary>
        public uint ObjectIndex { get; set; } // 0 = no variable needed
        public uint Value { get; set; }
        public uint Count { get; set; }
        /// <summary>
        /// Next map event to continue with if the condition was met.
        /// 0xffff means continue with next map event from the list.
        /// </summary>
        public uint ContinueIfFalseWithMapEventIndex { get; set; }

        public override string ToString()
        {
            string falseHandling = ContinueIfFalseWithMapEventIndex == 0xffff
                ? "Stop here if false"
                : $"Jump to event {ContinueIfFalseWithMapEventIndex:x2} if false";

            return TypeOfCondition switch
            {
                ConditionType.GlobalVariable => $"{Type}: Global variable {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.EventBit => $"{Type}: Event bit {ObjectIndex / 64 + 1}:{ObjectIndex % 64} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.DoorOpen => $"{Type}: Door {ObjectIndex} {(Value == 0 ? "closed" : "open")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.ChestOpen => $"{Type}: Chest {ObjectIndex} {(Value == 0 ? "closed" : "open")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.CharacterBit => $"{Type}: Character bit {ObjectIndex / 32 + 1}:{ObjectIndex % 32} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.PartyMember => $"{Type}: Has party member {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.ItemOwned => $"{Type}: {(Value == 0 ? $"Not own item" : $"Own item {Math.Max(1, Count)}x")} {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.UseItem => $"{Type}: Use item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.KnowsKeyword => $"{Type}: {(Value == 0 ? "Not know" : "Know")} keyword {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.LastEventResult => $"{Type}: Success of last event, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.GameOptionSet => $"{Type}: Game option {(Data.Enumerations.Option)(1 << (int)ObjectIndex)} is {(Value == 0 ? "not set" : "set")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.CanSee => $"{Type}: {(Value == 0 ? "Can't see" : "Can see")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.HasAilment => $"{Type}: {(Value == 0 ? "Has not" : "Has")} ailment {(Condition)(1 << (int)ObjectIndex)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.Hand => $"{Type}: Hand cursor, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.SayWord => $"{Type}: Say keyword {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.EnterNumber => $"{Type}: Enter number {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.Levitating => $"{Type}: Levitating, {falseHandling}",
                ConditionType.HasGold => $"{Type}: Gold {(Value == 0 ? "<" : ">=")} {ObjectIndex}, {falseHandling}",
                ConditionType.HasFood => $"{Type}: Food {(Value == 0 ? "<" : ">=")} {ObjectIndex}, {falseHandling}",
                ConditionType.Eye => $"{Type}: Eye cursor, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                _ => $"{Type}: Unknown ({TypeOfCondition}), Index {ObjectIndex}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
            };
        }
    }

    public class ActionEvent : Event
    {
        /// <summary>
        /// These are similar to the <see cref="ConditionEvent.ConditionType"/>
        /// but the following IDs are not used for actions:
        /// 5, 7, 9, 11, 14, 15, 16, 17 and 20
        /// </summary>
        public enum ActionType
        {
            SetGlobalVariable = 0x00,
            /// <summary>
            /// Sets an event (event list entry) to active or inactive.
            /// </summary>
            SetEventBit = 0x01,
            /// <summary>
            /// Locks or unlocks a door
            /// </summary>
            LockDoor = 0x02,
            /// <summary>
            /// Locks or unlocks a chest
            /// </summary>
            LockChest = 0x03,
            /// <summary>
            /// As event status can be set by SetEvent I guess
            /// this is used for more complex non-boolean values
            /// like amount of stones in the pond etc.
            /// </summary>
            SetCharacterBit = 0x04,
            /// <summary>
            /// Adds or removes some items
            /// </summary>
            AddItem = 0x06,
            /// <summary>
            /// Adds or removes a new dictionary entry
            /// </summary>
            AddKeyword = 0x08,
            /// <summary>
            /// Sets a game option
            /// </summary>
            SetGameOption = 0x0a,
            /// <summary>
            /// Sets the direction of the player
            /// </summary>
            SetDirection = 0x0c,
            /// <summary>
            /// Adds or removes an ailment
            /// </summary>
            AddAilment = 0x0d,
            /// <summary>
            /// Adds or removes gold
            /// </summary>
            AddGold = 0x12,
            /// <summary>
            /// Adds or removes food
            /// </summary>
            AddFood = 0x13
        }

        public ActionType TypeOfAction { get; set; }
        public byte[] Unknown1 { get; set; }
        /// <summary>
        /// This depends on condition type.
        /// It can be the item or variable index for example.
        /// </summary>
        public uint ObjectIndex { get; set; } // 0 = no variable needed
        public uint Value { get; set; }
        public uint Count { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return TypeOfAction switch
            {
                ActionType.SetGlobalVariable => $"{Type}: Set global variable {ObjectIndex} to {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.SetEventBit => $"{Type}: Set event bit {ObjectIndex / 64 + 1}:{ObjectIndex % 64} to {(Value != 0 ? "inactive" : "active")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.LockDoor => $"{Type}: {(Value == 0 ? "Lock" : "Unlock")} door {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.LockChest => $"{Type}: {(Value == 0 ? "Lock" : "Unlock")} chest {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",                
                ActionType.SetCharacterBit => $"{Type}: Set character bit {ObjectIndex / 32 + 1}:{ObjectIndex % 32} to {(Value != 0 ? "hidden" : "show")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddItem => $"{Type}: {(Value == 0 ? "Remove" : "Add")} {Math.Max(1, Count)}x item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddKeyword => $"{Type}: {(Value == 0 ? "Remove" : "Add")} keyword {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.SetGameOption => $"{Type}: {(Value == 0 ? "Deactivate" : "Activate")} game option {(Data.Enumerations.Option)(1 << (int)ObjectIndex)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddAilment => $"{Type}: {(Value == 0 ? "Remove" : "Add")} ailment {(Condition)(1 << (int)ObjectIndex)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddGold => $"{Type}: {(Value == 0 ? "Remove" : "Add")} {Math.Max(1, ObjectIndex)} gold, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddFood => $"{Type}: {(Value == 0 ? "Remove" : "Add")} {Math.Max(1, ObjectIndex)} food, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                _ => $"{Type}: Unknown ({TypeOfAction}), Index {ObjectIndex}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
            };
        }
    }

    public class Dice100RollEvent : Event
    {
        /// <summary>
        /// Chance in percent: 0 ~ 100
        /// </summary>
        public uint Chance { get; set; }
        /// <summary>
        /// Next map event to continue with if the condition was met.
        /// 0xffff means continue with next map event from the list.
        /// </summary>
        public uint ContinueIfFalseWithMapEventIndex { get; set; }
        public byte[] Unused { get; set; }

        public override string ToString()
        {
            string falseHandling = ContinueIfFalseWithMapEventIndex == 0xffff
                ? "Stop here if false"
                : $"Jump to event {ContinueIfFalseWithMapEventIndex:x2} if false";

            return $"{Type}: Chance {Chance}%, {falseHandling}, Unused {string.Join(" ", Unused.Select(u => u.ToString("x2")))}";
        }
    }

    public class ConversationEvent : Event
    {
        public enum InteractionType
        {
            Keyword = 0,
            ShowItem = 1,
            GiveItem = 2,
            GiveGold = 3,
            GiveFood = 4,
            JoinParty = 5,
            LeaveParty = 6,
            Talk = 7,
            Leave = 8
        }

        public InteractionType Interaction { get; set; }
        public ushort Value { get; set; }
        public uint KeywordIndex => Value;
        public uint ItemIndex => Value;
        public byte[] Unused1 { get; set; } // 4
        public byte[] Unused2 { get; set; } // 2

        public override string ToString()
        {
            string argument = Interaction switch
            {
                InteractionType.Keyword => $", KeywordIndex {KeywordIndex}",
                InteractionType.ShowItem => $", Item {ItemIndex}",
                InteractionType.GiveItem => $", Item {ItemIndex}",
                _ => ""
            };

            return $"{Type}: On interaction {Interaction}" + argument;
        }
    }

    public class PrintTextEvent : Event
    {
        public uint NPCTextIndex { get; set; }
        public byte[] Unused { get; set; } // 8

        public override string ToString()
        {
            return $"{Type}: NPCTextIndex {NPCTextIndex}";
        }
    }

    public class CreateEvent : Event
    {
        // TODO: maybe the other 5 bytes are also used? flags, charges, etc?
        public enum CreateType
        {
            Item,
            Gold,
            Food
        }

        public CreateType TypeOfCreation { get; set; }
        public uint Amount { get; set; }
        public uint ItemIndex { get; set; }
        public byte[] Unused { get; set; }

        public override string ToString()
        {
            switch (TypeOfCreation)
            {
                case CreateType.Item:
                    return $"{Type}: {Amount}x Item {ItemIndex}";
                case CreateType.Gold:
                    return $"{Type}: {Amount} Gold";
                default:
                    return $"{Type}: {Amount} Food";
            }
        }
    }

    public class DecisionEvent : Event
    {
        public uint TextIndex { get; set; }
        public byte[] Unknown1 { get; set; }
        /// <summary>
        /// Event index to continue with if "No" is selected.
        /// 0xffff means just stop the event list when selecting "No".
        /// </summary>
        public uint NoEventIndex { get; set; }

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Event index when selecting 'No' {(NoEventIndex == 0xffff ? "None" : $"{NoEventIndex + 1:x4}")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class ChangeMusicEvent : Event
    {
        public uint MusicIndex { get; set; }
        public byte Volume { get; set; }
        public byte[] Unknown1 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Music {(MusicIndex == 255 ? "<Map music>" : MusicIndex.ToString())}, Volume {(Volume / 255.0f).ToString("0.0", CultureInfo.InvariantCulture)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class ExitEvent : Event
    {
        public byte[] Unused { get; set; }

        public override string ToString()
        {
            return $"{Type}";
        }
    }

    public class SpawnEvent : Event
    {
        public uint X { get; set; }
        public uint Y { get; set; }
        public TravelType TravelType { get; set; }
        public byte[] Unknown1 { get; set; }
        public uint MapIndex { get; set; }
        public byte[] Unknown2 { get; set; }


        public override string ToString()
        {
            return $"{Type} {TravelType} on map {MapIndex} at {X}, {Y}";
        }
    }

    public class InteractEvent : Event
    {
        public byte[] Unused { get; set; }

        public override string ToString()
        {
            return $"{Type}";
        }
    }

    public class DebugEvent : Event
    {
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return Type.ToString() + ": " + string.Join(" ", Data.Select(d => d.ToString("X2")));
        }
    }
}
