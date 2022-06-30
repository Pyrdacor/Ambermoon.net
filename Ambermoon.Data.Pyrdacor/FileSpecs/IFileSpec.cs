using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs
{
    public interface IFileSpec
    {
        string Magic { get; }
        byte SupportedVersion { get; }
        ushort PreferredCompression { get; }
        void Read(IDataReader dataReader, uint index, GameData gameData);
        void Write(IDataWriter dataWriter);

        public static string GetMagic<T>() where T : IFileSpec, new()
        {
            return new T().Magic;
        }

        public static byte GetSupportedVersion<T>() where T : IFileSpec, new()
        {
            return new T().SupportedVersion;
        }

        public static ICompression GetPreferredCompression<T>() where T : IFileSpec, new()
        {
            return PADF.Compressions.TryGetValue(new T().PreferredCompression, out var compression) ? compression : ICompression.NoCompression;
        }
    }
}
