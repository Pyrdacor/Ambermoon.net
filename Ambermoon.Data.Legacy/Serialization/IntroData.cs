using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
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

    public class IntroData
    {
        readonly List<Graphic> introPalettes = new List<Graphic>();
        readonly Dictionary<IntroGraphic, Graphic> graphics = new Dictionary<IntroGraphic, Graphic>();
        static readonly Dictionary<IntroGraphic, byte> graphicPalettes = new Dictionary<IntroGraphic, byte>
        {
            { IntroGraphic.Frame, 0 }, // unknown
            { IntroGraphic.MainMenuBackground, 6 }, // 8 will work too
            { IntroGraphic.Gemstone, 4 },
            { IntroGraphic.Illien, 4 },
            { IntroGraphic.Snakesign, 4 },
            { IntroGraphic.DestroyedGemstone, 4 },
            { IntroGraphic.DestroyedIllien, 4 },
            { IntroGraphic.DestroyedSnakesign, 4 },
            { IntroGraphic.ThalionLogo, 0 },
            { IntroGraphic.SunAnimation, 3 },
            { IntroGraphic.Lyramion, 3 },
            { IntroGraphic.Morag, 3 },
            { IntroGraphic.ForestMoon, 3 },
            { IntroGraphic.Meteor, 3 },
            // TODO ...
        };
        static GraphicInfo paletteGraphicInfo = new GraphicInfo
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };

        public IReadOnlyList<Graphic> IntroPalettes => introPalettes.AsReadOnly();
        public static IReadOnlyDictionary<IntroGraphic, byte> GraphicPalettes => graphicPalettes;
        public IReadOnlyDictionary<IntroGraphic, Graphic> Graphics => graphics;


        public IntroData(GameData gameData)
        {
            var introHunks = AmigaExecutable.Read(gameData.Files["ambermoon_intro"].Files[1])
                .Where(h => h.Type == AmigaExecutable.HunkType.Data).Select(h => new DataReader(((AmigaExecutable.Hunk)h).Data))
                .ToList();
            var graphicReader = new GraphicReader();

            #region Hunk 0 - Palettes and texts

            Graphic LoadPalette()
            {
                var paletteGraphic = new Graphic();
                graphicReader.ReadGraphic(paletteGraphic, introHunks[0], paletteGraphicInfo);
                return paletteGraphic;
            }

            for (int i = 0; i < 18; ++i)
                introPalettes.Add(LoadPalette());
            // TODO: after the 18 palettes there are many texts (including the main menu texts)

            #endregion

            #region Hunk 1 - Main menu background and town graphics

            Size[] hunk1ImageSizes = new Size[8]
            {
                new Size(96, 300), // not sure
                new Size(320, 256),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128),
                new Size(160, 128)
            };

            for (int i = 0; i < 8; ++i)
            {
                var reader = introHunks[1];

                if (reader.PeekDword() == 0x494d5021) // "IMP!", may be imploded
                    reader = new DataReader(Deploder.DeplodeFimp(reader).Reverse().ToArray());

                var graphicInfo = new GraphicInfo
                {
                    Width = hunk1ImageSizes[i].Width,
                    Height = hunk1ImageSizes[i].Height,
                    GraphicFormat = GraphicFormat.Palette4Bit,
                    PaletteOffset = 0,
                    Alpha = false
                };
                var graphic = new Graphic();
                graphicReader.ReadGraphic(graphic, reader, graphicInfo);
                graphics.Add((IntroGraphic)i, graphic);
            }

            #endregion

            #region Hunk 2 - Unknown

            // TODO

            #endregion

            #region Hunk 3 - Intro graphics (planets, etc)

            Size[] hunk3ImageSizes = new Size[6]
            {
                new Size(128, 82), // Thalion Logo
                new Size(64, 64), // Sun
                new Size(128, 128), // Lyramion
                new Size(64, 64), // Morag
                new Size(64, 64), // Forest Moon
                new Size(96, 96), // Meteor
                // TODO ...
            };
            int[] hunk3FrameCounts = new int[6]
            {
                1,
                12,
                1,
                1,
                1,
                1,
                // TODO ...
            };

            for (int i = 0; i < 6; ++i)
            {
                var graphicInfo = new GraphicInfo
                {
                    Width = hunk3ImageSizes[i].Width,
                    Height = hunk3ImageSizes[i].Height,
                    GraphicFormat = GraphicFormat.Palette4Bit,
                    PaletteOffset = 0,
                    Alpha = false
                };
                Graphic graphic;
                int frames = hunk3FrameCounts[i];
                if (frames == 1)
                {
                    graphic = new Graphic();
                    graphicReader.ReadGraphic(graphic, introHunks[3], graphicInfo);
                }
                else
                {
                    graphic = new Graphic(frames * graphicInfo.Width, graphicInfo.Height, 0);

                    for (int f = 0; f < hunk3FrameCounts[i]; ++f)
                    {
                        var frameGraphic = new Graphic();
                        graphicReader.ReadGraphic(frameGraphic, introHunks[3], graphicInfo);
                        graphic.AddOverlay((uint)(f * frameGraphic.Width), 0, frameGraphic, false);
                    }
                }
                graphics.Add(IntroGraphic.ThalionLogo + i, graphic);
            }

            // TODO ...

            #endregion
        }
    }
}
