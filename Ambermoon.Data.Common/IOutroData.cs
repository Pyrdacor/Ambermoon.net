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

    public struct OutroAction
    {
        public OutroCommand Command { get; init; }
        public bool LargeText { get; init; }
        public int ScrollAmount { get; init; }
        public int TextDisplayX { get; init; }
        public int? TextIndex { get; init; }
        public uint? ImageOffset { get; init; }
    }

    public struct OutroGraphicInfo
    {
        public uint GraphicIndex { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public byte PaletteIndex { get; init; }
    }

    public interface IOutroData
    {
        public IReadOnlyDictionary<OutroOption, IReadOnlyList<OutroAction>> OutroActions { get; }
        public IReadOnlyList<Graphic> OutroPalettes { get; }
        public IReadOnlyList<Graphic> Graphics { get; }
        public IReadOnlyList<string> Texts { get; }
        public IReadOnlyDictionary<uint, OutroGraphicInfo> GraphicInfos { get; }
    }
}
