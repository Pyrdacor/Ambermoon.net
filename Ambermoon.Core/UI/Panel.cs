using Ambermoon.Render;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ambermoon.UI
{
    /// <summary>
    /// A panel is just a small gray area with a 3D border.
    /// 
    /// Basically a panel consists of 3 filled areas.
    /// A fill area and two border areas.
    /// </summary>
    class Panel
    {
        readonly FilledArea[] filledAreas = new FilledArea[3];

        public Panel(Game game, Rect fillArea, List<IColoredRect> layoutFilledAreas, Layout layout, byte displayLayer)
        {
            // right and bottom border
            filledAreas[0] = new FilledArea(layoutFilledAreas, layout.CreateArea(fillArea.CreateModified(0, 0, 1, 1), game.GetPaletteColor(50, 26), displayLayer));
            // left and top border
            filledAreas[1] = new FilledArea(layoutFilledAreas, layout.CreateArea(fillArea.CreateModified(-1, -1, 1, 1), game.GetPaletteColor(50, 31), (byte)(displayLayer + 1)));
            // fill area
            filledAreas[2] = new FilledArea(layoutFilledAreas, layout.CreateArea(fillArea, game.GetPaletteColor(50, 28), (byte)(displayLayer + 2)));
        }

        public void Destroy()
        {
            for (int i = 0; i < 3; ++i)
            {
                filledAreas[i]?.Destroy();
                filledAreas[i] = null;
            }
        }
    }
}
