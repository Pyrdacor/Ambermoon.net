using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Events;
    using Serialization;
    using Util;

    internal class EventDataProperty<T>
    {
        internal readonly int Index;
        internal readonly Func<T, byte[]> Serializer;
        internal readonly Func<byte[], T> Deserializer;

        public EventDataProperty(int index, Func<T, byte[]> serializer, Func<byte[], T> deserializer)
        {
            Index = index;
            Serializer = serializer;
            Deserializer = deserializer;
        }

        public T Get(EventData eventData)
        {
            var data = new Span<byte>(eventData.Data, Index, Index < 6 ? 1 : 2);
            return Deserializer(data.ToArray());
        }

        public void Set(EventData eventData, T value)
        {
            var data = new Span<byte>(eventData.Data, Index, Index < 6 ? 1 : 2);
            Serializer(value).CopyTo(data);
        }

        public void Copy(EventData source, EventData target)
        {
            Set(target, Get(source));
        }
    }

    internal class ByteEventDataProperty : EventDataProperty<uint>
    {
        public ByteEventDataProperty(int index)
            : base(index,
                value => new[] { (byte)(value & 0xff) },
                data => data[0])
        {
            if (index is < 1 or > 9)
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be in range 1 to 9.");
        }
    }

    internal class WordEventDataProperty : EventDataProperty<uint>
    {
        public WordEventDataProperty(int index)
            : base(index,
                value => new[]
                {
                    (byte)(value >> 8),
                    (byte)(value & 0xff)
                },
                data => (uint)((data[0] << 8) | data[1]))
        {
            if (index != 2 && index != 4 && index != 6 && index != 8)
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be 2, 4, 6 or 8.");
        }
    }

    internal class EnumEventDataProperty<TEnum> : EventDataProperty<TEnum>
    {
        public EnumEventDataProperty(int index)
            : this(index, e => Convert.ToByte(e), b => (TEnum)System.Enum.ToObject(typeof(TEnum), b))
        {

        }

        public EnumEventDataProperty(int index, Func<TEnum, byte> toByte, Func<byte, TEnum> toEnum)
            : base(index,
                value => new[] { toByte(value) },
                data => toEnum(data[0]))
        {
            if (index is < 1 or >= 6)
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be in range 1 to 5.");
        }
    }

    internal class NullableEventDataProperty<T> : EventDataProperty<T?>
        where T : struct
    {
        public NullableEventDataProperty(EventDataProperty<T> baseProperty, ushort nullValue)
            : base(baseProperty.Index,
                   CreateSerializer(baseProperty.Serializer, nullValue),
                   CreateDeserializer(baseProperty.Deserializer, nullValue))
        {
        }

        private static Func<T?, byte[]> CreateSerializer(Func<T, byte[]> baseSerializer, ushort nullValue)
        {
            return value =>
            {
                return value == null ? new[] { (byte)(nullValue >> 8), (byte)(nullValue & 0xff) } : baseSerializer(value.Value);
            };
        }

        private static Func<byte[], T?> CreateDeserializer(Func<byte[], T> baseDeserializer, ushort nullValue)
        {
            return data =>
            {
                ushort value = (ushort)((data[0] << 8) | data[1]);
                return value == nullValue ? null : baseDeserializer(data);
            };
        }
    }

    internal class EventReferenceDataProperty : NullableEventDataProperty<uint>
    {
        public EventReferenceDataProperty()
            : base(new WordEventDataProperty(8), ushort.MaxValue)
        {
        }
    }

    public class EventData : IMutableIndex, IIndexedData, IEquatable<EventData>, INotifyPropertyChanged
    {

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public EventType Type => (EventType)Data[0];

        [Range(0, ushort.MaxValue)]
        public uint? NextEventIndex
        {
            get
            {
                ushort index = Data[^2];
                index <<= 8;
                index |= Data[^1];

                return index == ushort.MaxValue ? null : index;
            }
            set
            {
                uint index = value ?? ushort.MaxValue;
                ValueChecker.Check(index, 0, ushort.MaxValue);
                Data[^2] = (byte)(index >> 8);
                Data[^1] = (byte)(index & 0xff);

                OnPropertyChanged();
            }
        }

        internal byte[] Data { get; private protected init; } = new byte[GameDataRepository.EventDataSize];

        public virtual bool AllowOnMaps => true;

        public virtual bool AllowInConversations => true;

        public virtual bool AllowAsEventEntry => true;

        public virtual bool AllowOn2DMaps => AllowOnMaps;

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write(Data);
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var eventData = new EventData
            {
                Data = dataReader.ReadBytes(GameDataRepository.EventDataSize)
            };

            return eventData.Type switch
            {
                EventType.Teleport => new TeleportEventData(eventData),
                EventType.Door => new DoorEventData(eventData),
                EventType.Chest => new ChestEventData(eventData),
                EventType.MapText => new MapTextEventData(eventData),
                EventType.Spinner => new SpinnerEventData(eventData),
                EventType.Trap => new TrapEventData(eventData),
                _ => eventData
            };
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var mapEventData = (EventData)Deserialize(dataReader, advanced);
            (mapEventData as IMutableIndex).Index = index;
            return mapEventData;
        }

        #endregion


        #region Equality

        public bool Equals(EventData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Data.Equals(other.Data);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EventData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(EventData? left, EventData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(EventData? left, EventData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public EventData Copy()
        {
            return new EventData
            {
                Data = (byte[])Data.Clone()
            };
        }

        public virtual object Clone() => Copy();

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private protected bool SetField<T>(EventDataProperty<T> field, T value, [CallerMemberName] string? propertyName = null)
        {
            var oldValue = field.Get(this);
            if (EqualityComparer<T>.Default.Equals(oldValue, value)) return false;
            field.Set(this, value);
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}
