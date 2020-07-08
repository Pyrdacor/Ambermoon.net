using System.Collections.Generic;

namespace Ambermoon.Data
{
    public enum GraphicType
    {
        Tileset1,
        Tileset2,
        Tileset3,
        Tileset4,
        Tileset5,
        Tileset6,
        Tileset7,
        Tileset8,
        Player,
        Map3D,
        Portrait,
        Item,
        Layout,
        // TODO ...
    }

    public interface IGraphicProvider
    {
        Dictionary<int, Graphic> Palettes { get; }
        List<Graphic> GetGraphics(GraphicType type);
    }
}
