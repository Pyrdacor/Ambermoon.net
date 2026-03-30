using Ambermoon.Data.Legacy.Serialization;
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
        private interface IGraphicInfo
        {
            int Index { get; }
            Size Size { get; }

            void Deconstruct(out int index, out Size size);
        }
        private record GraphicInfo(int Index, Size Size) : IGraphicInfo
        {
            public void Deconstruct(out int index, out Size size)
            {
                index = Index;
                size = Size;
            }
        }
        private record GraphicContainer(int Index, Size Size) : IGraphicInfo
        {
            public List<PlacedGraphic> Children { get; } = [];

            public GraphicContainer? Parent { get; private set; } = null;

            public void AddChild(Position position, IGraphicInfo child)
            {
                Children.Add(new PlacedGraphic(position, child));

                if (child is GraphicContainer container)
                    container.Parent = this;
            }

            public void Deconstruct(out int index, out Size size)
            {
                index = Index;
                size = Size;
            }
        }
        private record PlacedGraphic(Position Position, IGraphicInfo GraphicInfo);

        private class WidthPrioComparer : IComparer<IGraphicInfo>
        {
            public int Compare(IGraphicInfo? x, IGraphicInfo? y)
            {
                if (y is null) return -1;
                if (x is null) return 1;

                int result = x.Size.Width.CompareTo(y.Size.Width);

                return result != 0 ? result : x.Size.Height.CompareTo(y.Size.Height);
            }
        }

        private class HeightPrioComparer : IComparer<IGraphicInfo>
        {
            public int Compare(IGraphicInfo? x, IGraphicInfo? y)
            {
                if (y is null) return -1;
                if (x is null) return 1;

                int result = x.Size.Height.CompareTo(y.Size.Height);

                return result != 0 ? result : x.Size.Width.CompareTo(y.Size.Width);
            }
        }

        public static KeyValuePair<Graphic, SortedList<int, Rect>> PackTextureAtlas(IDictionary<int, Graphic> graphics)
        {
            if (graphics.Count == 0)
            {
                return KeyValuePair.Create<Graphic, SortedList<int, Rect>>(new Graphic(0, 0, 0), []);
            }
            else if (graphics.Count == 1)
            {
                var first = graphics.First();

                return KeyValuePair.Create(first.Value, new SortedList<int, Rect>(1)
                {
                    { first.Key, new Rect(0, 0, first.Value.Width, first.Value.Height) }
                });
            }

            var widthPrioComparer = new WidthPrioComparer();
            var heightPrioComparer = new HeightPrioComparer();
            IEnumerable<IGraphicInfo> graphicInfos = graphics.Select(g => new GraphicInfo(g.Key, new Size(g.Value.Width, g.Value.Height)));
            var widestList = graphicInfos.OrderBy(g => g.Size.Width).ThenBy(g => g.Size.Height).ToList();
            var highestList = graphicInfos.OrderBy(g => g.Size.Height).ThenBy(g => g.Size.Width).ToList();

            var emptyAreas = new List<(GraphicContainer, Rect)>(graphics.Count);
            int containerIndex = -1;

            while (widestList.Count != 1)
            {
                var widest = widestList[^1];
                var highest = highestList[^1];
                bool widthPrio = widest.Size.Width > highest.Size.Height;

                if (widestList.Count > 1)
                {
                    widthPrio = widestList[^2].Size.Width > highestList[^2].Size.Height;
                }

                GraphicContainer? container;

                if (widthPrio)
                {
                    container = ProcessStripePacking(true, widest.Index, widest.Size.Width);
                }
                else
                {
                    container = ProcessStripePacking(false, highest.Index, highest.Size.Height);
                }

                if (container == null)
                {
                    // No possible packing -> switch pack direction
                    if (widthPrio)
                    {
                        container = ProcessStripePacking(false, widest.Index, widest.Size.Height);
                    }
                    else
                    {
                        container = ProcessStripePacking(true, highest.Index, highest.Size.Width);
                    }

                    if (container?.Children.Count == 1)
                    {
                        var child = container.Children[0];

                        if (widest.Size.Width <= highest.Size.Height)
                        {
                            container = new GraphicContainer(container.Index, new Size(widest.Size.Width, widest.Size.Height + child.GraphicInfo.Size.Height));

                            container.AddChild(Position.Zero, widest);
                            container.AddChild(new Position(0, widest.Size.Height), child.GraphicInfo);
                        }
                        else
                        {
                            container = new GraphicContainer(container.Index, new Size(widest.Size.Width + child.GraphicInfo.Size.Width, widest.Size.Height));

                            container.AddChild(Position.Zero, widest);
                            container.AddChild(new Position(widest.Size.Width, 0), child.GraphicInfo);
                        }
                    }
                }
                else if (container.Children.Count == 1)
                {
                    var child = container.Children[0];

                    if (widthPrio)
                    {
                        container = new GraphicContainer(container.Index, new Size(widest.Size.Width, widest.Size.Height + child.GraphicInfo.Size.Height));

                        container.AddChild(Position.Zero, widest);
                        container.AddChild(new Position(0, widest.Size.Height), child.GraphicInfo);
                    }
                    else
                    {
                        container = new GraphicContainer(container.Index, new Size(widest.Size.Width + child.GraphicInfo.Size.Width, widest.Size.Height));

                        container.AddChild(Position.Zero, widest);
                        container.AddChild(new Position(widest.Size.Width, 0), child.GraphicInfo);
                    }
                }

                foreach (var child in container!.Children)
                {
                    widestList.Remove(child.GraphicInfo);
                    highestList.Remove(child.GraphicInfo);
                }

                if (container.Parent == null)
                {
                    widestList.AddSorted(container!, widthPrioComparer);
                    highestList.AddSorted(container!, heightPrioComparer);
                }
            }

            var rootContainer = (widestList[0] as GraphicContainer)!;
            var graphic = new Graphic(rootContainer.Size.Width, rootContainer.Size.Height, 0);
            var textureAreas = new SortedList<int, Rect>(graphics.Count);

            ProcessContainer(Position.Zero, rootContainer);

            void ProcessContainer(Position position, GraphicContainer container)
            {
                foreach (var entry in container.Children)
                {
                    if (entry.GraphicInfo is GraphicInfo graphicInfo)
                    {
                        var gfx = graphics[graphicInfo.Index];
                        var pos = position + entry.Position;
                        graphic.AddOverlay(pos.X, pos.Y, gfx, false);
                        textureAreas.Add(graphicInfo.Index, new Rect(pos, graphicInfo.Size));
                    }
                    else if (entry.GraphicInfo is GraphicContainer subContainer)
                    {
                        ProcessContainer(position + entry.Position, subContainer);
                    }
                }
            }

            return KeyValuePair.Create(graphic, textureAreas);

            GraphicContainer? ProcessStripePacking(bool buildWidth, int templateIndex, int templateDimension)
            {
                var collection = buildWidth ? widestList : highestList;
                int index = collection.Count - 2;
                int x = 0;
                int y = 0;
                int preferredOtherDim = -1;

                Func<Size, int> dimension = buildWidth ? g => g.Width : g => g.Height;
                Func<Size, int> otherDimension = buildWidth ? g => g.Height : g => g.Width;
                Action<Size> advancer = buildWidth ? g => x += g.Width : g => y += g.Height;
                Func<Size, bool> checker = buildWidth ? g => x + g.Width <= templateDimension : g => y + g.Height <= templateDimension;
                int bestMatchIndex = -1;
                int bestMatchDimension = -1;
                int bestMatchOtherDimension = -1;
                var mergedGraphics = new List<(Position Position, IGraphicInfo)>(40);
                var emptyContainerAreas = new List<Rect>(10);
                var biggestEmptyAreas = emptyAreas.OrderByDescending(a => buildWidth ? a.Item2.Width : a.Item2.Height).ThenBy(a => buildWidth ? a.Item2.Height : a.Item2.Width);

                var (key, size) = collection[index];

                var fittingEmptyArea = biggestEmptyAreas.FirstOrDefault(a => a.Item2.Width >= size.Width && a.Item2.Height >= size.Height);

                if (fittingEmptyArea.Item2 != null)
                {
                    var fittingContainer = fittingEmptyArea.Item1;
                    var area = fittingEmptyArea.Item2;

                    fittingContainer.AddChild(area.Position, collection[index]);

                    // TODO: Maybe later also keep the potential remaining area
                    emptyAreas.Remove(fittingEmptyArea);

                    return fittingContainer;
                }

                while (index >= 0)
                {
                    (key, size) = collection[index];

                    if (checker(size))
                    {
                        if (bestMatchIndex != -1)
                        {
                            // Already found a match but maybe this is better?
                            // It has to be the same width and closer height value.
                            int dim = dimension(size);

                            if (dim == bestMatchDimension)
                            {
                                int otherDim = otherDimension(size);
                                int diff = otherDim - preferredOtherDim;

                                if (diff == 0 || Math.Abs(diff) < Math.Abs(bestMatchOtherDimension - preferredOtherDim) ||
                                    (diff < 0 && Math.Abs(diff) == Math.Abs(bestMatchOtherDimension - preferredOtherDim)))
                                {
                                    bestMatchIndex = index;
                                    bestMatchOtherDimension = otherDim;

                                    if (diff == 0) // perfect match, directly use it!
                                    {
                                        bestMatchIndex = -1;
                                        bestMatchDimension = -1;
                                        bestMatchOtherDimension = -1;

                                        mergedGraphics.Add((new Position(x, y), collection[index]));
                                        advancer(size);
                                    }
                                }
                            }
                            else
                            {
                                // Keep the previous best match.
                                var entry = collection[bestMatchIndex];
                                (key, size) = entry;

                                if (buildWidth)
                                {
                                    if (size.Height > preferredOtherDim)
                                    {
                                        emptyContainerAreas.Add(new Rect(0, preferredOtherDim, x, size.Height - preferredOtherDim));
                                    }
                                    else if (size.Height < preferredOtherDim)
                                    {
                                        emptyContainerAreas.Add(new Rect(x, size.Height, size.Width, preferredOtherDim - size.Height));
                                    }
                                }
                                else
                                {
                                    if (size.Width > preferredOtherDim)
                                    {
                                        emptyContainerAreas.Add(new Rect(preferredOtherDim, 0, size.Width - preferredOtherDim, y));
                                    }
                                    else if (size.Width < preferredOtherDim)
                                    {
                                        emptyContainerAreas.Add(new Rect(size.Width, y, preferredOtherDim - size.Width, size.Height));
                                    }
                                }

                                index = bestMatchIndex;
                                preferredOtherDim = Math.Max(preferredOtherDim, otherDimension(size));
                                bestMatchIndex = -1;
                                bestMatchDimension = -1;
                                bestMatchOtherDimension = -1;

                                mergedGraphics.Add((new Position(x, y), entry));
                                advancer(size);
                            }

                            index--;

                            continue;
                        }

                        if (preferredOtherDim == -1)
                        {
                            preferredOtherDim = otherDimension(size);
                            bestMatchIndex = -1;
                            bestMatchDimension = -1;
                            bestMatchOtherDimension = -1;

                            mergedGraphics.Add((new Position(x, y), collection[index]));
                            advancer(size);
                        }
                        else
                        {
                            bestMatchIndex = index;
                            bestMatchOtherDimension = otherDimension(size);

                            if (bestMatchOtherDimension == preferredOtherDim)
                            {
                                bestMatchDimension = -1;
                                bestMatchOtherDimension = -1;
                            }
                            else
                            {
                                bestMatchDimension = dimension(size);
                            }
                        }
                    }

                    index--;
                }

                // Processed everything but there is a match pending?
                if (bestMatchIndex != -1)
                {
                    var entry = collection[bestMatchIndex];
                    size = entry.Size;

                    if (buildWidth)
                    {
                        if (size.Height > preferredOtherDim)
                        {
                            emptyContainerAreas.Add(new Rect(0, preferredOtherDim, x, size.Height - preferredOtherDim));
                        }
                        else if (size.Height < preferredOtherDim)
                        {
                            emptyContainerAreas.Add(new Rect(x, size.Height, size.Width, preferredOtherDim - size.Height));
                        }
                    }
                    else
                    {
                        if (size.Width > preferredOtherDim)
                        {
                            emptyContainerAreas.Add(new Rect(preferredOtherDim, 0, size.Width - preferredOtherDim, y));
                        }
                        else if (size.Width < preferredOtherDim)
                        {
                            emptyContainerAreas.Add(new Rect(size.Width, y, preferredOtherDim - size.Width, size.Height));
                        }
                    }

                    preferredOtherDim = Math.Max(preferredOtherDim, otherDimension(size));
                    mergedGraphics.Add((new Position(x, y), entry));
                    advancer(size);
                }

                var container = new GraphicContainer(containerIndex--, buildWidth ? new Size(x, preferredOtherDim) : new Size(preferredOtherDim, y));

                foreach (var (position, info) in mergedGraphics)
                    container.AddChild(position, info);

                foreach (var emptyContainerArea in emptyContainerAreas)
                {
                    emptyAreas.Add((container, emptyContainerArea));
                }

                return container;
            }
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
            var textureAtlasInfo = /*TexturePacker.PackTextureAtlas(graphics)*/TexturePacker.PackTextureAtlas(graphics);
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
            var textureAtlasInfo = TexturePacker.PackTextureAtlas/*TexturePacker.PackTextureAtlas*/(graphics
                .Select((g, i) => new { Graphic = g, Index = i })
                .ToDictionary(graphic => graphic.Index, graphic => graphic.Graphic));
            texture = textureAtlasInfo.Key;
            textureAreas = textureAtlasInfo.Value;
        }

        return new GraphicAtlasData(paletteIndex, flags, texture, textureAreas);
    }

    public static GraphicAtlasData FromTiles(int paletteIndex, IReadOnlyList<Graphic> tiles, bool alpha, int colorIndexOffset = 0, int? forcedTilesPerRow = null)
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
            int tilesPerRow = forcedTilesPerRow ?? Util.Floor(Math.Sqrt(totalDimensions) / tileWidth);
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

    public static GraphicAtlasData FromTiles(IReadOnlyList<Graphic> tiles, bool alpha, int colorIndexOffset = 0, int? forcedTilesPerRow = null)
    {
        return FromTiles(-1, tiles, alpha, colorIndexOffset, forcedTilesPerRow);
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
