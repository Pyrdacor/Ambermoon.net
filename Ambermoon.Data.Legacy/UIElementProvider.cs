using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public static class UIElementProvider
    {
        public static List<Graphic> Create()
        {
            // See UIElementGraphic
            return new List<Graphic>
            {
                // Small vertical scrollbar
                GraphicBuilder.Create(6, 28)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 4, 1), 30)
                .AddColoredArea(new Rect(5, 0, 1, 1), 28)
                .AddColoredArea(new Rect(0, 1, 1, 25), 30)
                .AddColoredArea(new Rect(1, 1, 4, 25), 29)
                .AddColoredArea(new Rect(5, 1, 1, 25), 27)
                .AddColoredArea(new Rect(0, 26, 1, 1), 28)
                .AddColoredArea(new Rect(1, 26, 5, 1), 27)
                .AddColoredArea(new Rect(0, 27, 6, 1), 26)
                .Build(),

                // Small vertical scrollbar highlighted
                GraphicBuilder.Create(6, 28)
                .AddColoredArea(new Rect(0, 0, 5, 1), 31)
                .AddColoredArea(new Rect(5, 0, 1, 1), 29)
                .AddColoredArea(new Rect(0, 1, 1, 25), 31)
                .AddColoredArea(new Rect(1, 1, 4, 25), 30)
                .AddColoredArea(new Rect(5, 1, 1, 25), 28)
                .AddColoredArea(new Rect(0, 26, 1, 1), 29)
                .AddColoredArea(new Rect(1, 26, 5, 1), 28)
                .AddColoredArea(new Rect(0, 27, 6, 1), 26)
                .Build(),

                // Large vertical scrollbar
                GraphicBuilder.Create(6, 57)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 4, 1), 30)
                .AddColoredArea(new Rect(5, 0, 1, 1), 28)
                .AddColoredArea(new Rect(0, 1, 1, 54), 30)
                .AddColoredArea(new Rect(1, 1, 4, 54), 29)
                .AddColoredArea(new Rect(5, 1, 1, 54), 27)
                .AddColoredArea(new Rect(0, 55, 1, 1), 28)
                .AddColoredArea(new Rect(1, 55, 5, 1), 27)
                .AddColoredArea(new Rect(0, 56, 6, 1), 26)
                .Build(),

                // Large vertical scrollbar highlighted
                GraphicBuilder.Create(6, 57)
                .AddColoredArea(new Rect(0, 0, 5, 1), 31)
                .AddColoredArea(new Rect(5, 0, 1, 1), 29)
                .AddColoredArea(new Rect(0, 1, 1, 54), 31)
                .AddColoredArea(new Rect(1, 1, 4, 54), 30)
                .AddColoredArea(new Rect(5, 1, 1, 54), 28)
                .AddColoredArea(new Rect(0, 55, 1, 1), 29)
                .AddColoredArea(new Rect(1, 55, 5, 1), 28)
                .AddColoredArea(new Rect(0, 56, 6, 1), 26)
                .Build(),

                // Small vertical scrollbar background
                GraphicBuilder.Create(6, 53)
                .AddColoredArea(new Rect(1, 1, 5, 52), 27)
                .AddColoredArea(new Rect(0, 1, 1, 52), 26)
                .AddColoredArea(new Rect(0, 0, 6, 1), 26)
                .Build(),

                // Small vertical scrollbar disabled
                GraphicBuilder.Create(6, 53)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 4, 1), 30)
                .AddColoredArea(new Rect(5, 0, 1, 1), 0)
                .AddColoredArea(new Rect(0, 1, 1, 51), 30)
                .AddColoredArea(new Rect(1, 1, 4, 51), 28)
                .AddColoredArea(new Rect(5, 1, 1, 51), 26)
                .AddColoredArea(new Rect(0, 52, 1, 1), 0)
                .AddColoredArea(new Rect(1, 52, 5, 1), 26)
                .Build(),

                // Large vertical scrollbar background
                GraphicBuilder.Create(6, 112)
                .AddColoredArea(new Rect(1, 1, 5, 111), 27)
                .AddColoredArea(new Rect(0, 1, 1, 111), 26)
                .AddColoredArea(new Rect(0, 0, 6, 1), 26)
                .Build(),

                // Large vertical scrollbar disabled
                GraphicBuilder.Create(6, 112)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 4, 1), 30)
                .AddColoredArea(new Rect(5, 0, 1, 1), 0)
                .AddColoredArea(new Rect(0, 1, 1, 110), 30)
                .AddColoredArea(new Rect(1, 1, 4, 110), 28)
                .AddColoredArea(new Rect(5, 1, 1, 110), 26)
                .AddColoredArea(new Rect(0, 111, 1, 1), 0)
                .AddColoredArea(new Rect(1, 111, 5, 1), 26)
                .Build(),

                // Item slot background
                new Graphic(16, 24, 27),

                // Item slot disabled
                GraphicBuilder.Create(16, 24)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 14, 1), 30)
                .AddColoredArea(new Rect(15, 0, 1, 1), 0)
                .AddColoredArea(new Rect(0, 1, 1, 22), 30)
                .AddColoredArea(new Rect(1, 1, 14, 22), 28)
                .AddColoredArea(new Rect(15, 1, 1, 22), 26)
                .AddColoredArea(new Rect(0, 23, 1, 1), 0)
                .AddColoredArea(new Rect(1, 23, 15, 1), 26)
                .Build(),

                // Portrait background
                Graphic.CreateGradient(32, 34, 4, 2, 8, 23),

                // Thin portrait border
                Graphic.FromIndexedData(32, 1, new byte[32]
                {
                    28, 28, 28, 28, 27, 28, 27, 28, 27, 28, 27, 28, 27, 28, 27, 28,
                    28, 27, 28, 27, 28, 27, 28, 27, 28, 27, 28, 27, 28, 28, 28, 28
                })
            };
        }
    }
}
