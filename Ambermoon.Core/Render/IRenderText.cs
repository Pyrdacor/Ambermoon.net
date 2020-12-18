using Ambermoon.Data;
using System;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    public interface IRenderText : IRenderNode
    {
        void Place(int x, int y);
        void Place(Rect rect, TextAlign textAlign = TextAlign.Left);
        TextColor TextColor { get; set; }
        TextAlign TextAlign { get; set; }
        bool Shadow { get; set; }
        IText Text { get; set; }
        byte DisplayLayer { get; set; }
    }

    public interface IRenderTextFactory
    {
        /// <summary>
        /// Mapping from glyph code to texture index.
        /// </summary>
        Dictionary<byte, Position> GlyphTextureMapping { get; set; }
        /// <summary>
        /// Mapping from digit (0 to 9) to texture index.
        /// </summary>
        Dictionary<byte, Position> DigitGlyphTextureMapping { get; set; }
        IRenderText Create();
        IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow);
        IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow,
            Rect bounds, TextAlign textAlign = TextAlign.Left);
        IRenderText CreateDigits(IRenderLayer layer, IText digits, TextColor textColor,
            bool shadow, Rect bounds, TextAlign textAlign = TextAlign.Left);
    }
}
