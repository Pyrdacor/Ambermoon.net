using System.IO.Compression;
using Ambermoon.Data;
using Ambermoon.Mod.Assets;
using Ambermoon.Mod.Packaging;

namespace Ambermoon.Mod.Tests;

static partial class Tests
{
    static void AssetModTests()
    {
        Console.WriteLine("Asset mod tests");

        var directory = Path.Combine(Path.GetTempPath(), "ambermoon-asset-mod-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            void Write(string path, string content)
            {
                var file = Path.Combine(directory, path);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, content);
            }

            Write("mod.json", """{ "Id": "asset-test", "Name": "Asset test", "Version": "1.0", "Base": { "Game": "Original" } }""");
            Write("assets/monsters/2.json", """{ "Name": "HUGE SPIDER", "DefeatExperience": 99 }""");
            Write("assets/monsters/90.json", """{ "Template": 2, "Name": "COPY SPIDER" }""");
            Write("assets/monstergroups/150.json", """{ "Monsters": [[0,0,0,0,0,0],[0,0,0,0,0,0],[0,0,2,90,0,0]] }""");
            Write("assets/items/211.json", """{ "Name": "SUPER POTION", "Price": 1 }""");
            Write("assets/maps/258.json", """{ "Texts": ["CHANGED"], "MusicIndex": 3 }""");
            Write("assets/maps/700.json", """
            {
              "Type": "Map3D",
              "Width": 5,
              "Height": 5,
              "Labdata": 2,
              "Palette": 6,
              "Music": 18,
              "Texts": ["TEST MAP", "Hello!"],
              "Legend": {
                "#": { "Wall": 1 },
                ".": {},
                "T": { "Event": 1, "Automap": "Exit" },
                "C": { "Object": 25, "Event": 2, "Automap": "Chest" }
              },
              "Grid": [
                "#####",
                "#.T.#",
                "#...#",
                "#C..#",
                "#####"
              ],
              "Events": [
                { "Type": "MapText", "TextIndex": 1, "PopupTrigger": "Move, EyeCursor", "EventImageIndex": 255, "Next": 3 },
                { "Type": "Chest", "ChestIndex": 0, "TextIndex": 255 },
                { "Type": "Teleport", "MapIndex": 258, "X": 2, "Y": 15, "Direction": "Down", "Transition": "MapChange" }
              ],
              "Characters": [
                { "Type": "Monster", "Index": 150, "Graphic": 13, "Flags": "RandomMovement", "CollisionClass": 1, "CombatBackground": 7, "Positions": [{ "X": 4, "Y": 4 }] }
              ]
            }
            """);

            // A 16x16 portrait replacement (portraits are 32x34)
            var portraitPath = Path.Combine(directory, "assets/graphics/Portrait/0.png");
            Directory.CreateDirectory(Path.GetDirectoryName(portraitPath)!);
            File.WriteAllBytes(portraitPath, CreatePng(32, 34, (x, y) => ((byte)(x * 8), (byte)(y * 7), 128, 255)));

            using var package = ModPackage.Open(directory);
            var gameData = BaseGameLoader.Load("English");
            var originalPortrait = ((IGraphicProvider)gameData.GraphicInfoProvider).GetGraphics(GraphicType.Portrait)[0];
            var runtime = ModRuntime.Load(package, gameData, "1.20");

            Check(runtime.Mod == null, "Asset mods have no code");
            Check(gameData.CharacterManager.GetMonster(2)!.Name == "HUGE SPIDER", "Monster patch applied");
            Check(gameData.CharacterManager.GetMonster(2)!.DefeatExperience == 99, "Monster patch applied (experience)");
            Check(gameData.CharacterManager.GetMonster(90)?.Name == "COPY SPIDER", "New monster from template");
            var group = gameData.CharacterManager.GetMonsterGroup(150);
            Check(group?.Monsters[2, 2]?.Index == 2 && group.Monsters[3, 2]?.Index == 90, "Monster group created");
            Check(gameData.ItemManager.GetItem(211).Name == "SUPER POTION", "Item patch applied");
            Check(gameData.MapManager.GetMap(258)!.Texts[0] == "CHANGED" && gameData.MapManager.GetMap(258)!.MusicIndex == 3, "Map patch applied");

            var map = gameData.MapManager.GetMap(700);
            Check(map != null && map.Type == MapType.Map3D && map.Width == 5, "New map created");

            if (map != null)
            {
                Check(map.EventList.Count == 2 && map.EventAutomapTypes.Count == 2, "Event list built");
                Check(map.Blocks[2, 1].MapEventId == 1 && map.Blocks[1, 3].MapEventId == 2, "Map event ids set");
                Check(map.Events[0].Next == map.Events[2], "Event chain linked");
                Check(map.CharacterReferences[0]?.Index == 150, "Character added");
                Check(map.Clone() != null, "Map can be cloned");
            }

            var newPortrait = ((IGraphicProvider)gameData.GraphicInfoProvider).GetGraphics(GraphicType.Portrait)[0];
            Check(newPortrait != originalPortrait && newPortrait.Width == 32 && newPortrait.Height == 34, "Portrait replaced");

            // Packed mods
            var packedFile = directory + PackedModSource.Extension;
            ZipFile.CreateFromDirectory(directory, packedFile);

            using (var packed = ModPackage.Open(packedFile))
            {
                Check(packed.IsPacked && packed.Manifest.Id == "asset-test", "Packed mod can be opened");
                Check(packed.Source.ListFiles("assets/maps").Count() == 2, "Packed mod lists files");
            }

            File.Delete(packedFile);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Writes a simple RGBA PNG (used to test the decoder).
    /// </summary>
    static byte[] CreatePng(int width, int height, Func<int, int, (byte R, byte G, byte B, byte A)> pixel)
    {
        using var stream = new MemoryStream();
        stream.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        void WriteChunk(string type, byte[] data)
        {
            void WriteUInt(uint value) => stream.Write([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
            WriteUInt((uint)data.Length);
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes);
            stream.Write(data);
            WriteUInt(Crc32(typeBytes.Concat(data).ToArray()));
        }

        var header = new byte[13];
        header[3] = (byte)width;
        header[2] = (byte)(width >> 8);
        header[7] = (byte)height;
        header[6] = (byte)(height >> 8);
        header[8] = 8; // bit depth
        header[9] = 6; // RGBA
        WriteChunk("IHDR", header);

        using var raw = new MemoryStream();

        for (int y = 0; y < height; y++)
        {
            // Alternate between filter types to test the decoder.
            byte filter = (byte)(y % 2 == 0 ? 0 : 1);
            raw.WriteByte(filter);
            var row = new byte[width * 4];

            for (int x = 0; x < width; x++)
            {
                var (r, g, b, a) = pixel(x, y);
                row[x * 4] = r;
                row[x * 4 + 1] = g;
                row[x * 4 + 2] = b;
                row[x * 4 + 3] = a;
            }

            if (filter == 1)
            {
                for (int i = row.Length - 1; i >= 4; i--)
                    row[i] = (byte)(row[i] - row[i - 4]);
            }

            raw.Write(row);
        }

        using var compressed = new MemoryStream();

        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, true))
            zlib.Write(raw.ToArray());

        WriteChunk("IDAT", compressed.ToArray());
        WriteChunk("IEND", []);

        var png = stream.ToArray();
        var decoded = PngDecoder.Decode(png);
        Check(decoded.Width == width && decoded.GetPixel(3, 1) == pixel(3, 1), "PNG decoder round trip");

        return png;
    }

    static uint Crc32(byte[] data)
    {
        uint crc = 0xffffffff;

        foreach (var b in data)
        {
            crc ^= b;

            for (int k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        }

        return crc ^ 0xffffffff;
    }
}
