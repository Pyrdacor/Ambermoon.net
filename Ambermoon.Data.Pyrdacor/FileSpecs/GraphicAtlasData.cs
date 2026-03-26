using Ambermoon.Data.Pyrdacor.Compressions;
using Ambermoon.Data.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Pyrdacor.FileSpecs;

/// <summary>
/// The graphic atlas stores all graphics for a specific render layer.
/// For example all event graphics or all item graphics.
/// It can store the graphics as a single texture atlas or as individual tiles.
/// The format is optimized for the original Amiga graphics but can also be used for RGBA graphics with some limitations.
/// The texture atlas can optionally use a palette from the game data and apply a color index offset to it.
/// If the palette is not fixed (e.g. for 3D walls), a value of 255 should be used for the palette index.
/// This allows to use the same texture atlas with different palettes.
/// </summary>
public class GraphicAtlasData : IFileSpec<GraphicAtlasData>, IFileSpec
{
    public const byte MultiplePalettes = 255;

    private static class TexturePacker
    {
        private class EmptyArea(Rect area)
        {
            public class Comparer : IComparer<EmptyArea>
            {
                int IComparer<EmptyArea>.Compare(EmptyArea? x, EmptyArea? y)
                {
                    if (x is null)
                        return -1;
                    if (y is null)
                        return 1;
                    return (x.area.Width * x.area.Height).CompareTo(y.area.Width * y.area.Height);
                }
            }

            public enum FillResult
            {
                NotFit,
                PartialFit,
                FullFit
            }
            private readonly Rect area = area;
            public static readonly Comparer AreaComparer = new();
            public List<EmptyArea> FreeChildAreas { get; } = [];

            private void AddFreeChildArea(Rect area)
            {
                FreeChildAreas.AddSorted(new EmptyArea(area), AreaComparer);
            }

            public FillResult TryAddArea(Size areaSize, out Position? position)
            {
                position = null;

                if (areaSize.Width > area.Width || areaSize.Height > area.Height)
                    return FillResult.NotFit;

                if (FreeChildAreas.Count == 0)
                {
                    position = new Position(area.Position);

                    if (areaSize.Width == area.Width)
                    {
                        if (areaSize.Height == area.Height)
                            return FillResult.FullFit;

                        AddFreeChildArea(new Rect(area.X, area.Y + areaSize.Height, area.Width, area.Height - areaSize.Height));
                        return FillResult.PartialFit;
                    }
                    else if (areaSize.Height == area.Height)
                    {
                        AddFreeChildArea(new Rect(area.X + areaSize.Width, area.Y, area.Width - areaSize.Width, area.Height));
                        return FillResult.PartialFit;
                    }
                    else
                    {
                        AddFreeChildArea(new Rect(area.X + areaSize.Width, area.Y, area.Width - areaSize.Width, areaSize.Height));
                        AddFreeChildArea(new Rect(area.X, area.Y + areaSize.Height, area.Width, area.Height - areaSize.Height));
                        return FillResult.PartialFit;
                    }
                }
                else
                {
                    // They are sorted small to large which is good.
                    // We try to fit the area into the smallest areas first.
                    foreach (var freeChildArea in FreeChildAreas)
                    {
                        var result = freeChildArea.TryAddArea(areaSize, out position);

                        if (result == FillResult.FullFit)
                        {
                            FreeChildAreas.Remove(freeChildArea);
                            return FillResult.PartialFit;
                        }
                        else if (result == FillResult.PartialFit)
                            return FillResult.PartialFit;
                    }

                    return FillResult.NotFit;
                }
            }
        }

