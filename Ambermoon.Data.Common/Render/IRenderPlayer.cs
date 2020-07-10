using Ambermoon.Data;

namespace Ambermoon.Render
{
    public interface IRenderPlayer
    {
        Position Position { get; } // in Tiles
        bool Move(int x, int y, uint ticks);
        void MoveTo(Map map, uint x, uint y, uint ticks, bool frameReset, CharacterDirection? newDirection);
    }
}
