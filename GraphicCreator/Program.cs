using System.Drawing;
using System.Runtime.InteropServices;
using Ambermoon.Data.Legacy.Serialization;

var image = (Bitmap)Image.FromFile(args[0]);

using var output = File.Create(args[1]);
var writer = new DataWriter();

int chunkSize = image.Width * image.Height;
var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
var pixelData = new byte[chunkSize * 4];

Marshal.Copy(data.Scan0, pixelData, 0, pixelData.Length);

var colors = new HashSet<int>();
var imageColors = new int[chunkSize];

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

var palette = colors.OrderBy(c => c).Select((color, index) => new { color, index }).ToDictionary(c => c.color, c => c.index);

writer.Write((byte)palette.Count);

foreach (var color in palette.Keys)
{
    writer.Write((byte)((color >> 16) & 0xFF)); // R
    writer.Write((byte)((color >> 8) & 0xFF));  // G
    writer.Write((byte)(color & 0xFF));         // B
}

for (int i = 0; i < chunkSize; i++)
{
    int color = imageColors[i];
    int index = palette[color];
    writer.Write((byte)index);
}


output.Write(writer.ToArray(), 0, writer.Size);