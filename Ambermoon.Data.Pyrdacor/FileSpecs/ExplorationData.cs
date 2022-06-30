using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs
{
    internal class ExplorationData : IFileSpec
    {
        public string Magic => "EXP";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<RLE0>();
        Automap? automap = null;

        public ExplorationData()
        {

        }

        public ExplorationData(Automap automap)
        {
            this.automap = automap;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            automap = Automap.Load(new AutomapReader(), dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (automap == null)
                throw new AmbermoonException(ExceptionScope.Application, "Automap data was null when trying to write it.");

            new AutomapWriter().WriteAutomap(automap, dataWriter);
        }
    }
}
