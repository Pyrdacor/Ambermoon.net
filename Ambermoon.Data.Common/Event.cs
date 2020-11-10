using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Ambermoon.Data
{
    public enum EventType
    {
        Unknown,
        MapChange, // open doors, exits, etc
        Door, // locked doors
        Chest, // all kinds of lootable map objects
        PopupText, // events with text popup
        Spinner, // rotates the player to a random direction
        Trap, // the burning fire places in grandfathers house, chest/door traps etc
        Unknown7,
        Riddlemouth,
        Award,
        ChangeTile,
        StartBattle,
        EnterPlace, // merchant, healer, etc
        Condition,
        Action,
        Dice100Roll,
        Conversation,
        PrintText,
        Create,
        Decision, // yes/no popup with text
        ChangeMusic,
        Exit,
        Spawn,
        Nop // null / no operation
    }

    public class Event
    {
        public uint Index { get; set; }
        public EventType Type { get; set; }
        public Event Next { get; set; }
    }

    public class MapChangeEvent : Event
    {
        public uint MapIndex { get; set; }
        public uint X { get; set; }
        public uint Y { get; set; }
        public CharacterDirection Direction { get; set; }
        public byte[] Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Map {MapIndex} / Position {X},{Y} / Direction {Direction}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class DoorEvent : Event
    {
        public byte[] Unknown { get; set; }
        public uint KeyIndex { get; set; }
        public uint UnlockFailedEventIndex { get; set; }

        public override string ToString()
        {
            return $"{Type}: Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Event index if unlock failed {UnlockFailedEventIndex:x4}, Unknown {string.Join(" ", Unknown.Select(b => b.ToString("x2")))}";
        }
    }

    public class ChestEvent : Event
    {
        [Flags]
        public enum LockFlags
        {
            // This only gives the initial state. The savegame has
            // it's one bits to decide if a chest is locker or not.
            // It is used to say if a chest is initial open, locked
            // with a lockpick or with a key.
            //
            // Seen these hex values only:
            // 01, 05, 0A, 0F, 14, 19, 1E, 32, 37, 4B, 55, 63, 64
            // In binary:
            // 0000 0001
            // 0000 0101
            // 0000 1010
            // 0000 1111
            // 0001 0100
            // 0001 1001
            // 0001 1110
            // 0011 0010
            // 0011 0111
            // 0100 1011
            // 0101 0101
            // 0110 0011
            // 0110 0100
            // ---------
            // 0x01 is a locked chest that can be opened with a lockpick.
            // 0x64 could be a locked chest that needs a special key.
            Open = 0,
            Lockpick = 0x01, // these also have a trap attached
            KeyLocked = 0x64 // locked with a special key
        }

        public LockFlags Lock { get; set; }
        public ushort Unknown { get; set; }        
        /// <summary>
        /// Note: This is 0-based but the files might by 1-based.
        /// </summary>
        public uint ChestIndex { get; set; }
        public bool RemoveWhenEmpty { get; set; }
        public uint KeyIndex { get; set; }
        public uint UnlockFailedEventIndex { get; set; }

        public override string ToString()
        {
            return $"{Type}: Chest {ChestIndex}, Lock=[{Lock}], RemovedWhenEmpty={RemoveWhenEmpty}, Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Event index if unlock failed {UnlockFailedEventIndex:x4}, Unknown1 {Unknown:x4}";
        }
    }

    public class PopupTextEvent : Event
    {
        public enum Response
        {
            Close,
            Yes,
            No
        }

        [Flags]
        public enum Trigger
        {
            None = 0,
            Move = 0x01,
            Cursor = 0x02, // Hand or Eye
            Always = Move | Cursor
        }

        public uint TextIndex { get; set; }
        /// <summary>
        /// From event_pix (0-based). 0xff -> no image.
        /// </summary>
        public uint EventImageIndex { get; set; }
        public bool HasImage => EventImageIndex != 0xff;
        public Trigger PopupTrigger { get; set; }
        public bool CanTriggerByMoving => PopupTrigger == Trigger.None || PopupTrigger.HasFlag(Trigger.Move);
        public bool CanTriggerByCursor => PopupTrigger == Trigger.None || PopupTrigger.HasFlag(Trigger.Cursor);
        public byte Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Image {(EventImageIndex == 0xff ? "None" : EventImageIndex.ToString())}, Trigger {PopupTrigger}, Unknown1 {Unknown1}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class SpinnerEvent : Event
    {
        public CharacterDirection Direction { get; set; }
        public byte[] Unknown { get; set; }

        public override string ToString()
        {
            return $"{Type}: Direction {Direction}, Unknown1 {string.Join(" ", Unknown.Select(u => u.ToString("x2")))}";
        }
    }

    public class TrapEvent : Event
    {
        public enum TrapType
        {
            Damage
            // TODO ...
            // 5 seems to be used for green slime, stinking stuff
            // "Gauners Keller" has many traps with many values for this
        }

        public enum TrapTarget
        {
            ActivePlayer,
            All
            // TODO: RandomPlayer? Hero?
        }

        public TrapType TypeOfTrap { get; set; }
        public TrapTarget Target { get; set; }
        /// <summary>
        /// Most of the times 3. Big water vortex has 150 and Value 0.
        /// </summary>
        public byte Unknown { get; set; }
        /// <summary>
        /// Value (e.g. damage). I guess it is in percentage of max health? Maybe for TrapType 0 only?
        /// </summary>
        public byte Value { get; set; }
        public byte[] Unused { get; set; } // 5 bytes

        public override string ToString()
        {
            return $"{Type}: {Value} {TypeOfTrap} on {Target} Unknown {Unknown:x2}";
        }
    }

    public class RiddlemouthEvent : Event
    {
        public uint RiddleTextIndex { get; set; }
        public uint SolutionTextIndex { get; set; }
        public uint CorrectAnswerDictionaryIndex { get; set; }
        public byte[] Unknown { get; set; }

        public override string ToString()
        {
            return $"{Type}: IntroText {RiddleTextIndex}, SolutionText {SolutionTextIndex}, Unknown1 {string.Join(" ", Unknown.Select(u => u.ToString("x2")))}";
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
            TrainingPoints = 0x05,
            Languages = 0x07,
            Experience = 0x08
            // TODO
        }

        public enum AwardOperation
        {
            Increase,
            Fill = 4
            // TODO: Decrease?, ... 
        }

        public enum AwardTarget
        {
            ActivePlayer,
            All
            // TODO: RandomPlayer? Hero?
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
        public Ability? Ability => TypeOfAward == AwardType.Ability ? (Ability)AwardTypeValue : (Ability?)null;
        public Language? Languages => TypeOfAward == AwardType.Languages ? (Language)AwardTypeValue : (Language?)null;
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
        public byte Unknown { get; set; }
        public uint BackTileIndex { get; set; }
        public uint FrontTileIndex { get; set; }
        /// <summary>
        /// 0 means same map
        /// </summary>
        public uint MapIndex { get; set; }

        public override string ToString()
        {
            return $"{Type}: Map {(MapIndex == 0 ? "Self" : MapIndex.ToString())}, X {X}, Y {Y}, Back tile {BackTileIndex}, Front tile {FrontTileIndex}, Unknown {Unknown}";
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
        public byte[] Unknown1 { get; set; }
        public byte Unknown2 { get; set; }
        public byte[] Unknown3 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Place index {PlaceIndex}, Open {OpeningHour:00}-{ClosingHour:00} Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {Unknown2:x2}, Unknown3 {string.Join(" ", Unknown3.Select(u => u.ToString("x2")))}";
        }
    }

    public class ConditionEvent : Event
    {
        public enum ConditionType
        {
            GlobalVariable = 0x00,
            EventBit = 0x01,
            CharacterBit = 0x04,
            PartyMember = 0x05,
            ItemOwned = 0x06,
            UseItem = 0x07,
            Success = 0x09, // treasure fully looted, battle won, etc
            Hand = 0x0e,
            // TODO
        }

        public ConditionType TypeOfCondition { get; set; }
        public byte[] Unknown1 { get; set; }
        /// <summary>
        /// This depends on condition type.
        /// It can be the item or variable index for example.
        /// </summary>
        public uint ObjectIndex { get; set; } // 0 = no variable needed
        public uint Value { get; set; }
        /// <summary>
        /// Next map event to continue with if the condition was met.
        /// 0xffff means continue with next map event from the list.
        /// </summary>
        public uint ContinueIfFalseWithMapEventIndex { get; set; }

        public override string ToString()
        {
            string falseHandling = ContinueIfFalseWithMapEventIndex == 0xffff
                ? "Stop here if false"
                : $"Jump to event {ContinueIfFalseWithMapEventIndex + 1:x2} if false";

            return TypeOfCondition switch
            {
                ConditionType.GlobalVariable => $"{Type}: Global variable {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.EventBit => $"{Type}: Event bit {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.CharacterBit => $"{Type}: Character bit {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.PartyMember => $"{Type}: Has party member {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.ItemOwned => $"{Type}: Own item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.UseItem => $"{Type}: Use item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.Success => $"{Type}: Success of last event, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                ConditionType.Hand => $"{Type}: Hand cursor, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
                _ => $"{Type}: Unknown ({TypeOfCondition}), Index {ObjectIndex}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}",
            };
        }
    }

    public class ActionEvent : Event
    {
        public enum ActionType
        {
            SetGlobalVariable = 0x00,
            /// <summary>
            /// Sets an event (event list entry) to active or inactive.
            /// </summary>
            SetEventBit = 0x01,
            /// <summary>
            /// As event status can be set by SetEvent I guess
            /// this is used for more complex non-boolean values
            /// like amount of stones in the pond etc.
            /// </summary>
            SetCharacterBit = 0x04,
            /// <summary>
            /// Adds or remove some item?
            /// </summary>
            Inventory = 0x06,
            /// <summary>
            /// Adds a new dictionary entry?
            /// </summary>
            Keyword = 0x08,

            // TODO
        }

        public ActionType TypeOfAction { get; set; }
        public byte[] Unknown1 { get; set; }
        /// <summary>
        /// This depends on condition type.
        /// It can be the item or variable index for example.
        /// </summary>
        public uint ObjectIndex { get; set; } // 0 = no variable needed
        public uint Value { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return TypeOfAction switch
            {
                ActionType.SetGlobalVariable => $"{Type}: Set global variable {ObjectIndex} to {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                // TODO
                ActionType.SetEventBit => $"{Type}: Set event bit {ObjectIndex} to {(Value != 0 ? "inactive" : "active")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.SetCharacterBit => $"{Type}: Set character bit {ObjectIndex} to {(Value != 0 ? "hidden" : "show")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                // TODO
                ActionType.Inventory => $"{Type}: Set item, ObjectIndex={ObjectIndex}, Value={Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                // TODO
                ActionType.Keyword => $"{Type}: Keyword, ObjectIndex={ObjectIndex}, Value={Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
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
                : $"Jump to event {ContinueIfFalseWithMapEventIndex + 1:x2} if false";

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
            // TODO: ask to join, ask to leave, give gold, give food
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
        // TODO
        public override string ToString()
        {
            return $"{Type}";
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

    public class DebugEvent : Event
    {
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return Type.ToString() + ": " + string.Join(" ", Data.Select(d => d.ToString("X2")));
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
        // TODO
        public override string ToString()
        {
            return $"{Type}";
        }
    }

    public class NopEvent : Event
    {
        public override string ToString()
        {
            return $"{Type}";
        }
    }
}
