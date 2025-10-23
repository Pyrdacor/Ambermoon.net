namespace Ambermoon.Data.GameDataRepository.Util;

public class ValueRange<T>
{
    public ValueRange(T minimum, T maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public T Minimum { get; }
    public T Maximum { get; }

    public override string ToString() => $"[{Minimum}..{Maximum}]";
}
