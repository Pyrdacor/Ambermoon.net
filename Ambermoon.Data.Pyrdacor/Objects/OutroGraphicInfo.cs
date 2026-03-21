namespace Ambermoon.Data.Pyrdacor.Objects;

internal readonly struct OutroGraphicInfo
{
    public uint ImageDataOffset { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public byte PaletteIndex { get; init; }
}
