using System.IO;

namespace Ambermoon.Data
{
    public interface IFileReader
    {
        IFileContainer ReadFile(string name, Stream stream);
    }
}