        public static KeyValuePair<Graphic, SortedList<int, Rect>> PackTextureAtlas(IDictionary<int, Graphic> graphics, bool indexed)
        {
            if (graphics.Count == 0)
                return KeyValuePair.Create(new Graphic(0, 0, 0) { IndexedGraphic = indexed }, new SortedList<int, Rect>());
            else if (graphics.Count == 1)
                return KeyValuePair.Create(graphics[0], new SortedList<int, Rect>() { { 0, new Rect(0, 0, graphics[0].Width, graphics[0].Height) } });

            // Order by max size graphics and prefer wider ones if equal
            var sortedGraphics = new List<KeyValuePair<int, Graphic>>(graphics.ToList());

            sortedGraphics.Sort((a, b) =>
            {
                int result = (b.Value.Width * b.Value.Height).CompareTo(a.Value.Width * a.Value.Height);

                return result != 0 ? result : b.Value.Width.CompareTo(a.Value.Width);
            });

            int totalPixels = sortedGraphics.Sum(g => g.Value.Width * g.Value.Height);
            int maxWidth = sortedGraphics.Max(g => g.Value.Width);
            int height = sortedGraphics[0].Value.Height;

            while (maxWidth < 320 && totalPixels < maxWidth * height * 2)
                maxWidth *= 2;

            var textureAreas = new Dictionary<int, Rect>();
            var emptyAreas = new List<EmptyArea>();
            var graphic = new Graphic(maxWidth, height, 0);

            graphic.AddOverlay(0, 0, sortedGraphics[0].Value);

            int index = sortedGraphics[0].Key;

            textureAreas.Add(index, new Rect(0, 0, graphic.Width, graphic.Height));

            void AddEmptyArea(Rect area)
            {
                emptyAreas.AddSorted(new EmptyArea(area), EmptyArea.AreaComparer);
            }

            void IncreaseHeight(int amount)
            {
                var newGraphic = new Graphic
                {
                    IndexedGraphic = graphic.IndexedGraphic,
                    Width = graphic.Width,
                    Height = graphic.Height + amount,
                    Data = new byte[graphic.Width * (graphic.Height + amount) * (graphic.IndexedGraphic ? 1 : 4)]
                };
                Array.Copy(graphic.Data, newGraphic.Data, graphic.Data.Length);
                AddEmptyArea(new Rect(0, graphic.Height, graphic.Width, amount));
                graphic = newGraphic;                    
            }

            if (sortedGraphics[0].Value.Width < maxWidth)
                AddEmptyArea(new Rect(sortedGraphics[0].Value.Width, 0, maxWidth - sortedGraphics[0].Value.Width, height));

            for (int i = 1; i < sortedGraphics.Count; ++i)
            {
                var nextGraphic = sortedGraphics[i];

                index = nextGraphic.Key;

                if (emptyAreas.Count == 0)
                {
                    int y = graphic.Height;
                    IncreaseHeight(nextGraphic.Value.Height);
                    graphic.AddOverlay(0u, (uint)y, nextGraphic.Value);
                    textureAreas.Add(index, new Rect(0, y, nextGraphic.Value.Width, nextGraphic.Value.Height));
                    continue;
                }

                var areaSize = new Size(nextGraphic.Value.Width, nextGraphic.Value.Height);
                bool success = false;

                foreach (var emptyArea in emptyAreas)
                {
                    var result = emptyArea.TryAddArea(areaSize, out var position);

                    if (result == EmptyArea.FillResult.FullFit)
                    {
                        success = true;
                        graphic.AddOverlay((uint)position!.X, (uint)position.Y, nextGraphic.Value);
                        textureAreas.Add(index, new Rect(position.X, position.Y, nextGraphic.Value.Width, nextGraphic.Value.Height));
                        emptyAreas.Remove(emptyArea);
                        break;
                    }
                    else if (result == EmptyArea.FillResult.PartialFit)
                    {
                        success = true;
                        graphic.AddOverlay((uint)position!.X, (uint)position.Y, nextGraphic.Value);
                        textureAreas.Add(index, new Rect(position.X, position.Y, nextGraphic.Value.Width, nextGraphic.Value.Height));
                        break;
                    }
                }

                if (!success)
                {
                    int y = graphic.Height;
                    IncreaseHeight(nextGraphic.Value.Height);
                    graphic.AddOverlay(0u, (uint)y, nextGraphic.Value);
                    textureAreas.Add(index, new Rect(0, y, nextGraphic.Value.Width, nextGraphic.Value.Height));
                }
            }

            return KeyValuePair.Create(graphic, new SortedList<int, Rect>(textureAreas));
        }
    }

