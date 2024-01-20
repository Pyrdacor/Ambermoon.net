namespace Ambermoon.Data.GameDataRepository
{
    using Ambermoon.Data.Serialization;
    using Data;

    public class TextList : List<string>
    {
        public TextList()
        {
        }

        public TextList(IEnumerable<string> texts)
            : base(texts)
        {
        }
    }

    public class TextList<T> : TextList, IIndexed, IMutableIndex, IIndexedDependentData<T>, IEquatable<TextList<T>>
        where T : IIndexedData, new()
    {
        public TextList()
        {
            AssociatedItem = new();
        }

        public TextList(T associatedItem)
        {
            AssociatedItem = associatedItem;
            (this as IMutableIndex).Index = associatedItem.Index;
        }

        public TextList(T associatedItem, IEnumerable<string> texts)
            : base(texts)
        {
            AssociatedItem = associatedItem;
            (this as IMutableIndex).Index = associatedItem.Index;
        }

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;
        public T AssociatedItem { get; }

        public static IIndexedDependentData<T> Deserialize(IDataReader dataReader, uint index, T providedData, bool advanced)
        {
            var textList = (TextList<T>)Deserialize(dataReader, providedData, advanced);
            (textList as IMutableIndex).Index = index;
            return textList;
        }

        public static IDependentData<T> Deserialize(IDataReader dataReader, T providedData, bool advanced)
        {
            return new TextList<T>(providedData, Legacy.Serialization.TextReader.ReadTexts(dataReader));
        }

        public TextList<T> Copy(T newAssociatedItem)
        {
            return new(newAssociatedItem, this);
        }

        public TextList<T> Copy() => Copy(AssociatedItem);

        public object Clone() => Copy();

        public bool Equals(TextList<T>? other)
        {
            if (other is null || other.Count != Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (this[i] != other[i])
                    return false;
            }

            return true;
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            Legacy.Serialization.TextWriter.WriteTexts(dataWriter, this);
        }
    }
}
