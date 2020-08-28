using Ambermoon.Data;
using System.Collections.Generic;

namespace Ambermoon.Render
{
    public interface IRenderText : IRenderNode
    {
        void Place(int x, int y);
        void Place(Rect rect, TextAlign textAlign = TextAlign.Left);
        TextColor TextColor { get; set; }
        bool Shadow { get; set; }
        IText Text { get; set; }
        byte DisplayLayer { get; set; }
    }

    public interface IRenderTextFactory
    {
        Dictionary<byte, Position> GlyphTextureMapping { get; set; }
        IRenderText Create();
        IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow);
        IRenderText Create(IRenderLayer layer, IText text, TextColor textColor, bool shadow, Rect bounds, TextAlign textAlign = TextAlign.Left);
    }
}
