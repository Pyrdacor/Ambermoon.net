using System;
using System.Collections;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class CharacterValue
    {
        public uint CurrentValue { get; set; }
        public uint MaxValue { get; set; }
        public uint BonusValue { get; set; }
        public uint Unknown { get; set; }
        public uint TotalCurrentValue => CurrentValue + BonusValue;
    }

    public class CharacterValueCollection<TType> : IEnumerable<CharacterValue> where TType : Enum
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
