using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class GlyphMappingData : IFileSpec<GlyphMappingData>, IFileSpec
{
    public static string Magic => "GLM";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeltaCompression>();
    GlyphMapping? glyphMapping = null;

    public const ushort OutroSmallGlyphMappingIndex = 1;
    public const ushort OutroLargeGlyphMappingIndex = 2;
    public const ushort IntroSmallGlyphMappingIndex = 3;
    public const ushort IntroLargeGlyphMappingIndex = 4;

    public GlyphMapping GlyphMapping => glyphMapping!;

    public GlyphMappingData()
    {

    }

    public GlyphMappingData(GlyphMapping glyphMapping)
    {
        this.glyphMapping = glyphMapping;
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        glyphMapping = new GlyphMapping(dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (glyphMapping == null)
            throw new AmbermoonException(ExceptionScope.Application, "Glyph mapping data was null when trying to write it.");

        glyphMapping.Write(dataWriter);
    }
}
