using System.Collections.Generic;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public enum IntroGraphic
    {
        Frame, // unknown
        MainMenuBackground,
        Gemstone,
        Illien,
        Snakesign,
        DestroyedGemstone,
        DestroyedIllien,
        DestroyedSnakesign,
        ThalionLogo,
        Ambermoon,
        SunAnimation,
        Lyramion,
        Morag,
        ForestMoon,
        Meteor,
        MeteorSparks,
        Twinlake
    }

    public enum IntroText
    {
        Gemstone,
        Illien,
        Snakesign,
        Presents,
        Twinlake,
        Lyramion,
        SeventyYears,
        After,
        Continue,
        NewGame,
        Intro,
        Quit
        // TODO: also extract credits?
    }

    public interface IIntroTwinlakeImagePart
    {
        public Position Position { get; }
        public Graphic Graphic { get; }
    }

    public enum IntroTextCommandType
    {
        Clear,
        Add,
        Render,
        Wait,
        SetTextColor,
        Unknown
    }

    public interface IIntroTextCommand
    {
        IntroTextCommandType Type { get; }
        int[] Args { get; }
    }

    public interface IIntroData
    {
        IReadOnlyList<Graphic> IntroPalettes { get; }
        static abstract IReadOnlyDictionary<IntroGraphic, byte> GraphicPalettes { get; }
        IReadOnlyDictionary<IntroGraphic, Graphic> Graphics { get; }
        IReadOnlyDictionary<IntroText, string> Texts { get; }
        IReadOnlyDictionary<char, Glyph> Glyphs { get; }
        IReadOnlyDictionary<char, Glyph> LargeGlyphs { get; }
        IReadOnlyList<IIntroTwinlakeImagePart> TwinlakeImageParts { get; }
        IReadOnlyList<IIntroTextCommand> TextCommands { get; }
        IReadOnlyList<string> TextCommandTexts { get; }
    }
}
