using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data.Events
{
    using Util;

    /// <summary>
    /// Shows a locked or opened chest/loot screen when triggered.
    ///
    /// Beside its main purpose of representing chests, this event is also
    /// used for any item pickup outside of merchants or conversations where
    /// you can transfer items to your inventory.
    /// </summary>
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
        [DefaultValue(null)]
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

        [Range(1, GameDataRepository.MaxChests)]
        internal uint SaveIndex
        {
            get => _saveIndex.Get(this);
            set
            {
                ValueChecker.Check(value, 1, GameDataRepository.MaxChests);
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
        [DefaultValue(ChestType.Chest)]
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
        /// If active, the chest contents are restored to their initial
        /// state after closing the window. Otherwise, the updated
        /// chest contents are saved inside the savegame.
        ///
        /// Note: This is never set for stationary chests but only for
        /// some piles which share the same chest data. For example the
        /// flowers on the forest moon. If you pick a flower, it resets
        /// the chest data to the original and won't save the empty
        /// chest data to the savegame. This way another flower event
        /// can use the same chest data as it will present the initial
        /// chest data again.
        /// </summary>
        [DefaultValue(false)]
        public bool NoSave
        {
            get => Flags.HasFlag(ChestEvent.ChestFlags.NoSave);
            set
            {
                if (value)
                    Flags |= ChestEvent.ChestFlags.NoSave;
                else
                    Flags &= ~ChestEvent.ChestFlags.NoSave;
            }
        }

        /// <summary>
        /// As the original version only supports 256 chests,
        /// the concept of extended chests was introduced in
        /// Ambermoon Advanced. This allows 128 more chests
        /// to use. But it also reduces the maximum number of
        /// dictionary entries from 256 to 128.
        ///
        /// This flag is automatically set dependent on the
        /// specified chest index. If the chest index exceeds
        /// 256, it is automatically set to true, otherwise false.
        /// </summary>
        [AdvancedOnly]
        public bool ExtendedChest
        {
            get => Flags.HasFlag(ChestEvent.ChestFlags.ExtendedChest);
            private set
            {
                if (value)
                    Flags |= ChestEvent.ChestFlags.ExtendedChest;
                else
                    Flags &= ~ChestEvent.ChestFlags.ExtendedChest;
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
        public uint ChestIndex
        {
            get => ExtendedChest ? 256 + SaveIndex : SaveIndex;
            set
            {
                ValueChecker.Check(value, 1, GameDataRepository.MaxChests + GameDataRepository.MaxExtendedChest);
                SaveIndex = value <= 256 ? value : value - 256;
                ExtendedChest = value > 256;
            }
        }

        /// <summary>
        /// Key index required to open the chest.
        /// </summary>
        [DefaultValue(null)]
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
        [DefaultValue(null)]
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

        public override bool AllowInConversations => false;

        #endregion


        #region Constructors

        public ChestEventData()
        {
            Data[0] = (byte)EventType.Chest;
            TextIndex = null;
            KeyIndex = null;
            NextEventIndex = null;
            UnlockFailEventIndex = null;
        }

        internal ChestEventData(EventData data)
        {
            _lockedPercentage.Copy(data, this);
            _hiddenPercentage.Copy(data, this);
            _textIndex.Copy(data, this);
            _saveIndex.Copy(data, this);
            _flags.Copy(data, this);
            _keyIndex.Copy(data, this);
            _unlockFailEventIndex.Copy(data, this);
        }

        #endregion

    }
}
