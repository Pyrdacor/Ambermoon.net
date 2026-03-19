using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.Objects;

internal class GlyphMapping
{
    readonly Dictionary<char, int> mapping;
    
    public Dictionary<char, int> Mapping => new(mapping);

    public GlyphMapping(Dictionary<char, int> mapping)
    {
        this.mapping = mapping;
    }

    public GlyphMapping(IDataReader dataReader)
    {
        int charCount = dataReader.ReadByte();
        var chars = dataReader.ReadBytes(charCount);
        var glyphIndices = dataReader.ReadBytes(charCount);

        mapping = new Dictionary<char, int>(charCount);

        for (int i = 0; i < charCount; ++i)
        {
            mapping[(char)chars[i]] = glyphIndices[i];
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((byte)mapping.Count);

        // Better compressed with delta compression when ordered
        var mappingEntries = mapping.OrderBy(m => m.Key).ToArray();

        dataWriter.Write(mappingEntries.Select(e => (byte)e.Key).ToArray());
        dataWriter.Write(mappingEntries.Select(e => (byte)e.Value).ToArray());
    }
}
