namespace Ambermoon.Data.GameDataRepository;

using Serialization;
using Data;

public class TextList : List<string>, IMutableIndex, IIndexedData, IEquatable<TextList>
{

    #region Properties

    uint IMutableIndex.Index
    {
        get;
        set;
    }

    public uint Index => (this as IMutableIndex).Index;

    #endregion


    #region Constructors

    public TextList()
    {

    }

    public TextList(uint index)
    {
        (this as IMutableIndex).Index = index;
    }

    public TextList(uint index, IEnumerable<string> texts)
        : base(texts)
    {
        (this as IMutableIndex).Index = index;
    }

    #endregion


    #region Serialization

    public void Serialize(IDataWriter dataWriter, int majorVersion, bool advanced)
    {
        Legacy.Serialization.TextWriter.WriteTexts(dataWriter, this);
    }

    public static IIndexedData Deserialize(IDataReader dataReader, uint index, int majorVersion, bool advanced)
    {
        var textList = (TextList)Deserialize(dataReader, majorVersion, advanced);
        (textList as IMutableIndex).Index = index;
        return textList;
    }

    public static IData Deserialize(IDataReader dataReader, int majorVersion, bool advanced)
    {
        return new TextList(0, Legacy.Serialization.TextReader.ReadTexts(dataReader));
    }

    #endregion


    #region Equality

    public bool Equals(TextList? other)
    {
        if (other is null || other.Count != Count || other.Index != Index)
            return false;

        for (int i = 0; i < Count; i++)
        {
            if (this[i] != other[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TextList)obj);
    }

    public override int GetHashCode() => (int)Index;

    public static bool operator ==(TextList? left, TextList? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TextList? left, TextList? right)
    {
        return !Equals(left, right);
    }

    #endregion


    #region Cloning

    public TextList Copy()
    {
        return new(Index, this);
    }

    public virtual object Clone() => Copy();

    #endregion

}

public class TextList<T> : TextList, IIndexedDependentData<T>, IEquatable<TextList<T>>
    where T : IIndexedData, new()
{

    #region Properties

    public T AssociatedItem { get; }

    #endregion


    #region Constructors

    public TextList()
    {
        AssociatedItem = new();
    }

    public TextList(uint index)
        : base(index)
    {
        AssociatedItem = new();
    }

    public TextList(uint index, T associatedItem)
        : base(index)
    {
        AssociatedItem = associatedItem;
        (this as IMutableIndex).Index = associatedItem.Index;
    }

    public TextList(uint index, T associatedItem, IEnumerable<string> texts)
        : base(index, texts)
    {
        AssociatedItem = associatedItem;
        (this as IMutableIndex).Index = associatedItem.Index;
    }

    #endregion


    #region Serialization

    public static IIndexedDependentData<T> Deserialize(IDataReader dataReader, uint index, T providedData, int majorVersion, bool advanced)
    {
        var textList = (TextList<T>)Deserialize(dataReader, providedData, majorVersion, advanced);
        (textList as IMutableIndex).Index = index;
        return textList;
    }

    public static IDependentData<T> Deserialize(IDataReader dataReader, T providedData, int majorVersion, bool advanced)
    {
        return new TextList<T>(0, providedData, Legacy.Serialization.TextReader.ReadTexts(dataReader));
    }

    #endregion


    #region Equality

    public bool Equals(TextList<T>? other)
    {
        if (other is null || other.Count != Count || other.Index != Index)
            return false;

        for (int i = 0; i < Count; i++)
        {
            if (this[i] != other[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TextList<T>)obj);
    }

    public override int GetHashCode() => (int)Index;

    public static bool operator ==(TextList<T>? left, TextList<T>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TextList<T>? left, TextList<T>? right)
    {
        return !Equals(left, right);
    }

    #endregion


    #region Cloning

    public TextList<T> Copy(T newAssociatedItem)
    {
        return new(Index, newAssociatedItem, this);
    }

    public new TextList<T> Copy() => Copy(AssociatedItem);

    public override object Clone() => Copy();

    #endregion

}
