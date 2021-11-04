using System;
using System.Collections;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    [Serializable]
    public class CharacterValue
    {
        /// <summary>
        /// Current value without bonus.
        /// While exhausted this will be the exhausted value
        /// (half the previous value) and the actual value
        /// is stored temporarly in <see cref="StoredValue"/>.
        /// </summary>
        public uint CurrentValue { get; set; }
        /// <summary>
        /// Maximum value for the character.
        /// </summary>
        public uint MaxValue { get; set; }
        /// <summary>
        /// Bonus from equipment.
        /// </summary>
        public int BonusValue { get; set; } // can be negative if item is cursed
        /// <summary>
        /// This stores the actual value while exhaustion is active.
        /// </summary>
        public uint StoredValue { get; set; }
        public uint TotalCurrentValue => (uint)Math.Max(0, (int)CurrentValue + BonusValue);
        public uint TotalMaxValue => (uint)Math.Max(0, (int)MaxValue + BonusValue);
    }

    [Serializable]
    public class CharacterValueCollection<TType> : IEnumerable<CharacterValue> where TType : System.Enum
    {
        readonly CharacterValue[] values = null;

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
    }
}
