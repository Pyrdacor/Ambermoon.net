using System.ComponentModel;
using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class ItemSlotData : IMutableIndex, IIndexedData, IEquatable<ItemSlotData>
    {

        #region Constants

        public const uint UnlimitedAmount = byte.MaxValue;

        #endregion


        #region Fields

        private uint _amount = 0;
        private uint _numRemainingCharges = 0;
        private uint _numRecharges = 0;
        private ItemSlotFlags _flags = ItemSlotFlags.None;
        private uint _itemIndex = 0;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        [Range(0, byte.MaxValue)]
        public uint Amount
        {
            get => _amount;
            set
            {
                if (value != 255 && value > 99)
                    throw new ArgumentOutOfRangeException(nameof(Amount), $"Amount is limited to the range 0 to 99 and 255 for unlimited amount.");

                if (_amount == value) return;

                var oldValue = _amount;
                _amount = value;
                AmountChanged?.Invoke(oldValue, _amount);
            }
        }

        [Range(0, byte.MaxValue)]
        public uint NumberOfRemainingCharges
        {
            get => _numRemainingCharges;
            set
            {
                if (value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(NumberOfRemainingCharges), $"Number of remaining charges is limited to the range 0 to {byte.MaxValue}.");

                _numRemainingCharges = value;
            }
        }

        [Range(0, byte.MaxValue)]
        public uint NumberOfRecharges
        {
            get => _numRecharges;
            set
            {
                if (value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(NumberOfRecharges),
                        $"Number of recharges is limited to the range 0 to {byte.MaxValue}.");

                _numRecharges = value;
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint ItemIndex
        {
            get => _itemIndex;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(ItemIndex), $"Item index is limited to the range 0 to {ushort.MaxValue}.");

                if (_itemIndex == value) return;

                var oldValue = _itemIndex;
                _itemIndex = value;
                ItemChanged?.Invoke(oldValue, _itemIndex);
            }
        }

        public ItemSlotFlags Flags
        {
            get => _flags;
            set
            {
                if (_flags == value) return;

                bool wasCursed = _flags.HasFlag(ItemSlotFlags.Cursed);
                _flags = value;
                bool isCursed = _flags.HasFlag(ItemSlotFlags.Cursed);
                if (wasCursed != isCursed)
                    CursedChanged?.Invoke(wasCursed, isCursed);
            }
        }

        #endregion


        #region Methods

        public void Clear()
        {
            Amount = 0;
            NumberOfRemainingCharges = 0;
            NumberOfRecharges = 0;
            Flags = ItemSlotFlags.None;
            ItemIndex = 0;
        }

        public void SetTwoHandedWeaponShieldHand()
        {
            Amount = 1;
            NumberOfRemainingCharges = 0;
            NumberOfRecharges = 0;
            Flags = ItemSlotFlags.None;
            ItemIndex = 0;
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)Amount);
            dataWriter.Write((byte)NumberOfRemainingCharges);
            dataWriter.Write((byte)NumberOfRecharges);
            dataWriter.Write((byte)Flags);
            dataWriter.Write((ushort)ItemIndex);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var itemSlotData = new ItemSlotData();

            itemSlotData.Amount = dataReader.ReadByte();
            itemSlotData.NumberOfRemainingCharges = dataReader.ReadByte();
            itemSlotData.NumberOfRecharges = dataReader.ReadByte();
            itemSlotData.Flags = (ItemSlotFlags)dataReader.ReadByte();
            itemSlotData.ItemIndex = dataReader.ReadWord();

            return itemSlotData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var itemSlotData = (ItemSlotData)Deserialize(dataReader, advanced);
            (itemSlotData as IMutableIndex).Index = index;
            return itemSlotData;
        }

        #endregion


        #region Equality

        public bool Equals(ItemSlotData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _amount == other._amount && _numRemainingCharges == other._numRemainingCharges && _numRecharges == other._numRecharges && _itemIndex == other._itemIndex && Index == other.Index && Flags == other.Flags;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemSlotData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(ItemSlotData? left, ItemSlotData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ItemSlotData? left, ItemSlotData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public ItemSlotData Copy()
        {
            ItemSlotData copy = new()
            {
                Amount = Amount,
                NumberOfRemainingCharges = NumberOfRemainingCharges,
                NumberOfRecharges = NumberOfRecharges,
                Flags = Flags,
                ItemIndex = ItemIndex
            };

            (copy as IMutableIndex).Index = Index;

            return copy;
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event Action<uint, uint>? ItemChanged;
        public event Action<uint, uint>? AmountChanged;
        public event Action<bool, bool>? CursedChanged;

        #endregion

    }
}
