using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor
{
    internal class FileHeader
    {
        public static bool CheckHeader(IDataReader dataReader, string header, bool skipIfMatch)
        {
            int position = dataReader.Position;

            if (dataReader.Size - position < header.Length)
                return false;

            bool match = dataReader.ReadString(header.Length) == header;

            if (!skipIfMatch || !match)
                dataReader.Position = position;

            return match;
        }
    }
}
