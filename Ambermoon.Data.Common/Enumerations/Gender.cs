using System;

namespace Ambermoon.Data
{
    public enum Gender : byte
    {
        Male,
        Female
    }

    [Flags]
    public enum GenderFlag : byte
    {
        None = 0x00,
        Male = 0x01,
        Female = 0x02,
        Both = 0x03
    }

    public static class GenderExtensions
    {
        public static bool Contains(this GenderFlag genders, Gender gender) => genders.HasFlag((GenderFlag)(1 << (int)gender));
    }
}
