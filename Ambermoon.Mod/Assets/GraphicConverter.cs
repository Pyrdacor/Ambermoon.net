using Ambermoon.Data;

namespace Ambermoon.Mod.Assets;

/// <summary>
/// Converts true color images to the indexed graphics used by the game.
/// </summary>
public static class GraphicConverter
{
    /// <summary>
    /// Maps each pixel to the nearest color of the palette.
    /// Pixels with an alpha below 128 become color index 0 (transparent).
    /// </summary>
    /// <param name="palette">A palette graphic (32x1 RGBA pixels).</param>
    /// <param name="firstColorIndex">First usable color index (index 0 is reserved for transparency).</param>
    public static Graphic ToIndexedGraphic(PngDecoder.Image image, Graphic palette, int firstColorIndex = 1)
    {
        int colorCount = palette.Data.Length / 4;
        var data = new byte[image.Width * image.Height];
        var cache = new Dictionary<int, byte>();

        for (int i = 0; i < data.Length; i++)
        {
            int offset = i * 4;

            if (image.Rgba[offset + 3] < 128)
                continue;

            int r = image.Rgba[offset];
            int g = image.Rgba[offset + 1];
            int b = image.Rgba[offset + 2];
            int key = (r << 16) | (g << 8) | b;

            if (!cache.TryGetValue(key, out var colorIndex))
            {
                int bestDistance = int.MaxValue;

                for (int c = firstColorIndex; c < colorCount; c++)
                {
                    int dr = r - palette.Data[c * 4];
                    int dg = g - palette.Data[c * 4 + 1];
                    int db = b - palette.Data[c * 4 + 2];
                    int distance = dr * dr + dg * dg + db * db;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        colorIndex = (byte)c;
                    }
                }

                cache.Add(key, colorIndex);
            }

            data[i] = colorIndex;
        }

        return new Graphic
        {
            Width = image.Width,
            Height = image.Height,
            Data = data,
            IndexedGraphic = true
        };
    }
}
