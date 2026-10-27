using System.IO.Compression;

namespace Ambermoon.Mod.Assets;

/// <summary>
/// Minimal PNG decoder (no external dependencies) which supports
/// non-interlaced 8-bit grayscale, RGB, palette, gray+alpha and RGBA images.
/// The result is always RGBA (4 bytes per pixel).
/// </summary>
public static class PngDecoder
{
    static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public record Image(int Width, int Height, byte[] Rgba)
    {
        public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
        {
            int index = (y * Width + x) * 4;
            return (Rgba[index], Rgba[index + 1], Rgba[index + 2], Rgba[index + 3]);
        }
    }

    public static Image Decode(byte[] data)
    {
        if (data.Length < Signature.Length || !data.AsSpan(0, Signature.Length).SequenceEqual(Signature))
            throw new InvalidDataException("No PNG file.");

        int offset = Signature.Length;
        int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
        byte[]? palette = null;
        byte[]? paletteAlpha = null;
        using var idat = new MemoryStream();

        static uint ReadUInt(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

        while (offset + 8 <= data.Length)
        {
            int length = (int)ReadUInt(data, offset);
            string type = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);
            int chunkData = offset + 8;

            if (length < 0 || chunkData + length > data.Length)
                throw new InvalidDataException("Corrupt PNG chunk.");

            switch (type)
            {
                case "IHDR":
                    width = (int)ReadUInt(data, chunkData);
                    height = (int)ReadUInt(data, chunkData + 4);
                    bitDepth = data[chunkData + 8];
                    colorType = data[chunkData + 9];
                    interlace = data[chunkData + 12];
                    break;
                case "PLTE":
                    palette = data.AsSpan(chunkData, length).ToArray();
                    break;
                case "tRNS":
                    paletteAlpha = data.AsSpan(chunkData, length).ToArray();
                    break;
                case "IDAT":
                    idat.Write(data, chunkData, length);
                    break;
                case "IEND":
                    offset = data.Length;
                    continue;
            }

            offset = chunkData + length + 4; // skip CRC
        }

        if (width <= 0 || height <= 0)
            throw new InvalidDataException("PNG has no valid header.");
        if (bitDepth != 8)
            throw new NotSupportedException("Only PNG files with 8 bits per channel are supported.");
        if (interlace != 0)
            throw new NotSupportedException("Interlaced PNG files are not supported.");

        int channels = colorType switch
        {
            0 => 1, // gray
            2 => 3, // rgb
            3 => 1, // palette
            4 => 2, // gray + alpha
            6 => 4, // rgba
            _ => throw new NotSupportedException($"Unsupported PNG color type {colorType}.")
        };

        if (colorType == 3 && palette == null)
            throw new InvalidDataException("PNG palette is missing.");

        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        int stride = width * channels;
        var raw = new byte[height * (stride + 1)];
        zlib.ReadExactly(raw);

        var current = new byte[stride];
        var previous = new byte[stride];
        var rgba = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (stride + 1);
            byte filter = raw[rowStart];

            for (int i = 0; i < stride; i++)
            {
                int value = raw[rowStart + 1 + i];
                int left = i >= channels ? current[i - channels] : 0;
                int up = previous[i];
                int upLeft = i >= channels ? previous[i - channels] : 0;

                value += filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    4 => Paeth(left, up, upLeft),
                    _ => throw new InvalidDataException($"Invalid PNG filter {filter}.")
                };

                current[i] = (byte)value;
            }

            for (int x = 0; x < width; x++)
            {
                int target = (y * width + x) * 4;
                int source = x * channels;

                switch (colorType)
                {
                    case 0:
                        rgba[target] = rgba[target + 1] = rgba[target + 2] = current[source];
                        rgba[target + 3] = 255;
                        break;
                    case 2:
                        rgba[target] = current[source];
                        rgba[target + 1] = current[source + 1];
                        rgba[target + 2] = current[source + 2];
                        rgba[target + 3] = 255;
                        break;
                    case 3:
                        int paletteIndex = current[source];
                        rgba[target] = palette![paletteIndex * 3];
                        rgba[target + 1] = palette[paletteIndex * 3 + 1];
                        rgba[target + 2] = palette[paletteIndex * 3 + 2];
                        rgba[target + 3] = paletteAlpha != null && paletteIndex < paletteAlpha.Length ? paletteAlpha[paletteIndex] : (byte)255;
                        break;
                    case 4:
                        rgba[target] = rgba[target + 1] = rgba[target + 2] = current[source];
                        rgba[target + 3] = current[source + 1];
                        break;
                    case 6:
                        Buffer.BlockCopy(current, source, rgba, target, 4);
                        break;
                }
            }

            (previous, current) = (current, previous);
        }

        return new Image(width, height, rgba);
    }

    static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);

        if (pa <= pb && pa <= pc)
            return a;

        return pb <= pc ? b : c;
    }
}
