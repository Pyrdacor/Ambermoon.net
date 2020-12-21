using System.Collections.Generic;
using System.Linq;

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
                }),

                // Disabled button overlay (palette 51 -> 0 = transparent, 26 = dark gray)
                Graphic.FromIndexedData(24, 11, Enumerable.Range(0, 24 * 11).Select(i => (byte)((i + i / 24) % 2 == 0 ? 0 : 26)).ToArray()),

                // Map disable overlay (palette 50 -> 0 = black, 25 = transparent)
                Graphic.FromIndexedData(320, 144, Enumerable.Range(0, 320 * 144).Select(i => (byte)((i + i / 320) % 2 == 0 ? 25 : 0)).ToArray()),

                // Ambermoon info box (shown over the map when opening option menu)
                GraphicBuilder.Create(128, 19)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 126, 1), 30)
                .AddColoredArea(new Rect(127, 0, 1, 1), 28)
                .AddColoredArea(new Rect(0, 1, 1, 17), 30)
                .AddColoredArea(new Rect(1, 1, 126, 17), 29)
                .AddColoredArea(new Rect(127, 1, 1, 17), 27)
                .AddColoredArea(new Rect(0, 18, 1, 1), 28)
                .AddColoredArea(new Rect(1, 18, 127, 1), 27)
                .Build(),

                // Bigger info box
                GraphicBuilder.Create(144, 26)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 142, 1), 30)
                .AddColoredArea(new Rect(143, 0, 1, 1), 28)
                .AddColoredArea(new Rect(0, 1, 1, 24), 30)
                .AddColoredArea(new Rect(1, 1, 142, 24), 29)
                .AddColoredArea(new Rect(143, 1, 1, 24), 27)
                .AddColoredArea(new Rect(0, 25, 1, 1), 28)
                .AddColoredArea(new Rect(1, 25, 143, 1), 27)
                .Build(),

                // BattleFieldYellowBorder (5 in text palette = yellow)
                GraphicBuilder.Create(16, 13)
                .AddColoredArea(new Rect(0, 0, 16, 1), 5)
                .AddColoredArea(new Rect(0, 1, 1, 11), 5)
                .AddColoredArea(new Rect(15, 1, 1, 11), 5)
                .AddColoredArea(new Rect(0, 12, 16, 1), 5)
                .Build(),

                // BattleFieldOrangeBorder (6 in text palette = orange)
                GraphicBuilder.Create(16, 13)
                .AddColoredArea(new Rect(0, 0, 16, 1), 6)
                .AddColoredArea(new Rect(0, 1, 1, 11), 6)
                .AddColoredArea(new Rect(15, 1, 1, 11), 6)
                .AddColoredArea(new Rect(0, 12, 16, 1), 6)
                .Build(),

                // BattleFieldGreenHighlight (8 in text palette = light green)
                GraphicBuilder.Create(16, 13)
                .AddColoredArea(new Rect(1, 1, 14, 11), 8)
                .Build(),

                // HealingStarAnimation (3 frames of a redish star)
                Graphic.FromIndexedData(7 * 3, 7, new byte[7 * 7 * 3]
                {
                    // Transparency: 25
                    // Red: 20 (CC4433)
                    // Orange: 21 (EE6633)
                    // Yellow: 16 (FFCC00)
                    // White: 1 (EEDDCC)
                    // -----------------------
                    // 1st frame: Big star
                    25, 25, 25, 20, 25, 25, 25,
                    25, 25, 25, 21, 25, 25, 25,
                    25, 25, 20, 16, 20, 25, 25,
                    20, 21, 16,  1, 16, 21, 20,
                    25, 25, 20, 16, 20, 25, 25,
                    25, 25, 25, 21, 25, 25, 25,
                    25, 25, 25, 20, 25, 25, 25,
                    // 2nd frame: Small star
                    25, 25, 25, 25, 25, 25, 25,
                    25, 25, 25, 20, 25, 25, 25,
                    25, 25, 25, 21, 25, 25, 25,
                    25, 20, 21, 16, 21, 20, 25,
                    25, 25, 25, 21, 25, 25, 25,
                    25, 25, 25, 20, 25, 25, 25,
                    25, 25, 25, 25, 25, 25, 25,
                    // 3rd frame: Single orange pixel
                    25, 25, 25, 25, 25, 25, 25,
                    25, 25, 25, 25, 25, 25, 25,
                    25, 25, 25, 25, 25, 25, 25,
                    25, 25, 25, 21, 25, 25, 25,
                    25, 25, 25, 25, 25, 25, 25,
                    25, 25, 25, 25, 25, 25, 25,
                    25, 25, 25, 25, 25, 25, 25
                }),
            };
        }
    }
}
