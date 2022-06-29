using System.IO;

namespace Ambermoon.Data.Serialization
{
    public interface IFileReader
    {
        IFileContainer ReadRawFile(string name, Stream stream);
    }
}
