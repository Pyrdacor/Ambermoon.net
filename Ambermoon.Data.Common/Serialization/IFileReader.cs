using System.IO;

namespace Ambermoon.Data.Serialization
{
    public interface IFileReader
    {
        IFileContainer ReadFile(string name, Stream stream);
    }
}
