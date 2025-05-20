using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class FontData : IFileSpec<FontData>, IFileSpec
{
    public static string Magic => "FNT";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
    Font? font = null;

    public const ushort IngameFontIndex = 1;
    public const ushort IngameDigitFontIndex = 2;
    public const ushort OutroSmallFontIndex = 3;
    public const ushort OutroLargeFontIndex = 4;
    public const ushort IntroSmallFontIndex = 5;
    public const ushort IntroLargeFontIndex = 6;

    public Font Font => font!;

    public FontData()
    {

    }

    public FontData(Font font)
    {
        this.font = font;
    }

    public void Read(IDataReader dataReader, uint _, GameData __)
    {
        font = new Font(dataReader);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (font == null)
            throw new AmbermoonException(ExceptionScope.Application, "Font data was null when trying to write it.");

        font.Write(dataWriter);
    }
}
