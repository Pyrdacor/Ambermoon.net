namespace Ambermoon.Render
{
    internal interface IMapCharacter
    {
        void Move(int x, int y, uint ticks);
        bool Interact(MapEventTrigger trigger);
        Position Position { get; }
    }
}
