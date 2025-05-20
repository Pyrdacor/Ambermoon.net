namespace Ambermoon.Data.Pyrdacor.Extensions;

internal static class CollectionExtensions
{
    public static T? GetByIndex<T>(this IDictionary<uint, T> dictionary, uint index)
        where T : class
    {
        if (index == 0 || !dictionary.TryGetValue(index, out var value))
            return null;

        return value;
    }
}
