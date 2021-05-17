using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SonicArranger
{
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class BinaryReaderExtensions
    {
        public static uint ReadBEUInt32(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        }

        public static int ReadBEInt32(this BinaryReader reader)
        {
            return unchecked((int)ReadBEUInt32(reader));
        }

        public static ushort ReadBEUInt16(this BinaryReader reader)
        {
            return (ushort)(((int)reader.ReadByte() << 8) | reader.ReadByte());
        }

        public static short ReadBEInt16(this BinaryReader reader)
        {
            return unchecked((short)ReadBEUInt16(reader));
        }
    }
}
