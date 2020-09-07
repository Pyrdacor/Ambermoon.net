using Ambermoon.Render;
using System.Linq;

namespace Ambermoon
{
    public partial class Global
    {
        public const int LayoutX = 0;
        public const int LayoutY = 37;
        public const int Map2DViewX = 16;
        public const int Map2DViewY = 49;
        public const int Map2DViewWidth = RenderMap2D.NUM_VISIBLE_TILES_X * RenderMap2D.TILE_WIDTH;
        public const int Map2DViewHeight = RenderMap2D.NUM_VISIBLE_TILES_Y * RenderMap2D.TILE_HEIGHT;
        public const int Map3DViewX = 32;
        public const int Map3DViewY = 49;
        public const int Map3DViewWidth = 144;
        public const int Map3DViewHeight = 144;
        public const int ButtonGridX = 208;
        public const int ButtonGridY = 143;
        /// <summary>
        /// This includes a 1-pixel border around the portrait.
        /// </summary>
        public static readonly Rect[] PartyMemberPortraitAreas = Enumerable.Range(0, 6).Select(index =>
            new Rect(15 + index * 48, 0, 32, 36)).ToArray();
        /// <summary>
        /// This includes a 1-pixel border around the portrait.
        /// This also includes the ailment icon and the bars for HP and SP.
        /// </summary>
        public static readonly Rect[] ExtendedPartyMemberPortraitAreas = Enumerable.Range(0, 6).Select(index =>
            new Rect(15 + index * 48, 0, 48, 36)).ToArray();
        public const int GlyphWidth = 6;
        public const int GlyphLineHeight = 6;
    }
}
