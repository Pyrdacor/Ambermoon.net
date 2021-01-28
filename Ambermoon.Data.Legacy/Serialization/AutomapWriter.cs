using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class AutomapWriter : IAutomapWriter
    {
        public void WriteAutomap(Automap automap, IDataWriter dataWriter)
        {
            dataWriter.Write(automap.ExplorationBits);
        }
    }
}
