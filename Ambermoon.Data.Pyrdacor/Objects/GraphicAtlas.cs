namespace Ambermoon.Data.Pyrdacor.Objects;

public sealed class GraphicAtlas : IGraphicAtlas
{
    public Graphic Graphic { get; }
    public Dictionary<uint, Position> Offsets { get; }

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
