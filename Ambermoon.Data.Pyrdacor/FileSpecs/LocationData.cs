using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs
{
    internal class LocationData : IFileSpec
    {
        public string Magic => "LOC";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<RLE0>();
        Place? place = null;

        public Place Place => place!;

        public LocationData()
        {

        }

        public LocationData(Place place)
        {
            this.place = place;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            place = new Place { Data = dataReader.ReadBytes(32) };
        }

        public void Write(IDataWriter dataWriter)
        {
            if (place == null || place.Data == null)
                throw new AmbermoonException(ExceptionScope.Application, "Place data was null when trying to write it.");

            if (place.Data.Length != 32)
                throw new AmbermoonException(ExceptionScope.Application, "Place data had wrong length when trying to write it.");

            dataWriter.Write(place.Data);
        }
    }
}
