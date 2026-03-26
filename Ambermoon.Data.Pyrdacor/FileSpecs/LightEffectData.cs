using Ambermoon.Data.Legacy.ExecutableData;
using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

internal class LightEffectData : IFileSpec<LightEffectData>, IFileSpec
{
    public static string Magic => "LED";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    LightEffectDataProvider? lightEffectDataProvider;

    public LightEffectDataProvider LightEffectDataProvider => lightEffectDataProvider!;

    public LightEffectData()
    {

    }

    public LightEffectData(ExecutableData executableData)
    {
        this.lightEffectDataProvider = new(executableData.SkyGradients, executableData.DaytimePaletteReplacements);
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        void ReadPaletteColors(byte[] data)
        {
            int colorCount = data.Length / 4;
            int index = 0;

            for (int c = 0; c < colorCount; c++)
            {
                int r = dataReader.ReadByte();
                int gb = dataReader.ReadByte();
                int g = gb >> 4;
                int b = gb & 0xf;

                r |= r << 4;
                g |= g << 4;
                b |= b << 4;

                data[index++] = (byte)r;
                data[index++] = (byte)g;
                data[index++] = (byte)b;
                data[index++] = 0xff;
            }
        }

        // 3 sky gradients per world (night, twilight, day)
        // Each 72 pixels height so 72 colors (16 bit).
        var skyGradients = new Graphic[9];

        for (int i = 0; i < skyGradients.Length; i++)
        {
            var skyGradient = new Graphic
            {
                Width = 1,
                Height = 72,
                Data = new byte[72 * 4],
                IndexedGraphic = false
            };

            ReadPaletteColors(skyGradient.Data);

            skyGradients[i] = skyGradient;
        }

        // 2 daytime palette replacements per world (night and twilight, day is the map's palette)
        // Each 16 pixels height so 16 colors (16 bit). Those are the first 16 colors in the palette.
        var daytimePaletteReplacements = new Graphic[6];

        for (int i = 0; i < daytimePaletteReplacements.Length; i++)
        {
            var daytimePaletteReplacement = new Graphic
            {
                Width = 1,
                Height = 16,
                Data = new byte[16 * 4],
                IndexedGraphic = false
            };

            ReadPaletteColors(daytimePaletteReplacement.Data);

            daytimePaletteReplacements[i] = daytimePaletteReplacement;
        }

        lightEffectDataProvider = new(skyGradients, daytimePaletteReplacements);
    }

    public void Write(IDataWriter dataWriter)
    {
        if (lightEffectDataProvider?.SkyGradients == null || lightEffectDataProvider.SkyGradients.Length != 9 ||
            lightEffectDataProvider.DaytimePaletteReplacements == null || lightEffectDataProvider.DaytimePaletteReplacements.Length != 6)
            throw new InvalidDataException("Invalid light effect data.");

        void WriteColorData(byte[] pixelData)
        {
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                int r = pixelData[i] >> 4;
                int g = pixelData[i + 1] >> 4;
                int b = pixelData[i + 2] >> 4;
                int gb = b | (g << 4);

                dataWriter.Write((byte)r);
                dataWriter.Write((byte)gb);
            }
        }

        foreach (var skyGradient in lightEffectDataProvider.SkyGradients)
        {
            WriteColorData(skyGradient.Data);
        }

        foreach (var daytimePaletteReplacement in lightEffectDataProvider.DaytimePaletteReplacements)
        {
            WriteColorData(daytimePaletteReplacement.Data);
        }
    }
}
