using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Util;

using Legacy;

internal static class ValueChecker
{
    public static void Check(int value, int min, int max, int? additionalAllowedValue = null,
        [CallerMemberName] string? name = null)
    {
        if (value == additionalAllowedValue)
            return;
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, $"{name} is limited to the range {min} to {max}.");
    }

    public static void Check(uint value, uint min, uint max, uint? additionalAllowedValue = null,
        [CallerMemberName] string? name = null)
    {
        if (value == additionalAllowedValue)
            return;
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, $"{name} is limited to the range {min} to {max}.");
    }

    public static void Check(string value, int maxLength, [CallerMemberName] string? name = null)
    {
        if (new AmbermoonEncoding().GetByteCount(value) > maxLength)
            throw new ArgumentOutOfRangeException(name, $"{name} length is limited to {maxLength} single-byte characters.");
    }
}
