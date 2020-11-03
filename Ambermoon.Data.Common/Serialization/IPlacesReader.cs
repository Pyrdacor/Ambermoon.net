namespace Ambermoon.Data.Serialization
{
    public interface IPlacesReader
    {
        void ReadPlaces(Places places, IDataReader dataReader);
    }
}
