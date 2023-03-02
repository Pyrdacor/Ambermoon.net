using System.Collections.Generic;
using System;

namespace Ambermoon.Data
{
    public enum FantasyIntroCommand
    {
        FadeIn,
        MoveFairy,
        PlayFairyAnimation,
        AddWritingPart,
        UpdateSparkLine,
        UpdateSparkStar,
        SetSparkLineColor,
        SetSparkStarColor,
        DrawSparkLine,
        DrawSparkStar,
        DrawSparkDot,
        FadeOut
    }

    public readonly struct FantasyIntroAction
    {
        internal FantasyIntroAction(uint frames, FantasyIntroCommand command, params int[] parameters)
        {
            Frames = frames;
            Command = command;
            Parameters = parameters;
        }

        public uint Frames { get; }

        public FantasyIntroCommand Command { get; }

        public int[] Parameters { get; }

    }

    public enum FantasyIntroGraphic
    {
        /// <summary>
        /// Tiny stars falling the fairy and her wand.
        /// </summary>
        FairySparks,
        /// <summary>
        /// Sprites of the fairy character.
        /// </summary>
        Fairy,
        /// <summary>
        /// Background with a big blue Thalion logo in the center.
        /// The background is purple greyish and also contains small Thalion logos.
        /// </summary>
        Background,
        /// <summary>
        /// 12 star frames (multiple color and sizes, each is 32x9 pixels in size).
        /// Each has the graphic (first 16 pixel block) and a mask (second 16 pixel block).
        /// The mask has color index 31 where colored pixels are and index 0 where only blackness is.
        /// </summary>
        WritingSparks,
        /// <summary>
        /// The Fantasy writing.
        /// </summary>
        Writing

    }

    public interface IFantasyIntroData
    {
        public Queue<FantasyIntroAction> Actions { get; }
        public IReadOnlyList<Graphic> FantasyIntroPalettes { get; }
        public static IReadOnlyDictionary<FantasyIntroGraphic, byte> GraphicPalettes { get; }
        public IReadOnlyDictionary<FantasyIntroGraphic, Graphic> Graphics { get; }
    }
}
