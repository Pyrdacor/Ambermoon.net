using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class AutomapReader : IAutomapReader
    {
        public void ReadAutomap(Automap automap, IDataReader dataReader)
        {
            automap.ExplorationBits = dataReader.ReadToEnd();
        }
    }
}
