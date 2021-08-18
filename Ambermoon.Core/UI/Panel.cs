/*
 * Panel.cs - A panel with a 3D border
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

using Ambermoon.Render;
using System.Collections.Generic;

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
            filledAreas[0] = new FilledArea(layoutFilledAreas, layout.CreateArea(fillArea.CreateModified(0, 0, 1, 1), game.GetUIColor(26), displayLayer));
            // left and top border
            filledAreas[1] = new FilledArea(layoutFilledAreas, layout.CreateArea(fillArea.CreateModified(-1, -1, 1, 1), game.GetUIColor(31), (byte)(displayLayer + 1)));
            // fill area
            filledAreas[2] = new FilledArea(layoutFilledAreas, layout.CreateArea(fillArea, game.GetUIColor(28), (byte)(displayLayer + 2)));
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