    private readonly SortedList<int, Rect> textureAreas = [];
    private int? paletteIndex;
    private byte flags;

    public static string Magic => "GFX";
    public static byte SupportedVersion => 0;
    public static ushort PreferredCompression => ICompression.GetIdentifier<DeflateCompression>();
    public IReadOnlyList<Rect> TextureAreas => textureAreas.Values.AsReadOnly();
    public GraphicAtlas? Atlas { get; private set; }

    public GraphicAtlasData()
    {

    }

    private GraphicAtlasData(int? paletteIndex, byte flags, Graphic texture, SortedList<int, Rect> textureAreas)
    {
        this.paletteIndex = paletteIndex >= 0 ? paletteIndex : null;
        this.flags = flags;
        Atlas = new(texture, textureAreas.ToDictionary(area => (uint)area.Key, area => new Position(area.Value.Position)));
        this.textureAreas = textureAreas;
    }

    public static GraphicAtlasData FromIndexedGraphics(int? paletteIndex, IDictionary<int, Graphic> graphics, bool alpha, int colorIndexOffset = 0)
    {
        if (graphics.Count != 0)
        {
            var first = graphics.First().Value;

            if (paletteIndex >= 0 && !first.IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Data, "FromIndexedGraphics was called with a palette index but the given graphics are not indexed. Use indexed graphics instead.");
            else if (paletteIndex < 0 && first.IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Data, "FromIndexedGraphics was called without a palette index but the given graphics are indexed. Use RGBA graphics instead.");
        }

        byte flags = 0x00; // no tiles

        if (alpha)
            flags |= 0x40;
        if (paletteIndex != null)
            flags |= 0x20;

        flags |= (byte)(colorIndexOffset & 0x1f);

        Graphic texture;
        SortedList<int, Rect> textureAreas;

        if (graphics.Count == 0)
        {
            texture = new Graphic(0, 0, 0) { IndexedGraphic = paletteIndex != null };
            textureAreas = [];
        }
        else
        {
            var textureAtlasInfo = TexturePacker.PackTextureAtlas(graphics, graphics.First().Value.IndexedGraphic);
            texture = textureAtlasInfo.Key;
            textureAreas = textureAtlasInfo.Value;
        }

        return new GraphicAtlasData(paletteIndex, flags, texture, textureAreas);
    }


    public static GraphicAtlasData FromGraphics(int paletteIndex, IReadOnlyList<Graphic> graphics, bool alpha, int colorIndexOffset = 0)
    {
        if (graphics.Count != 0)
        {
            if (paletteIndex >= 0 && !graphics[0].IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Data, "FromGraphics was called with a palette index but the given graphics are not indexed. Use indexed graphics instead.");
            else if (paletteIndex < 0 && graphics[0].IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Data, "FromGraphics was called without a palette index but the given graphics are indexed. Use RGBA graphics instead.");
        }

        byte flags = 0x00; // no tiles

        if (alpha)
            flags |= 0x40;
        if (paletteIndex >= 0)
            flags |= 0x20;

        flags |= (byte)(colorIndexOffset & 0x1f);

        Graphic texture;
        SortedList<int, Rect> textureAreas;

        if (graphics.Count == 0)
        {
            texture = new Graphic(0, 0, 0) { IndexedGraphic = paletteIndex >= 0 };
            textureAreas = [];
        }
        else
        {
            var textureAtlasInfo = TexturePacker.PackTextureAtlas(graphics
                .Select((g, i) => new { Graphic = g, Index = i })
                .ToDictionary(graphic => graphic.Index, graphic => graphic.Graphic),
                graphics[0].IndexedGraphic);
            texture = textureAtlasInfo.Key;
            textureAreas = textureAtlasInfo.Value;
        }

        return new GraphicAtlasData(paletteIndex, flags, texture, textureAreas);
    }

