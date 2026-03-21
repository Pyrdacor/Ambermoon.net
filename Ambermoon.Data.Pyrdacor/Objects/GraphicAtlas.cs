namespace Ambermoon.Data.Pyrdacor.Objects;

public sealed class GraphicAtlas : IGraphicAtlas
{
    public Graphic Graphic { get; }
    public Dictionary<uint, Position> Offsets { get; }

    public IReadOnlyDictionary<uint, Graphic> ToDictionary(Dictionary<uint, Size> graphicSizes)
    {
        var graphics = new Dictionary<uint, Graphic>();

        foreach (var offset in Offsets)
        {
            var size = graphicSizes[offset.Key];
            var partialGraphic = Graphic.GetArea(offset.Value.X, offset.Value.Y, size.Width, size.Height);

            graphics.Add(offset.Key, partialGraphic);
        }

        return graphics;
    }

    public IReadOnlyDictionary<TKey, Graphic> ToDictionary<TKey>(IDictionary<uint, Size> graphicSizes) where TKey : struct, Enum
    {
        var graphics = new Dictionary<TKey, Graphic>();

        foreach (var offset in Offsets)
        {
            var size = graphicSizes[offset.Key];
            var partialGraphic = Graphic.GetArea(offset.Value.X, offset.Value.Y, size.Width, size.Height);

            graphics.Add((TKey)Enum.ToObject(typeof(TKey), offset.Key), partialGraphic);
        }

        return graphics;
    }

    internal GraphicAtlas()
    {
        Graphic = new Graphic()
        {
            Width = 0,
            Height = 0,
            IndexedGraphic = true,
            Data = []
        };
        Offsets = [];
    }

    internal GraphicAtlas(Graphic graphic, Dictionary<uint, Position> offsets)
    {
        Graphic = graphic;
        Offsets = offsets;
    }
}
