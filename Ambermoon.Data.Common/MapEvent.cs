using System;
using System.Globalization;
using System.Linq;

namespace Ambermoon.Data
{
    public enum MapEventType
    {
        Unknown,
        MapChange, // doors, etc
        Unknown2,
        Chest, // all kinds of lootable map objects
        TextEvent, // events with text popup
        Spinner, // rotates the player to a random direction
        Damage, // the burning fire places in grandfathers house have these
        Unknown7,
        Riddlemouth,
        ChangePlayerAttribute,
        ChangeTile,
        Unknown11,
        Unknown12,
        Condition,
        Action,
        Unknown15,
        Unknown16,
        Unknown17,
        Unknown18,
        Question, // yes/no popup with text
        ChangeMusic,
        // TODO ...
        // Maybe: Message popup, activatable by hand/eye/mouth cursor, etc
    }

    public enum MapEventTrigger
    {
        Move,
        Hand,
        Eye,
        Mouth
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
        public ushort Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Chest {ChestIndex}, Lock=[{Lock}], RemovedWhenEmpty={RemoveWhenEmpty}, Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Unknown1 {Unknown1:X4}, Unknown2 {Unknown2:X4}";
        }
    }

    public class TextEvent : MapEvent
    {
        public uint TextIndex { get; set; }
        public byte[] Unknown1 { get; set; }
        public byte[] Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
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

    public class ChangePlayerAttributeEvent : MapEvent
    {
        public Attribute Attribute { get; set; }
        public uint Value { get; set; }
        public byte[] Unknown1 { get; set; }
        public byte Unknown2 { get; set; }

        public override string ToString()
        {
            return $"{Type}: Attribute {Attribute}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {Unknown2:x2}";
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

    public class ConditionEvent : MapEvent
    {
        public enum ConditionType
        {
            MapVariable = 0x00,
            GlobalVariable = 0x01,
            UseItem = 0x07,
            TreasureLooted = 0x09,
            Hand = 0x0e,
            // TODO
        };

        public ConditionType TypeOfCondition { get; set; }
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
            switch (TypeOfCondition)
            {
                case ConditionType.MapVariable:
                    return $"{Type}: Map variable {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                case ConditionType.GlobalVariable:
                    return $"{Type}: Global variable {ObjectIndex} = {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                case ConditionType.UseItem:
                    return $"{Type}: Item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                case ConditionType.TreasureLooted:
                    return $"{Type}: Treasure looted, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                case ConditionType.Hand:
                    return $"{Type}: Hand cursor, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
                default:
                    return $"{Type}: Unknown ({TypeOfCondition}), Index {ObjectIndex}, Value {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}";
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
        };

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

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
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
