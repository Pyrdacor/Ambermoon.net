using System.Collections.Generic;

namespace Ambermoon.Data
{
    // Note: 3D graphics are loaded through labdata
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
        Portrait,
        Item,
        Layout,
        LabBackground,
        Cursor
        // TODO ...
    }

    public interface IGraphicProvider
    {
        Dictionary<int, Graphic> Palettes { get; }
        List<Graphic> GetGraphics(GraphicType type);
    }
}
