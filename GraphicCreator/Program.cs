using System.Drawing;
using System.Runtime.InteropServices;
using Ambermoon.Data.Legacy.Serialization;

using Ambermoon.Data.Legacy;

var gameData = new GameData();

gameData.Load(@"D:\Projects\Ambermoon Advanced\german\Amberfiles");

var map1 = gameData.MapManager.GetMap(1);

for (uint i = 2; i <= 256; i++)
{
    var map = gameData.MapManager.GetMap(i);

    for (int y = 0; y < 50; y++)
    {
        for (int x = 0; x < 50; x++)
        {
            var tile1 = map1.Tiles[x, y];
            var tile = map.Tiles[x, y];
            
            if (tile1.BackTileIndex != tile.BackTileIndex ||
                tile1.FrontTileIndex != tile.FrontTileIndex)
                goto next;
        }
    }

    Console.WriteLine($"Map {i} is identical to map 1 (X={(i % 16)*50}, Y={(i / 16) * 50}).");

next:
    continue;
}

return;

var allInfos = new Dictionary<uint, List<GraphicInfo>>();
gameData.MapManager.Labdata.ToList().ForEach(labData =>
{
    labData.ObjectInfos.ToList().ForEach((info) =>
    {
        if (!allInfos.ContainsKey(info.TextureIndex))
            allInfos[info.TextureIndex] = [new(info.TextureWidth, info.TextureHeight, info.NumAnimationFrames)];
        else
            allInfos[info.TextureIndex].Add(new(info.TextureWidth, info.TextureHeight, info.NumAnimationFrames));
    });
});

var sortedInfos = allInfos.OrderBy(z => z.Key).ToList();

foreach (var item in sortedInfos)
{
    Console.WriteLine(item.Key);

    foreach (var info in item.Value.Distinct())
        Console.WriteLine($"\t{info.Width}, {info.Height} (Frames: {info.Frames})");
}

record GraphicInfo(uint Width, uint Height, uint Frames);

//var image = (Bitmap)Image.FromFile(args[0]);

//string outPath = args[1];

//if (Directory.Exists(outPath))
//    outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(args[0]));

//using var output = File.Create(outPath);
//var writer = new DataWriter();

//int chunkSize = image.Width * image.Height;
//var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
//var pixelData = new byte[chunkSize * 4];

//Marshal.Copy(data.Scan0, pixelData, 0, pixelData.Length);

//var colors = new HashSet<int>();
//var imageColors = new int[chunkSize];

//var colorReplacements = new Dictionary<int, int>();

//if (args.Length > 2)
//{
//    var replacements = args[2].Split(';');

//    foreach (var replacement in replacements)
//    {
//        var parts = replacement.Split(':');

//        if (parts.Length == 2 &&
//            int.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out int oldColor) &&
//            int.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out int newColor))
//        {
//            colorReplacements[oldColor] = newColor;
//        }
//    }
//}

//int transparentColor = -1;

//if (args.Length > 3 &&
//    !int.TryParse(args[3], System.Globalization.NumberStyles.HexNumber, null, out transparentColor))
//{
//    throw new Exception("Invalid transparent color specified. Use a hex value like 'FF00FF' for magenta.");
//}

//for (int i = 0; i < chunkSize; i++)
//{
//    var r = pixelData[i * 4 + 2];
//    var g = pixelData[i * 4 + 1];
//    var b = pixelData[i * 4 + 0];
//    var a = pixelData[i * 4 + 3];

//    int color = a == 0 ? 0 : (r << 16) | (g << 8) | b;

//    imageColors[i] = color;
//    colors.Add(color);
//}

//if (colors.Count > 256)
//    throw new Exception("More than 256 colors in the image.");

//writer.Write((ushort)image.Width);
//writer.Write((ushort)image.Height);

//var palette = colors.OrderBy(c => transparentColor == c ? -1 : c).Select((color, index) => new { color, index }).ToDictionary(c => c.color, c => c.index);

//writer.Write((byte)palette.Count);

//foreach (var color in palette.Keys)
//{
//    if (colorReplacements.TryGetValue(color, out var colorReplacement))
//    {
//        writer.Write((byte)((colorReplacement >> 16) & 0xFF)); // R
//        writer.Write((byte)((colorReplacement >> 8) & 0xFF));  // G
//        writer.Write((byte)(colorReplacement & 0xFF));         // B
//    }
//    else
//    {
//        writer.Write((byte)((color >> 16) & 0xFF)); // R
//        writer.Write((byte)((color >> 8) & 0xFF));  // G
//        writer.Write((byte)(color & 0xFF));         // B
//    }
//}

//for (int i = 0; i < chunkSize; i++)
//{
//    int color = imageColors[i];
//    int index = palette[color];
//    writer.Write((byte)index);
//}

///*if (colors.Count < 128)
//{
//    writer = TryRLE(writer);
//}*/

//output.Write(writer.ToArray(), 0, writer.Size);

//DataWriter TryRLE(DataWriter writer)
//{
//    var writerData = writer.ToArray();
//    int headerSize = 5 + writerData[4] * 3;
//    var header = writerData[..headerSize]; // width, height, color count
//    var data = writerData[headerSize..]; // skip width, height and color count
//    const byte rleMask = 0x80;
//    var compressedData = new List<byte>();
//    byte currentSymbol = rleMask;
//    int count = 0;

//    void WriteRLE()
//    {
//        if (count > 4210815)
//            throw new IndexOutOfRangeException("RLE count was too large");

//        if (count > 0)
//        {
//            if (count < 3) // not worth it
//            {
//                for (int c = 0; c < count; c++)
//                    compressedData.Add(currentSymbol);
//            }
//            else
//            {
//                // count--
//                // 0 - 127: 0x00 - 0x7F
//                // 128 - 16511: 0x8000 - 0xFF7F
//                // 16512 - 4210815: 0x808000 - 0xFFFFFF

//                compressedData.Add((byte)(currentSymbol | rleMask));

//                count--; // 0-based

//                if (count < 128)                    
//                    compressedData.Add((byte)count); // 0x00 - 0x7F
//                else
//                {
//                    count -= 128;

//                    if (count < 16384) // 14 bits
//                    {
//                        compressedData.Add((byte)(0x80 | (count >> 7)));
//                        compressedData.Add((byte)(count & 0x7f));
//                    }
//                    else
//                    {
//                        count -= 16384;

//                        compressedData.Add((byte)(0x80 | (count >> 15)));
//                        compressedData.Add((byte)(0x80 | ((count >> 8) & 0x7f)));
//                        compressedData.Add((byte)(count & 0xff));
//                    }
//                }
//            }
//        }
//    }

//    for (int i = 0; i < data.Length; i++)
//    {
//        if (data[i] == currentSymbol)
//        {
//            count++;
//        }
//        else
//        {
//            WriteRLE();            

//            currentSymbol = data[i];
//            count = 1;
//        }
//    }

//    WriteRLE();

//    if (compressedData.Count > data.Length)
//        return writer; // RLE compression did not help, return original data

//    return new DataWriter([..header, ..compressedData.ToArray()]);
//}