using Ambermoon.Data.Serialization.FileSystem;
using System.IO;

namespace Ambermoon.Data.FileSystems.OperatingSystem
{
    internal class FileStream : ReadOnlyFileStream, IFileStream
    {
        public FileStream(FileInfo fileInfo)
            : base(fileInfo)
        {

        }

        public IDisposableDataWriter GetWriter()
        {
            var stream = file.OpenWrite();
            return new StreamedDataWriter(stream, false);
        }
    }
}
