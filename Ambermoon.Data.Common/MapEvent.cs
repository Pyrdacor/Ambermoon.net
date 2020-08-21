using System;
using System.Globalization;
using System.Linq;

namespace Ambermoon.Data
{
    public enum MapEventType
    {
        Unknown,
        MapChange, // open doors, exits, etc
        Door, // locked doors
        Chest, // all kinds of lootable map objects
        PopupText, // events with text popup
        Spinner, // rotates the player to a random direction
        Damage, // the burning fire places in grandfathers house have these
        Unknown7,
        Riddlemouth,
        Award,
        ChangeTile,
        StartBattle,
        EnterPlace, // merchant, healer, etc
        Condition,
        Action,
        Dice100Roll,
        ConversationAction,
        PrintText,
        Create,
        Question, // yes/no popup with text
        ChangeMusic,
        Exit,
        Spawn,
        Nop // null / no operation
    }

    public class MapEvent
    {
        public uint Index { get; set; }
        public MapEventType Type { get; set; }
        public MapEvent Next { get; set; }
    }

    public class MapChangeEvent : MapEvent
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

    public class ChestMapEvent : MapEvent
    {
        [Flags]
        public enum LockFlags
        {
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
            Lockpick = 0x01 // these also have a trap attached
        }

        public ushort Unknown1 { get; set; }
        public LockFlags Lock { get; set; }
        /// <summary>
        /// Note: This is 0-based but the files might by 1-based.
        /// </summary>
        public uint ChestIndex { get; set; }
        public bool RemoveWhenEmpty { get; set; }
        public uint KeyIndex { get; set; }
        public uint UnlockFailedEventIndex { get; set; }

        public override string ToString()
        {
            return $"{Type}: Chest {ChestIndex}, Lock=[{Lock}], RemovedWhenEmpty={RemoveWhenEmpty}, Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Event index if unlock failed {UnlockFailedEventIndex:x4}, Unknown1 {Unknown1:x4}";
        }
    }

    public class PopupTextEvent : MapEvent
    {
        public uint TextIndex { get; set; }
        /// <summary>
        /// From event_pix (0-based). 0xff -> no image.
        /// </summary>
        public uint EventImageIndex { get; set; }
        public byte[] Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Image {(EventImageIndex == 0xff ? "None" : EventImageIndex.ToString())}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class SpinnerEvent : MapEvent
    {
        public CharacterDirection Direction { get; set; }
        public byte[] Unknown1 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Direction {Direction}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class DamageEvent : MapEvent
    {
        public byte[] Unknown1 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class RiddlemouthEvent : MapEvent
    {
        public uint IntroTextIndex { get; set; }
        public uint SolutionTextIndex { get; set; }
        public byte[] Unknown1 { get; set; }

        public override string ToString()
        {
            return $"{Type}: IntroText {IntroTextIndex}, SolutionText {SolutionTextIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class AwardEvent : MapEvent
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

    public class ChangeTileEvent : MapEvent
    {
        public uint X { get; set; }
        public uint Y { get; set; }
        public byte[] Unknown1 { get; set; }
        public uint FrontTileIndex { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: X {X}, Y {Y}, Front tile {FrontTileIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class StartBattleEvent : MapEvent
    {
        public uint MonsterGroupIndex { get; set; }
        public byte[] Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Monster group {MonsterGroupIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
        }
    }

    public class ConditionEvent : MapEvent
    {
        public enum ConditionType
        {
            MapVariable = 0x00,
            GlobalVariable = 0x01,
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

            switch (TypeOfCondition)
            {
                case ConditionType.MapVariable:
                    return $"{Type}: Map variable {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}";
                case ConditionType.GlobalVariable:
                    return $"{Type}: Global variable {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}";
                case ConditionType.UseItem:
                    return $"{Type}: Item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}";
                case ConditionType.Success:
                    return $"{Type}: Success of last event, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}";
                case ConditionType.Hand:
                    return $"{Type}: Hand cursor, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}";
                default:
                    return $"{Type}: Unknown ({TypeOfCondition}), Index {ObjectIndex}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, {falseHandling}";
            }
        }
    }

    public class ActionEvent : MapEvent
    {
        public enum ActionType
        {
            SetMapVariable = 0x00,
            SetGlobalVariable = 0x01,
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
            switch (TypeOfAction)
            {
                case ActionType.SetMapVariable:
                    return $"{Type}: Set map variable {ObjectIndex} to {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                case ActionType.SetGlobalVariable:
                    return $"{Type}: Set global variable {ObjectIndex} to {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                default:
                    return $"{Type}: Unknown ({TypeOfAction}), Index {ObjectIndex}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
            }
        }
    }

    public class QuestionEvent : MapEvent
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

    public class ChangeMusicEvent : MapEvent
    {
        public uint MusicIndex { get; set; }
        public byte Volume { get; set; }
        public byte[] Unknown1 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Music {(MusicIndex == 255 ? "<Map music>" : MusicIndex.ToString())}, Volume {(Volume / 255.0f).ToString("0.0", CultureInfo.InvariantCulture)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class DebugMapEvent : MapEvent
    {
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return Type.ToString() + ": " + string.Join(" ", Data.Select(d => d.ToString("X2")));
        }
    }
}
