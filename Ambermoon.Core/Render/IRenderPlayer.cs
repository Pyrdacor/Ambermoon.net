using Ambermoon.Data;

namespace Ambermoon.Render
{
    public interface IRenderPlayer
    {
        Position Position { get; } // in Tiles
        void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection);
    }
}
