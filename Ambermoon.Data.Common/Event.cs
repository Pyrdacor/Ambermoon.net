using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        MapText,
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
        ChangeBuffs,
        /// <summary>
        /// Opens the riddlemouth window with some riddle
        /// </summary>
        Riddlemouth,
        /// <summary>
        /// Rewards and punishments
        /// </summary>
        Reward,
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
        /// <summary>
        /// Removes a party member and optionally stores its belongings in one or two chests.
        /// </summary>
        RemovePartyMember,
        /// <summary>
        /// Adds a non-interactive game delay (Ambermoon Advanced only).
        /// </summary>
        Delay,
        /// <summary>
        /// Tests some condition for a specific party member (Ambermoon Advanced only).
        /// </summary>
        PartyMemberCondition,
        /// <summary>
		/// Shakes the screen (Ambermoon Advanced only).
		/// </summary>
        Shake,
    }

    public class Event
    {
        public uint Index { get; set; }
        public EventType Type { get; set; }
        public Event Next { get; set; }

        protected void CloneProperties(Event @event, bool keepNext)
        {
            @event.Index = Index;
            @event.Type = Type;
            @event.Next = keepNext ? Next : null;
        }

        protected static byte[] CloneBytes(byte[] bytes)
        {
            var newBytes = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, newBytes, 0, bytes.Length);
            return newBytes;
        }

        public virtual Event Clone(bool keepNext)
        {
            return new Event
            {
                Index = Index,
                Type = Type,
                Next = keepNext ? Next : null
            };
        }
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

        public override Event Clone(bool keepNext)
        {
            var clone = new TeleportEvent
            {
                MapIndex = MapIndex,
                X = X,
                Y = Y,
                Direction = Direction,
                NewTravelType = NewTravelType,
                Transition = Transition,
                Unknown2 = CloneBytes(Unknown2)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new DoorEvent
            {
                LockpickingChanceReduction = LockpickingChanceReduction,
                DoorIndex = DoorIndex,
                TextIndex = TextIndex,
                UnlockTextIndex = UnlockTextIndex,
                Unused = Unused,
                KeyIndex = KeyIndex,
                UnlockFailedEventIndex = UnlockFailedEventIndex
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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
            /// <summary>
            /// Close the chest window when looted.
            /// 
            /// This can also be interpreted as "temporary chest" which can't
            /// store any new items.
            /// </summary>
            JunkPile = 0x01,
            /// <summary>
            /// If true the chest contents are restored after closing the
            /// window.
            /// </summary>
            NoSave = 0x02,
            /// <summary>
            /// Extended chest (Ambermoon Advanced only)
            /// </summary>
            ExtendedChest = 0x04
        }

        public uint LockpickingChanceReduction { get; set; }
        public uint TextIndex { get; set; } // 255 = none
        /// <summary>
        /// Note: This is 0-based but the files might by 1-based.
        /// </summary>
        public uint ChestIndex { get; set; }
        /// <summary>
        /// This is a 1-based index and also respects extended
        /// chests of Ambermoon Advanced.
        /// </summary>
        public uint RealChestIndex
        {
            get
            {
                return Flags.HasFlag(ChestFlags.ExtendedChest)
                    ? 257 + ChestIndex
                    : 1 + ChestIndex;
            }
        }
        public ChestFlags Flags { get; set; }
        public bool CloseWhenEmpty => Flags.HasFlag(ChestFlags.JunkPile);
        public bool NoSave => Flags.HasFlag(ChestFlags.NoSave);
        public uint KeyIndex { get; set; }
        public uint UnlockFailedEventIndex { get; set; }
        /// <summary>
        /// This gives the value to reduce the chance to find the chest.
        /// A value of 0 means that the chest is always available.
        /// Normally a value above 0 would be subtracted from the
        /// active player's search skill and then a dice roll is
        /// performed to check if the chest is found.
        ///
        /// However, the original implementation just checks if the
        /// value is non-zero and just dice rolls against the search
        /// skill, so this value is not used at all. Only as a switch
        /// for the search check between off (0) and on (not 0).
        /// </summary>
        public byte FindChanceReduction { get; set; }
        public bool SearchSkillCheck => FindChanceReduction != 0;

        public override Event Clone(bool keepNext)
        {
            var clone = new ChestEvent
            {
                LockpickingChanceReduction = LockpickingChanceReduction,
                TextIndex = TextIndex,
                ChestIndex = ChestIndex,
                Flags = Flags,
                KeyIndex = KeyIndex,
                UnlockFailedEventIndex = UnlockFailedEventIndex,
                FindChanceReduction = FindChanceReduction
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            string lockType = LockpickingChanceReduction == 0 ? "Open" : LockpickingChanceReduction >= 100 ? "No Lockpicking" : $"-{LockpickingChanceReduction}% Chance";
            string chestType = Flags.HasFlag(ChestFlags.JunkPile) ? "Pile" : "Chest";
            List<string> flags = new();
            if (Flags.HasFlag(ChestFlags.NoSave))
                flags.Add("NoSave");
            if (SearchSkillCheck)
                flags.Add("SearchCheck");
            string flagString = flags.Count == 0 ? "" : "Flags=" + string.Join(",", flags) + ", ";
            return $"{Type}: {chestType} {RealChestIndex}, Lock=[{lockType}], {flagString}Key={(KeyIndex == 0 ? "None" : KeyIndex.ToString())}, Event index if unlock failed {UnlockFailedEventIndex:x4}, Text {(TextIndex == 0xff ? "none" : TextIndex.ToString())}";
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

        public override Event Clone(bool keepNext)
        {
            var clone = new PopupTextEvent
            {
                TextIndex = TextIndex,
                EventImageIndex = EventImageIndex,
                PopupTrigger = PopupTrigger,
                TriggerIfBlind = TriggerIfBlind,
                Unknown = CloneBytes(Unknown)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type}: Text {TextIndex}, Image {(EventImageIndex == 0xff ? "None" : EventImageIndex.ToString())}, Trigger {PopupTrigger}, {(TriggerIfBlind ? "" : "Not ")}Trigger If Blind, Unknown {string.Join(" ", Unknown.Select(u => u.ToString("x2")))}";
        }
    }

    public class SpinnerEvent : Event
    {
        public CharacterDirection Direction { get; set; }
        public byte[] Unused { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new SpinnerEvent
            {
                Direction = Direction,
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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
            TrapAilment.Crazy => Condition.Crazy,
            TrapAilment.Blind => Condition.Blind,
            TrapAilment.Stoned => Condition.Drugged,
            TrapAilment.Paralyzed => Condition.Lamed,
            TrapAilment.Poisoned => Condition.Poisoned,
            TrapAilment.Petrified => Condition.Petrified,
            TrapAilment.Diseased => Condition.Diseased,
            TrapAilment.Aging => Condition.Aging,
            TrapAilment.Dead => Condition.DeadCorpse,
            _ => Condition.None
        };

        public override Event Clone(bool keepNext)
        {
            var clone = new TrapEvent
            {
                Ailment = Ailment,
                Target = Target,
                BaseDamage = BaseDamage,
                AffectedGenders = AffectedGenders,
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type}: {BaseDamage} damage with ailment {Ailment} on {Target}, Affected genders {AffectedGenders}";
        }
    }

    public class ChangeBuffsEvent : Event
    {
        /// <summary>
        /// 0 means all.
        /// </summary>
        public ActiveSpellType? AffectedBuff { get; set; }
        public bool Add { get; set; }
        public byte Unused1 { get; set; }
        /// <summary>
        /// Only used when adding buffs. Gives the level/value of the buff.
        /// </summary>
        public ushort Value { get; set; }
        /// <summary>
        /// Only used when adding buffs. Gives the duration in 5 min chunks.
        /// Should be in the range 5 to 180 (5 minutes to 15 hours).
        /// </summary>
        public ushort Duration { get; set; }
        public byte[] Unused2 { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new ChangeBuffsEvent
            {
                AffectedBuff = AffectedBuff,
                Add = Add,
                Unused1 = Unused1,
                Value = Value,
                Duration = Duration,
                Unused2 = CloneBytes(Unused2)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            string operation = Add ? "AddBuff" : "RemoveBuff";
            string values = Add ? $" , Value {Value}, Duration {Duration * 5} minutes" : "";

            return $"{operation}: Affected buff {(AffectedBuff == null ? "all" : AffectedBuff.ToString())}{values}";
        }
    }

    public class RiddlemouthEvent : Event
    {
        public uint RiddleTextIndex { get; set; }
        public uint SolutionTextIndex { get; set; }
        public uint CorrectAnswerDictionaryIndex1 { get; set; }
        public uint CorrectAnswerDictionaryIndex2 { get; set; }
        public byte[] Unused { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new RiddlemouthEvent
            {
                RiddleTextIndex = RiddleTextIndex,
                SolutionTextIndex = SolutionTextIndex,
                CorrectAnswerDictionaryIndex1 = CorrectAnswerDictionaryIndex1,
                CorrectAnswerDictionaryIndex2 = CorrectAnswerDictionaryIndex2,
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            string answerIndices = CorrectAnswerDictionaryIndex1 == CorrectAnswerDictionaryIndex2
                ? $"AnswerIndex {CorrectAnswerDictionaryIndex1}"
                : $"AnswerIndices {CorrectAnswerDictionaryIndex1} or {CorrectAnswerDictionaryIndex2}";
            return $"{Type}: RiddleText {RiddleTextIndex}, SolvedText {SolutionTextIndex}, {answerIndices}";
        }
    }

    public class RewardEvent : Event
    {
        public enum RewardType
        {
            Attribute = 0x00,
            Skill = 0x01,
            HitPoints = 0x02,
            SpellPoints = 0x03,
            SpellLearningPoints = 0x04,
            Conditions = 0x05,
            UsableSpellTypes = 0x06,
            Languages = 0x07,
            Experience = 0x08,
            MaxAttribute = 0x09,
            AttacksPerRound = 0x0a,
            TrainingPoints = 0x0b,
            Level = 0x0c,
            Damage = 0x0d,
            Defense = 0x0e,
            MaxHitPoints = 0x0f,
            MaxSpellPoints = 0x10,
            EmpowerSpells = 0x11,
            ChangePortrait = 0x12,
            MaxSkill = 0x13,
            MagicArmorLevel = 0x14,
            MagicWeaponLevel = 0x15,
			Spells = 0x16,
		}

        public enum RewardOperation
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
        public enum RewardTarget
        {
            ActivePlayer,
            All,
            // Ambermoon Advanced only
            RandomPlayer,
            FirstAnimal,
            FirstPartyMember = 100,
            AllButFirstPartyMember = 200
        }

        public RewardType TypeOfReward { get; set; }
        public RewardTarget Target { get; set; }
        public RewardOperation Operation { get; set; }
        /// <summary>
        /// If set the real value is random in the range 0 to Value.
        /// </summary>
        public bool Random { get; set; }
        public ushort RewardTypeValue { get; set; }
        public Attribute? Attribute => TypeOfReward == RewardType.Attribute || TypeOfReward == RewardType.MaxAttribute ? (Attribute)RewardTypeValue : null;
        public Skill? Skill => TypeOfReward == RewardType.Skill || TypeOfReward == RewardType.MaxSkill ? (Skill)RewardTypeValue : null;
        public Language? Languages => TypeOfReward == RewardType.Languages ? (Language)(1 << (int)RewardTypeValue) : null;
        public Condition? Conditions => TypeOfReward == RewardType.Conditions ? (Condition)(1 << (int)RewardTypeValue) : null;
        public SpellTypeMastery? UsableSpellTypes => TypeOfReward == RewardType.UsableSpellTypes ? (SpellTypeMastery)(1 << (int)RewardTypeValue) : null;
		public uint? Spells => TypeOfReward == RewardType.Spells ? (1u << (int)RewardTypeValue) : null;
		public uint Value { get; set; }
        public byte Unused { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new RewardEvent
            {
                TypeOfReward = TypeOfReward,
                Target = Target,
                Operation = Operation,
                Random = Random,
                RewardTypeValue = RewardTypeValue,
                Value = Value,
                Unused = Unused
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            string operationString = Operation switch
            {
                RewardOperation.Increase => Random ? $"+rand(0~{Value})" : $"+{Value}",
                RewardOperation.Fill => "max",
				RewardOperation.IncreasePercentage => Random ? $"+rand(0%~{Value}%)" : $"+{Value}%",
				RewardOperation.DecreasePercentage => Random ? $"-rand(0%~{Value}%)" : $"-{Value}%",
				RewardOperation.Remove => "Remove",
				RewardOperation.Add => "Add",
				RewardOperation.Toggle => "Toggle",
					_ => $"?op={(int)Operation}?"
            };

            string target = Target >= RewardTarget.AllButFirstPartyMember
                ? $"All but PartyMember with index {1 + Target - RewardTarget.AllButFirstPartyMember}" : Target >= RewardTarget.FirstPartyMember
				? $"PartyMember with index {1 + Target - RewardTarget.FirstPartyMember}" : Target.ToString();

			string EmpowerString()
            {
                var element = Value switch
                {
                    0 => "earth",
                    1 => "wind",
                    2 => "fire",
                    _ => null
                };

                return element == null ? "Invalid" :
                    $"Grants empowered {element} spells for {target}";
            }

            return TypeOfReward switch
            {
                RewardType.Attribute => $"{Type}: {Attribute} on {target} {operationString}",
                RewardType.Skill => $"{Type}: {Skill} on {target} {operationString}",
                RewardType.HitPoints => $"{Type}: HP on {target} {operationString}",
                RewardType.SpellPoints => $"{Type}: SP on {target} {operationString}",
                RewardType.SpellLearningPoints => $"{Type}: SLP on {target} {operationString}",
				RewardType.Conditions => $"{Type}: {operationString} {Conditions} on {target}",
				RewardType.UsableSpellTypes => $"{Type}: {operationString} {UsableSpellTypes} on {target}",
				RewardType.Languages => $"{Type}: {operationString} {Languages} on {target}",
                RewardType.Experience => $"{Type}: Exp on {target} {operationString}",
                RewardType.MaxAttribute => $"{Type}: Max {Attribute} on {target} {operationString}",
                RewardType.AttacksPerRound => $"{Type}: APR on {Target} {operationString}",
                RewardType.TrainingPoints => $"{Type}: TP on {target} {operationString}",
				RewardType.Level => $"{Type}: Level on {target} {operationString}",
				RewardType.Damage => $"{Type}: Damage on {target} {operationString}",
                RewardType.Defense => $"{Type}: Defense on {target} {operationString}",
                RewardType.MaxHitPoints => $"{Type}: Max HP on {target} {operationString}",
                RewardType.MaxSpellPoints => $"{Type}: Max SP on {target} {operationString}",
                RewardType.EmpowerSpells => $"{Type}: {EmpowerString()}",
                RewardType.ChangePortrait => $"{Type}: Change portrait to {Value} for {target}",
                RewardType.MaxSkill => $"{Type}: Max {Skill} on {target} {operationString}",
                RewardType.MagicArmorLevel => $"{Type}: M-B-A on {target} {operationString}",
                RewardType.MagicWeaponLevel => $"{Type}: M-B-W on {target} {operationString}",
				RewardType.Spells => $"{Type}: {operationString} spell {RewardTypeValue} on {target}",
				_ => $"{Type}: Unknown ({(int)TypeOfReward}:{RewardTypeValue}) on {target} {operationString}"
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

        public override Event Clone(bool keepNext)
        {
            var clone = new ChangeTileEvent
            {
                X = X,
                Y = Y,
                FrontTileIndex = FrontTileIndex,
                MapIndex = MapIndex,
                Unknown = CloneBytes(Unknown)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new StartBattleEvent
            {
                MonsterGroupIndex = MonsterGroupIndex,
                Unknown1 = CloneBytes(Unknown1),
                Unknown2 = CloneBytes(Unknown2)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new EnterPlaceEvent
            {
                OpeningHour = OpeningHour,
                ClosingHour = ClosingHour,
                PlaceIndex = PlaceIndex,
                ClosedTextIndex = ClosedTextIndex,
                PlaceType = PlaceType,
                UsePlaceTextIndex = UsePlaceTextIndex,
                MerchantDataIndex = MerchantDataIndex
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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
            HasCondition = 0x0d,
            Hand = 0x0e,
            SayWord = 0x0f, // it also pops up the dictionary to say something
            EnterNumber = 0x10, // enter number popup with correct number
            Levitating = 0x11,
            HasGold = 0x12,
            HasFood = 0x13,
            Eye = 0x14,
            Mouth = 0x15,
            TransportAtLocation = 0x16,
            MultiCursor = 0x17,
            TravelType = 0x18,
            LeadClass = 0x19,
            SpellEmpowered = 0x1a,
            IsNight = 0x1b,
            Attribute = 0x1c,
            Skill = 0x1d
        }

        public ConditionType TypeOfCondition { get; set; }
        public Condition DisallowedAilments { get; set; }
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

        public override Event Clone(bool keepNext)
        {
            var clone = new ConditionEvent
            {
                TypeOfCondition = TypeOfCondition,
                ObjectIndex = ObjectIndex,
                Value = Value,
                Count = Count,
                ContinueIfFalseWithMapEventIndex = ContinueIfFalseWithMapEventIndex,
                DisallowedAilments = DisallowedAilments
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            string falseHandling = ContinueIfFalseWithMapEventIndex == 0xffff
                ? "Stop here if false"
                : $"Jump to event {ContinueIfFalseWithMapEventIndex:x2} if false";

            string GetMultiCursorString()
            {
                string cursors = "";

                if ((ObjectIndex & 0x1) != 0)
                    cursors += "Hand";
                if ((ObjectIndex & 0x2) != 0)
                    cursors += "Eye";
                if ((ObjectIndex & 0x4) != 0)
                    cursors += "Mouth";

                if (cursors.Length == 0)
                    return "None";

                return cursors;
            }

            return TypeOfCondition switch
            {
                ConditionType.GlobalVariable => $"{Type}: Global variable {ObjectIndex} = {Value}, {falseHandling}",
                ConditionType.EventBit => $"{Type}: Event bit {ObjectIndex / 64 + 1}:{1 + ObjectIndex % 64} = {Value}, {falseHandling}",
                ConditionType.DoorOpen => $"{Type}: Door {ObjectIndex} {(Value == 0 ? "closed" : "open")}, {falseHandling}",
                ConditionType.ChestOpen => $"{Type}: Chest {ObjectIndex} {(Value == 0 ? "closed" : "open")}, {falseHandling}",
                ConditionType.CharacterBit => $"{Type}: Character bit {ObjectIndex / 32 + 1}:{1 + ObjectIndex % 32} = {Value}, {falseHandling}",
                ConditionType.PartyMember => $"{Type}: Has party member {ObjectIndex} without ailments {EnumHelper.GetFlagNames(DisallowedAilments, 2)}, {falseHandling}",
                ConditionType.ItemOwned => $"{Type}: {(Value == 0 ? $"Not own item" : $"Own item {Math.Max(1, Count)}x")} {ObjectIndex}, {falseHandling}",
                ConditionType.UseItem => $"{Type}: Use item {ObjectIndex}, {falseHandling}",
                ConditionType.KnowsKeyword => $"{Type}: {(Value == 0 ? "Not know" : "Know")} keyword {ObjectIndex}, {falseHandling}",
                ConditionType.LastEventResult => $"{Type}: Success of last event, {falseHandling}",
                ConditionType.GameOptionSet => $"{Type}: Game option {(Option)(1 << (int)ObjectIndex)} is {(Value == 0 ? "not set" : "set")}, {falseHandling}",
                ConditionType.CanSee => $"{Type}: {(Value == 0 ? "Can't see" : "Can see")}, {falseHandling}",
                ConditionType.HasCondition => $"{Type}: {(Value == 0 ? "Has not" : "Has")} condition {(Condition)(1 << (int)ObjectIndex)}, {falseHandling}",
                ConditionType.Hand => $"{Type}: Hand cursor {(Value == 0 ? "not " : "")}used, {falseHandling}",
                ConditionType.SayWord => $"{Type}: Say keyword {ObjectIndex}, {falseHandling}",
                ConditionType.EnterNumber => $"{Type}: Enter number {ObjectIndex}, {falseHandling}",
                ConditionType.Levitating => $"{Type}: Levitating, {falseHandling}",
                ConditionType.HasGold => $"{Type}: Gold {(Value == 0 ? "<" : ">=")} {ObjectIndex}, {falseHandling}",
                ConditionType.HasFood => $"{Type}: Food {(Value == 0 ? "<" : ">=")} {ObjectIndex}, {falseHandling}",
                ConditionType.Eye => $"{Type}: Eye cursor {(Value == 0 ? " not " : "")}used, {falseHandling}",
                ConditionType.Mouth => $"{Type}: Mouth cursor {(Value == 0 ? " not " : "")}used, {falseHandling}",
                ConditionType.TransportAtLocation => $"{Type}: Transport {(Value == 0 ? "not " : "")}at event location , {falseHandling}",
                ConditionType.MultiCursor => $"{Type}: Any cursor of {GetMultiCursorString()} {(Value == 0 ? "not " : "")}used, {falseHandling}",
                ConditionType.TravelType => $"{Type}: Travel type {(Value == 0 ? "not " : "")}{EnumHelper.GetName((TravelType)ObjectIndex)}, {falseHandling}",
                ConditionType.LeadClass => $"{Type}: Active party member has {(Value == 0 ? "not " : "")}class {EnumHelper.GetName((Class)ObjectIndex)}, {falseHandling}",
                ConditionType.SpellEmpowered => $"{Type}: Active party member has {(Value == 0 ? "not " : "")}{((CharacterElement)(1 << (4 + (int)Util.Limit(0, ObjectIndex, 2)))).ToString().ToLower()} spells empowered, {falseHandling}",
                ConditionType.IsNight => $"{Type}: Is {(Value == 0 ? "not " : "")}night, {falseHandling}",
                ConditionType.Attribute => $"{Type}: Active player {(Attribute)ObjectIndex} {(Value == 0 ? "<" : ">=")} {Count}, {falseHandling}",
                ConditionType.Skill => $"{Type}: Active player {(Skill)ObjectIndex} {(Value == 0 ? "<" : ">=")} {Count}, {falseHandling}",
				_ => $"{Type}: Unknown ({TypeOfCondition}), Index {ObjectIndex}, Value {Value}, {falseHandling}",
            };
        }
    }

	public class PartyMemberConditionEvent : Event
	{
		public enum PartyMemberConditionType : byte
		{
            Level = 0x00,
			Attribute = 0x01,
			Skill = 0x02,
			TrainingPoints = 0x03
		}

		public enum PartyMemberConditionTarget : byte
		{
			ActivePlayer,
            All,
            Any,
            Min,
            Max,
            Average,
            Random,
            FirstCharacter, // Thalion, and then all others
            ActiveInventory = 0xff
        }

		public PartyMemberConditionType TypeOfCondition { get; set; }
		public Condition DisallowedAilments { get; set; }
		public uint Value { get; set; }
		public uint ConditionValueIndex { get; set; } // Which attribute, skill, etc
		public PartyMemberConditionTarget Target { get; set; }
		/// <summary>
		/// Next map event to continue with if the condition was met.
		/// 0xffff means continue with next map event from the list.
		/// </summary>
		public uint ContinueIfFalseWithMapEventIndex { get; set; }

		public override Event Clone(bool keepNext)
		{
			var clone = new PartyMemberConditionEvent
			{
				TypeOfCondition = TypeOfCondition,
				Target = Target,
				Value = Value,
                ConditionValueIndex = ConditionValueIndex,
				ContinueIfFalseWithMapEventIndex = ContinueIfFalseWithMapEventIndex,
				DisallowedAilments = DisallowedAilments
			};
			CloneProperties(clone, keepNext);
			return clone;
		}

		public override string ToString()
		{
            string target = Target switch
            {
                PartyMemberConditionTarget.ActivePlayer => "Active player",
                PartyMemberConditionTarget.All => "All players",
                PartyMemberConditionTarget.Any => "Any player",
                PartyMemberConditionTarget.Min => "Min",
                PartyMemberConditionTarget.Max => "Max",
                PartyMemberConditionTarget.Average => "Average",
                PartyMemberConditionTarget.Random => "Random player",
                PartyMemberConditionTarget.ActiveInventory => "Active inventory",
                >= PartyMemberConditionTarget.FirstCharacter => $"Char {1 + (int)Target - (int)PartyMemberConditionTarget.FirstCharacter}",
            };
			string falseHandling = ContinueIfFalseWithMapEventIndex == 0xffff
				? "Stop here if false"
				: $"Jump to event {ContinueIfFalseWithMapEventIndex:x2} if false";
            string disallowedAilments = DisallowedAilments == Condition.None
                ? ""
                : $" (not {EnumHelper.GetFlagNames(DisallowedAilments, 2)})";

			return TypeOfCondition switch
			{
				PartyMemberConditionType.Level => $"{Type}: {target} Level >= {Value}{disallowedAilments}, {falseHandling}",
				PartyMemberConditionType.Attribute => $"{Type}: {target} {(Attribute)ConditionValueIndex} >= {Value}{disallowedAilments}, {falseHandling}",
				PartyMemberConditionType.Skill => $"{Type}: {target} {(Skill)ConditionValueIndex} >= {Value}{disallowedAilments}, {falseHandling}",
				PartyMemberConditionType.TrainingPoints => $"{Type}: {target} TP >= {Value}{disallowedAilments}, {falseHandling}",
				_ => $"{Type}: Unknown ({TypeOfCondition}), Target {target}, Value {Value}, {falseHandling}",
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
            /// Adds or removes a condition
            /// </summary>
            AddCondition = 0x0d,
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

        public override Event Clone(bool keepNext)
        {
            var clone = new ActionEvent
            {
                TypeOfAction = TypeOfAction,
                ObjectIndex = ObjectIndex,
                Value = Value,
                Count = Count,
                Unknown1 = CloneBytes(Unknown1),
                Unknown2 = CloneBytes(Unknown2)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return TypeOfAction switch
            {
                ActionType.SetGlobalVariable => $"{Type}: Set global variable {ObjectIndex} to {Value}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.SetEventBit => $"{Type}: Set event bit {ObjectIndex / 64 + 1}:{1 + ObjectIndex % 64} to {(Value != 0 ? "inactive" : "active")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.LockDoor => $"{Type}: {(Value == 0 ? "Lock" : "Unlock")} door {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.LockChest => $"{Type}: {(Value == 0 ? "Lock" : "Unlock")} chest {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",                
                ActionType.SetCharacterBit => $"{Type}: Set character bit {ObjectIndex / 32 + 1}:{1 + ObjectIndex % 32} to {(Value != 0 ? "hidden" : "show")}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddItem => $"{Type}: {(Value == 0 ? "Remove" : "Add")} {Math.Max(1, Count)}x item {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddKeyword => $"{Type}: {(Value == 0 ? "Remove" : "Add")} keyword {ObjectIndex}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.SetGameOption => $"{Type}: {(Value == 0 ? "Deactivate" : "Activate")} game option {(Data.Enumerations.Option)(1 << (int)ObjectIndex)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
                ActionType.AddCondition => $"{Type}: {(Value == 0 ? "Remove" : "Add")} condition {(Condition)(1 << (int)ObjectIndex)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}, Unknown2 {string.Join(" ", Unknown2.Select(u => u.ToString("x2")))}",
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

        public override Event Clone(bool keepNext)
        {
            var clone = new Dice100RollEvent
            {
                Chance = Chance,
                ContinueIfFalseWithMapEventIndex = ContinueIfFalseWithMapEventIndex,
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new ConversationEvent
            {
                Interaction = Interaction,
                Value = Value,
                Unused1 = CloneBytes(Unused1),
                Unused2 = CloneBytes(Unused2)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new PrintTextEvent
            {
                NPCTextIndex = NPCTextIndex,
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new CreateEvent
            {
                TypeOfCreation = TypeOfCreation,
                Amount = Amount,
                ItemIndex = ItemIndex,
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new DecisionEvent
            {
                TextIndex = TextIndex,
                NoEventIndex = NoEventIndex,
                Unknown1 = CloneBytes(Unknown1)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new ChangeMusicEvent
            {
                MusicIndex = MusicIndex,
                Volume = Volume,
                Unknown1 = CloneBytes(Unknown1)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type}: Music {(MusicIndex == 255 ? "<Map music>" : MusicIndex.ToString())}, Volume {(Volume / 255.0f).ToString("0.0", CultureInfo.InvariantCulture)}, Unknown1 {string.Join(" ", Unknown1.Select(u => u.ToString("x2")))}";
        }
    }

    public class ExitEvent : Event
    {
        public byte[] Unused { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new ExitEvent
            {
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

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

        public override Event Clone(bool keepNext)
        {
            var clone = new SpawnEvent
            {
                X = X,
                Y = Y,
                TravelType = TravelType,
                MapIndex = MapIndex,
                Unknown1 = CloneBytes(Unknown1),
                Unknown2 = CloneBytes(Unknown2)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type} {TravelType} on map {MapIndex} at {X}, {Y}";
        }
    }

    public class InteractEvent : Event
    {
        public byte[] Unused { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new InteractEvent
            {
                Unused = CloneBytes(Unused)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type}";
        }
    }

    public class RemovePartyMemberEvent : Event
    {
        public byte CharacterIndex { get; set; }
        public byte ChestIndexEquipment { get; set; }
        public byte ChestIndexInventory { get; set; }
        public byte[] Unused { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new RemovePartyMemberEvent
            {
                CharacterIndex = CharacterIndex,
                ChestIndexEquipment = ChestIndexEquipment,
                ChestIndexInventory = ChestIndexInventory,
                Unused = CloneBytes(Unused),
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type} {CharacterIndex} (Equip -> Chest {ChestIndexEquipment}, Items -> Chest {ChestIndexInventory})";
        }
    }

    public class DelayEvent : Event
    {
        public uint Milliseconds { get; set; }
        public byte[] Unused1 { get; set; }
        public ushort Unused2 { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new DelayEvent
            {
                Milliseconds = Milliseconds,
                Unused1 = CloneBytes(Unused1),
                Unused2 = Unused2,
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type} {Milliseconds} ms";
        }
    }

    public class ShakeEvent : Event
    {
        public uint Shakes { get; set; }
        public byte[] Unused1 { get; set; }
        public ushort Unused2 { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new ShakeEvent
            {
                Shakes = Shakes,
                Unused1 = CloneBytes(Unused1),
                Unused2 = Unused2,
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return $"{Type} {Shakes} shakes";
        }
    }

    public class DebugEvent : Event
    {
        public byte[] Data { get; set; }

        public override Event Clone(bool keepNext)
        {
            var clone = new DebugEvent
            {
                Data = CloneBytes(Data)
            };
            CloneProperties(clone, keepNext);
            return clone;
        }

        public override string ToString()
        {
            return Type.ToString() + ": " + string.Join(" ", Data.Select(d => d.ToString("X2")));
        }
    }
}
