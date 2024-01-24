using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    using Util;

    public class ChestEventData : EventData, IBranchEvent
    {

        #region Fields

        private readonly ByteEventDataProperty _lockedPercentage = new(1);
        private readonly ByteEventDataProperty _hiddenPercentage = new(2);
        private readonly NullableEventDataProperty<uint> _textIndex = new(new ByteEventDataProperty(3), 0xff);
        private readonly ByteEventDataProperty _saveIndex = new(4);
        private readonly EnumEventDataProperty<ChestEvent.ChestFlags> _flags = new(5);
        private readonly NullableEventDataProperty<uint> _keyIndex = new(new WordEventDataProperty(6), 0);
        private readonly EventReferenceDataProperty _unlockFailEventIndex = new();

        #endregion


        #region Properties

        /// <summary>
        /// Specifies how well the chest is locked. 0 means unlocked, 100 means
        /// it can only be opened with a specific key. If the value is between
        /// those values, the chest can be opened with a lock pick or with the
        /// lock picking skill. The higher the value, the harder it is to open.
        /// If the skill is used, a dice roll is performed against the user's
        /// skill level. If the roll is successful, the chest is opened.
        /// Otherwise, traps associated with the chest are triggered and the
        /// chest remains closed.
        ///
        /// This event is also used for simple item pickups. In this case, this
        /// value should be set to 0. Chest with a value of 0 are always open
        /// and their contents can be accessed immediately.
        /// </summary>
        [Range(0, 100)]
        public uint LockedPercentage
        {
            get => _lockedPercentage.Get(this);
            set
            {
                ValueChecker.Check(value, 0, 100);
                if (value < 100 && KeyIndex is not null)
                    throw new ArgumentException("Locked percentage must be 100 if a key index is set.");
                SetField(_lockedPercentage, value);
            }
        }

        /// <summary>
        /// Specifies how well the chest is hidden. 0 means it is not hidden at
        /// all and everybody can see it. All other values reduce the chance
        /// of finding the chest. The active party member's search skill is
        /// randomly tested against that value. If rand(0..skillLevel) is
        /// greater or equal to the given value, the chest is found.
        ///
        /// Note: This is not properly implemented in the original version as
        /// it will always perform a random check against your skill level
        /// regardless of the given value. So it is either always found when
        /// the value is 0 or it just activates the skill check otherwise.
        ///
        /// Note: In the original there is only one chest event which is using
        /// this. A skeleton in the Antique Area where you can find the second
        /// flamethrower. The event has a value of 50, but as mentioned the
        /// value does not matter. In Ambermoon Advanced the search skill is
        /// used more often but a new condition event type is used there instead.
        /// So it's best to just leave this value at 0 and use the condition
        /// event type instead.
        /// </summary>
        [Range(0, 100)]
        public uint HiddenPercentage
        {
            get => _hiddenPercentage.Get(this);
            set
            {
                ValueChecker.Check(value, 0, 100);
                SetField(_hiddenPercentage, value);
            }
        }

        /// <summary>
        /// If given, the text with this index is displayed when the
        /// open chest is approached.
        /// </summary>
        [Range(0, byte.MaxValue - 1)]
        public uint? TextIndex
        {
            get => _textIndex.Get(this);
            set
            {
                if (value is not null)
                    ValueChecker.Check(value.Value, 0, byte.MaxValue - 1);
                SetField(_textIndex, value);
            }
        }

        /// <summary>
        /// Index of the chest which is used to reference it in the savegame.
        ///
        /// This index is 1-based, so the first chest has index 1.
        ///
        /// Note: In Ambermoon Advanced extended chests were introduced to
        /// unlock 128 more chests. If you stick to the original, please ensure
        /// that the index does not exceed <see cref="GameDataRepository.MaxChests"/>.
        /// </summary>
        [Range(1, GameDataRepository.MaxChests + GameDataRepository.MaxExtendedChest)]
        public uint SaveIndex
        {
            get => _saveIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 0, GameDataRepository.MaxChests + GameDataRepository.MaxExtendedChest);
                SetField(_saveIndex, value);
            }
        }

        private ChestEvent.ChestFlags Flags
        {
            get => _flags.Get(this);
            set => SetField(_flags, value);
        }

        /// <summary>
        /// Type of the chest.
        ///
        /// Normal chests stay accessible, and you can put items, gold and
        /// rations in and out.
        /// 
        /// Junk piles are closed when they are empty. You can't put items,
        /// gold or rations in them.
        /// </summary>
        public ChestType ChestType
        {
            get =>
                Flags.HasFlag(ChestEvent.ChestFlags.JunkPile)
                    ? ChestType.Junk
                    : ChestType.Chest;
            set
            {
                if (value == ChestType.Junk)
                    Flags |= ChestEvent.ChestFlags.JunkPile;
                else
                    Flags &= ~ChestEvent.ChestFlags.JunkPile;
            }
        }

        /// <summary>
        /// If active, emptied junk piles are removed from the map.
        /// This means that the associated map event entry is marked
        /// as inactive.
        ///
        /// Note: In the original this is more related to storing the
        /// state of the chest. This means if the contents of the chest
        /// are written back to the savegame. But for normal chests this
        /// is always desired and for junk it is only not desired if there
        /// is a single item and the chest is shared between multiple
        /// event usages (for example the same plant type on the forest moon).
        ///
        /// The original default logic for junk would be that the flag is not
        /// set and so the data is stored back to the savegame. If it is empty
        /// this means that every 
        /// </summary>
        public bool AutoRemove
        {
            get => !Flags.HasFlag(ChestEvent.ChestFlags.NoSave);
            set
            {
                if (!value)
                    Flags |= ChestEvent.ChestFlags.NoSave;
                else
                    Flags &= ~ChestEvent.ChestFlags.NoSave;
            }
        }
        }

        /// <summary>
        /// Key index required to open the chest.
        /// </summary>
        [Range(1, GameDataRepository.MaxItems)]
        public uint? KeyIndex
        {
            get => _keyIndex.Get(this);
            set
            {
                if (value is not null)
                {
                    ValueChecker.Check(value.Value, 1, GameDataRepository.MaxItems);
                    LockedPercentage = 100; // Always set to 100 when requiring a key
                }
                SetField(_keyIndex, value);
            }
        }

        [Range(0, GameDataRepository.MaxEvents)]
        public uint? UnlockFailEventIndex
        {
            get => _unlockFailEventIndex.Get(this);
            set
            {
                if (value is not null)
                    ValueChecker.Check(value.Value, 0, GameDataRepository.MaxEvents);
                SetField(_unlockFailEventIndex, value);
            }
        }

        uint? IBranchEvent.BranchEventIndex => UnlockFailEventIndex;

        #endregion


        #region Constructors

        internal ChestEventData(EventData data)
        {
            _lockedPercentage.Copy(data, this);
            _saveIndex.Copy(data, this);
            _closedDoorTextIndex.Copy(data, this);
            _unlockTextIndex.Copy(data, this);
            _keyIndex.Copy(data, this);
            _unlockFailEventIndex.Copy(data, this);
        }

        #endregion

    }
}
