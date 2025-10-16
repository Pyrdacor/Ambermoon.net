using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data
{
    public class CharacterValue : INotifyPropertyChanged, IEquatable<CharacterValue>
    {
        private uint _currentValue;
        private uint _maxValue;
        private int _bonusValue;
        private uint _storedValue;

        public CharacterValue()
        {

        }

        public CharacterValue(CharacterValue other)
        {
            _currentValue = other._currentValue;
			_maxValue = other._maxValue;
			_bonusValue = other._bonusValue;
			_storedValue = other._storedValue;
        }

		/// <summary>
		/// Current value without bonus.
		/// While exhausted this will be the exhausted value
		/// (half the previous value) and the actual value
		/// is stored temporarily in <see cref="StoredValue"/>.
		/// </summary>
		public uint CurrentValue
        {
            get => _currentValue;
            set => SetField(ref _currentValue, value);
        }

        /// <summary>
        /// Maximum value for the character.
        /// </summary>
        public uint MaxValue
        {
            get => _maxValue;
            set => SetField(ref _maxValue, value);
        }

        /// <summary>
        /// Bonus from equipment.
        /// </summary>
        public int BonusValue
        {
            get => _bonusValue;
            set => SetField(ref _bonusValue, value);
        } // can be negative if item is cursed

        /// <summary>
        /// This stores the actual value while exhaustion is active.
        /// </summary>
        public uint StoredValue
        {
            get => _storedValue;
            set => SetField(ref _storedValue, value);
        }

        public uint TotalCurrentValue => (uint)Math.Max(0, (int)CurrentValue + BonusValue);
        public uint TotalMaxValue => (uint)Math.Max(0, (int)MaxValue + BonusValue);
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

#nullable enable
        public bool Equals(CharacterValue? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return _currentValue == other._currentValue &&
                   _maxValue == other._maxValue &&
                   _bonusValue == other._bonusValue &&
                   _storedValue == other._storedValue;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CharacterValue)obj);
        }

        public override int GetHashCode()
        {
            int hashCode = 0;

            hashCode ^= (int)_currentValue;
            hashCode ^= (int)_maxValue;
            hashCode ^= _bonusValue;
            hashCode ^= (int)_storedValue;

            return hashCode;
        }

        public static bool operator ==(CharacterValue? left, CharacterValue? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CharacterValue? left, CharacterValue? right)
        {
            return !Equals(left, right);
        }
#nullable restore
    }

    public class CharacterValueCollection<TType> : IEnumerable<CharacterValue>, IEquatable<CharacterValueCollection<TType>> where TType : Enum
    {
        readonly CharacterValue[] values = null;

		public CharacterValueCollection(CharacterValueCollection<TType> other)
		{
			values = new CharacterValue[other.values.Length];

			for (int i = 0; i < other.values.Length; ++i)
				values[i] = new(other.values[i]);
		}

		public CharacterValueCollection(int size)
        {
            values = new CharacterValue[size];

            for (int i = 0; i < size; ++i)
                values[i] = new CharacterValue();
        }

        public CharacterValueCollection(params CharacterValue[] values)
        {
            this.values = values;
        }

        public CharacterValue this[TType index]
        {
            get => values[Convert.ToInt32(index)];
            set => values[Convert.ToInt32(index)] = value;
        }

        public CharacterValue this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        public CharacterValue this[uint index]
        {
            get => values[index];
            set => values[index] = value;
        }

        public int Length => values.Length;

        public static implicit operator CharacterValue[](CharacterValueCollection<TType> collection)
        {
            return collection.values;
        }

        public IEnumerator<CharacterValue> GetEnumerator()
        {
            return ((IEnumerable<CharacterValue>)values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#nullable enable
        public bool Equals(CharacterValueCollection<TType>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (values is null) return other.values is null;
            if (other.values is null) return false;
            if (values.Length != other.values.Length) return false;
            
            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].Equals(other.values[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CharacterValueCollection<TType>)obj);
        }

        public override int GetHashCode()
        {
            if (values is null)
                return 0;

            int hashCode = 0;

            for (int i = 0; i < values.Length; i++)
            {
                hashCode ^= values[i].GetHashCode();
            }

            return hashCode;
        }

        public static bool operator ==(CharacterValueCollection<TType>? left, CharacterValueCollection<TType>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CharacterValueCollection<TType>? left, CharacterValueCollection<TType>? right)
        {
            return !Equals(left, right);
        }
#nullable restore
    }
}