    public static GraphicAtlasData FromTiles(int paletteIndex, IReadOnlyList<Graphic> tiles, bool alpha, int colorIndexOffset = 0)
    {
        if (tiles.Select(t => new { t.Width, t.Height, t.IndexedGraphic, t.Data.Length }).Distinct().Count() > 1)
            throw new AmbermoonException(ExceptionScope.Data, "Tile graphics are expected to have all the same size and color depth.");

        if (tiles.Count != 0)
        {
            if (paletteIndex >= 0 && !tiles[0].IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Data, "FromTiles was called with a palette index but the given graphics are not indexed. Use indexed graphics instead.");
            else if (paletteIndex < 0 && tiles[0].IndexedGraphic)
                throw new AmbermoonException(ExceptionScope.Data, "FromTiles was called without a palette index but the given graphics are indexed. Use RGBA graphics instead.");
        }

        byte flags = 0x80; // tiles

        if (alpha)
            flags |= 0x40;
        if (paletteIndex >= 0)
            flags |= 0x20;

        flags |= (byte)(colorIndexOffset & 0x1f);

        Graphic texture;
        SortedList<int, Rect> textureAreas = [];

        if (tiles.Count == 0)
            texture = new Graphic(0, 0, 0) { IndexedGraphic = paletteIndex >= 0 };
        else
        {
            int tileWidth = tiles[0].Width;
            int tileHeight = tiles[0].Height;

            if (tileWidth == 0 || tileHeight == 0)
                throw new AmbermoonException(ExceptionScope.Data, "Tile graphics are expected to have a size greater than zero.");

            int totalDimensions = tiles.Count * tileWidth * tileHeight;
            int tilesPerRow = Util.Floor(Math.Sqrt(totalDimensions) / tileWidth);
            int rows = (tiles.Count + tilesPerRow - 1) / tilesPerRow;
            int atlasWidth = tilesPerRow * tileWidth;
            int atlasHeight = rows * tileHeight;

            if (paletteIndex == -1)
            {
                texture = new Graphic
                {
                    Width = atlasWidth,
                    Height = atlasHeight,
                    IndexedGraphic = false,
                    Data = new byte[atlasWidth * atlasHeight * 4]
                };
            }
            else
            {
                texture = new Graphic
                {
                    Width = atlasWidth,
                    Height = atlasHeight,
                    IndexedGraphic = true,
                    Data = new byte[atlasWidth * atlasHeight]
                };
            }

            int tileIndex = 0;

            foreach (var tile in tiles)
            {
                int column = tileIndex % tilesPerRow;
                int row = tileIndex / tilesPerRow;

                texture.AddOverlay((uint)(column * tileWidth), (uint)(row * tileHeight), tile, false);
                textureAreas.Add(tileIndex++, new Rect(column * tileWidth, row * tileHeight, tileWidth, tileHeight));
            }
        }

        return new GraphicAtlasData(paletteIndex, flags, texture, textureAreas);
    }

    public static GraphicAtlasData FromGraphics(IReadOnlyList<Graphic> graphics, bool alpha, int colorIndexOffset = 0)
    {
        return FromGraphics(-1, graphics, alpha, colorIndexOffset);
    }

    public static GraphicAtlasData FromTiles(IReadOnlyList<Graphic> tiles, bool alpha, int colorIndexOffset = 0)
    {
        return FromTiles(-1, tiles, alpha, colorIndexOffset);
    }

    internal void ReuseTextureArea(int index, int reuseAreaIndex)
    {
        textureAreas.Add(index, textureAreas[reuseAreaIndex]);
    }

