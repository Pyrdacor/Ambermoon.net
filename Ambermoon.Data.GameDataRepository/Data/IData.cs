using Ambermoon.Data.Serialization;
using System;
using System.Collections;
using System.ComponentModel;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public interface IData : ICloneable
    {
        /// <summary>
        /// Serializes the data to a data writer.
        /// </summary>
        /// <param name="dataWriter">Writer to store the serialized data.</param>
        /// <param name="advanced">Ambermoon Advanced data flag.</param>
        void Serialize(IDataWriter dataWriter, bool advanced);

        /// <summary>
        /// Deserializes data from a data reader.
        /// </summary>
        /// <param name="dataReader">Reader which provides the data.</param>
        /// <param name="advanced">Ambermoon Advanced data flag.</param>
        /// <returns></returns>
        static abstract IData Deserialize(IDataReader dataReader, bool advanced);
    }

    /// <summary>
    /// This serves as a general interface for indexed data
    /// but without the requirement for specific serialization.
    /// </summary>
    public interface IIndexed : ICloneable
    {
        /// <summary>
        /// Index of the data.
        /// </summary>
        uint Index { get; }
    }

    internal interface IMutableIndex
    {
        uint Index { get;  set; }
    }

    /// <summary>
    /// Data which is indexed.
    /// 
    /// The index is not part of the data but provided by an external
    /// source.
    /// 
    /// For example a list item has an index but this index is
    /// given by its position in the containing list.
    /// 
    /// Often the index is given by the index of the file which
    /// contains the data.
    /// </summary>
    public interface IIndexedData : IData, IIndexed
    {
        /// <summary>
        /// Deserializes data from a data reader.
        /// </summary>
        /// <param name="dataReader">Reader which provides the data.</param>
        /// <param name="index">Manually provider index of the data.</param>
        /// <param name="advanced">Ambermoon Advanced data flag.</param>
        /// <returns></returns>
        static abstract IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced);
    }

    /// <summary>
    /// Data which is dependent on some other data to be deserialized.
    /// </summary>
    public interface IDependentData<T> : ICloneable where T : IData
    {
        /// <summary>
        /// Serializes the data to a data writer.
        /// </summary>
        /// <param name="dataWriter">Writer to store the serialized data.</param>
        /// <param name="advanced">Ambermoon Advanced data flag.</param>
        void Serialize(IDataWriter dataWriter, bool advanced);

        /// <summary>
        /// Deserializes data from a data reader.
        /// </summary>
        /// <param name="dataReader">Reader which provides the data.</param>
        /// <param name="providedData">Provided data this data depends on.</param>
        /// <param name="advanced">Ambermoon Advanced data flag.</param>
        /// <returns></returns>
        static abstract IDependentData<T> Deserialize(IDataReader dataReader, T providedData, bool advanced);
    }

    /// <summary>
    /// Data which is dependent on some other data to be deserialized and also
    /// is indexed. See <see cref="IIndexedData"/> and <see cref="IDependentData{T}"/>.
    /// </summary>
    public interface IIndexedDependentData<T> : IDependentData<T>, IIndexed where T : IData
    {
        /// <summary>
        /// Deserializes data from a data reader.
        /// </summary>
        /// <param name="dataReader">Reader which provides the data.</param>
        /// <param name="index">Manually provider index of the data.</param>
        /// <param name="providedData">Provided data this data depends on.</param>
        /// <param name="advanced">Ambermoon Advanced data flag.</param>
        /// <returns></returns>
        static abstract IIndexedDependentData<T> Deserialize(IDataReader dataReader, uint index, T providedData, bool advanced);
    }
}
