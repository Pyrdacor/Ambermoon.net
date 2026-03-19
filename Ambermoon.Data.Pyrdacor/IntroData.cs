namespace Ambermoon.Data.Pyrdacor;

internal class IntroData : IIntroData
{
    public required IReadOnlyList<Graphic> IntroPalettes { get; internal init; }
    public required IReadOnlyList<IIntroTwinlakeImagePart> TwinlakeImageParts { get; internal init; }
    public required IReadOnlyList<IIntroTextCommand> TextCommands { get; internal init; }
    public required IReadOnlyList<string> TextCommandTexts { get; internal init; }
    public required IReadOnlyDictionary<IntroGraphic, Graphic> Graphics { get; internal init; }
    public required IReadOnlyDictionary<IntroText, string> Texts { get; internal init; }
    public required IReadOnlyDictionary<char, Glyph> Glyphs { get; internal init; }
    public required IReadOnlyDictionary<char, Glyph> LargeGlyphs { get; internal init; }
}
