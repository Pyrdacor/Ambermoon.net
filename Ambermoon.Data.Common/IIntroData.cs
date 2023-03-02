using System.Collections.Generic;

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
        SunAnimation,
        Lyramion,
        Morag,
        ForestMoon,
        Meteor
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

    public interface IIntroData
    {
        IReadOnlyList<Graphic> IntroPalettes { get; }
        static abstract IReadOnlyDictionary<IntroGraphic, byte> GraphicPalettes { get; }
        IReadOnlyDictionary<IntroGraphic, Graphic> Graphics { get; }
        IReadOnlyDictionary<IntroText, string> Texts { get; }
    }
}
