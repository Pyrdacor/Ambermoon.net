using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    public enum FantasyIntroGraphic
    {
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
        Stars
    }

    public class FantasyIntroData
    {
        readonly List<Graphic> fantasyIntroPalettes = new List<Graphic>();
        readonly Dictionary<FantasyIntroGraphic, Graphic> graphics = new Dictionary<FantasyIntroGraphic, Graphic>();
        static readonly Dictionary<FantasyIntroGraphic, byte> graphicPalettes = new Dictionary<FantasyIntroGraphic, byte>
        {
            { FantasyIntroGraphic.Background, 1 }, // 0 seem to be used as a darker version maybe for fading from dark to bright
            { FantasyIntroGraphic.Stars, 1 }
        };
        static GraphicInfo paletteGraphicInfo = new GraphicInfo
        {
            Width = 32,
            Height = 1,
            GraphicFormat = GraphicFormat.XRGB16
        };

        public IReadOnlyList<Graphic> FantasyIntroPalettes => fantasyIntroPalettes.AsReadOnly();
        public static IReadOnlyDictionary<FantasyIntroGraphic, byte> GraphicPalettes => graphicPalettes;
        public IReadOnlyDictionary<FantasyIntroGraphic, Graphic> Graphics => graphics;


        public FantasyIntroData(GameData gameData)
        {
            var fantasyIntroHunks = AmigaExecutable.Read(gameData.Files["Fantasy_intro"].Files[1])
                .Where(h => h.Type == AmigaExecutable.HunkType.Data).Select(h => new DataReader(((AmigaExecutable.Hunk)h).Data))
                .ToList();
            var graphicReader = new GraphicReader();

            #region Hunk 0 - Palettes and graphics

            var hunk0 = fantasyIntroHunks[0];

            Graphic LoadPalette()
            {
                var paletteGraphic = new Graphic();
                graphicReader.ReadGraphic(paletteGraphic, hunk0, paletteGraphicInfo);
                return paletteGraphic;
            }

            for (int i = 0; i < 2; ++i)
                fantasyIntroPalettes.Add(LoadPalette());

            // unknown bytes (maybe copper instructions?)

            hunk0.Position = 0xD38C;
            AddGraphic(FantasyIntroGraphic.Background, 320, 256);
            AddGraphic(FantasyIntroGraphic.Stars, 32, 108);

            // unknown data
            // TODO: somewhere there must be the fairy sprites and the text "Fantasy"

            void AddGraphic(FantasyIntroGraphic fantasyIntroGraphic, int width, int height)
            {
                var graphicInfo = new GraphicInfo
                {
                    Width = width,
                    Height = height,
                    GraphicFormat = GraphicFormat.Palette5Bit,
                    PaletteOffset = 0,
                    Alpha = false
                };
                var graphic = new Graphic();
                graphicReader.ReadGraphic(graphic, hunk0, graphicInfo);
                graphics.Add(fantasyIntroGraphic, graphic);
            }

            #endregion
        }
    }
}
