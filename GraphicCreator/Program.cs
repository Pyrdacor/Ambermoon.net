using System.Drawing;
using System.Runtime.InteropServices;
using Ambermoon.Data.Legacy.Serialization;

var image = (Bitmap)Image.FromFile(args[0]);

string outPath = args[1];

if (Directory.Exists(outPath))
    outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(args[0]));

using var output = File.Create(outPath);
var writer = new DataWriter();

int chunkSize = image.Width * image.Height;
var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
var pixelData = new byte[chunkSize * 4];

Marshal.Copy(data.Scan0, pixelData, 0, pixelData.Length);

var colors = new HashSet<int>();
var imageColors = new int[chunkSize];

var colorReplacements = new Dictionary<int, int>();

if (args.Length > 2)
{
    var replacements = args[2].Split(';');

    foreach (var replacement in replacements)
    {
        var parts = replacement.Split(':');

        if (parts.Length == 2 &&
            int.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out int oldColor) &&
            int.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out int newColor))
        {
            colorReplacements[oldColor] = newColor;
        }
    }
}

int transparentColor = -1;

if (args.Length > 3 &&
    !int.TryParse(args[3], System.Globalization.NumberStyles.HexNumber, null, out transparentColor))
{
    throw new Exception("Invalid transparent color specified. Use a hex value like 'FF00FF' for magenta.");
}

for (int i = 0; i < chunkSize; i++)
{
    var r = pixelData[i * 4 + 2];
    var g = pixelData[i * 4 + 1];
    var b = pixelData[i * 4 + 0];
    var a = pixelData[i * 4 + 3];

    int color = a == 0 ? 0 : (r << 16) | (g << 8) | b;

    imageColors[i] = color;
    colors.Add(color);
}

if (colors.Count > 256)
    throw new Exception("More than 256 colors in the image.");

writer.Write((ushort)image.Width);
writer.Write((ushort)image.Height);

var palette = colors.OrderBy(c => transparentColor == c ? -1 : c).Select((color, index) => new { color, index }).ToDictionary(c => c.color, c => c.index);

writer.Write((byte)palette.Count);

foreach (var color in palette.Keys)
{
    if (colorReplacements.TryGetValue(color, out var colorReplacement))
    {
        writer.Write((byte)((colorReplacement >> 16) & 0xFF)); // R
        writer.Write((byte)((colorReplacement >> 8) & 0xFF));  // G
        writer.Write((byte)(colorReplacement & 0xFF));         // B
    }
    else
    {
        writer.Write((byte)((color >> 16) & 0xFF)); // R
        writer.Write((byte)((color >> 8) & 0xFF));  // G
        writer.Write((byte)(color & 0xFF));         // B
    }
}

for (int i = 0; i < chunkSize; i++)
{
    int color = imageColors[i];
    int index = palette[color];
    writer.Write((byte)index);
}


output.Write(writer.ToArray(), 0, writer.Size);