using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Collections;
    using Serialization;

    public sealed class MonsterGroupData : IMutableIndex, IIndexedData, IEquatable<MonsterGroupData>, INotifyPropertyChanged
    {

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public TwoDimensionalData<uint> MonsterIndices { get; } = new(6, 3);

        #endregion


        #region Constructors

        internal MonsterGroupData()
        {
            MonsterIndices.ItemChanged += (_, _) => OnPropertyChanged(nameof(MonsterIndices));
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    dataWriter.Write((ushort)MonsterIndices.Get(x, y));
                }
            }
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var monsterGroupData = new MonsterGroupData();

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    monsterGroupData.MonsterIndices.Set(x, y, dataReader.ReadWord());
                }
            }

            return monsterGroupData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var monsterGroupData = (MonsterGroupData)Deserialize(dataReader, advanced);
            (monsterGroupData as IMutableIndex).Index = index;
            return monsterGroupData;
        }

        #endregion


        #region Equality

        public bool Equals(MonsterGroupData? other)
        {
            if (other is null)
                return false;

            return MonsterIndices.Select((item, index) => new { Item = item, Index = index })
                .All(entry => other.MonsterIndices[entry.Index] == entry.Item);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MonsterGroupData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(MonsterGroupData? left, MonsterGroupData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MonsterGroupData? left, MonsterGroupData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public MonsterGroupData Copy()
        {
            MonsterGroupData copy = new();

            (copy as IMutableIndex).Index = Index;

            for (int i = 0; i < MonsterIndices.Count; i++)
                copy.MonsterIndices[i] = MonsterIndices[i];

            return copy;
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}
