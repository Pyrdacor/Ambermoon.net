using Ambermoon.Data.Pyrdacor.Extensions;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal class TextList
{
    [Flags]
    enum Flags
    {
        None = 0,
        HasLongTexts = 0x1
    }

    readonly List<string> texts;

    public TextList(IReadOnlyList<string> texts)
    {
        this.texts = new(texts);
    }

    public TextList(List<string> texts)
    {
        this.texts = texts;
    }

    public TextList(IDataReader dataReader)
    {
        var flags = dataReader.ReadEnum8<Flags>();
        int count = dataReader.ReadWord();
        Func<string> read = flags.HasFlag(Flags.HasLongTexts)
            ? dataReader.ReadLongString
            : dataReader.ReadString;

        texts = new List<string>(count);

        for (int i = 0; i < count; ++i)
            texts.Add(read());
    }

    public string? First() => GetText(0);

    public string? GetText(int index) => index >= texts.Count ? null : texts[index];

    public void Write(IDataWriter dataWriter)
    {
        var flags = Flags.None;

        if (texts.Any(text => text.Length > byte.MaxValue))
            flags |= Flags.HasLongTexts;

        dataWriter.WriteEnum8(flags);
        dataWriter.Write((ushort)texts.Count);

        Action<string> write = flags.HasFlag(Flags.HasLongTexts)
            ? dataWriter.WriteLongString
            : dataWriter.Write;

        foreach (var text in texts)
            write(text);
    }

    public List<string> ToList() => [.. texts];

    public Dictionary<int, string> ToDictionary(int startIndex = 0) => texts.Select((text, index) => new { Text = text, Index = index }).ToDictionary(t => startIndex + t.Index, t => t.Text);
}
