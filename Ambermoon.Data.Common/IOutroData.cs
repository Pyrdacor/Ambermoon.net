using System.Collections.Generic;

namespace Ambermoon.Data
{
    public enum OutroCommand
    {
        PrintTextAndScroll,
        WaitForClick,
        ChangePicture
    }

    public enum OutroOption
    {
        /// <summary>
        /// Valdyn is inside the party when you leave the machine room.
        /// And you got the yellow sphere from the dying moranian.
        /// </summary>
        ValdynInPartyNoYellowSphere,
        /// <summary>
        /// Valdyn is inside the party when you leave the machine room.
        /// And you did not get the yellow sphere from the dying moranian.
        /// </summary>
        ValdynInPartyWithYellowSphere,
        /// <summary>
        /// Valdyn is not inside the party when you leave the machine room.
        /// </summary>
        ValdynNotInParty
    }

    public readonly struct OutroAction
    {
        public OutroCommand Command { get; init; }
        public bool LargeText { get; init; }
        public int ScrollAmount { get; init; }
        public int TextDisplayX { get; init; }
        public int? TextIndex { get; init; }
        public uint? ImageOffset { get; init; }
    }

    public readonly struct OutroGraphicInfo
    {
        public uint GraphicIndex { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public byte PaletteIndex { get; init; }
    }

    public interface IOutroData
    {
        IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> OutroActions { get; }
        IReadOnlyList<Graphic> OutroPalettes { get; }
        IReadOnlyList<Graphic> Graphics { get; }
        IReadOnlyList<string> Texts { get; }
        IReadOnlyDictionary<uint, OutroGraphicInfo> GraphicInfos { get; }
        IReadOnlyDictionary<char, Glyph> Glyphs { get; }
        IReadOnlyDictionary<char, Glyph> LargeGlyphs { get; }
    }
}