    public void Read(IDataReader dataReader, uint _, GameData __, byte ___)
    {
        int numGraphics = dataReader.ReadWord();
        List<int>? indices = null;

        if ((numGraphics & 0x8000) != 0) // Explicit indices flag
        {
            numGraphics &= 0x7fff;
            indices = new(numGraphics);

            for (int i = 0; i < numGraphics; ++i)
                indices.Add(dataReader.ReadWord());
        }

        flags = dataReader.ReadByte();
        bool usePalette = (flags & 0x20) != 0;

        paletteIndex = usePalette ? dataReader.ReadByte() : -1;        
        textureAreas.Clear();

        if (numGraphics == 0)
        {
            Atlas = new(new Graphic
            {
                Width = 0,
                Height = 0,
                IndexedGraphic = usePalette,
                Data = []
            }, []);
            return;
        }

        bool tiles = (flags & 0x80) != 0;
        bool alpha = (flags & 0x40) != 0;
        int colorIndexOffset = flags & 0x1f;
        Graphic texture;

        void LoadAtlas()
        {
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();

            texture = new Graphic
            {
                Width = width,
                Height = height,
                IndexedGraphic = usePalette,
                Data = dataReader.ReadBytes(width * height * (usePalette ? 1 : 4))
            };

            if (usePalette && colorIndexOffset != 0)
            {
                if (alpha)
                {
                    for (int i = 0; i < texture.Data.Length; ++i)
                        texture.Data[i] = texture.Data[i] == 0 ? (byte)0 : (byte)(colorIndexOffset + texture.Data[i]);
                }
                else
                {
                    for (int i = 0; i < texture.Data.Length; ++i)
                        texture.Data[i] = (byte)(colorIndexOffset + texture.Data[i]);
                }
            }
        }

        if (tiles)
        {
            int width = dataReader.ReadWord();
            int height = dataReader.ReadWord();

            LoadAtlas();

            if (texture!.Width % width != 0 || texture.Height % height != 0)
                throw new AmbermoonException(ExceptionScope.Data, "Tiled texture atlas dimensions don't fit to the given tile size.");

            int tilesPerRow = texture.Width / width;

            for (int i = 0; i < numGraphics; ++i)
            {
                int column = i % tilesPerRow;
                int row = i / tilesPerRow;
                int index = indices != null ? indices[i] : i;

                textureAreas.Add(index, new Rect(column * width, row * height, width, height));
            }
        }
        else
        {
            for (int i = 0; i < numGraphics; ++i)
            {
                int width = dataReader.ReadWord();
                int height = dataReader.ReadWord();
                int x = dataReader.ReadWord();
                int y = dataReader.ReadWord();
                int index = indices != null ? indices[i] : i;

                textureAreas.Add(index, new Rect(x, y, width, height));
            }

            LoadAtlas();

            if (textureAreas.Values.Min(a => a.Left) < 0 ||
                textureAreas.Values.Min(a => a.Top) < 0 ||
                textureAreas.Values.Max(a => a.Right) > texture!.Width ||
                textureAreas.Values.Max(a => a.Bottom) > texture.Height)
                throw new AmbermoonException(ExceptionScope.Data, "Texture atlas entry was outside the atlas boundaries.");
        }

        Atlas = new(texture, textureAreas.ToDictionary(area => (uint)area.Key, area => new Position(area.Value.Position)));
    }

