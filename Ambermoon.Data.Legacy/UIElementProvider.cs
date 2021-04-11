using Ambermoon.Data.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy
{
    public static class UIElementProvider
    {
        class Star
        {
            public Position Position;
            public int Frame;
        }

        public static List<Graphic> Create()
        {
            // This is used by the healing animation (falling stars)
            // and the magic item animation (blinking stars).
            // There are 3 frames.
            byte[] redStarFrameData = new byte[]
            {
                // Transparency: 0
                // Red: 20 (CC4433)
                // Orange: 21 (EE6633)
                // Yellow: 16 (FFCC00)
                // White: 1 (EEDDCC)
                // -----------------------
                // 1st frame: Big star
                 0,  0,  0, 20,  0,  0,  0,
                 0,  0,  0, 21,  0,  0,  0,
                 0,  0, 20, 16, 20,  0,  0,
                20, 21, 16,  1, 16, 21, 20,
                 0,  0, 20, 16, 20,  0,  0,
                 0,  0,  0, 21,  0,  0,  0,
                 0,  0,  0, 20,  0,  0,  0,
                // 2nd frame: Small star
                 0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0, 20,  0,  0,  0,
                 0,  0,  0, 21,  0,  0,  0,
                 0, 20, 21, 16, 21, 20,  0,
                 0,  0,  0, 21,  0,  0,  0,
                 0,  0,  0, 20,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,
                // 3rd frame: Single orange pixel
                 0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0, 21,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0,
                 0,  0,  0,  0,  0,  0,  0
            };

            Graphic GetRedStarFrame(int frame)
            {
                return new Graphic
                {
                    Width = 7,
                    Height = 7,
                    Data = redStarFrameData.Skip(frame * 49).Take(49).ToArray(),
                    IndexedGraphic = true
                };
            }

            Graphic CreateBlinkingStars()
            {
                var stars = new List<Star>();
                var addAmountsPerFrame = new int[] { 1, 1, 0, 1, 0, 0, 0, 0 };
                var random = new Random(); // always use a new random to produce the same effect
                var animation = new Graphic(8 * 16, 16, 0); // 11 frames with 16x16

                for (int i = 0; i < 8; ++i) // 8 frames in total
                {
                    // in each frame we have several stars in specific states (star frame)
                    var frame = new Graphic(16, 16, 0);

                    // first update exising stars
                    for (int s = stars.Count - 1; s >= 0; --s)
                    {
                        var star = stars[s];

                        if (++star.Frame == 5)
                            stars.RemoveAt(s);
                        else
                            frame.AddOverlay((uint)star.Position.X, (uint)star.Position.Y, GetRedStarFrame(star.Frame > 1 ? star.Frame - 2 : 2 - star.Frame));
                    }
                    // then add new stars
                    for (int a = 0; a < addAmountsPerFrame[i]; ++a)
                    {
                        var star = new Star
                        {
                            Position = new Position((int)random.Next() % 10, (int)random.Next() % 10),
                            Frame = 0
                        };
                        stars.Add(star);
                        frame.AddOverlay((uint)star.Position.X, (uint)star.Position.Y, GetRedStarFrame(0));
                    }

                    animation.AddOverlay((uint)i * 16, 0, frame);
                }

                animation.ReplaceColor(0, 25);

                return animation;
            }

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
                .AddColoredArea(new Rect(5, 0, 1, 1), 32)
                .AddColoredArea(new Rect(0, 1, 1, 51), 30)
                .AddColoredArea(new Rect(1, 1, 4, 51), 28)
                .AddColoredArea(new Rect(5, 1, 1, 51), 26)
                .AddColoredArea(new Rect(0, 52, 1, 1), 32)
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
                .AddColoredArea(new Rect(5, 0, 1, 1), 32)
                .AddColoredArea(new Rect(0, 1, 1, 110), 30)
                .AddColoredArea(new Rect(1, 1, 4, 110), 28)
                .AddColoredArea(new Rect(5, 1, 1, 110), 26)
                .AddColoredArea(new Rect(0, 111, 1, 1), 32)
                .AddColoredArea(new Rect(1, 111, 5, 1), 26)
                .Build(),

                // Item slot background
                new Graphic(16, 24, 27),

                // Item slot disabled
                GraphicBuilder.Create(16, 24)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 14, 1), 30)
                .AddColoredArea(new Rect(15, 0, 1, 1), 32)
                .AddColoredArea(new Rect(0, 1, 1, 22), 30)
                .AddColoredArea(new Rect(1, 1, 14, 22), 28)
                .AddColoredArea(new Rect(15, 1, 1, 22), 26)
                .AddColoredArea(new Rect(0, 23, 1, 1), 32)
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

                // Map disable overlay (UI palette-> 32 = black, 0 = transparent)
                Graphic.FromIndexedData(320, 144, Enumerable.Range(0, 320 * 144).Select(i => (byte)((i + i / 320) % 2 == 0 ? 0 : 32)).ToArray()),

                // Ambermoon info box (shown over the map when opening option menu)
                GraphicBuilder.Create(128, 19)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 126, 1), 30)
                .AddColoredArea(new Rect(127, 0, 1, 1), 27)
                .AddColoredArea(new Rect(0, 1, 1, 17), 30)
                .AddColoredArea(new Rect(1, 1, 126, 17), 28)
                .AddColoredArea(new Rect(127, 1, 1, 17), 26)
                .AddColoredArea(new Rect(0, 18, 1, 1), 27)
                .AddColoredArea(new Rect(1, 18, 127, 1), 26)
                .Build(),

                // Bigger info box
                GraphicBuilder.Create(144, 26)
                .AddColoredArea(new Rect(0, 0, 1, 1), 31)
                .AddColoredArea(new Rect(1, 0, 142, 1), 30)
                .AddColoredArea(new Rect(143, 0, 1, 1), 27)
                .AddColoredArea(new Rect(0, 1, 1, 24), 30)
                .AddColoredArea(new Rect(1, 1, 142, 24), 28)
                .AddColoredArea(new Rect(143, 1, 1, 24), 26)
                .AddColoredArea(new Rect(0, 25, 1, 1), 27)
                .AddColoredArea(new Rect(1, 25, 143, 1), 26)
                .Build(),

                // BattleFieldYellowBorder
                GraphicBuilder.Create(16, 13)
                .AddColoredArea(new Rect(0, 0, 16, 1), Color.ActivePartyMember)
                .AddColoredArea(new Rect(0, 1, 1, 11), Color.ActivePartyMember)
                .AddColoredArea(new Rect(15, 1, 1, 11), Color.ActivePartyMember)
                .AddColoredArea(new Rect(0, 12, 16, 1), Color.ActivePartyMember)
                .Build(),

                // BattleFieldOrangeBorder
                GraphicBuilder.Create(16, 13)
                .AddColoredArea(new Rect(0, 0, 16, 1), Color.LightRed)
                .AddColoredArea(new Rect(0, 1, 1, 11), Color.LightRed)
                .AddColoredArea(new Rect(15, 1, 1, 11), Color.LightRed)
                .AddColoredArea(new Rect(0, 12, 16, 1), Color.LightRed)
                .Build(),

                // BattleFieldGreenHighlight
                GraphicBuilder.Create(16, 13)
                .AddColoredArea(new Rect(1, 1, 14, 11), Color.LightGreen)
                .Build(),

                // HealingStarAnimation (3 frames of a redish star)
                Graphic.FromIndexedData(7 * 3, 7, redStarFrameData),

                // BattleFieldBlockedMovementCursor
                Graphic.FromIndexedData(14, 11, new byte[14 * 11]
                {
                    // Transparency: 0
                    // Red: 19 (881122)
                    // -----------------------
                     0, 19, 19,  0,  0,  0,  0,  0,  0,  0, 19, 19, 19,  0,
                    19, 19, 19, 19, 19,  0,  0,  0, 19, 19, 19, 19, 19, 19,
                    19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19,  0,
                     0,  0, 19, 19, 19, 19, 19, 19, 19, 19, 19,  0,  0,  0,
                     0,  0,  0,  0, 19, 19, 19, 19, 19, 19,  0,  0,  0,  0,
                     0,  0,  0, 19, 19, 19, 19, 19, 19, 19, 19,  0,  0,  0,
                     0,  0, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19,  0,
                     0, 19, 19, 19, 19,  0,  0,  0, 19, 19, 19, 19, 19, 19,
                    19, 19, 19, 19,  0,  0,  0,  0,  0,  0, 19, 19, 19, 19,
                    19, 19, 19, 19,  0,  0,  0,  0,  0,  0,  0, 19, 19,  0,
                     0, 19, 19,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
                }),

                // ItemMagicAnimation
                CreateBlinkingStars(),

                // BrokenItemOverlay
                Graphic.FromIndexedData(16, 16, new byte[16 * 16]
                {
                    // Transparency: 0
                    // Dark gray: 26 (222222)
                    // -----------------------
                     0,  0,  0,  0,  0,  0,  0,  0,  0, 26, 26, 26, 26,  0,  0,  0,
                     0,  0,  0,  0,  0,  0,  0,  0,  0, 26, 26, 26,  0,  0,  0,  0,
                     0,  0, 26,  0,  0, 26,  0,  0, 26, 26, 26,  0,  0,  0,  0,  0,
                     0,  0,  0,  0,  0,  0, 26, 26, 26,  0,  0,  0,  0,  0,  0,  0,
                    26, 26,  0,  0,  0,  0,  0, 26, 26,  0,  0,  0,  0,  0,  0,  0,
                    26, 26, 26, 26,  0,  0,  0, 26, 26,  0,  0,  0,  0, 26,  0,  0,
                    26, 26, 26, 26, 26, 26,  0, 26, 26,  0,  0,  0,  0,  0,  0,  0,
                     0,  0, 26, 26, 26, 26, 26, 26, 26, 26,  0,  0,  0,  0,  0,  0,
                     0,  0,  0, 26,  0, 26, 26, 26, 26, 26,  0,  0,  0, 26,  0,  0,
                     0,  0, 26,  0,  0,  0,  0,  0,  0, 26, 26, 26, 26, 26, 26, 26,
                     0,  0,  0,  0,  0,  0,  0,  0,  0, 26, 26, 26, 26, 26, 26,  0,
                     0,  0,  0, 26,  0,  0,  0,  0,  0, 26,  0,  0,  0,  0,  0,  0,
                     0,  0,  0,  0,  0,  0,  0,  0, 26,  0,  0,  0,  0,  0,  0,  0,
                     0,  0,  0,  0,  0,  0,  0, 26,  0,  0,  0,  0, 26,  0,  0,  0,
                     0,  0,  0,  0,  0, 26, 26, 26,  0,  0,  0, 26,  0,  0,  0,  0,
                     0,  0,  0, 26, 26, 26, 26,  0, 26,  0,  0, 26,  0,  0,  0,  0,
                }),

                // AutomapWallFrames
                Graphic.Concat
                (
                    // Map background: 6
                    // Wall color: 7
                    // -----------------------
                    // End pieces
                    Graphic.FromIndexedData(32, 8, new byte[32 * 8]
                    {
                        //  top open         right open       bottom open        left open    
                        6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  6,6,6,6,6,6,6,6,  6,6,6,6,6,6,6,6,
                        6,7,7,6,6,7,7,6,  6,7,7,7,7,7,7,7,  6,7,7,7,7,7,7,6,  7,7,7,7,7,7,7,6,
                        6,7,7,6,6,7,7,6,  6,7,7,7,7,7,7,7,  6,7,7,7,7,7,7,6,  7,7,7,7,7,7,7,6,
                        6,7,7,6,6,7,7,6,  6,7,7,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,7,7,6,
                        6,7,7,6,6,7,7,6,  6,7,7,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,7,7,6,
                        6,7,7,7,7,7,7,6,  6,7,7,7,7,7,7,7,  6,7,7,6,6,7,7,6,  7,7,7,7,7,7,7,6,
                        6,7,7,7,7,7,7,6,  6,7,7,7,7,7,7,7,  6,7,7,6,6,7,7,6,  7,7,7,7,7,7,7,6,
                        6,6,6,6,6,6,6,6,  6,6,6,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,
                    }),
                    // Corners
                    Graphic.FromIndexedData(32, 8, new byte[32 * 8]
                    {
                        //  b+r open          b+l open      //  t+r open          t+l open    
                        6,6,6,6,6,6,6,6,  6,6,6,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,
                        6,7,7,7,7,7,7,7,  7,7,7,7,7,7,7,6,  6,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,6,
                        6,7,7,7,7,7,7,7,  7,7,7,7,7,7,7,6,  6,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,6,
                        6,7,7,6,6,6,6,6,  6,6,6,6,6,7,7,6,  6,7,7,6,6,6,6,6,  6,6,6,6,6,7,7,6,
                        6,7,7,6,6,6,6,6,  6,6,6,6,6,7,7,6,  6,7,7,6,6,6,6,6,  6,6,6,6,6,7,7,6,
                        6,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,6,  6,7,7,7,7,7,7,7,  7,7,7,7,7,7,7,6,
                        6,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,6,  6,7,7,7,7,7,7,7,  7,7,7,7,7,7,7,6,
                        6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  6,6,6,6,6,6,6,6,
                    }),
                    // T-crossings
                    Graphic.FromIndexedData(32, 8, new byte[32 * 8]
                    {
                        // t+l+r open        t+b+r open        b+l+r open        t+b+l open 
                        6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  6,7,7,6,6,7,7,6,
                        7,7,7,6,6,7,7,7,  6,7,7,6,6,7,7,7,  7,7,7,7,7,7,7,7,  7,7,7,6,6,7,7,6,
                        7,7,7,6,6,7,7,7,  6,7,7,6,6,7,7,7,  7,7,7,7,7,7,7,7,  7,7,7,6,6,7,7,6,
                        6,6,6,6,6,6,6,6,  6,7,7,6,6,6,6,6,  6,6,6,6,6,6,6,6,  6,6,6,6,6,7,7,6,
                        6,6,6,6,6,6,6,6,  6,7,7,6,6,6,6,6,  6,6,6,6,6,6,6,6,  6,6,6,6,6,7,7,6,
                        7,7,7,7,7,7,7,7,  6,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,6,
                        7,7,7,7,7,7,7,7,  6,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,7,  7,7,7,6,6,7,7,6,
                        6,6,6,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,
                    }),
                    // crossing, vertical and horizontal piece and single closed tile
                    Graphic.FromIndexedData(32, 8, new byte[32 * 8]
                    {
                        //  all open          t+b open          l+r open           Closed
                        6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  7,7,7,7,7,7,7,7,
                        7,7,7,6,6,7,7,7,  6,7,7,6,6,7,7,6,  7,7,7,7,7,7,7,7,  7,7,7,7,7,7,7,7,
                        7,7,7,6,6,7,7,7,  6,7,7,6,6,7,7,6,  7,7,7,7,7,7,7,7,  7,7,6,6,6,6,7,7,
                        6,6,6,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  7,7,6,6,6,6,7,7,
                        6,6,6,6,6,6,6,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  7,7,6,6,6,6,7,7,
                        7,7,7,6,6,7,7,7,  6,7,7,6,6,7,7,6,  7,7,7,7,7,7,7,7,  7,7,6,6,6,6,7,7,
                        7,7,7,6,6,7,7,7,  6,7,7,6,6,7,7,6,  7,7,7,7,7,7,7,7,  7,7,7,7,7,7,7,7,
                        6,7,7,6,6,7,7,6,  6,7,7,6,6,7,7,6,  6,6,6,6,6,6,6,6,  7,7,7,7,7,7,7,7,
                    })
                ),

                // FakeWallOverlay
                Graphic.FromIndexedData(8, 8, new byte[8 * 8]
                {
                    6,0,6,0,6,0,6,0,
                    0,6,0,6,0,6,0,6,
                    6,0,6,0,6,0,6,0,
                    0,6,0,6,0,6,0,6,
                    6,0,6,0,6,0,6,0,
                    0,6,0,6,0,6,0,6,
                    6,0,6,0,6,0,6,0,
                    0,6,0,6,0,6,0,6,
                })
            };
        }
    }
}
