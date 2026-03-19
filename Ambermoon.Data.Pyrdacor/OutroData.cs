namespace Ambermoon.Data.Pyrdacor;

internal class OutroData : IOutroData
{
    public required IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> OutroActions { get; internal init; }
    public required IReadOnlyList<Graphic> OutroPalettes { get; internal init; }
    public required IReadOnlyList<Graphic>? Graphics { get; internal init; }
    public required IGraphicAtlas? GraphicAtlas { get; internal init; }
    public required IReadOnlyList<string> Texts { get; internal init; }
    public required IReadOnlyDictionary<uint, OutroGraphicInfo> GraphicInfos { get; internal init; }
    public required IReadOnlyDictionary<char, Glyph> Glyphs { get; internal init; }
    public required IReadOnlyDictionary<char, Glyph> LargeGlyphs { get; internal init; }
}