    public void Write(IDataWriter dataWriter)
    {
        if (textureAreas.Count > short.MaxValue)
            throw new AmbermoonException(ExceptionScope.Data, $"Texture atlas areas are limited to {short.MaxValue}.");

        bool usePalette = (flags & 0x20) != 0;

        if (paletteIndex is null && usePalette)
            throw new AmbermoonException(ExceptionScope.Data, "Texture atlas has the 'use palette' flag set but no palette index was given.");

        if (paletteIndex is not null && !usePalette)
            throw new AmbermoonException(ExceptionScope.Data, "Texture atlas has the 'use palette' flag cleared but a palette index was given.");

        bool explicitIndices = textureAreas.Keys.Order().Select((index, i) => index != i).Any(differ => differ);
        ushort count = (ushort)(explicitIndices ? textureAreas.Count | 0x8000 : textureAreas.Count);

        dataWriter.Write(count);

        if (explicitIndices)
        {
            foreach (var area in textureAreas.OrderBy(area => area.Key))
                dataWriter.Write((ushort)area.Key);
        }

        dataWriter.Write(flags);

        if (usePalette)
            dataWriter.Write((byte)paletteIndex!);       

        if (textureAreas.Count == 0)
            return;

        bool tiles = (flags & 0x80) != 0;
        bool alpha = (flags & 0x40) != 0;
        int colorIndexOffset = flags & 0x1f;

        void WriteImageSize(int width, int height)
        {
            if (width <= 0 || height <= 0 || width > ushort.MaxValue || height > ushort.MaxValue)
                throw new AmbermoonException(ExceptionScope.Data, $"Given texture size is out of range. Should be 1x1 to {ushort.MaxValue}x{ushort.MaxValue}.");

            dataWriter.Write((ushort)width);
            dataWriter.Write((ushort)height);
        }

        var texture = Atlas!.Graphic;

        void WriteAtlas()
        {
            if (texture.Width > ushort.MaxValue || texture.Height > ushort.MaxValue)
                throw new AmbermoonException(ExceptionScope.Data, $"Texture atlas size exceeded max value of {ushort.MaxValue}x{ushort.MaxValue}.");

            if (texture.Width * texture.Height * (usePalette ? 1 : 4) != texture.Data.Length)
                throw new AmbermoonException(ExceptionScope.Data, "Texture atlas data size does not match the excepted size based on the dimensions and format.");

            var data = usePalette && colorIndexOffset != 0 ? new byte[texture.Data.Length] : texture.Data;

            if (usePalette && colorIndexOffset != 0)
            {
                byte AdjustAndCheck(byte colorIndex)
                {
                    if (colorIndex < colorIndexOffset)
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid color indices with the given color index offset.");

                    return (byte)(colorIndex - colorIndexOffset);
                }

                if (alpha)
                {
                    for (int i = 0; i < texture.Data.Length; ++i)
                        data[i] = texture.Data[i] == 0 ? (byte)0 : AdjustAndCheck(texture.Data[i]);
                }
                else
                {
                    for (int i = 0; i < texture.Data.Length; ++i)
                        data[i] = AdjustAndCheck(texture.Data[i]);
                }
            }

            dataWriter.Write((ushort)texture.Width);
            dataWriter.Write((ushort)texture.Height);
            dataWriter.Write(data);
        }

        if (tiles)
        {
            int width = textureAreas[0].Width;
            int height = textureAreas[0].Height;

            if (texture!.Width % width != 0 || texture.Height % height != 0)
                throw new AmbermoonException(ExceptionScope.Data, "Tiled texture atlas dimensions don't fit to the given tile size.");

            WriteImageSize(width, height);
        }
        else
        {
            var totalArea = new Rect(0, 0, texture!.Width, texture.Height);

            foreach (var (_, area) in textureAreas.OrderBy(area => area.Key))
            {
                WriteImageSize(area.Width, area.Height);

                if (area.X < 0 || area.Y < 0 || area.X > ushort.MaxValue || area.Y > ushort.MaxValue ||
                    !totalArea.Contains(area.Position) || !totalArea.Contains(new Position(area.Right - 1, area.Bottom - 1)))
                    throw new AmbermoonException(ExceptionScope.Data, "The given texture area was outside the texture atlas.");

                dataWriter.Write((ushort)area.X);
                dataWriter.Write((ushort)area.Y);
            }
        }

        WriteAtlas();
    }
}
