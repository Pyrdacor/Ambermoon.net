using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class ItemSlotData : IIndexedData
    {
        public const uint UnlimitedAmount = byte.MaxValue;
        private uint _amount = 0;
        private uint _numRemainingCharges = 0;
        private uint _numRecharges = 0;
        private uint _itemIndex = 0;

        public uint Index { get; private set; }

        [Range(0, byte.MaxValue)]
        public uint Amount
        {
            get => _amount;
            set
            {
                if (value != 255 && value > 99)
                    throw new ArgumentOutOfRangeException(nameof(Amount), $"Amount is limited to the range 0 to 99 and 255 for unlimited amount.");

                _amount = value;
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
                    throw new ArgumentOutOfRangeException(nameof(NumberOfRecharges), $"Number of recharges is limited to the range 0 to {byte.MaxValue}.");

                _numRecharges = value;
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint ItemIndex
        {
            get => _itemIndex;
            set
            {
                if (value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(ItemIndex), $"Item index is limited to the range 0 to {ushort.MaxValue}.");

                _itemIndex = value;
            }
        }

        public ItemSlotFlags Flags { get; set; }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var itemSlotData = (ItemSlotData)Deserialize(dataReader, advanced);
            itemSlotData.Index = index;
            return itemSlotData;
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

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)Amount);
            dataWriter.Write((byte)NumberOfRemainingCharges);
            dataWriter.Write((byte)NumberOfRecharges);
            dataWriter.Write((byte)Flags);
            dataWriter.Write((ushort)ItemIndex);
        }
    }
}
