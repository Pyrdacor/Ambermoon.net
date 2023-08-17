using Ambermoon.Data.Serialization.FileSystem;
using System.IO;

namespace Ambermoon.Data.FileSystems.OperatingSystem
{
    internal class ReadOnlyFileStream : IReadOnlyFileStream
    {
        protected readonly FileInfo file;

        public ReadOnlyFileStream(FileInfo fileInfo)
        {
            file = fileInfo;
        }

        public int Size => (int)file.Length;

        public IDisposableDataReader GetReader()
        {
            var stream = file.OpenRead();
            return new StreamedDataReader(stream, false);
        }
    }
}
